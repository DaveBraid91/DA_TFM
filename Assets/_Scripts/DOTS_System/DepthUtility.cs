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
            float depthScale = 1f,
            float offset = 0f)
        {
            var worldPos = localToWorld.Position;

            var desiredWorldZ = worldPos.y * depthScale - offset * depthScale;

            var desiredWorldPos = new float3(
                worldPos.x,
                worldPos.y,
                desiredWorldZ);

            if (parentLookup.HasComponent(entity))
            {
                var parent = parentLookup[entity].Value;
                var parentWorld = localToWorldLookup[parent].Value;

                var desiredLocalPos =
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