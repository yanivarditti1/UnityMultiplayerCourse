using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager Local { get; private set; }
    
    private static readonly Dictionary<PlayerRef, PlayerManager> _registry = new Dictionary<PlayerRef, PlayerManager>();
    public static IReadOnlyDictionary<PlayerRef, PlayerManager> Registry => _registry;
    
    [Networked, OnChangedRender (nameof(OnNicknameChanged))]
    public NetworkString<_32> Nickname { get; private set; }
    
    [Networked, OnChangedRender(nameof(OnPlayerColorChanged))]
    public Color PlayerColor { get; private set; } = Color.white;

    [Networked] public int MaxHealth { get; private set; }
    [Networked] public string PlayerCharacter { get; private set; }
    
    public static event Action<PlayerRef, string> OnAnyNicknameChanged;
    public static event Action<PlayerRef, Color> OnAnyPlayerColorChanged;


    public override void Spawned()
    {
        _registry[Object.InputAuthority] = this;
        
        if (Object.HasStateAuthority)
        {
            Local = this;
            var data = PlayerDataPersistanceManager.Instance;
            var nickname = string.IsNullOrEmpty(data.Nickname)
                ? $"Player { Object.InputAuthority.PlayerId }"
                : data.Nickname;
            
            Nickname = nickname;
            MaxHealth = data.MaxHealth;
            PlayerCharacter = data.PlayerCharacter;
        }
        
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.NotifyPlayerManagerSpawned();
    }

    public void SetNameColor(Color color)
    {
        if (Object.HasStateAuthority)
            PlayerColor = color;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _registry.Remove(Object.InputAuthority);
        if (Object.HasStateAuthority)
            Local = null;
    }

    private void OnNicknameChanged()
    {
        OnAnyNicknameChanged?.Invoke(Object.InputAuthority, Nickname.ToString());
    }

    private void OnPlayerColorChanged()
    {
        OnAnyPlayerColorChanged?.Invoke(Object.InputAuthority, PlayerColor);
    }
}
