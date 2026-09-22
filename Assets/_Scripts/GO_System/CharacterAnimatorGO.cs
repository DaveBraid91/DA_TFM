using UnityEngine;

public enum MoveDirection : byte
{
    Up,
    Right,
    Left,
    Down,
    None = byte.MaxValue
}

[RequireComponent(typeof(Rigidbody))]
public class CharacterAnimatorGO : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Rigidbody rb;

    [Header("Params")]
    [SerializeField] private float moveThreshold = 0.15f;

    private static readonly int AnimationIndexId = Shader.PropertyToID("_AnimationIndex");
    private MaterialPropertyBlock _mpb;
    private MoveDirection _currentDir = MoveDirection.Down;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();

        _mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (!rb || !targetRenderer) return;

        // Para top‑down en XY
        var v2 = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y);
        var speed = v2.magnitude;

        var moving = speed > moveThreshold;

        if (moving)
        {
            // Decide la dirección principal
            if (Mathf.Abs(v2.x) > Mathf.Abs(v2.y))
                _currentDir = v2.x > 0 ? MoveDirection.Right : MoveDirection.Left;
            else
                _currentDir = v2.y > 0 ? MoveDirection.Up : MoveDirection.Down;
        }

        // Misma convención que el DOTS: fila base = dirección, +4 si camina
        var index = (float)_currentDir;
        if (moving)
            index += 4f;

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(AnimationIndexId, index);
        targetRenderer.SetPropertyBlock(_mpb);
    }
}