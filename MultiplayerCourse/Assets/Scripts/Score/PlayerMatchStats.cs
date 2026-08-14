using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class PlayerMatchStats : NetworkBehaviour
{
    public static readonly Dictionary<PlayerRef, PlayerMatchStats> Registry = new();

    public static event Action OnPlayerListChanged;
    public static event Action<PlayerRef> OnAnyStatsChanged;

    [Header("References")]
    [SerializeField] private PlayerChairCombat chairCombat;

    [Networked, OnChangedRender(nameof(HandleChanged))]
    public int Kills { get; private set; }

    [Networked, OnChangedRender(nameof(HandleChanged))]
    public int Deaths { get; private set; }

    [Networked, OnChangedRender(nameof(HandleChanged))]
    public ChairCombatMode CombatMode { get; private set; }

    [Networked, OnChangedRender(nameof(HandleChanged))]
    public NetworkString<_32> Nickname { get; private set; }

    public PlayerRef Player => Object.InputAuthority;

    public override void Spawned()
    {
        Registry[Object.InputAuthority] = this;

        if (Object.HasStateAuthority)
        {
            Kills = 0;
            Deaths = 0;

            if (chairCombat != null)
                CombatMode = chairCombat.CombatMode;
        }

        OnPlayerListChanged?.Invoke();
        OnAnyStatsChanged?.Invoke(Object.InputAuthority);
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        Registry.Remove(Object.InputAuthority);

        OnPlayerListChanged?.Invoke();
    }

    public void SetNicknameServer(string nickname)
    {
        if (!Object.HasStateAuthority)
            return;

        if (string.IsNullOrWhiteSpace(nickname))
            return;

        nickname = nickname.Trim();

        if (nickname.Length > 32)
            nickname = nickname.Substring(0, 32);

        Nickname = nickname;

        OnAnyStatsChanged?.Invoke(Object.InputAuthority);
    }

    public void AddKill()
    {
        if (!Object.HasStateAuthority)
            return;

        Kills++;
    }

    public void AddDeath()
    {
        if (!Object.HasStateAuthority)
            return;

        Deaths++;
    }

    private void HandleChanged()
    {
        OnAnyStatsChanged?.Invoke(Object.InputAuthority);
    }

    public static bool TryGet(
        PlayerRef player,
        out PlayerMatchStats stats)
    {
        return Registry.TryGetValue(player, out stats);
    }
}