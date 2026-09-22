using UnityEngine;

namespace DepthSystem
{
    public static class DepthUtilityGO
    {
        /// <summary>
        /// Ajusta la Z mundial de un Transform en función de su Y mundial.
        /// Si useParentSpace es true, calcula la posición local relativa al padre
        /// igual que hacía la versión DOTS con Parent/LocalToWorld.
        /// </summary>
        public static void ApplyDepthFromWorldY(
            Transform t,
            float depthScale = 1f,
            float offset = 0f,
            bool useParentSpace = false)
        {
            if (!t) return;

            var worldPos = t.position;

            var desiredWorldZ = worldPos.y * depthScale - offset * depthScale;
            var desiredWorldPos = new Vector3(
                worldPos.x,
                worldPos.y,
                desiredWorldZ);

            if (useParentSpace && t.parent)
            {
                // Convertimos a coordenadas locales del padre (equivalente a math.transform)
                var desiredLocalPos = t.parent.InverseTransformPoint(desiredWorldPos);
                t.localPosition = desiredLocalPos;
            }
            else
            {
                t.position = desiredWorldPos;
            }
        }
    }
}