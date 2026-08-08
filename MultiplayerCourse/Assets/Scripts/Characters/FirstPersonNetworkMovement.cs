using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FirstPersonNetworkMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownPitchLimit = 85f;

    [Networked]
    private float VerticalVelocity { get; set; }

    [Networked]
    private float NetworkPitch { get; set; }

    private bool _isLocalPlayer;
    private bool _cursorLocked;

    public override void Spawned()
    {
        
        _isLocalPlayer = Object.HasInputAuthority;

        SetCameraActive(_isLocalPlayer);

        if (!_isLocalPlayer)
            return;

       
        if (FusionInputProvider.Instance != null)
        {
            FusionInputProvider.Instance.EnsureReady(Runner);
        }

        LockCursor();
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!_isLocalPlayer)
            return;

        UnlockCursor();
    }

    private void Update()
    {
        if (!_isLocalPlayer)
            return;

        HandleCursor();

        HandleCaptureTheFlagInput();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!GetInput(out NetworkInputData inputData))
            return;

        RotatePlayer(inputData);
        MovePlayer(inputData);
    }

    public override void Render()
    {
      
        if (!Object.HasInputAuthority)
            return;

        if (cameraRoot == null)
            return;

        cameraRoot.localRotation =
            Quaternion.Euler(
                NetworkPitch,
                0f,
                0f);
    }

    private void RotatePlayer(
        NetworkInputData inputData)
    {
        float mouseX =
            inputData.lookInput.x *
            mouseSensitivity;

        float mouseY =
            inputData.lookInput.y *
            mouseSensitivity;
        
        transform.Rotate(
            Vector3.up * mouseX);

       
        NetworkPitch -= mouseY;

        NetworkPitch = Mathf.Clamp(
            NetworkPitch,
            -upDownPitchLimit,
            upDownPitchLimit);
    }

    private void MovePlayer(
        NetworkInputData inputData)
    {
        Vector3 moveDirection =
            transform.right *
            inputData.moveInput.x +
            transform.forward *
            inputData.moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        bool isGrounded =
            characterController == null || characterController.isGrounded;

        if (isGrounded &&
            VerticalVelocity < 0f)
        {
            VerticalVelocity = -2f;
        }

        if (isGrounded &&
            inputData.jumpRequested)
        {
            VerticalVelocity = jumpForce;
        }

        VerticalVelocity +=
            gravity * Runner.DeltaTime;

        float gameModeSpeedMultiplier = 1f;

        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (captureTheFlagManager != null &&
            captureTheFlagManager.IsReady &&
            captureTheFlagManager.IsCarrier(
                Object.InputAuthority))
        {
            gameModeSpeedMultiplier =
                captureTheFlagManager
                    .CarrierSpeedMultiplier;
        }

        float currentSpeed =
            moveSpeed *
            gameModeSpeedMultiplier;

        if (inputData.sprintRequested)
        {
            currentSpeed *=
                sprintMultiplier;
        }

        Vector3 velocity =
            moveDirection *
            currentSpeed;

        velocity.y =
            VerticalVelocity;

        if (characterController != null)
        {
            characterController.Move(
                velocity *
                Runner.DeltaTime);
        }
        else
        {
            transform.position +=
                velocity *
                Runner.DeltaTime;
        }

        UpdateMovementAnimation(
            inputData);
    }

    private void UpdateMovementAnimation(
        NetworkInputData inputData)
    {
        if (animationController == null)
            return;

        animationController.SetMovementSpeed(
            inputData.moveInput.magnitude);
    }

    private void HandleCaptureTheFlagInput()
    {
        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (captureTheFlagManager == null)
            return;

        if (!captureTheFlagManager.IsReady)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.gKey
                .wasPressedThisFrame)
            return;

        captureTheFlagManager.RequestDrop(
            Object.InputAuthority);
    }

    private void HandleCursor()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey
                .wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (!_cursorLocked &&
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private void OnApplicationFocus(
        bool hasFocus)
    {
        if (!_isLocalPlayer)
            return;

        if (!hasFocus)
            return;

        if (!_cursorLocked)
            return;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }

    private void SetCameraActive(
        bool active)
    {
        if (playerCamera != null)
        {
            playerCamera.enabled =
                active;
        }

        if (audioListener != null)
        {
            audioListener.enabled =
                active;
        }
    }

    private void LockCursor()
    {
        _cursorLocked = true;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        _cursorLocked = false;

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;
    }
}