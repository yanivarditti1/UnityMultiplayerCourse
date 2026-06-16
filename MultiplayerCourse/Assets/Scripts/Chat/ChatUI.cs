using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private ScrollRect messageScrollRect;
    [SerializeField] private Transform messageContainer;
    [SerializeField] private TMP_InputField messageInputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Button toggleChatButton;
    [SerializeField] private TMP_Text placeholderText;
    
    [Header("Message Prefabs")]
    [SerializeField] private GameObject globalMessagePrefab;
    [SerializeField] private GameObject privateMessagePrefab;
    [SerializeField] private GameObject systemMessagePrefab;
    
    [Header("Settings")]
    [SerializeField] private int maxDisplayedMessages = 50;
    [SerializeField] private bool autoOpenOnMessage = true;
    [SerializeField] private float autoCloseDelay = 10f;
    
    private bool _isChatOpen = false;
    private readonly Queue<GameObject> _messageObjects = new Queue<GameObject>();
    private float _lastMessageTime;
    private Canvas _parentCanvas;

    private void Awake()
    {
        _parentCanvas = GetComponentInParent<Canvas>();
        
        // Initialize UI
        if (chatPanel)
            chatPanel.SetActive(_isChatOpen);
            
        UpdatePlaceholderText();
        DontDestroyOnLoad(transform.root.gameObject);
    
    }

    // ChatUI.cs OnEnable
    void OnEnable()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.OnChatMessageReceived += OnMessageReceived;
        LobbyManager.Instance.OnChatErrorReceived   += OnChatError;
    }

    void OnDisable()
    {
        if (LobbyManager.Instance == null) return;
        LobbyManager.Instance.OnChatMessageReceived -= OnMessageReceived;
        LobbyManager.Instance.OnChatErrorReceived   -= OnChatError;
    }

    private void Update()
    {
        // Handle hotkey to toggle chat
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!_isChatOpen)
            {
                OpenChat();
            }
            else if (messageInputField != null && messageInputField.isFocused)
            {
                SendMessage();
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Escape) && _isChatOpen)
        {
            CloseChat();
        }
        
        // Auto-close chat after delay
        if (_isChatOpen && autoCloseDelay > 0 && Time.time - _lastMessageTime > autoCloseDelay)
        {
            if (messageInputField == null || !messageInputField.isFocused)
            {
                CloseChat();
            }
        }
    }

    private void OnMessageReceived(ChatMessage message, string senderNickname)
    {
        DisplayMessage(message, senderNickname);
        
        _lastMessageTime = Time.time;
        
        // Auto-open chat on new message
        if (autoOpenOnMessage && !_isChatOpen)
        {
            OpenChat();
        }
    }

    private void OnChatError(string errorMessage)
    {
        // Display error as system message
        var systemMessage = new ChatMessage(default, errorMessage, ChatMessageType.System);
        DisplayMessage(systemMessage, "System");
    }

    private void DisplayMessage(ChatMessage message, string senderNickname)
    {
        GameObject messagePrefab = GetMessagePrefab(message.Type);
        if (messagePrefab == null || messageContainer == null)
            return;

        // Instantiate message UI object
        GameObject messageObj = Instantiate(messagePrefab, messageContainer);
        
        // Configure message content based on type
        ConfigureMessageObject(messageObj, message, senderNickname);
        
        // Add to queue and manage max messages
        _messageObjects.Enqueue(messageObj);
        while (_messageObjects.Count > maxDisplayedMessages)
        {
            GameObject oldMessage = _messageObjects.Dequeue();
            if (oldMessage != null)
                Destroy(oldMessage);
        }

        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        if (messageScrollRect != null)
        {
            messageScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private GameObject GetMessagePrefab(ChatMessageType messageType)
    {
        return messageType switch
        {
            ChatMessageType.Global => globalMessagePrefab,
            ChatMessageType.Private => privateMessagePrefab,
            ChatMessageType.System => systemMessagePrefab,
            _ => globalMessagePrefab
        };
    }

    private void ConfigureMessageObject(GameObject messageObj, ChatMessage message, string senderNickname)
    {
        // Find text components in the message object
        TMP_Text[] textComponents = messageObj.GetComponentsInChildren<TMP_Text>();
        
        if (textComponents.Length == 0)
            return;

        string displayText = FormatMessage(message, senderNickname);
        
        // Assign text to the first text component
        textComponents[0].text = displayText;
        
        // Set color based on message type
        Color messageColor = GetMessageColor(message.Type);
        textComponents[0].color = messageColor;
    }

    private string FormatMessage(ChatMessage message, string senderNickname)
    {
        return message.Type switch
        {
            ChatMessageType.Global => $"<b>[Global] {senderNickname}:</b> {message.Content}",
            ChatMessageType.Private => $"<b>[Private] {senderNickname}:</b> {message.Content}",
            ChatMessageType.System => $"<i>[System] {message.Content}</i>",
            _ => $"{senderNickname}: {message.Content}"
        };
    }

    private Color GetMessageColor(ChatMessageType messageType)
    {
        return messageType switch
        {
            ChatMessageType.Global => Color.white,
            ChatMessageType.Private => Color.cyan,
            ChatMessageType.System => Color.yellow,
            _ => Color.white
        };
    }

    public void ToggleChat()
    {
        if (_isChatOpen)
            CloseChat();
        else
            OpenChat();
    }

    public void OpenChat()
    {
        _isChatOpen = true;
        
        if (chatPanel != null)
            chatPanel.SetActive(true);
            
        if (messageInputField != null)
        {
            messageInputField.ActivateInputField();
            messageInputField.Select();
        }
        
        _lastMessageTime = Time.time;
    }

    public void CloseChat()
    {
        _isChatOpen = false;
        
        if (chatPanel != null)
            chatPanel.SetActive(false);
            
        if (messageInputField != null)
            messageInputField.DeactivateInputField();
    }

    public void SendMessage()
    {
        if (messageInputField == null || ChatManager.Instance == null)
            return;
            
        string messageText = messageInputField.text.Trim();
        if (string.IsNullOrEmpty(messageText))
            return;

        ChatManager.Instance.SendMessage(messageText);
        
        // Clear input field
        messageInputField.text = "";
        messageInputField.ActivateInputField();
    }

    private void OnInputEndEdit(string input)
    {
        // Send message when Enter is pressed
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessage();
        }
    }

    private void OnInputValueChanged(string input)
    {
        UpdatePlaceholderText();
    }

    private void UpdatePlaceholderText()
    {
        if (placeholderText != null)
        {
            placeholderText.text = "Type message... (/msg PlayerName for private message)";
        }
    }

    public void LoadMessageHistory()
    {
        if (ChatManager.Instance == null)
            return;

        var history = ChatManager.Instance.GetMessageHistory();
        
        // Clear existing messages
        ClearDisplayedMessages();
        
        // Display all messages from history
        foreach (var message in history)
        {
            string senderNickname = GetCachedPlayerNickname(message.Sender);
            DisplayMessage(message, senderNickname);
        }
    }

    private string GetCachedPlayerNickname(PlayerRef player)
    {
        // Try to find the player's nickname from active PlayerManager instances
        var playerManagers = FindObjectsOfType<PlayerManager>();
        foreach (var pm in playerManagers)
        {
            if (pm.Object.InputAuthority == player)
            {
                return pm.Nickname.Value;
            }
        }
        return $"Player {player}";
    }

    private void ClearDisplayedMessages()
    {
        while (_messageObjects.Count > 0)
        {
            GameObject messageObj = _messageObjects.Dequeue();
            if (messageObj != null)
                Destroy(messageObj);
        }
    }
}