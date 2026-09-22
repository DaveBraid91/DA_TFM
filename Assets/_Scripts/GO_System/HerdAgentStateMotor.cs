using System.Collections;
using HerdAI;
using AISystem;
using UnityEngine;

namespace HerdAI
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AIBehaviour))]
    [RequireComponent(typeof(HerdIdleState))]
    [RequireComponent(typeof(HerdWanderState))]
    [RequireComponent(typeof(HerdFleeState))]
    public class HerdAgentStateMotor : MonoBehaviour
    {
        [Header("State")]
        public HerdState stateEnum = HerdState.Idle;

        [Header("References")]
        public HerdGroupController group;
        public Transform player;   // Asigna el jugador desde el editor
        public Rigidbody rb;
        public AIBehaviour aiBehaviour;
        public HerdAgentSettingsGO settings;

        [Header("Debug")]
        public float timeInState;

        private HerdBaseState _state;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            aiBehaviour = GetComponent<AIBehaviour>();
            settings = GetComponent<HerdAgentSettingsGO>();

            if (group != null)
                group.Register(this);

            // Estado inicial
            _state = GetComponent<HerdIdleState>();
        }

        private void OnEnable()
        {
            if (group != null)
                group.Register(this);
        }

        private void OnDisable()
        {
            if (group != null)
                group.Unregister(this);
        }

        private void Start()
        {
            _state.Construct();
        }

        private void Update()
        {
            timeInState += Time.deltaTime;
            _state.Transition();
        }

        private void FixedUpdate()
        {
            _state.FixedUpdateState();
        }

        public void ChangeState(HerdBaseState newState, HerdState newEnum)
        {
            _state.Destruct();
            stateEnum = newEnum;
            timeInState = 0f;
            _state = newState;
            _state.Construct();
        }

        public void ForceEnterFlee()
        {
            var fleeState = GetComponent<HerdFleeState>();
            ChangeState(fleeState, HerdState.Flee);
        }
    }
}