using Fusion;
using UnityEngine;

public sealed class FirstPersonNetworkMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private CharacterController characterController;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 6f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownPitchLimit = 85f;
    
    [SerializeField] private PlayerAnimationController animationController;

    [Networked]
    private float VerticalVelocity { get; set; }

    private float _pitch;
    private bool _isLocalPlayer;
    private bool _cursorLocked;

    public override void Spawned()
    {
        _isLocalPlayer = Object.HasInputAuthority;

        SetCameraActive(_isLocalPlayer);

        if (!_isLocalPlayer)
            return;

        LockCursor();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!_isLocalPlayer)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!_isLocalPlayer)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }

        if (!_cursorLocked && Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!_isLocalPlayer)
            return;

        if (hasFocus && _cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!GetInput(out NetworkInputData inputData))
            return;

        RotatePlayer(inputData);
        Move(inputData);
    }

    private void RotatePlayer(NetworkInputData inputData)
    {
        float mouseX = inputData.lookInput.x * mouseSensitivity;
        float mouseY = inputData.lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -upDownPitchLimit, upDownPitchLimit);

        if (cameraRoot != null && Object.HasInputAuthority)
        {
            cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }

    private void Move(NetworkInputData inputData)
    {
        Vector3 moveDirection =
            transform.right * inputData.moveInput.x +
            transform.forward * inputData.moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        bool isGrounded =
            characterController != null
                ? characterController.isGrounded
                : true;

        if (isGrounded && VerticalVelocity < 0f)
            VerticalVelocity = -2f;

        if (isGrounded && inputData.jumpRequested)
            VerticalVelocity = jumpForce;

        VerticalVelocity += gravity * Runner.DeltaTime;

        float currentSpeed =
            moveSpeed *
            (inputData.sprintRequested ? sprintMultiplier : 1f);

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = VerticalVelocity;

        if (characterController != null)
        {
            characterController.Move(
                velocity * Runner.DeltaTime);
        }
        else
        {
            transform.position +=
                velocity * Runner.DeltaTime;
        }
        
        float animationSpeed = inputData.moveInput.magnitude;
        animationController.SetMovementSpeed(animationSpeed);
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera != null)
            playerCamera.enabled = active;

        if (audioListener != null)
            audioListener.enabled = active;
    }

    private void LockCursor()
    {
        _cursorLocked = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        _cursorLocked = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}