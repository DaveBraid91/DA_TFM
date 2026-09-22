using Unity.Entities;
using UnityEngine;

namespace DOTSAuthoring
{
    public struct CameraOrthoSizeNormalized : IComponentData
    {
        public float Value;
    }
    [RequireComponent(typeof(Camera))]
    public class CameraAuthoring : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private class Baker : Baker<CameraAuthoring>
        {
            public override void Bake(CameraAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var orthoSize = 15f;
                
                authoring.targetCamera = authoring.GetComponent<Camera>();
                
                orthoSize = authoring.targetCamera.orthographicSize;
                

                AddComponent(entity, new CameraOrthoSizeNormalized
                {
                    Value = 1 / (orthoSize * 2f)
                });
            }
        }
    }
}