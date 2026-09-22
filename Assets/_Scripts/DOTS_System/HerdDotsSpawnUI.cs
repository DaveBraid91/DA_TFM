using Unity.Entities;
using UnityEngine;

public class HerdDotsSpawnUI : MonoBehaviour
{
    [Header("Rebaños por click")]
    [SerializeField] private int herdsPerClick = 1;
    
    public void SpawnHerdGroup()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var em = world.EntityManager;

        using var query = em.CreateEntityQuery(
            typeof(DOTSAuthoring.HerdSpawnerConfig),
            typeof(DOTSAuthoring.HerdSpawnRequest));

        if (query.IsEmpty) return;

        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var spawnerEntity = entities[0];

        var req = em.GetComponentData<DOTSAuthoring.HerdSpawnRequest>(spawnerEntity);
        req.PendingHerdGroups += Mathf.Max(1, herdsPerClick);
        em.SetComponentData(spawnerEntity, req);
    }
}