using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUiManager : MonoBehaviour
{
    //panels
    [Header("Panels")]
    [SerializeField] private GameObject _connectPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _roomPanel;
    
    //connect panel
    [Header("Connect Panel")]
    [SerializeField] private TMP_InputField _lobbyIdInputField;
    [SerializeField] private Button _joinLobbyButton;
    [SerializeField] private TextMeshProUGUI _connectStatusText;
    
    //lobby panel
    [Header("Lobby Panel")] 
    [SerializeField] private Transform _sessionListContainer;
    [SerializeField] private GameObject _sessionEntryPrefab;
    [SerializeField] private TMP_InputField _nicknameInputField;
    [SerializeField] private TMP_InputField _roomNameInputField;
    [SerializeField] private TMP_InputField _maxPlayersInputField;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    [SerializeField] private Button _leaveLobbyButton;
    [SerializeField] private TextMeshProUGUI _lobbyStatusText;
    
    //room panel
    [Header("Room Panel")] 
    [SerializeField] private Transform _playerListContainer;
    [SerializeField] private TextMeshProUGUI _playerListLabel;
    [SerializeField] private GameObject _playerEntryPrefab;
    [SerializeField] private Button _startMatchButton;
    [SerializeField] private Button _leaveRoomButton;
    [SerializeField] private TextMeshProUGUI _roomNameText;
    [SerializeField] private TextMeshProUGUI _roomStatusText;
    
    //state
    private SessionInfo _selectedSession;
    private readonly List<GameObject> _sessionEntries = new();
    private readonly Dictionary<PlayerRef, GameObject> _playerEntries = new();

    private void Start()
    {
        SubscribeToManager();
        ShowConnectPanel();
        SetupButtonListeners();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnsubscribeFromManager();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    #region Subscriptions

    private void SubscribeToManager()
    {
        var manager = LobbyManager.Instance;
        if (manager == null) return;

        manager.OnLobbyJoined           += HandleLobbyJoined;
        manager.OnLobbyJoinFailed       += HandleLobbyJoinFailed;
        manager.OnLobbyLeave            += HandleLobbyLeave;
        manager.OnSessionListRefreshed  += HandleSessionListRefreshed;
        manager.OnRoomCreated           += HandleRoomCreated;
        manager.OnRoomCreateFailed      += HandleRoomCreateFailed;
        manager.OnRoomJoined            += HandleRoomJoined;
        manager.OnRoomJoinFailed        += HandleRoomJoinFailed;
        manager.OnRoomLeft              += HandleRoomLeft;
        manager.OnRoomListUpdate        += HandleRoomListUpdate;
        manager.OnMatchStarted          += HandleMatchStarted;
        manager.OnPlayerNicknameChanged += HandlePlayerNicknameChanged;
    }

    private void UnsubscribeFromManager()
    {
        var manager = LobbyManager.Instance;
        if (manager == null) return;

        manager.OnLobbyJoined           -= HandleLobbyJoined;
        manager.OnLobbyJoinFailed       -= HandleLobbyJoinFailed;
        manager.OnLobbyLeave            -= HandleLobbyLeave;
        manager.OnSessionListRefreshed  -= HandleSessionListRefreshed;
        manager.OnRoomCreated           -= HandleRoomCreated;
        manager.OnRoomCreateFailed      -= HandleRoomCreateFailed;
        manager.OnRoomJoined            -= HandleRoomJoined;
        manager.OnRoomJoinFailed        -= HandleRoomJoinFailed;
        manager.OnRoomLeft              -= HandleRoomLeft;
        manager.OnRoomListUpdate        -= HandleRoomListUpdate;
        manager.OnMatchStarted          -= HandleMatchStarted;
        manager.OnPlayerNicknameChanged -= HandlePlayerNicknameChanged;
    }
    #endregion
    
    #region ButtonWiring

    private void SetupButtonListeners()
    {
        _joinLobbyButton.onClick.AddListener(OnJoinLobbyButtonClicked);
        _leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
        _createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        _joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        _leaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
        _startMatchButton.onClick.AddListener(OnStartMatchButtonClicked);
    }

    private void OnJoinLobbyButtonClicked()
    {
        SetConnectButtons(false);
        SetStatus(_connectStatusText, "Joining lobby...");
        LobbyManager.Instance.JoinLobby(_lobbyIdInputField.text.Trim());
    }

    private void OnLeaveLobbyClicked()
    {
        SetLobbyButtons(false);
        _leaveLobbyButton.interactable = false;
        LobbyManager.Instance.LeaveLobby();
    }

    private void OnCreateRoomButtonClicked()
    {
        string roomName = _roomNameInputField.text.Trim();
        if (string.IsNullOrEmpty(roomName))
        {
            SetStatus(_lobbyStatusText, "Room name cannot be empty");
            return;
        }
        
        string playerNickname = _nicknameInputField.text.Trim();
        if (string.IsNullOrEmpty(playerNickname))
        {
            SetStatus(_lobbyStatusText, "Enter a nickname first!");
            return;
        }

        if (!int.TryParse(_maxPlayersInputField.text, out int max) || max < 2)
        {
            SetStatus(_lobbyStatusText, "Invalid max players, enter a number equal to or greater than 2");
            return;
        }

        LobbyManager.Instance.SetNickname(playerNickname);
        SetLobbyButtons(false);
        SetStatus(_lobbyStatusText, $"Creating room: {roomName} - max players: {max}...");
        LobbyManager.Instance.CreateRoom(roomName, max);
    }

    private void OnJoinRoomButtonClicked()
    {
        if (_selectedSession == null)
        {
            SetStatus(_lobbyStatusText, "Select a session first");
            return;
        }
        
        string playerNickname = _nicknameInputField.text.Trim();
        if (string.IsNullOrEmpty(playerNickname))
        {
            SetStatus(_lobbyStatusText, "Enter a nickname first!");
            return;
        }
        
        string nickName = _nicknameInputField.text.Trim();
        LobbyManager.Instance.SetNickname(nickName);
        SetLobbyButtons(false);
        SetStatus(_lobbyStatusText, $"Joining room: {_selectedSession.Name}...");
        LobbyManager.Instance.JoinRoom(_selectedSession.Name);
    }

    private void OnLeaveRoomButtonClicked()
    {
        _leaveRoomButton.interactable = false;
        LobbyManager.Instance.LeaveRoom();
    }
    
    private void OnStartMatchButtonClicked()
    {
        LobbyManager.Instance.StartMatch();
    }
    
    #endregion
    
    #region EventHandlers

    private void HandleLobbyJoined(List<SessionInfo> sessions)
    {
        SetStatus(_connectStatusText, "");
        ShowLobbyPanel();
        SetLobbyButtons(true);
        _leaveLobbyButton.interactable = true;
        SetStatus(_lobbyStatusText, "In lobby. Waiting for sessions...");
    }

    private void HandleLobbyJoinFailed(string reason)
    {
        SetConnectButtons(true);
        SetStatus(_connectStatusText, $"Failed to join lobby: {reason}");
    }


    private void HandleLobbyLeave()
    {
        if (!LobbyManager.Instance.IsInLobby && !LobbyManager.Instance.IsInRoom)
        {
            ShowConnectPanel();
            SetConnectButtons(true);
        }
    }
    
    private void HandleSessionListRefreshed(List<SessionInfo> sessions)
    {
        foreach (var entry in _sessionEntries) Destroy(entry);
        _sessionEntries.Clear();
        _selectedSession = null;
        SetJoinRoomButtonActive(false);
        
        foreach (var session in sessions)
        {
            var entry = Instantiate(_sessionEntryPrefab, _sessionListContainer);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"{session.Name} [{session.PlayerCount}/{session.MaxPlayers}]";
            }
            
            //click a session to select it
            var button = entry.GetComponent<Button>() ?? entry.GetComponentInChildren<Button>();
            if (button != null)
            {
                var captured = session;
                button.onClick.AddListener(() => SelectSession(captured));
                
                if (session.PlayerCount >= session.MaxPlayers)
                {
                    button.interactable = false;
                }
            }
            
            _sessionEntries.Add(entry);
        }
    }
    
    private void HandleRoomCreated(SessionInfo info)
    {
        ShowRoomPanel();
        _roomNameText.text = info.Name;
        SetStatus(_roomStatusText, $"Hosting room: {info.Name} - waiting for players...");
        _leaveRoomButton.interactable = true;
    }
    
    private void HandleRoomCreateFailed(string reason)
    {
        SetLobbyButtons(true);
        SetStatus(_lobbyStatusText, $"Failed to create room: {reason}");
    }
    
    private void HandleRoomJoined(string roomName)
    {
        ShowRoomPanel();
        _roomNameText.text = roomName;
        SetStatus(_roomStatusText, "Joined room");
        _leaveRoomButton.interactable = true;
        
        bool isMaster = LobbyManager.Instance.Runner.LocalPlayer.IsMasterClient;
        if (!isMaster)
        {
            SetStatus(_roomStatusText, "Waiting for host to start match...");
            _startMatchButton.interactable = false;
        }
    }

    private void HandleRoomJoinFailed(string reason)
    {
        SetLobbyButtons(true);
        SetStatus(_lobbyStatusText, $"Failed to join room: {reason}");
    }

    private void HandleRoomLeft()
    {
        //ShowLobbyPanel();
        SetLobbyButtons(true);
        SetStatus(_roomStatusText, "Left room");
        _leaveRoomButton.interactable = false;
    }

    private void HandleRoomListUpdate(List<PlayerRef> players)
    {
        foreach (var entry in _playerEntries.Values) Destroy(entry);
        _playerEntries.Clear();
        
        foreach (var player in players)
        {
            var entry = Instantiate(_playerEntryPrefab, _playerListContainer);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool isLocal = player == LobbyManager.Instance.Runner.LocalPlayer;
                string nickname = PlayerManager.Registry.TryGetValue(player, out var pm)?
                    pm.Nickname.ToString()
                    : isLocal
                        ? PlayerDataPersistanceManager.Instance.Nickname
                        : $"Player {player.PlayerId}";
                label.text = isLocal ? $"{nickname} (You)" : nickname;
            }
            _playerEntries[player] = entry;
            //_playerEntries.Add(entry);
        }
        
        SetStatus(_playerListLabel, $"Active Players: {players.Count} / {LobbyManager.Instance.Runner.SessionInfo.MaxPlayers}");
    }
    
    private void HandleMatchStarted()
    {
        _startMatchButton.interactable = false;
        SetStatus(_roomStatusText, "Starting Match...");
    }

    private void HandlePlayerNicknameChanged(PlayerRef player, string nickname)
    {
        /*var playersList = LobbyManager.Instance.Runner.ActivePlayers;
        var players = new List<PlayerRef>(playersList);
        var index = players.IndexOf(player);
        if (index < 0 || index >= _playerEntries.Count) return;
        
        var label = _playerEntries[index].GetComponentInChildren<TextMeshProUGUI>();*/
        
        if (!_playerEntries.TryGetValue(player, out var entry)) return;
        
        var label = entry.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;
        
        bool isLocal = player == LobbyManager.Instance.Runner.LocalPlayer;
        label.text = isLocal ? $"{nickname} (You)" : $"{nickname}";
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HidePanelsOnLoadScene();
    }
    
    #endregion
    
    #region Helpers

    private void ShowConnectPanel()
    {
        _connectPanel.SetActive(true);
        _lobbyPanel.SetActive(false);
        _roomPanel.SetActive(false);
    }
    
    private void ShowLobbyPanel()
    {
        _connectPanel.SetActive(false);
        _lobbyPanel.SetActive(true);
        _roomPanel.SetActive(false);
    }

    private void ShowRoomPanel()
    {
        _connectPanel.SetActive(false);
        _lobbyPanel.SetActive(false);
        _roomPanel.SetActive(true);   
    }

    private void HidePanelsOnLoadScene()
    {
        if (_connectPanel.activeSelf) _connectPanel.SetActive(false);
        if (_lobbyPanel.activeSelf) _lobbyPanel.SetActive(false);
        if (_roomPanel.activeSelf) _roomPanel.SetActive(false);
    }

    private void SelectSession(SessionInfo session)
    {
        _selectedSession = session;
        SetJoinRoomButtonActive(true);
        SetStatus(_lobbyStatusText, $"Selected: {session.Name}");
    }

    private static void SetStatus(TextMeshProUGUI text, string status)
    {
        if (text != null) text.text = status;
    }
    
    private void SetConnectButtons(bool newEnabled) => _joinLobbyButton.interactable = newEnabled;
    private void SetJoinRoomButtonActive(bool newActive) => _joinRoomButton.interactable = newActive;

    private void SetLobbyButtons(bool newEnabled)
    {
        _createRoomButton.interactable = newEnabled;
        _joinLobbyButton.interactable = newEnabled;
        if (!newEnabled) SetJoinRoomButtonActive(false);
    }
    
    #endregion
}
