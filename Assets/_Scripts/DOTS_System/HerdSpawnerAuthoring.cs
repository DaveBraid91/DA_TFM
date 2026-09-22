using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTSAuthoring
{
    public struct HerdSpawnerConfig : IComponentData
    {
        public Entity HerdGroupPrefab;
        public Entity HerdAgentPrefab;

        public int AgentsPerHerd;
        public float SpawnRadius;

        public int NextHerdId;
    }

    public struct HerdSpawnRequest : IComponentData
    {
        public int PendingHerdGroups;
    }

    public class HerdSpawnerAuthoring : MonoBehaviour
    {
        [Header("Prefabs DOTS")]
        [SerializeField] private GameObject herdGroupPrefab;   // Tiene HerdGroupAuthoring
        [SerializeField] private GameObject herdAgentPrefab;   // Tiene HerdAgentAuthoring

        [Header("Config de cada rebaño")]
        [SerializeField] private int agentsPerHerd = 10;
        [SerializeField] private float spawnRadius = 1.5f;
        [SerializeField] private int startingHerdId = 0;

        private class Baker : Baker<HerdSpawnerAuthoring>
        {
            public override void Bake(HerdSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var groupPrefabEntity = GetEntity(
                    authoring.herdGroupPrefab,
                    TransformUsageFlags.Dynamic);

                var agentPrefabEntity = GetEntity(
                    authoring.herdAgentPrefab,
                    TransformUsageFlags.Dynamic);

                AddComponent(entity, new HerdSpawnerConfig
                {
                    HerdGroupPrefab = groupPrefabEntity,
                    HerdAgentPrefab = agentPrefabEntity,
                    AgentsPerHerd   = math.max(1, authoring.agentsPerHerd),
                    SpawnRadius     = math.max(0.1f, authoring.spawnRadius),
                    NextHerdId      = authoring.startingHerdId
                });

                // Singleton de petición
                AddComponent<HerdSpawnRequest>(entity);
            }
        }
    }
}