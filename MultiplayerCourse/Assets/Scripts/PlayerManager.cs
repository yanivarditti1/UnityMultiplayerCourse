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

    [Networked] public int MaxHealth { get; private set; }
    [Networked] public string PlayerCharacter { get; private set; }
    
    public static event Action<PlayerRef, string> OnAnyNicknameChanged;


    public override void Spawned()
    {
        _registry[Object.InputAuthority] = this;
        
        if (Object.HasStateAuthority)
        {
            Local = this;
            //DontDestroyOnLoad(Object);
            var data = PlayerDataPersistanceManager.Instance;
            var nickname = string.IsNullOrEmpty(data.Nickname)
                ? $"Player { Object.InputAuthority.PlayerId }"
                : data.Nickname;
            
            Nickname = nickname;
            MaxHealth = data.MaxHealth;
            PlayerCharacter = data.PlayerCharacter;
            
            //SetNickname(nickname);
        }
        
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.NotifyPlayerManagerSpawned();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _registry.Remove(Object.InputAuthority);
        if (Object.HasStateAuthority)
            Local = null;
    }

    /*private void SetNickname(string nickname)
    {
        if (!Object.HasStateAuthority) return;
        Nickname = nickname;
    }*/

    private void OnNicknameChanged()
    {
        OnAnyNicknameChanged?.Invoke(Object.InputAuthority, Nickname.ToString());
    }
}
