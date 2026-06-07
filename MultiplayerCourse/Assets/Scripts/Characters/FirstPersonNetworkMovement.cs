using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FirstPersonNetworkMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundY = 0f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private bool _isLocalPlayer;
    private Vector2 _moveInput;
    private bool _jumpRequested;
    private float _verticalVelocity;
    private float _pitch;

    public override void Spawned()
    {
        _isLocalPlayer = Object.HasInputAuthority;

        if (!_isLocalPlayer)
        {
            SetCameraActive(false);
            return;
        }

        moveAction.action.actionMap.Enable();

        SetCameraActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!_isLocalPlayer)
            return;

        moveAction.action.actionMap.Disable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!_isLocalPlayer)
            return;

        ReadInput();
        Look();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        Move();
    }

    private void ReadInput()
    {
        _moveInput = moveAction.action.ReadValue<Vector2>();

        if (jumpAction.action.WasPressedThisFrame())
            _jumpRequested = true;
    }

    private void Move()
    {
        Vector3 moveDirection =
            transform.right * _moveInput.x +
            transform.forward * _moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        bool isGrounded = transform.position.y <= groundY + 0.05f;

        if (isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = 0f;

        if (isGrounded && _jumpRequested)
            _verticalVelocity = jumpForce;

        _jumpRequested = false;

        _verticalVelocity += gravity * Runner.DeltaTime;

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = _verticalVelocity;

        transform.position += velocity * Runner.DeltaTime;

        if (transform.position.y < groundY)
        {
            transform.position = new Vector3(
                transform.position.x,
                groundY,
                transform.position.z);

            _verticalVelocity = 0f;
        }
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

    private void SetCameraActive(bool active)
    {
        if (playerCamera != null)
            playerCamera.enabled = active;
    }
}