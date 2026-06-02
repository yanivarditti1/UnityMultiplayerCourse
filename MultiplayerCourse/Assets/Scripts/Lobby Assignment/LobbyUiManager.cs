using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
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
    [Header("Lobby Panel")] [SerializeField]
    private Transform _sessionListContainer;
    [SerializeField] private GameObject _sessionEntryPrefab;
    [SerializeField] private TMP_InputField _roomNameInputField;
    [SerializeField] private TMP_InputField _maxPlayersInputField;
    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    [SerializeField] private Button _leaveLobbyButton;
    [SerializeField] private TextMeshProUGUI _lobbyStatusText;
    
    //room panel
    [Header("Room Panel")] 
    [SerializeField] private Transform _playerListContainer;
    [SerializeField] private GameObject _playerEntryPrefab;
    [SerializeField] private Button _leaveRoomButton;
    [SerializeField] private TextMeshProUGUI _roomNameText;
    [SerializeField] private TextMeshProUGUI _roomStatusText;
    
    //state
    private SessionInfo _selectedSession;
    private readonly List<GameObject> _sessionEntries = new();
    private readonly List<GameObject> _playerEntries = new();

    private void Start()
    {
        SubscribeToManager();
        ShowConnectPanel();
        SetupButtonListeners();
    }

    private void OnDestroy()
    {
        UnsubscribeFromManager();
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

        if (!int.TryParse(_maxPlayersInputField.text, out int max) || max < 2)
        {
            SetStatus(_lobbyStatusText, "Invalid max players, enter a number equal to or greater than 2");
            return;
        }
        
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

        SetLobbyButtons(false);
        SetStatus(_lobbyStatusText, $"Joining room: {_selectedSession.Name}...");
        LobbyManager.Instance.JoinRoom(_selectedSession.Name);
    }

    private void OnLeaveRoomButtonClicked()
    {
        _leaveRoomButton.interactable = false;
        LobbyManager.Instance.LeaveRoom();
    }
    
    #endregion
    
    #region EventHandlers

    private void HandleLobbyJoined()
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
        foreach (var entry in _playerEntries) Destroy(entry);
        _playerEntries.Clear();
        
        foreach (var player in players)
        {
            var entry = Instantiate(_playerEntryPrefab, _playerListContainer);
            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool isLocal = player == LobbyManager.Instance.Runner.LocalPlayer;
                label.text = isLocal ? $"Player {player.PlayerId} (You)" : $"Player {player.PlayerId}";
            }
            _playerEntries.Add(entry);
        }
        
        SetStatus(_roomStatusText, $"Players: {players.Count}");
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
