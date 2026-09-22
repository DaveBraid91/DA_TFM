using UnityEngine;
using AISystem;   // Para AIBehaviour
// Ojo: este BaseState es independiente del que ya tienes en AISystem.

namespace HerdAI
{
    [RequireComponent(typeof(HerdAgentStateMotor))]
    public abstract class HerdBaseState : MonoBehaviour
    {
        protected HerdAgentStateMotor motor;
        protected HerdGroupController group;
        protected AIBehaviour aiBehaviour;
        protected Rigidbody rb;

        protected virtual void Awake()
        {
            motor = GetComponent<HerdAgentStateMotor>();
            group = motor.group;
            aiBehaviour = motor.aiBehaviour;
            rb = motor.rb;
        }

        public virtual void Construct() { }
        public virtual void Destruct() { }

        public virtual void Transition() { }

        public virtual void FixedUpdateState() { }
    }
}