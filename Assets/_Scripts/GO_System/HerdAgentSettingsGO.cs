using UnityEngine;

public class HerdAgentSettingsGO : MonoBehaviour
{
    [Header("Radios")]
    public float neighborRadius    = 2.5f;
    public float separationRadius  = 0.75f;
    public float reachedDistance   = 0.2f;
    public float herdJoinDistance  = 1.5f;

    [Header("Pesos")]
    public float cohesionWeight    = 1.2f;
    public float alignmentWeight   = 1.0f;
    public float separationWeight  = 2.5f;
    public float targetWeight      = 1.5f;
    public float avoidanceWeight   = 2.0f;
    public float containmentWeight = 2.0f;
}