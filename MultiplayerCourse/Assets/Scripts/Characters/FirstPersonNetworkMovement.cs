using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonNetworkMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private float _verticalVelocity;
    private float _pitch;
    private bool _isLocalPlayer;

    private void Awake()
    {
        if (!characterController)
            characterController = GetComponent<CharacterController>();

        SetCameraActive(false);
    }

    public override void Spawned()
    {
        _isLocalPlayer = Object.HasInputAuthority;

        if (!_isLocalPlayer)
        {
            SetCameraActive(false);
            return;
        }

        if (!Object.HasStateAuthority)
            Object.RequestStateAuthority();

        EnableInput();
        SetCameraActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!_isLocalPlayer)
            return;

        DisableInput();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!_isLocalPlayer)
            return;

        Move();
        Look();
        
      
        
       
    }

    private void Move()
    {
        Vector2 moveInput = Keyboard.current != null
            ? new Vector2(
                (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0),
                (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0))
            : Vector2.zero;

        Debug.Log($"MOVE INPUT = {moveInput}");
        

        Vector3 moveDirection =
            transform.right * moveInput.x +
            transform.forward * moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        if (characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        if (characterController.isGrounded && jumpAction.action.WasPressedThisFrame())
            _verticalVelocity = jumpForce;

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMovement = moveDirection * moveSpeed;
        finalMovement.y = _verticalVelocity;
        

        characterController.Move(finalMovement * Time.deltaTime);
        
    }

    private void Look()
    {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void EnableInput()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
    }

    private void DisableInput()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera)
            playerCamera.enabled = active;
        
    }
}