using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class PlayerMatchStats : NetworkBehaviour
{
    public static readonly Dictionary<PlayerRef, PlayerMatchStats>
        Registry = new();

    public static event Action OnPlayerListChanged;
    public static event Action<PlayerRef> OnAnyStatsChanged;

    [Header("References")]
    [SerializeField] private PlayerChairCombat chairCombat;

    [Networked, OnChangedRender(nameof(HandleStatsChanged))]
    public int Kills { get; private set; }

    [Networked, OnChangedRender(nameof(HandleStatsChanged))]
    public int Deaths { get; private set; }

    [Networked, OnChangedRender(nameof(HandleStatsChanged))]
    public ChairCombatMode CombatMode { get; private set; }

    public PlayerRef Player =>
        Object.InputAuthority;

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
        OnAnyStatsChanged?.Invoke(
            Object.InputAuthority);
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        Registry.Remove(
            Object.InputAuthority);

        OnPlayerListChanged?.Invoke();
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

    private void HandleStatsChanged()
    {
        OnAnyStatsChanged?.Invoke(
            Object.InputAuthority);
    }

    public static bool TryGet(
        PlayerRef player,
        out PlayerMatchStats stats)
    {
        return Registry.TryGetValue(
            player,
            out stats);
    }
}