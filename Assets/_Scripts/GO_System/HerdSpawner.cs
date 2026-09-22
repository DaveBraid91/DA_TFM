using HerdAI;
using UnityEngine;

public class HerdSpawner : MonoBehaviour
{
    [Header("Prefab del rebaño")]
    [SerializeField] private HerdAI.HerdGroupController herdPrefab;

    [Header("Límites del escenario (XY)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2( 10f,  10f);

    [Header("Parent opcional")]
    [SerializeField] private Transform parent;
    
    [Header("Referencia al Player")]
    [SerializeField] private Transform fleeOrigin;
    
    [Header("Rebaños por click")]
    [SerializeField] private int herdsPerClick = 1;
    
    private int _herdIndex = 0;

    // Método compatible con botón de UI (public, sin parámetros)
    public void SpawnHerd()
    {
        var count = Mathf.Max(1, herdsPerClick);
        for (int i = 0; i < count; i++)
        {
            // Punto aleatorio dentro de los límites
            var x = Random.Range(minBounds.x, maxBounds.x);
            var y = Random.Range(minBounds.y, maxBounds.y);
            var spawnPos = new Vector3(x, y, 0f);

            // Instanciamos el prefab
            var go = Instantiate(herdPrefab, spawnPos, Quaternion.identity, parent);

            var controller = go.GetComponent<HerdGroupController>();
            if (controller != null)
            {
                controller.fleeOrigin = fleeOrigin;
                controller.herdId = _herdIndex;
            }

            _herdIndex++;
        }

    }
}