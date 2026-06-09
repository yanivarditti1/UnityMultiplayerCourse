using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;



public sealed class FirstPersonNetworkMovement : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController characterController;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")] 
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownPitchLimit = 85f;

    [Networked] private float _verticalVelocity { get; set; }

    private float _pitch;
    private bool _isLocalPlayer;
    private bool _jumpRequested;
    private Vector2 _accumulatedLookInput;

    public override void Spawned()
    {
        _isLocalPlayer = Object.HasInputAuthority;

        if (!_isLocalPlayer)
        {
            SetCameraActive(false);
            return;
        }

        moveAction.action.actionMap.Enable();
        lookAction.action.actionMap.Enable();
        jumpAction.action.actionMap.Enable();
        sprintAction.action.actionMap.Enable();

        SetCameraActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (Runner != null)
        {
            Runner.AddCallbacks(this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!_isLocalPlayer) return;

        moveAction.action.actionMap.Disable();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (runner != null)
        {
            runner.RemoveCallbacks(this);
        }
    }

    private void Update()
    {
        if (!_isLocalPlayer) return;

        _accumulatedLookInput += lookAction.action.ReadValue<Vector2>();

        if (jumpAction.action.WasPressedThisFrame())
        {
            _jumpRequested = true;
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var inputData = new NetworkInputData();

        inputData.moveInput = moveAction.action.ReadValue<Vector2>();
        inputData.lookInput = _accumulatedLookInput;
        _accumulatedLookInput = Vector2.zero;
        
        inputData.sprintRequested = sprintAction.action.IsPressed();
        inputData.jumpRequested = _jumpRequested;
        _jumpRequested = false; 

        input.Set(inputData);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData inputData))
        {
            RotatePlayer(inputData);
            Move(inputData);
        }
    }

    private void RotatePlayer(NetworkInputData inputData)
    {
        float mouseX = inputData.lookInput.x * mouseSensitivity;
        float mouseY = inputData.lookInput.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -upDownPitchLimit, upDownPitchLimit);

        cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void Move(NetworkInputData inputData)
    {
        Vector3 moveDirection = (transform.right * inputData.moveInput.x) + (transform.forward * inputData.moveInput.y);
        
        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        bool isGrounded = characterController != null ? characterController.isGrounded : true;

        if (isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = -2f;
        }

        if (isGrounded && inputData.jumpRequested)
        {
            _verticalVelocity = jumpForce;
        }

        _verticalVelocity += gravity * Runner.DeltaTime;

        float currentSpeed = moveSpeed * (inputData.sprintRequested ? sprintMultiplier : 1f);
        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = _verticalVelocity;

        if (characterController != null)
        {
            characterController.Move(velocity * Runner.DeltaTime);
        }
        else
        {
            transform.position += velocity * Runner.DeltaTime;
        }
    }

    private void SetCameraActive(bool active)
    {
        if (playerCamera != null)
            playerCamera.enabled = active;
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}