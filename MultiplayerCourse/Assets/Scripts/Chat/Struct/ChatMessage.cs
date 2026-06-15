using System;
using Fusion;

[Serializable]
public struct ChatMessage : INetworkStruct
{

    public PlayerRef Sender;
    [Networked, Capacity(200)]
    public string Content { get; set; }
    
    public ChatMessageType Type;

    public PlayerRef Target;
    
    public float Timestamp;
    
    public ChatMessage(PlayerRef sender, string content, ChatMessageType type, PlayerRef target = default)
    {
        Sender = sender;
        Content = content;
        Type = type;
        Target = target;
        Timestamp = UnityEngine.Time.time;
    }
}

public enum ChatMessageType : byte
{
    Global = 0,
    Private = 1,
    System = 2
}

