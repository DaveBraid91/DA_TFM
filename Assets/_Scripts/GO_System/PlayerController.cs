using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    
    private Rigidbody _rb;
    private Vector3 _finalVelocity;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        MovementLogic();
        
        Debug.Log(_finalVelocity);
        
        _rb.linearVelocity = _finalVelocity;
    }
    
    private void MovementLogic()
    {
        var movementInput = GetMovementInput();
        var movementDirection = new Vector3(movementInput.x, movementInput.y, 0f);
        
        if(movementDirection.sqrMagnitude > 1)
            movementDirection.Normalize();
        
        _finalVelocity = movementDirection * moveSpeed;
    }
    
    private Vector2 GetMovementInput()
    {
        return InputManager.Instance.GetMovement();
    }
    
    private void ScarePerformed()
    {
        //TODO: Sonido + partículas
    }

    private void OnEnable()
    {
        InputManager.Instance.ScarePerformed += ScarePerformed;
    }

    private void OnDisable()
    {
        InputManager.Instance.ScarePerformed -= ScarePerformed;
    }
}
