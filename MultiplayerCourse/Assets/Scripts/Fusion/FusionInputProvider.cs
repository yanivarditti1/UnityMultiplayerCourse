using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class FusionInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;

    private NetworkRunner _runner;
    private Vector2 _accumulatedLookInput;
    private bool _jumpRequested;

    private void Awake()
    {
        _runner = GetComponent<NetworkRunner>();
    }

    private void OnEnable()
    {
        EnableInput();

        if (_runner != null)
            _runner.AddCallbacks(this);
    }

    private void OnDisable()
    {
        DisableInput();

        if (_runner != null)
            _runner.RemoveCallbacks(this);
    }

    private void Update()
    {
        _accumulatedLookInput += lookAction.action.ReadValue<Vector2>();

        if (jumpAction.action.WasPressedThisFrame())
            _jumpRequested = true;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData inputData = new NetworkInputData
        {
            moveInput = moveAction.action.ReadValue<Vector2>(),
            lookInput = _accumulatedLookInput,
            jumpRequested = _jumpRequested,
            sprintRequested = sprintAction.action.IsPressed()
        };

        _accumulatedLookInput = Vector2.zero;
        _jumpRequested = false;

        input.Set(inputData);
    }

    private void EnableInput()
    {
        moveAction.action.actionMap.Enable();
    }

    private void DisableInput()
    {
        moveAction.action.actionMap.Disable();
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