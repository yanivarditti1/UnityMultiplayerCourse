using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FirstPersonNetworkMovement : NetworkBehaviour
{
    [Header("References")] [SerializeField]
    private Transform cameraRoot;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private CharacterController characterController;

    [Header("Movement Settings")] [SerializeField]
    private float moveSpeed = 6f;

    [SerializeField] private float sprintMultiplier = 6f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")] [SerializeField]
    private float mouseSensitivity = 0.1f;

    [SerializeField] private float upDownPitchLimit = 85f;

    [SerializeField] private PlayerAnimationController animationController;

    [Networked] private float VerticalVelocity { get; set; }

    private float _pitch;
    private bool _isLocalPlayer;
    private bool _cursorLocked;
    private bool _loggedMissingInput;
    private Vector2 _fallbackMoveInput;
    private Vector2 _fallbackLookInput;
    private bool _fallbackJumpRequested;
    private bool _fallbackSprintRequested;

    public override void Spawned()
    {
        _isLocalPlayer =
            Object.HasInputAuthority ||
            (Runner.GameMode == GameMode.Shared &&
             Object.HasStateAuthority);

        Debug.Log(
            $"[Movement] Spawned player {Object.InputAuthority.PlayerId}. " +
            $"InputAuthority={Object.HasInputAuthority}, " +
            $"StateAuthority={Object.HasStateAuthority}, " +
            $"LocalControl={_isLocalPlayer}, " +
            $"ProvideInput={Runner.ProvideInput}");

        SetCameraActive(_isLocalPlayer);

        if (!_isLocalPlayer)
            return;

        enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (FusionInputProvider.Instance != null)
            FusionInputProvider.Instance.EnsureReady(Runner);

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

        ReadFallbackInput();

        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (captureTheFlagManager != null &&
            captureTheFlagManager.IsReady &&
            Keyboard.current != null &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            captureTheFlagManager.RequestDrop(
                Object.InputAuthority);
        }

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (!_cursorLocked &&
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
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

        NetworkInputData inputData;

        if (!GetInput(out inputData))
        {
            if (!_isLocalPlayer)
                return;

            inputData = ConsumeFallbackInput();

            if (!_loggedMissingInput)
            {
                _loggedMissingInput = true;

                Debug.LogWarning(
                    $"[Movement] No Fusion input for player " +
                    $"{Object.InputAuthority.PlayerId}. " +
                    "Using local Shared Mode input fallback.");
            }
        }
        else
        {
            _loggedMissingInput = false;
            ClearOneShotFallbackInput();
        }

        RotatePlayer(inputData);
        Move(inputData);
    }

    private void ReadFallbackInput()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed)
                horizontal -= 1f;

            if (keyboard.dKey.isPressed)
                horizontal += 1f;

            if (keyboard.sKey.isPressed)
                vertical -= 1f;

            if (keyboard.wKey.isPressed)
                vertical += 1f;

            _fallbackMoveInput = new Vector2(horizontal, vertical);

            if (_fallbackMoveInput.sqrMagnitude > 1f)
                _fallbackMoveInput.Normalize();

            if (keyboard.spaceKey.wasPressedThisFrame)
                _fallbackJumpRequested = true;

            _fallbackSprintRequested =
                keyboard.leftShiftKey.isPressed ||
                keyboard.rightShiftKey.isPressed;
        }

        if (_cursorLocked && Mouse.current != null)
            _fallbackLookInput += Mouse.current.delta.ReadValue();
    }

    private NetworkInputData ConsumeFallbackInput()
    {
        NetworkInputData inputData = new NetworkInputData
        {
            moveInput = _fallbackMoveInput,
            lookInput = _fallbackLookInput,
            jumpRequested = _fallbackJumpRequested,
            sprintRequested = _fallbackSprintRequested
        };

        _fallbackLookInput = Vector2.zero;
        _fallbackJumpRequested = false;

        return inputData;
    }

    private void ClearOneShotFallbackInput()
    {
        _fallbackLookInput = Vector2.zero;
        _fallbackJumpRequested = false;
    }

    private void RotatePlayer(NetworkInputData inputData)
    {
        float mouseX = inputData.lookInput.x * mouseSensitivity;
        float mouseY = inputData.lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(
            _pitch,
            -upDownPitchLimit,
            upDownPitchLimit);

        if (cameraRoot != null && _isLocalPlayer)
            cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
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

        float gameModeSpeedMultiplier = 1f;

        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (captureTheFlagManager != null &&
            captureTheFlagManager.IsReady &&
            captureTheFlagManager.IsCarrier(
                Object.InputAuthority))
        {
            gameModeSpeedMultiplier =
                captureTheFlagManager.CarrierSpeedMultiplier;
        }

        float currentSpeed =
            moveSpeed *
            gameModeSpeedMultiplier *
            (inputData.sprintRequested
                ? sprintMultiplier
                : 1f);

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = VerticalVelocity;

        if (characterController != null)
            characterController.Move(velocity * Runner.DeltaTime);
        else
            transform.position += velocity * Runner.DeltaTime;

        if (animationController != null)
            animationController.SetMovementSpeed(inputData.moveInput.magnitude);
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