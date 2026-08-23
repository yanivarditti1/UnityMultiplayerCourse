using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Manages chat functionality using RPCs
// Handles both global room chat and private direct messages

public class ChatManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxMessageHistory = 100;
    [SerializeField] private float messageTimeoutSeconds = 300f; // 5 minutes
    [SerializeField] private int maxMessageLength = 200;
    
    // Event fired when a new chat message is received
    public static event Action<ChatMessage, string> OnMessageReceived;
    
    // Event fired when a chat error occurs
    public static event Action<string> OnChatError;
    
    private static ChatManager _instance;
    public static ChatManager Instance => _instance;
    
    // Message history for UI display
    private readonly List<ChatMessage> _messageHistory = new List<ChatMessage>();
    
    // Cache for player nicknames to avoid lookups
    private readonly Dictionary<PlayerRef, string> _playerNicknames = new Dictionary<PlayerRef, string>();
    
    #region Lifecycle
    public override void Spawned()
    {
        if (_instance == null)
        {
            _instance = this;
            PlayerManager.OnAnyNicknameChanged += OnPlayerNicknameChanged;
            // Debug.Log($"[ChatManager] Spawned. LobbyManager.Instance null: {LobbyManager.Instance == null}");

            // Tell LobbyManager chat is ready
            // if (LobbyManager.Instance != null)
            //     LobbyManager.Instance.NotifyChatManagerSpawned();

            Debug.Log("[ChatManager] Spawned and ready");
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_instance == this)
        {
            PlayerManager.OnAnyNicknameChanged -= OnPlayerNicknameChanged;
            _instance = null;
        }
    }
    
    private void Update()
    {
        // Clean up old messages periodically
        if (_messageHistory.Count > 0)
        {
            float currentTime = Time.time;
            _messageHistory.RemoveAll(msg => currentTime - msg.Timestamp > messageTimeoutSeconds);
        }
    }
    
    #endregion

    private void OnPlayerNicknameChanged(PlayerRef player, string nickname)
    {
        _playerNicknames[player] = nickname;
    }

    // Sends a chat message. Supports both global messages and private messages with /msg command
    public void SendChatMessage(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return;
            
        if (messageText.Length > 200)
        {
            OnChatError?.Invoke("Message too long (max 200 characters)");
            return;
        }

        // Parse for private message command
        if (ChatCommand.ProcessCommand(messageText))
            return;
        // Send global message
        SendGlobalMessage(messageText);
    }

    // Sends a global message to all players in the room
    public void SendGlobalMessage(string messageText)
    {
        RPC_RequestSendGlobalMessage(messageText);
    }
    
    public void SendPrivateMessage(string targetNickname, string messageText)
    {
        PlayerRef targetPlayer = FindPlayerByNickname(targetNickname);
    
        if (targetPlayer == PlayerRef.None)
        {
            OnChatError?.Invoke($"Player '{targetNickname}' not found");
            return;
        }

        RPC_RequestSendPrivateMessage(targetPlayer, messageText);
    }

    private PlayerRef FindPlayerByNickname(string nickname)
    {
        if (ServerLobbyManager.Instance == null)
            return PlayerRef.None;

        //search active players list
        var playersList = ServerLobbyManager.Instance.GetPlayers();
        foreach (var entry in playersList)
        {
            if (!entry.Value.HasNickname)
                continue;
            
            string existingNickname = entry.Value.Nickname.ToString();
            if (string.Equals(existingNickname, nickname, StringComparison.OrdinalIgnoreCase))
                return entry.Key;
        } 
        
        return PlayerRef.None;
    }

    private string GetPlayerNickname(PlayerRef player)
    {
        // Check cache first
        if (_playerNicknames.TryGetValue(player, out string cachedNickname))
            return cachedNickname;

        // If not found in cache, try to get from server
        if (ServerLobbyManager.Instance != null &&
            ServerLobbyManager.Instance.TryGetPlayerNickname(player, out string lobbyNickname))
        {
            _playerNicknames[player] = lobbyNickname;
            return lobbyNickname;
        }

        return $"Player {player.PlayerId}";
    }

    #region RPCs

    // request to send global chat message
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSendGlobalMessage(string messageText, RpcInfo info = default)
    {
        PlayerRef sender = info.Source;
        
        if (!CanPlayerChat(sender))
        {
            Debug.Log($"[ChatManager] {sender} tried to send global message but is not allowed");
            return;
        }

        if (string.IsNullOrWhiteSpace(messageText))
            return;
        
        messageText = messageText.Trim();
        
        if (messageText.Length > maxMessageLength)
            messageText = messageText.Substring(0, maxMessageLength);
        
        RPC_ReceiveGlobalMessage(sender, messageText);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ReceiveGlobalMessage(PlayerRef sender, string messageText)
    {
        var message = new ChatMessage(sender, messageText, ChatMessageType.Global);
        ProcessReceivedMessage(message);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestSendPrivateMessage(PlayerRef target, string messageText, RpcInfo info = default)
    {
        PlayerRef sender = info.Source;

        if (!CanPlayerChat(sender))
            return;
        
        if (!CanPlayerChat(target))
            return;

        if (sender == target)
            return;
        
        if (string.IsNullOrWhiteSpace(messageText))
            return;
        
        messageText = messageText.Trim();
        
        if (messageText.Length > maxMessageLength)
            messageText = messageText.Substring(0, maxMessageLength);
        
        RPC_SendPrivateMessage(sender, target, messageText);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SendPrivateMessage(PlayerRef sender, PlayerRef target, string messageText)
    {
        // Only the sender and target should process this message
        if (Runner.LocalPlayer != sender && Runner.LocalPlayer != target)
            return;

        var message = new ChatMessage(sender, messageText, ChatMessageType.Private, target);
        ProcessReceivedMessage(message);
    }

    #endregion
    
    #region Helpers

    private bool CanPlayerChat(PlayerRef player)
    {
        return ServerLobbyManager.Instance != null && ServerLobbyManager.Instance.IsPlayerInLobby(player);
    }

    private void ProcessReceivedMessage(ChatMessage message)
    {
        // Add to message history
        _messageHistory.Add(message);
        
        // Maintain max history size
        if (_messageHistory.Count > maxMessageHistory)
        {
            _messageHistory.RemoveAt(0);
        }

        // Get sender nickname
        string senderNickname = GetPlayerNickname(message.Sender);
        
        // Notify UI
        OnMessageReceived?.Invoke(message, senderNickname);
        
        Debug.Log($"[ChatManager] {message.Type} message from {senderNickname}: {message.Content}");
    }

    // Gets the current message history for UI display
    public List<ChatMessage> GetMessageHistory()
    {
        return new List<ChatMessage>(_messageHistory);
    }

    // Clears the local message history
    public void ClearMessageHistory()
    {
        _messageHistory.Clear();
    }
    public static void RaiseSystemMessage(string message)
    {
        OnChatError?.Invoke(message);
    }
    
    #endregion
}