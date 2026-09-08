using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace DOTSAuthoring
{
    
    /// <summary>
    /// Tag component for the player
    /// </summary>
    public struct PlayerTag : IComponentData {}
    
    /// <summary>
    /// Event for the Scare action. The value is the position of the player
    /// </summary>
    public struct PlayerScareEvent : IComponentData, IEnableableComponent
    {
        public float2 Value;
    }
    
    public struct PlayerScareEventCooldown : IComponentData
    {
        public float Duration;
        public double NextReadyTime;
    }

    
    [RequireComponent(typeof(CharacterAuthoring))]
    public class PlayerAuthoring : MonoBehaviour
    {
        [SerializeField] private float scareCooldownSeconds = 2f;
        
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
                AddComponent<PlayerScareEvent>(entity);
                SetComponentEnabled<PlayerScareEvent>(entity, false);
                AddComponent(entity, new PlayerScareEventCooldown
                {
                    Duration = authoring.scareCooldownSeconds,
                    NextReadyTime = 0.0
                });
            }
        }
    }
    
    public partial class PlayerInputSystem : SystemBase
    {
        private DefaultInputActions inputActions;

        protected override void OnCreate()
        {
            inputActions = new DefaultInputActions();
            inputActions.Enable();
        }

        protected override void OnUpdate()
        {
            var currentInput = inputActions.Player.Move.ReadValue<Vector2>();
            var scarePressed = inputActions.Player.Scare.WasCompletedThisFrame();
            var scareEventLookup = SystemAPI.GetComponentLookup<PlayerScareEvent>();

            foreach (var direction in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<PlayerTag>())
            {
                direction.ValueRW.Value = currentInput;
            }
            
            foreach (var (scareEventCooldown, localToWorld, entity) in 
                     SystemAPI.Query<
                         RefRW<PlayerScareEventCooldown>,
                         RefRO<LocalToWorld>>()
                         .WithAll<PlayerTag>()
                         .WithEntityAccess())
            {
                if (!scarePressed)
                    continue;
                
                var now = SystemAPI.Time.ElapsedTime;
                
                if(now < scareEventCooldown.ValueRO.NextReadyTime)
                    continue;
                
                scareEventLookup.GetRefRW(entity).ValueRW.Value = localToWorld.ValueRO.Position.xy;
                SystemAPI.SetComponentEnabled<PlayerScareEvent>(entity, true);
                
                scareEventCooldown.ValueRW.NextReadyTime = now + scareEventCooldown.ValueRO.Duration;
            }
        }
        
        protected override void OnDestroy()
        {
            inputActions.Disable();
        }
    }
}