using UnityEngine;

namespace HerdAI
{
    public class HerdWanderState : HerdBaseState
    {
        [SerializeField] private float reachedDistance = 0.2f;
        
        [Header("Wander Speeds")]
        [SerializeField] private float wanderMaxSpeed = 3f;
        [SerializeField] private float wanderSteeringMaxSpeed = 8f;
        [SerializeField] private float wanderStoppingDistance = 0.1f;

        public override void Construct()
        {
            aiBehaviour.maxSpeed = wanderMaxSpeed;
            aiBehaviour.steeringMaxSpeed = wanderSteeringMaxSpeed;
            aiBehaviour.stoppingDistance = wanderStoppingDistance;
        }

        public override void Transition()
        {
            if (motor.stateEnum != HerdState.Wander)
                return;
            if (!group)
                return;

            var myPos  = transform.position;
            var target = group.sharedWanderTarget;

            // Distancia en XY, ignorando Z
            var toTarget = new Vector2(target.x - myPos.x, target.y - myPos.y);

            if (toTarget.sqrMagnitude <= reachedDistance * reachedDistance)
            {
                var idleState = GetComponent<HerdIdleState>();
                motor.ChangeState(idleState, HerdState.Idle);
            }
        }

        public override void FixedUpdateState()
        {
            if (!group)
                return;
            
            var settings = motor.settings;
            if (!settings) return;

            // Dirección final con pesos (cohesión, separación, target, avoidance, etc.)
            Vector3 dir = group.ComputeFlockDirection(
                motor,
                HerdState.Wander,
                motor.timeInState,
                settings);

            // Si no hay dirección clara, te puedes quedar quieto o hacer un fallback
            if (dir.sqrMagnitude < 0.0001f)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }

            float speed = aiBehaviour.maxSpeed;   // reutilizas el valor del AIBehaviour
            rb.linearVelocity = dir * speed;
        }
    }
}