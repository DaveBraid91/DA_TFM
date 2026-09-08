using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DOTSAuthoring
{
    public class HerdWorldBoundsAuthoring : MonoBehaviour
    {
        [SerializeField] private Vector2 min = new(-20f, -10f);
        [SerializeField] private Vector2 max = new(20f, 10f);
        [SerializeField] private Vector2 margin = new(1.5f, 1.5f);

        private class Baker : Baker<HerdWorldBoundsAuthoring>
        {
            public override void Bake(HerdWorldBoundsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new HerdWorldBounds
                {
                    Min = new float2(authoring.min.x, authoring.min.y),
                    Max = new float2(authoring.max.x, authoring.max.y),
                    Margin = new float2(authoring.margin.x, authoring.margin.y)
                });
            }
        }
    }
}