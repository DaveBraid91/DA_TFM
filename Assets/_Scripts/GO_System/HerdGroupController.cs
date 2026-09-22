using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HerdAI
{
    public class HerdGroupController : MonoBehaviour
    {
        [Header("Herd Id")]
        public int herdId = 0;

        [Header("Wander")]
        public float wanderRadius = 4f;
        public float wanderTargetMargin = 0.5f;

        [Header("Flee")]
        public float fleeTriggerRadius = 6f;
        public float fleeDuration = 2f;
        public float herdJoinDistance = 1.5f;
        public Transform fleeOrigin;
        [SerializeField] private float fleeCoolDown = 1f;
        
        [Header("Obstacle avoidance")]
        [SerializeField] private LayerMask obstacleMask;

        [Header("World Bounds (XY)")]
        public Vector2 minBounds = new Vector2(-10, -10);
        public Vector2 maxBounds = new Vector2(10, 10);
        public Vector2 margin = new Vector2(1, 1);

        [HideInInspector] public Vector3 sharedWanderTarget;
        
        private Coroutine _scareCoolDown;
        private bool _canBeScared;

        private readonly List<HerdAgentStateMotor> _agents = new List<HerdAgentStateMotor>();

        public void Register(HerdAgentStateMotor agent)
        {
            if (!_agents.Contains(agent))
                _agents.Add(agent);
        }

        public void Unregister(HerdAgentStateMotor agent)
        {
            _agents.Remove(agent);
        }

        public Vector3 GetHerdCenter()
        {
            if (_agents.Count == 0) return transform.position;

            Vector3 acc = Vector3.zero;
            foreach (var a in _agents)
                acc += a.transform.position;

            return acc / _agents.Count;
        }

        public void PickNewWanderTarget(Vector3 origin)
        {
            var min = minBounds + margin;
            var max = maxBounds - margin;

            // círculo alrededor de origin
            var angle = Random.Range(0f, Mathf.PI * 2f);
            var radius = Random.Range(0.75f, wanderRadius);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            var candidate = new Vector2(origin.x, origin.z) + offset;

            var targetMargin = Mathf.Max(0.5f, wanderRadius * 0.25f);
            var tMin = min + new Vector2(targetMargin, targetMargin);
            var tMax = max - new Vector2(targetMargin, targetMargin);

            if (tMin.x > tMax.x) { tMin.x = min.x; tMax.x = max.x; }
            if (tMin.y > tMax.y) { tMin.y = min.y; tMax.y = max.y; }

            candidate = Vector2.Min(Vector2.Max(candidate, tMin), tMax);

            sharedWanderTarget = new Vector3(candidate.x, candidate.y, origin.z);
        }
        
        private void OnScarePerformed()
        {
            TriggerScare(fleeOrigin.position);
        }
        
        public void TriggerScare(Vector3 scarePosition)
        {
            if (!_canBeScared) return;

            _canBeScared = false;
            
            _scareCoolDown = StartCoroutine(ScareCoolDown());

            foreach (var agent in _agents)
            {
                var pos = agent.transform.position;
                var away = pos - scarePosition;
                var distSq = away.sqrMagnitude;

                if (distSq > fleeTriggerRadius * fleeTriggerRadius)
                    continue;

                agent.ForceEnterFlee();
            }
        }

        public Vector3 ClampPositionToBounds(Vector3 pos)
        {
            var min = minBounds + margin;
            var max = maxBounds - margin;

            // Mundo en XY, los bounds están definidos como (x, y)
            pos.x = Mathf.Clamp(pos.x, min.x, max.x);
            pos.y = Mathf.Clamp(pos.y, min.y, max.y);

            return pos;
        }
        
        private IEnumerator ScareCoolDown()
        {
            yield return new WaitForSeconds(fleeCoolDown);
            _canBeScared = true;
        }
        
        public Vector3 ComputeFlockDirection(HerdAgentStateMotor self, HerdState state, float timeInState, HerdAgentSettingsGO settings)
        {
            if (settings == null || state == HerdState.Idle) return Vector3.zero;

            var myPos3 = self.transform.position;
            var myPos  = new Vector2(myPos3.x, myPos3.y);

            var neighborRadiusSq = settings.neighborRadius   * settings.neighborRadius;
            var separationRadiusSq = settings.separationRadius * settings.separationRadius;

            var cohesionAcc = Vector2.zero;
            var alignmentAcc = Vector2.zero;
            var separationAcc = Vector2.zero;
            var neighborCount = 0;

            // 1) Vecinos: cohesión / alineamiento / separación (todo en XY)
            foreach (var other in _agents)
            {
                if (other == null || other == self) continue;

                var otherPos3 = other.transform.position;
                var otherPos = new Vector2(otherPos3.x, otherPos3.y);
                var toOther = otherPos - myPos;
                var distSq = toOther.sqrMagnitude;
                if (distSq > neighborRadiusSq) continue;

                neighborCount++;
                cohesionAcc  += otherPos;

                var otherVel3 = other.rb.linearVelocity;
                var otherVel  = new Vector2(otherVel3.x, otherVel3.y);
                alignmentAcc     += otherVel;

                if (distSq < separationRadiusSq && distSq > 0.0001f)
                {
                    separationAcc -= toOther / Mathf.Max(distSq, 0.0001f);
                }
            }

            var cohesion = Vector2.zero;
            var alignment = Vector2.zero;
            var separation = Vector2.zero;

            if (neighborCount > 0)
            {
                var center   = cohesionAcc / neighborCount;
                var toCenter = center - myPos;

                cohesion = toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized   : Vector2.zero;
                alignment = alignmentAcc.sqrMagnitude > 0.0001f ? alignmentAcc.normalized : Vector2.zero;
                separation = separationAcc.sqrMagnitude > 0.0001f ? separationAcc.normalized : Vector2.zero;
            }

            // 2) Centro global del rebaño (para Flee fase 2)
            var herdCenter = Vector2.zero;
            var herdCount = 0;
            foreach (var other in _agents)
            {
                if (other == null) continue;
                var p3 = other.transform.position;
                herdCenter += new Vector2(p3.x, p3.y);
                herdCount++;
            }
            if (herdCount > 0)
                herdCenter /= herdCount;

            // 3) Containment desde bounds (XY)
            var containment = Vector2.zero;
            var min = minBounds + margin;
            var max = maxBounds - margin;

            if (myPos.x < min.x) containment.x += 1f;
            if (myPos.x > max.x) containment.x -= 1f;
            if (myPos.y < min.y) containment.y += 1f;
            if (myPos.y > max.y) containment.y -= 1f;

            if (containment.sqrMagnitude > 0.0001f)
                containment = containment.normalized;

            // 4) Target según estado (Idle / Wander / Flee)
            var targetDir = Vector2.zero;
            var containmentMult = 1f;

            var cohesionWeight = settings.cohesionWeight;
            var alignmentWeight = settings.alignmentWeight;
            var separationWeight = settings.separationWeight;
            var containmentWeight = settings.containmentWeight;
            var targetWeight = settings.targetWeight;
            var avoidanceWeight = settings.avoidanceWeight;

            switch (state)
            {
                case HerdState.Wander:
                {
                    var target2D = new Vector2(sharedWanderTarget.x, sharedWanderTarget.y);
                    var toTarget = target2D - myPos;
                    targetDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;
                    break;
                }

                case HerdState.Flee:
                {
                    if (timeInState < fleeDuration)
                    {
                        // FASE 1: huida del origen del susto
                        var origin2D = new Vector2(fleeOrigin.position.x, fleeOrigin.position.y);
                        var away = myPos - origin2D;

                        targetDir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.zero;

                        cohesionWeight  = 0f;
                        alignmentWeight = 0f;
                    }
                    else
                    {
                        // FASE 2: vuelta al centro del rebaño
                        if (herdCount > 0)
                        {
                            var toCenter = herdCenter - myPos;
                            targetDir = toCenter.sqrMagnitude > 0.0001f ? toCenter.normalized : Vector2.zero;
                        }
                        else
                        {
                            var origin2D = new Vector2(fleeOrigin.position.x, fleeOrigin.position.y);
                            var away     = myPos - origin2D;
                            targetDir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.zero;
                        }
                    }

                    containmentMult = 2f;
                    break;
                }
            }

            // 5) Obstacle avoidance por raycast (plano XY)
            var avoidance2D = Vector2.zero;

            if (state != HerdState.Idle && obstacleMask.value != 0)
            {
                var rayDir2D = targetDir;
                if (rayDir2D.sqrMagnitude < 0.0001f)
                {
                    rayDir2D = cohesion + separation + alignment;
                }

                if (rayDir2D.sqrMagnitude > 0.0001f)
                {
                    rayDir2D.Normalize();
                    var rayLength = settings.neighborRadius;
                    var origin = new Vector3(myPos.x, myPos.y, 0f) + Vector3.forward * 0.1f;

                    var dir3 = new Vector3(rayDir2D.x, rayDir2D.y, 0f);

                    if (Physics.Raycast(origin, dir3, out var hit, rayLength, obstacleMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.rigidbody == null || hit.rigidbody != self.rb)
                        {
                            // Proyectamos la normal en XY (ignorando Z)
                            var n2 = new Vector2(hit.normal.x, hit.normal.y);
                            if (n2.sqrMagnitude > 0.0001f)
                                avoidance2D = n2.normalized;
                        }
                    }
                }
            }

            // 6) Combinar fuerzas con pesos
            var final2D =
                targetDir * targetWeight +
                cohesion * cohesionWeight +
                alignment * alignmentWeight +
                separation * separationWeight +
                containment * containmentWeight * containmentMult +
                avoidance2D * avoidanceWeight;

            if (final2D.sqrMagnitude > 0.0001f)
                final2D = final2D.normalized;

            // Volvemos a Vector3 en XY (Z = 0)
            return new Vector3(final2D.x, final2D.y, 0f);
        }

        private void OnEnable()
        {
            InputManager.Instance.ScarePerformed += OnScarePerformed;
            _canBeScared = true;
        }

        private void OnDisable()
        {
            InputManager.Instance.ScarePerformed -= OnScarePerformed;
            if (_scareCoolDown == null) return;
            
            StopCoroutine(_scareCoolDown);
        }
    }
}