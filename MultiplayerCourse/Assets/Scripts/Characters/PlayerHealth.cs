using System;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

public sealed class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int CurrentHealth { get; private set; }

    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;

    public UnityEvent<int, int> HealthChanged = new();

    
    public UnityEvent Died = new();
    
    public event Action<PlayerRef> DiedWithAttacker;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        OnHealthChanged();
    }

    public void RequestDamage(
        int damage,
        PlayerRef attacker)
    {
        if (damage <= 0)
            return;

        if (Object.HasStateAuthority)
        {
            ApplyDamage(damage, attacker);
            return;
        }

        RPC_RequestDamage(
            damage,
            attacker);
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(
        int damage,
        PlayerRef attacker)
    {
        ApplyDamage(
            damage,
            attacker);
    }

    private void ApplyDamage(
        int damage,
        PlayerRef attacker)
    {
        if (!Object.HasStateAuthority)
            return;

        if (CurrentHealth <= 0)
            return;

        CurrentHealth =
            Mathf.Max(
                CurrentHealth - damage,
                0);

        Debug.Log(
            $"[Health] Player {Object.InputAuthority.PlayerId} " +
            $"took {damage} damage. " +
            $"Health: {CurrentHealth}/{maxHealth}");

        OnHealthChanged();

        if (CurrentHealth > 0)
            return;

        HandleDeath(attacker);
    }

    private void HandleDeath(
        PlayerRef attacker)
    {
        
        Died?.Invoke();

        
        DiedWithAttacker?.Invoke(attacker);

       
        if (PlayerMatchStats.TryGet(
                Object.InputAuthority,
                out PlayerMatchStats victimStats))
        {
            victimStats.AddDeath();
        }
    }

    public void RestoreFullHealth()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentHealth = maxHealth;

        OnHealthChanged();
    }

    private void OnHealthChanged()
    {
        HealthChanged?.Invoke(
            CurrentHealth,
            maxHealth);
    }
}