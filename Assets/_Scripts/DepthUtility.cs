using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DOTSAuthoring
{
    public static class DepthUtility
    {
        public static void ApplyDepthFromWorldY(
            Entity entity,
            ref LocalTransform localTransform,
            in LocalToWorld localToWorld,
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<LocalToWorld> localToWorldLookup,
            float depthScale,
            float offset)
        {
            float3 worldPos = localToWorld.Position;

            float desiredWorldZ = worldPos.y * depthScale - offset;

            float3 desiredWorldPos = new float3(
                worldPos.x,
                worldPos.y,
                desiredWorldZ);

            if (parentLookup.HasComponent(entity))
            {
                Entity parent = parentLookup[entity].Value;
                float4x4 parentWorld = localToWorldLookup[parent].Value;

                float3 desiredLocalPos =
                    math.transform(math.inverse(parentWorld), desiredWorldPos);

                localTransform.Position = desiredLocalPos;
            }
            else
            {
                localTransform.Position = desiredWorldPos;
            }
        }
    }
}