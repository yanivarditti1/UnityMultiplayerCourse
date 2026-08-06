using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerUiManager : MonoBehaviour
{
    #region References
    
    [Header("Panels")] 
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private GameObject preMatchPanel;
    
    [Header("Connection")]
    [SerializeField] private Button joinServerButton;
    [SerializeField] private TextMeshProUGUI connectStatusText;
    
    [Header("Pre-Match")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private Button confirmGameModeButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Toggle isReadyToggle;
    [SerializeField] private TextMeshProUGUI preMatchStatusText;
    
    [Header("References")]
    [SerializeField] private NetworkStartupManager networkStartupManager;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private ServerLobbyPlayerUiEntry playerEntryPrefab;
    
    private readonly Dictionary<PlayerRef, ServerLobbyPlayerUiEntry> _playerEntries = new();

    private bool _isServerOnline = false;
    private bool _subscribedToServerManagers = false;
    
    #endregion

    private bool _pendingReadyAfterNicknameChanged;
    
    #region Unity Lifecycle

    private void Start()
    {
        ShowConnectPanel();
        
        joinServerButton.onClick.AddListener(HandleJoinServerClicked);
        readyButton.onClick.AddListener(HandleReadyClicked);
        startMatchButton.onClick.AddListener(HandleStartMatchClicked);
        confirmGameModeButton.onClick.AddListener(HandleConfirmGameModeClicked);

        if (networkStartupManager != null)
        {
            networkStartupManager.LocalManagersReady += HandleLocalManagersReady;
            networkStartupManager.ServerStarted += HandleServerStarted;
            networkStartupManager.ServerStartFailed += HandleServerStartFailed;
            networkStartupManager.ClientStarted += HandleClientStarted;
            networkStartupManager.ClientStartFailed += HandleClientStartFailed;
            
            Debug.Log("[ServerUiManager] Client subscribed to NetworkStartupManager events");
        }
        else
        {
            Debug.LogError("[ServerUiManager] NetworkStartupManager is null");
        }
        
        SetConnectedStatus("[ServerUiManager] Waiting for server managers...");
        
        SetupGameModeDropdown();
    }

    private void OnDestroy()
    {
        if (joinServerButton != null)
            joinServerButton.onClick.RemoveListener(HandleJoinServerClicked);
        
        if (startMatchButton != null)
            startMatchButton.onClick.RemoveListener(HandleStartMatchClicked);
        
        if (readyButton != null)
            readyButton.onClick.RemoveListener(HandleReadyClicked);
        
        if (confirmGameModeButton != null)
            confirmGameModeButton.onClick.RemoveListener(HandleConfirmGameModeClicked);
        
        if (networkStartupManager != null)
        {
            networkStartupManager.ServerStarted -= HandleServerStarted;
            networkStartupManager.ServerStartFailed -= HandleServerStartFailed;
            networkStartupManager.ClientStarted -= HandleClientStarted;
            networkStartupManager.ClientStartFailed -= HandleClientStartFailed;
        }
        
        UnsubscribeToServerManagers();
    }
    
    #endregion
    
    #region Handlers

    private void HandleLocalManagersReady()
    {
        joinServerButton.interactable = true;
        SetConnectedStatus("[ServerUiManager] Ready to connect");
    }

    private void HandleServerStarted()
    {
        _isServerOnline = ManagersReady();
        joinServerButton.interactable = _isServerOnline;
        SetConnectedStatus("[ServerUiManager] Server online");
    }
    
    private void HandleServerStartFailed(string reason)
    {
        _isServerOnline = ManagersReady();
        joinServerButton.interactable = _isServerOnline;
        SetConnectedStatus($"[ServerUiManager] Failed to start server: {reason}");
    }
    
    private void HandleJoinServerClicked()
    {
        if (networkStartupManager == null)
        {
            SetConnectedStatus("[ServerUiManager] NetworkStartupManager is null");
            return;
        }
        
        joinServerButton.interactable = false;
        SetConnectedStatus("[ServerUiManager] Connecting to server...");
        
        networkStartupManager.StartClient();
    }
    
    private void HandleConfirmGameModeClicked()
    {
        if (!ManagersReady())
            return;

        if (!ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader())
        {
            SetPreMatchStatus("[ServerUiManager] Only lobby leader can confirm game mode");
            return;
        }
        
        GameModeType requestedGameMode = GetSelectedGameMode();
        NetworkMatchManager.Instance.RequestGameMode(requestedGameMode);
        SetPreMatchStatus($"[ServerUiManager] Set game mode: {requestedGameMode}");

        confirmGameModeButton.interactable = false;
        gameModeDropdown.interactable = false;
    }

    private void HandleReadyClicked()
    {
        if (!ManagersReady())
            return;
        
        //validate nickname
        var requestedNickname = nicknameInputField.text.Trim();
        if (!ValidateNickname(requestedNickname))
            return;

        _pendingReadyAfterNicknameChanged = true;
        
        Debug.Log($"[ServerUiManager] Requesting validated nickname: {requestedNickname}");
        
        SetPreMatchStatus($"[ServerUiManager] Pending nickname validation...");
        
        ServerLobbyManager.Instance.RequestNickname(requestedNickname);
    }
    
    private void HandleStartMatchClicked()
    {
        if (!ManagersReady())
            return;

        if (!ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader())
        {
            SetPreMatchStatus("[ServerUiManager] Only lobby leader can start match");
            return;
        }

        if (!ServerLobbyManager.Instance.CanStartMatch())
        {
            SetPreMatchStatus("[ServerUiManager] Cannot start match while players are not ready");
            return;       
        }
        
        startMatchButton.interactable = false;
        SetPreMatchStatus("[ServerUiManager] Starting match...");
        
        NetworkMatchManager.Instance.RequestStartMatch();
    }

    private void HandleClientStarted()
    {
        ShowPreMatchPanel();
        SetConnectedStatus("[ServerUiManager] Connected to server");
        SubscribeToServerManagers();
        RefreshPreMatchControls();
    }

    private void HandleClientStartFailed(string reason)
    {
        joinServerButton.interactable = true;
        SetConnectedStatus($"[ServerUiManager] Failed to connect to server: {reason}");
    }

    private void HandleLobbyStateChanged()
    {
        RefreshPreMatchControls();
    }

    private void HandleLobbyLeaderChanged(PlayerRef newLeader)
    {
        RefreshPreMatchControls();
        RefreshPlayerList();
    }

    private void HandleNicknameAccepted(PlayerRef player, string nickname)
    {
        SetPreMatchStatus($"Nickname set: {nickname}\nReduesting ready...");
        
        nicknameInputField.interactable = false;

        if (!_pendingReadyAfterNicknameChanged)
        {
            Debug.Log("[ServerUiManager] Nickname accepted without ready request");
            return;
        }
        
        StartCoroutine(RequestReadyNextFrame());
    }

    private void HandleNicknameRejected(PlayerRef player, string reason)
    {
        _pendingReadyAfterNicknameChanged = false;
        SetPreMatchStatus(reason);
        readyButton.interactable = true;
    }

    private void HandleReadyStateChanged(PlayerRef player, bool newReadyState)
    {
        if (ServerLobbyManager.Instance != null && ServerLobbyManager.Instance.Runner.LocalPlayer == player)
        {
            isReadyToggle.isOn = newReadyState;
            SetPreMatchStatus(newReadyState ? "Ready" : "Not Ready");
            _pendingReadyAfterNicknameChanged = false;
            readyButton.interactable = false;
        }
        
        if (_playerEntries.TryGetValue(player, out var entry))
            entry.SetReady(newReadyState);
        
        RefreshPreMatchControls();
        RefreshPlayerList();
    }

    private void HandlePlayerJoinedLobby(PlayerRef player)
    {
        RefreshPlayerList();
    }

    private void HandlePlayerLeftLobby(PlayerRef player)
    {
        if (_playerEntries.TryGetValue(player, out var entry))
        {
            Destroy(entry.gameObject);
            _playerEntries.Remove(player);
        }

        RefreshPlayerList();
    }

    private void HandleNicknameChanged(PlayerRef player, string nickname)
    {
        RefreshPlayerList();
    }
    
    #endregion
    
    #region Helpers

    private IEnumerator RequestReadyNextFrame()
    {
        yield return null;
        
        if (!ManagersReady())
            yield break;
        
        Debug.Log("[ServerUiManager] Waited 1 frame, requesting ready after nickname change");
        
        _pendingReadyAfterNicknameChanged = false;
        ServerLobbyManager.Instance.RequestReady(true);
    }

    private bool ManagersReady()
    {
        if (ServerLobbyManager.Instance == null)
        {
            SetConnectedStatus("[ServerUiManager] ServerLobbyManager not ready yet");
            SetPreMatchStatus("[ServerUiManager] ServerLobbyManager not ready yet");
            return false;
        }

        if (NetworkMatchManager.Instance == null)
        {
            SetConnectedStatus("[ServerUiManager] NetworkMatchManager not ready yet");
            SetPreMatchStatus("[ServerUiManager] NetworkMatchManager not ready yet");
            return false;
        }

        return true;
    }

    private void RefreshPlayerList()
    {
        if (!ManagersReady())
            return;

        foreach (var entry in _playerEntries.Values)
        {
            Destroy(entry.gameObject);
        }

        _playerEntries.Clear();

        foreach (var playerEntry in ServerLobbyManager.Instance.GetPlayers())
        {
            PlayerRef playerRef = playerEntry.Key;
            LobbyPlayerState playerState = playerEntry.Value;
            
            var uiEntry = Instantiate(playerEntryPrefab, playerListContainer);
            
            string nickname = playerState.HasNickname?
                playerState.Nickname.ToString() : $"Player {playerRef.PlayerId}";
            bool isLeader = ServerLobbyManager.Instance.IsLobbyLeader(playerRef);
            bool isLocal = ServerLobbyManager.Instance.IsLocalPlayer(playerRef);
            
            uiEntry.Setup(playerRef, nickname, playerState.IsReady, isLeader, isLocal);
            
            _playerEntries[playerRef] = uiEntry;
        }
    }
    
    private void ShowConnectPanel()
    {
        connectPanel.SetActive(true);
        preMatchPanel.SetActive(false);
        
        joinServerButton.interactable = _isServerOnline;
    }
    
    private void ShowPreMatchPanel()
    {
        connectPanel.SetActive(false);
        preMatchPanel.SetActive(true);
    }

    private void SetupGameModeDropdown()
    {
        if (gameModeDropdown == null)
        {
            Debug.LogError($"[ServerUiManager] GameModeDropdown is null");
            return;
        }
        
        gameModeDropdown.ClearOptions();
        gameModeDropdown.AddOptions(new List<string>
        {
            "Free For All",
            "Conquest",
            "Capture The Flag"
        });
        
        gameModeDropdown.SetValueWithoutNotify(0);
    }

    private void SetConnectedStatus(string newStatus)
    {
        if (connectStatusText == null) return;
        connectStatusText.text = newStatus;
    }
    
    private void SetPreMatchStatus(string newStatus)
    {
        if (preMatchStatusText == null) return;
        preMatchStatusText.text = newStatus;
    }

    private bool ValidateNickname(string nickname)
    {
        if (String.IsNullOrEmpty(nickname))
        {
            SetPreMatchStatus("Nickname cannot be empty");
            return false;
        }
        
        if (nickname.Length > 16)
        {
            SetPreMatchStatus("Nickname cannot be longer than 16 characters");
            return false;
        }
        
        return true;   
    }
    
    private GameModeType GetSelectedGameMode()
    {
        int value = gameModeDropdown != null ? gameModeDropdown.value : 0;

        if (!System.Enum.IsDefined(typeof(GameModeType), value))
            value = 0;
        
        return (GameModeType) value;
    }

    private void SubscribeToServerManagers()
    {
        if (_subscribedToServerManagers)
            return;
        
        if (ServerLobbyManager.Instance == null)
        {
            Debug.LogError("[ServerUiManager] ServerLobbyManager not ready yet");
            return;
        }

        ServerLobbyManager.Instance.NicknameAccepted += HandleNicknameAccepted;
        ServerLobbyManager.Instance.NicknameRejected += HandleNicknameRejected;
        ServerLobbyManager.Instance.ReadyStateChanged += HandleReadyStateChanged;
        ServerLobbyManager.Instance.LobbyLeaderChanged += HandleLobbyLeaderChanged;
        ServerLobbyManager.Instance.LobbyStateChanged += HandleLobbyStateChanged;
        ServerLobbyManager.Instance.PlayerJoinedLobby += HandlePlayerJoinedLobby;
        ServerLobbyManager.Instance.PlayerLeftLobby += HandlePlayerLeftLobby;
        ServerLobbyManager.Instance.NicknameChanged += HandleNicknameChanged;
        
        Debug.Log("[ServerUiManager] Client subscribed to ServerLobbyManager events");
        
        _subscribedToServerManagers = true;
        RefreshPlayerList();
    }

    private void UnsubscribeToServerManagers()
    {
        if (ServerLobbyManager.Instance == null)
            return;

        ServerLobbyManager.Instance.NicknameAccepted -= HandleNicknameAccepted;
        ServerLobbyManager.Instance.NicknameRejected -= HandleNicknameRejected;
        ServerLobbyManager.Instance.ReadyStateChanged -= HandleReadyStateChanged;
        ServerLobbyManager.Instance.LobbyLeaderChanged -= HandleLobbyLeaderChanged;
        ServerLobbyManager.Instance.LobbyStateChanged -= HandleLobbyStateChanged;
        ServerLobbyManager.Instance.PlayerJoinedLobby -= HandlePlayerJoinedLobby;
        ServerLobbyManager.Instance.PlayerLeftLobby -= HandlePlayerLeftLobby;
        ServerLobbyManager.Instance.NicknameChanged -= HandleNicknameChanged;
        
        _subscribedToServerManagers = false;   
    }

    private void RefreshPreMatchControls()
    {
        if (preMatchPanel == null || !preMatchPanel.activeSelf)
            return;

        if (ServerLobbyManager.Instance == null || NetworkMatchManager.Instance == null)
            return;

        bool isLeader = ServerLobbyManager.Instance.IsLocalPlayerLobbyLeader();
        bool matchWaiting = NetworkMatchManager.Instance.MatchState == ServerMatchState.WaitingForPlayers;
        bool canChangeMode = matchWaiting && isLeader && !ServerLobbyManager.Instance.CanStartMatch();
        
        if (gameModeDropdown != null)
            gameModeDropdown.interactable = canChangeMode;
        
        if (confirmGameModeButton != null)
            confirmGameModeButton.interactable = canChangeMode;
        
        if (startMatchButton != null)
            startMatchButton.interactable = matchWaiting && isLeader;
        
        if (readyButton != null && isReadyToggle != null)
            readyButton.interactable = matchWaiting && !isReadyToggle.isOn;
    }
    
    #endregion
}
