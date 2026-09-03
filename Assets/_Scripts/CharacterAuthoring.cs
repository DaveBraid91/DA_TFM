using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSAuthoring
{
    public enum MoveDirection : byte
    {
        Up,
        Right,
        Left,
        Down,
        None = byte.MaxValue
    }

    public struct InitializeCharacterFlag : IComponentData, IEnableableComponent {}
    
    public struct CharacterDepthTag : IComponentData {}

    public struct CharacterMoveDirection : IComponentData
    {
        public float2 Value;
    }

    public struct CharacterMoveSpeed : IComponentData
    {
        public float Value;
    }

    public struct CharacterAnimationDirection : IComponentData
    {
        public MoveDirection Value;
    }

    /// <summary>
    /// Reference to the entity containing the Mesh Renderer.
    /// </summary>
    public struct CharacterVisualReference : IComponentData
    {
        public Entity Value;
    }

    [Serializable]
    public class CharacterAuthoring : MonoBehaviour
    {
        [field: SerializeField] public float MoveSpeed { get; private set; } = 5f;

        [SerializeField] private GameObject rendererChild;
        
        [SerializeField] private Camera worldCamera;

        private class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<InitializeCharacterFlag>(entity);
                AddComponent<CharacterMoveDirection>(entity);
                AddComponent<CharacterAnimationDirection>(entity);
                AddComponent<CharacterDepthTag>(entity);

                AddComponent(entity, new CharacterMoveSpeed
                {
                    Value = authoring.MoveSpeed
                });

                if (authoring.rendererChild != null)
                {
                    var visualEntity = GetEntity(
                        authoring.rendererChild,
                        TransformUsageFlags.Renderable);

                    AddComponent(entity, new CharacterVisualReference
                    {
                        Value = visualEntity
                    });
                }

                if (authoring.worldCamera == null) return;
            }
        }
    }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CharacterInitializationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (mass, shouldInitialize) in
                     SystemAPI.Query<RefRW<PhysicsMass>, EnabledRefRW<InitializeCharacterFlag>>())
            {
                mass.ValueRW.InverseInertia = float3.zero;
                shouldInitialize.ValueRW = false;
            }
        }
    }

    public partial struct CharacterMoveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraOrthoSizeNormalized>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationIndexLookup = SystemAPI.GetComponentLookup<AnimationIndexOverride>();

            foreach (var (
                         velocity,
                         animationDirection,
                         visualReference,
                         direction,
                         speed)
                     in SystemAPI.Query<
                         RefRW<PhysicsVelocity>,
                         RefRW<CharacterAnimationDirection>,
                         RefRW<CharacterVisualReference>,
                         CharacterMoveDirection,
                         CharacterMoveSpeed>())
            {
                var currentDirection = direction.Value;

                var moveStep2D = currentDirection * speed.Value;
                velocity.ValueRW.Linear = new float3(moveStep2D, 0f);

                switch (currentDirection.y)
                {
                    case > 0.15f:
                        animationDirection.ValueRW.Value = MoveDirection.Up;
                        break;

                    case < -0.15f:
                        animationDirection.ValueRW.Value = MoveDirection.Down;
                        break;
                }

                switch (currentDirection.x)
                {
                    case > 0.15f:
                        animationDirection.ValueRW.Value = MoveDirection.Right;
                        break;

                    case < -0.15f:
                        animationDirection.ValueRW.Value = MoveDirection.Left;
                        break;
                }

                var visualEntity = visualReference.ValueRW.Value;

                if (animationIndexLookup.HasComponent(visualEntity))
                {
                    animationIndexLookup.GetRefRW(visualEntity).ValueRW.Value =
                        math.abs(currentDirection.x) + math.abs(currentDirection.y) > 0.15f
                            ? (float)animationDirection.ValueRW.Value + 4.0f
                            : (float)animationDirection.ValueRW.Value;
                }
            }
        }
    }
    
    public partial struct CharacterDepthSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CameraOrthoSizeNormalized>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var visualTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>();

            var visualLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

            var parentLookup = SystemAPI.GetComponentLookup<Parent>(true);

            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

            var depthScale = SystemAPI.GetSingleton<CameraOrthoSizeNormalized>().Value;

            foreach (var (characterLocalToWorld, visualReference) in
                     SystemAPI.Query<RefRO<LocalToWorld>, RefRO<CharacterVisualReference>>())
            {
                Entity visualEntity = visualReference.ValueRO.Value;

                if (!visualTransformLookup.HasComponent(visualEntity) ||
                    !visualLocalToWorldLookup.HasComponent(visualEntity))
                {
                    continue;
                }

                var visualTransform =
                    visualTransformLookup.GetRefRW(visualEntity);

                var visualLocalToWorld =
                    visualLocalToWorldLookup[visualEntity];

                var visualTransformValue = visualTransform.ValueRW;

                DepthUtility.ApplyDepthFromWorldY(
                    visualEntity,
                    ref visualTransformValue,
                    characterLocalToWorld.ValueRO,
                    ref parentLookup,
                    ref localToWorldLookup,
                    depthScale,
                    0.5f);

                visualTransform.ValueRW.Position = new float3(visualTransform.ValueRW.Position.x, visualTransform.ValueRW.Position.x, visualTransformValue.Position.z);
            }
        }
    }

    public partial struct GlobalTimeUpdateSystem : ISystem
    {
        private static int _globalTimeShaderPropertyId;

        public void OnCreate(ref SystemState state)
        {
            _globalTimeShaderPropertyId =
                Shader.PropertyToID("_GlobalTime");
        }

        public void OnUpdate(ref SystemState state)
        {
            Shader.SetGlobalFloat(_globalTimeShaderPropertyId, (float)SystemAPI.Time.ElapsedTime);
        }
    }
}

// using System;
// using Unity.Burst;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Physics;
// using Unity.Rendering;
// using Unity.Transforms;
// using UnityEngine;
//
// namespace Characters
// {
//     public enum MoveDirection : byte
//     {
//         Up,
//         Right,
//         Left,
//         Down,
//         None = byte.MaxValue
//     }
//     
//     /// <summary>
//     /// Flag for a character thas can be initialized
//     /// </summary>
//     public struct InitializeCharacterFlag : IComponentData, IEnableableComponent {}
//     
//     /// <summary>
//     /// Data component for the movement direction of a character
//     /// </summary>
//     public struct CharacterMoveDirection : IComponentData
//     {
//         public float2 Value;
//     }
//     /// <summary>
//     /// Data component for the movement speed of a character
//     /// </summary>
//     public struct CharacterMoveSpeed : IComponentData
//     {
//         public float Value;
//     }
//     
//     [MaterialProperty("_AnimationIndex")]
//     public struct AnimationIndexOverride : IComponentData
//     {
//         public float Value;
//     }
//     
//     public struct CharacterVisualReference : IComponentData
//     {
//         public Entity Value;
//     }
//
//     public struct CharacterAnimationDirection : IComponentData
//     {
//         public MoveDirection Value;
//     }
//
//     [Serializable]
//     public class CharacterAuthoring : MonoBehaviour
//     {
//         [field:SerializeField] public float MoveSpeed{get; private set;}
//
//         /// <summary>
//         /// Reference to the GameObject that contains the Mesh Renderer.
//         /// That way, MaterialProperty applies to the same entity as the DynamicRenderer.
//         /// </summary>
//         public GameObject rendererChild;
//
//         /// <summary>
//         /// Defines how this Monobehaviour is going to be converted to ECS
//         /// </summary>
//         private class Baker : Baker<CharacterAuthoring>
//         {
//             public override void Bake(CharacterAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.Dynamic);
//                 AddComponent<InitializeCharacterFlag>(entity);
//                 AddComponent<CharacterMoveDirection>(entity);
//                 AddComponent<CharacterAnimationDirection>(entity);
//                 AddComponent(entity, new CharacterMoveSpeed
//                 {
//                     Value = authoring.MoveSpeed
//                 });
//
//                 // Add AnimationIndexOverride to the CHILD entity (The one with the MeshRenderer),
//                 if (authoring.rendererChild != null)
//                 {
//                     var childEntity = GetEntity(authoring.rendererChild, TransformUsageFlags.Dynamic);
//                     AddComponent<AnimationIndexOverride>(childEntity);
//                 }
//                 else
//                 {
//                     // Fallback: If the child is empty, it is added to the father.
//                     AddComponent<AnimationIndexOverride>(entity);
//                 }
//             }
//         }
//     }
//
//     //This makes the update to happen at the beginning of the frame
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     public partial struct CharacterInitializationSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             // EnabledRefRW<> allows to enable and disable the component
//             foreach (var (mass, shouldInitialize) in SystemAPI.Query<RefRW<PhysicsMass>, EnabledRefRW<InitializeCharacterFlag>>())
//             {
//                 //Diables physics rotations
//                 mass.ValueRW.InverseInertia = float3.zero;
//                 //Disable the component once it has fulfilled its task
//                 shouldInitialize.ValueRW = false;
//             }
//         }
//     }
//
//     public partial struct CharacterMoveSystem : ISystem
//     {
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             foreach (var (velocity, transform, animationIndex, animationDirection, direction, speed) 
//                      in SystemAPI.Query<RefRW<PhysicsVelocity>, RefRW<LocalTransform>, RefRW<AnimationIndexOverride>, RefRW<CharacterAnimationDirection>,CharacterMoveDirection, CharacterMoveSpeed>())
//             {
//                 var moveStep2D = direction.Value * speed.Value;
//                 //TODO: Agregar movimiento en z para superposición
//                 var moveStep3D = new float3(moveStep2D, 0);
//                 velocity.ValueRW.Linear = moveStep3D;
//                 switch (direction.Value.y)
//                 {
//                     case > 0.15f:
//                         animationDirection.ValueRW.Value = MoveDirection.Up;
//                         break;
//                     case < -0.15f:
//                         animationDirection.ValueRW.Value = MoveDirection.Down;
//                         break;
//                 }
//                 switch (direction.Value.x)
//                 {
//                     case > 0.15f:
//                         animationDirection.ValueRW.Value = MoveDirection.Right;
//                         break;
//                     case < -0.15f:
//                         animationDirection.ValueRW.Value = MoveDirection.Left;
//                         break;
//                 }
//
//                 animationIndex.ValueRW.Value = (float)animationDirection.ValueRW.Value;
//             }
//         }
//     }
//
//     public partial struct GlobalTimeUpdateSystem : ISystem
//     {
//         private static int _globalTimeShaderPropertyID;
//
//         public void OnCreate(ref SystemState state)
//         {
//             _globalTimeShaderPropertyID = Shader.PropertyToID("_GlobalTime");
//         }
//
//         public void OnUpdate(ref SystemState state)
//         {
//             Shader.SetGlobalFloat(_globalTimeShaderPropertyID, (float)SystemAPI.Time.ElapsedTime);
//         }
//     }
// }