using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTSAuthoring
{
    public class HerdGroupAuthoring : MonoBehaviour
    {
        [SerializeField] private int herdId = 0;
        [SerializeField] private float triggerRadius = 3f;

        private class Baker : Baker<HerdGroupAuthoring>
        {
            public override void Bake(HerdGroupAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new HerdGroup
                {
                    HerdId = authoring.herdId
                });

                AddComponent(entity, new HerdSharedTarget
                {
                    Value = float2.zero
                });

                AddComponent(entity, new HerdFleeData
                {
                    Direction = float2.zero,
                    Origin = float2.zero,
                    TriggerRadius = authoring.triggerRadius
                });
            }
        }
    }
}