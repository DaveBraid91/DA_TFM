using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
{
    private DefaultInputActions _inputActions;
    
    public Action ScarePerformed,
        PausePerformed,
        UnPausePerformed;

    protected override void Awake()
    {
        base.Awake();
        _inputActions = new DefaultInputActions();
        SwitchUIToPlayer();
    }

    private void Start()
    {
        _inputActions.Player.Scare.performed += ScareOnPerformed;
        _inputActions.Player.Pause.performed += PauseOnPerformed;
        _inputActions.UI.UnPause.performed += UnPauseOnPerformed;
    }

    private void PauseOnPerformed(InputAction.CallbackContext obj)
    {
        PausePerformed?.Invoke();
        SwitchPlayerToUI();
    }

    private void UnPauseOnPerformed(InputAction.CallbackContext obj)
    {
        UnPausePerformed?.Invoke();
        SwitchUIToPlayer();
    }

    private void ScareOnPerformed(InputAction.CallbackContext obj)
    {
        ScarePerformed?.Invoke();
    }

    public Vector2 GetMovement()
    {
        return _inputActions.Player.Move.ReadValue<Vector2>();
    }

    public void SwitchUIToPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _inputActions.UI.Disable();
        _inputActions.Player.Enable();
    }

    public void SwitchPlayerToUI()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        _inputActions.Player.Disable();
        _inputActions.UI.Enable();
    }
}
