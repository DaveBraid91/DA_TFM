using UnityEngine;

namespace DepthSystem
{
    /// <summary>
    /// Ajusta automáticamente la Z del GameObject a partir de su Y mundial,
    /// para simular profundidad en un plano XY.
    /// Pon este componente en Player y agentes.
    /// </summary>
    public class DepthFromY : MonoBehaviour
    {
        [Header("Depth Settings")]
        [SerializeField] private float depthScale = 1f;
        [SerializeField] private float offset = 0f;
        [SerializeField] private bool useParentSpace = false;
        [SerializeField] private bool runInUpdate = false;
        
        private void Start()
        {
            DepthUtilityGO.ApplyDepthFromWorldY(transform, depthScale, offset, useParentSpace);
        }
        
        private void LateUpdate()
        {
            if (!runInUpdate) return;
            DepthUtilityGO.ApplyDepthFromWorldY(transform, depthScale, offset, useParentSpace);
        }
    }
}