using Unity.Entities;
using UnityEngine;

namespace DOTSAuthoring
{
    
    /// <summary>
    /// Tag component for the player
    /// </summary>
    public struct PlayerTag : IComponentData {}
    
    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerTag>(entity);
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
            foreach (var direction in SystemAPI.Query<RefRW<CharacterMoveDirection>>().WithAll<PlayerTag>())
            {
                direction.ValueRW.Value = currentInput;
            }
        }
    }
}