using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DOTSAuthoring
{
    public class HerdAgentAuthoring : MonoBehaviour
    {
        [SerializeField] private int herdId = 0;

        [SerializeField] private float neighborRadius = 2.5f;
        [SerializeField] private float separationRadius = 0.75f;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float reachedDistance = 0.2f;
        [SerializeField] private float herdJoinDistance = 1.5f;

        [SerializeField] private float cohesionWeight = 1.2f;
        [SerializeField] private float alignmentWeight = 1.0f;
        [SerializeField] private float separationWeight = 2.5f;
        [SerializeField] private float targetWeight = 1.5f;
        [SerializeField] private float avoidanceWeight = 2.0f;
        [SerializeField] private float containmentWeight = 2.0f;

        [SerializeField] private float idleDuration = 1.5f;
        [SerializeField] private float fleeDuration = 2.0f;
        [SerializeField] private float wanderRadius = 4.0f;

        private class Baker : Baker<HerdAgentAuthoring>
        {
            public override void Bake(HerdAgentAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new HerdAgent
                {
                    HerdId = authoring.herdId
                });

                AddComponent(entity, new HerdStateMachine
                {
                    Current = HerdState.Idle,
                    Previous = HerdState.Idle,
                    TimeInState = 0f
                });

                AddComponent(entity, new HerdDesiredDirection
                {
                    Value = float2.zero
                });

                AddComponent(entity, new HerdVelocity2D
                {
                    Value = float2.zero
                });

                AddComponent(entity, new HerdAgentSettings
                {
                    NeighborRadius = authoring.neighborRadius,
                    SeparationRadius = authoring.separationRadius,
                    CellSize = authoring.cellSize,
                    ReachedDistance = authoring.reachedDistance,
                    HerdJoinDistance = authoring.herdJoinDistance
                });

                AddComponent(entity, new HerdSteeringWeights
                {
                    Cohesion = authoring.cohesionWeight,
                    Alignment = authoring.alignmentWeight,
                    Separation = authoring.separationWeight,
                    Target = authoring.targetWeight,
                    Avoidance = authoring.avoidanceWeight,
                    Containment = authoring.containmentWeight
                });

                AddComponent(entity, new HerdBehaviorTimings
                {
                    IdleDuration = authoring.idleDuration,
                    FleeDuration = authoring.fleeDuration,
                    WanderRadius = authoring.wanderRadius
                });

                var seed = (uint)math.max(1, entity.Index + 1);
                AddComponent(entity, new HerdRandom
                {
                    Value = Random.CreateFromIndex(seed)
                });
            }
        }
    }
}