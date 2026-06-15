using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

/// <summary>
/// Manages chat functionality using Photon Fusion RPCs
/// Handles both global room chat and private direct messages
/// </summary>
public class ChatManager : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int maxMessageHistory = 100;
    [SerializeField] private float messageTimeoutSeconds = 300f; // 5 minutes
    
    /// <summary>
    /// Event fired when a new chat message is received
    /// Parameters: message, senderNickname
    /// </summary>
    public static event Action<ChatMessage, string> OnMessageReceived;
    
    /// <summary>
    /// Event fired when a chat error occurs
    /// Parameters: errorMessage
    /// </summary>
    public static event Action<string> OnChatError;
    
    private static ChatManager _instance;
    public static ChatManager Instance => _instance;
    
    // Message history for UI display
    private readonly List<ChatMessage> _messageHistory = new List<ChatMessage>();
    
    // Cache for player nicknames to avoid lookups
    private readonly Dictionary<PlayerRef, string> _playerNicknames = new Dictionary<PlayerRef, string>();
    public override void Spawned()
    {
        if (_instance == null)
        {
            _instance = this;
            PlayerManager.OnAnyNicknameChanged += OnPlayerNicknameChanged;

            // Tell LobbyManager chat is ready
            if (LobbyManager.Instance != null)
                LobbyManager.Instance.NotifyChatManagerSpawned();

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

    private void OnPlayerNicknameChanged(PlayerRef player, string nickname)
    {
        _playerNicknames[player] = nickname;
    }

    /// <summary>
    /// Sends a chat message. Supports both global messages and private messages with /msg command
    /// </summary>
    /// <param name="messageText">The message text to send</param>
    public void SendMessage(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText))
            return;
            
        if (messageText.Length > 200)
        {
            OnChatError?.Invoke("Message too long (max 200 characters)");
            return;
        }

        // Parse for private message command
        if (messageText.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
        {
            ParseAndSendPrivateMessage(messageText);
        }
        else
        {
            // Send global message
            SendGlobalMessage(messageText);
        }
    }

    /// <summary>
    /// Sends a global message to all players in the room
    /// </summary>
    /// <param name="messageText">Message content</param>
    public void SendGlobalMessage(string messageText)
    {
        if (!Object.HasInputAuthority)
        {
            OnChatError?.Invoke("Cannot send messages without input authority");
            return;
        }

        RPC_SendGlobalMessage(Runner.LocalPlayer, messageText);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    /// <summary>
    /// Sends a private message to a specific player
    /// </summary>
    /// <param name="targetNickname">Target player's nickname</param>
    /// <param name="messageText">Message content</param>
    public void SendPrivateMessage(string targetNickname, string messageText)
    {
        if (!Object.HasInputAuthority)
        {
            OnChatError?.Invoke("Cannot send messages without input authority");
            return;
        }

        // Find a target player by nickname
        PlayerRef targetPlayer = FindPlayerByNickname(targetNickname);
        
        if (targetPlayer == PlayerRef.None)
        {
            OnChatError?.Invoke($"Player '{targetNickname}' not found");
            return;
        }

        RPC_SendPrivateMessage(Runner.LocalPlayer, targetPlayer, messageText);
    }

    private void ParseAndSendPrivateMessage(string fullMessage)
    {
        // Format: /msg PlayerName Message content here
        string[] parts = fullMessage.Split(' ', 3);
        
        if (parts.Length < 3)
        {
            OnChatError?.Invoke("Private message format: /msg PlayerName Your message here");
            return;
        }

        string targetNickname = parts[1];
        string messageContent = parts[2];
        
        SendPrivateMessage(targetNickname, messageContent);
    }

    private PlayerRef FindPlayerByNickname(string nickname)
    {
        // First check our cached nicknames
        foreach (var kvp in _playerNicknames)
        {
            if (string.Equals(kvp.Value, nickname, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Key;
            }
        }

        // If not found in cache, try to get from PlayerManager instances
        var allPlayerManagers = FindObjectsOfType<PlayerManager>();
        foreach (var playerManager in allPlayerManagers)
        {
            if (string.Equals(playerManager.Nickname.Value, nickname, StringComparison.OrdinalIgnoreCase))
            {
                _playerNicknames[playerManager.Object.InputAuthority] = nickname;
                return playerManager.Object.InputAuthority;
            }
        }

        return PlayerRef.None;
    }

    private string GetPlayerNickname(PlayerRef player)
    {
        // Check cache first
        if (_playerNicknames.TryGetValue(player, out string cachedNickname))
        {
            return cachedNickname;
        }

        // Try to find PlayerManager for this player
        var playerManager = FindObjectsOfType<PlayerManager>()
            .FirstOrDefault(pm => pm.Object.InputAuthority == player);
            
        if (playerManager != null)
        {
            string nickname = playerManager.Nickname.Value;
            _playerNicknames[player] = nickname;
            return nickname;
        }

        return $"Player {player}";
    }

    #region RPCs

    /// <summary>
    /// RPC for sending global chat messages to all players
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SendGlobalMessage(PlayerRef sender, string messageText)
    {
        var message = new ChatMessage(sender, messageText, ChatMessageType.Global);
        ProcessReceivedMessage(message);
    }

    /// <summary>
    /// RPC for sending private messages between specific players
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SendPrivateMessage(PlayerRef sender, PlayerRef target, string messageText)
    {
        // Only the sender and target should process this message
        if (Runner.LocalPlayer != sender && Runner.LocalPlayer != target)
            return;

        var message = new ChatMessage(sender, messageText, ChatMessageType.Private, target);
        ProcessReceivedMessage(message);
    }

    #endregion

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

    /// <summary>
    /// Gets the current message history for UI display
    /// </summary>
    public List<ChatMessage> GetMessageHistory()
    {
        return new List<ChatMessage>(_messageHistory);
    }

    /// <summary>
    /// Clears the local message history
    /// </summary>
    public void ClearMessageHistory()
    {
        _messageHistory.Clear();
    }
    public static void RaiseSystemMessage(string message)
    {
        OnChatError?.Invoke(message);
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
}