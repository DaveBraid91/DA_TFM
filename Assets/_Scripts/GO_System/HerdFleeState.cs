using UnityEngine;

namespace HerdAI
{
    public class HerdFleeState : HerdBaseState
    {
        [Header("Flee Speeds")]
        [SerializeField] private float fleeMaxSpeed = 5f;
        [SerializeField] private float fleeSteeringMaxSpeed = 10f;
        
        public override void Construct()
        {
            aiBehaviour.maxSpeed        = fleeMaxSpeed;
            aiBehaviour.steeringMaxSpeed = fleeSteeringMaxSpeed;
        }

        public override void Transition()
        {
            if (!group)
                return;

            var timeInState  = motor.timeInState;
            var fleeDuration = group.fleeDuration;

            // Centro global del rebaño
            var herdCenter = group.GetHerdCenter();
            herdCenter.z = transform.position.z; // Z fija

            var myPos = transform.position;
            var toCenter = new Vector2(herdCenter.x - myPos.x, herdCenter.y - myPos.y);
            var distSq = toCenter.sqrMagnitude;

            var joinDist = group.herdJoinDistance;

            if (timeInState >= fleeDuration && distSq <= joinDist * joinDist)
            {
                var wanderState = GetComponent<HerdWanderState>();
                motor.ChangeState(wanderState, HerdState.Wander);
            }
        }

        public override void FixedUpdateState()
        {
            if (!group)
                return;

            var settings = motor.settings;
            if (!settings) return;

            Vector3 dir = group.ComputeFlockDirection(
                motor,
                HerdState.Flee,
                motor.timeInState,
                settings);

            if (dir.sqrMagnitude < 0.0001f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            float speed = aiBehaviour.maxSpeed; // o un fleeMaxSpeed específico
            rb.linearVelocity = dir * speed;
            
            var pos     = transform.position;
            var clamped = group.ClampPositionToBounds(pos);

            if (pos == clamped) return;
            
            transform.position   = clamped;
            rb.linearVelocity    = Vector3.zero; // al tocar muro, se frena
        }
    }
}