using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1000)]
public sealed class FusionInputProvider : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionInputProvider Instance { get; private set; }

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;

    private NetworkRunner _runner;
    private Vector2 _accumulatedLookInput;
    private bool _jumpRequested;
    private bool _callbacksRegistered;

    private void Awake()
    {
        Instance = this;
        _runner = GetComponent<NetworkRunner>();
    }

    private void OnEnable()
    {
        EnsureReady(_runner);
    }

    private void Start()
    {
        EnsureReady(_runner);
    }

    private void OnDisable()
    {
        DisableInput();

        if (_runner != null && _callbacksRegistered)
        {
            _runner.RemoveCallbacks(this);
            _callbacksRegistered = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (lookAction == null || jumpAction == null)
            return;

        _accumulatedLookInput += lookAction.action.ReadValue<Vector2>();

        if (jumpAction.action.WasPressedThisFrame())
            _jumpRequested = true;
    }

    public void EnsureReady(NetworkRunner runner)
    {
        if (runner != null && _runner != runner)
        {
            if (_runner != null && _callbacksRegistered)
                _runner.RemoveCallbacks(this);

            _runner = runner;
            _callbacksRegistered = false;
        }

        if (_runner == null)
            _runner = GetComponent<NetworkRunner>();

        if (_runner == null)
            return;

        _runner.ProvideInput = true;
        EnableInput();

        if (_callbacksRegistered)
            return;

        _runner.AddCallbacks(this);
        _callbacksRegistered = true;
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (moveAction == null ||
            lookAction == null ||
            jumpAction == null ||
            sprintAction == null)
            return;

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
        if (moveAction != null)
            moveAction.action.actionMap.Enable();
    }

    private void DisableInput()
    {
        if (moveAction != null)
            moveAction.action.actionMap.Disable();
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        EnsureReady(runner);
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
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}
