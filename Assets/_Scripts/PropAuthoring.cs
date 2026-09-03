using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;


namespace DOTSAuthoring
{
    public struct InitializePropFlag : IComponentData, IEnableableComponent {}
    
    public class PropAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PropAuthoring>
        {
            public override void Bake(PropAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent<InitializePropFlag>(entity);
            }
        }
    }
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct PropInitializationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraOrthoSizeNormalized>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);

            var depthScale = SystemAPI.GetSingleton<CameraOrthoSizeNormalized>().Value;
            
            foreach (var (transform, shouldInitialize, localToWorld, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, EnabledRefRW<InitializePropFlag>, RefRO<LocalToWorld>>().WithEntityAccess())
            {
                float3 worldPos = localToWorld.ValueRO.Position;
                var t = transform.ValueRW;

                DepthUtility.ApplyDepthFromWorldY(entity, ref t, localToWorld.ValueRO, ref parentLookup, ref localToWorldLookup, depthScale, 0.483f);

                transform.ValueRW = t;
                
                shouldInitialize.ValueRW = false;
            }
        }
    }
}

