using Unity.Entities;
using UnityEngine;

namespace DOTSAuthoring
{
    public struct CameraOrthoSizeSingleton : IComponentData
    {
        public float Value;
    }
    
    public class CameraOrthoSizeBridge : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        private EntityManager entityManager;
        private Entity singletonEntity;

        private void Start()
        {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var query = entityManager.CreateEntityQuery(typeof(CameraOrthoSizeSingleton));
            if (query.IsEmptyIgnoreFilter)
            {
                singletonEntity = entityManager.CreateEntity(typeof(CameraOrthoSizeSingleton));
                entityManager.SetComponentData(singletonEntity, new CameraOrthoSizeSingleton
                {
                    Value = targetCamera != null ? targetCamera.orthographicSize : 15f
                });
            }
            else
            {
                singletonEntity = query.GetSingletonEntity();
            }
        }
        
        //Uncomment if the camera size is necessary during the gameplay.
        // private void Update()
        // {
        //     if (targetCamera == null || !entityManager.Exists(singletonEntity)) return;
        //
        //     entityManager.SetComponentData(singletonEntity, new CameraOrthoSizeSingleton
        //     {
        //         Value = targetCamera.orthographicSize
        //     });
        // }
    }

    public partial struct CameraOrthoSizeBootstrapSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingletonEntity<CameraOrthoSizeSingleton>(out _)) return;
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new CameraOrthoSizeSingleton
            {
                Value = 15f
            });
        }
    }
}