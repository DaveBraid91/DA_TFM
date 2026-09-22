using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace DOTSAuthoring
{
    [MaterialProperty("_AnimationIndex")]
    public struct AnimationIndexOverride : IComponentData
    {
        public float Value;
    }
    
    /// <summary>
    /// Class for the Aesthetics GameObject.
    /// </summary>
    public class CharacterRendererAuthoring : MonoBehaviour
    {
        private class Baker : Baker<CharacterRendererAuthoring>
        {
            public override void Bake(CharacterRendererAuthoring authoring)
            {
                var entity = GetEntity(
                    TransformUsageFlags.Renderable);

                AddComponent(entity, new AnimationIndexOverride
                {
                    Value = 0f
                });
            }
        }
    }
}