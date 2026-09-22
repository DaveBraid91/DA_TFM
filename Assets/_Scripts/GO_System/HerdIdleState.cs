using System.Collections;
using UnityEngine;

namespace HerdAI
{
    public class HerdIdleState : HerdBaseState
    {
        [SerializeField] private float idleDuration = 1.5f;

        private Coroutine _idleRoutine;

        public override void Construct()
        {
            rb.linearVelocity = Vector3.zero;

            if (_idleRoutine != null)
                StopCoroutine(_idleRoutine);

            _idleRoutine = StartCoroutine(IdleCoroutine());
        }

        public override void Destruct()
        {
            if (_idleRoutine == null) return;
            
            StopCoroutine(_idleRoutine);
        }

        public override void Transition()
        {
            // Si alguien ha disparado flee desde el grupo (ForceEnterFlee), este estado será sustituido
            // por HerdFleeState mediante ChangeState, así que no hace falta lógica aquí.
        }

        private IEnumerator IdleCoroutine()
        {
            yield return new WaitForSeconds(idleDuration);

            if (group != null)
            {
                // Elegimos nuevo target de wander desde la posición actual.
                group.PickNewWanderTarget(transform.position);
            }

            var wanderState = GetComponent<HerdWanderState>();
            motor.ChangeState(wanderState, HerdState.Wander);
        }
    }
}