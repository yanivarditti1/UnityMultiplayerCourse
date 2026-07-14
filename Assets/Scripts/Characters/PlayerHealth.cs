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

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            CurrentHealth = maxHealth;

        OnHealthChanged();
    }

    public void RequestDamage(int damage)
    {
        RPC_RequestDamage(damage);
    }

    public void RestoreFullHealth()
    {
        if (!Object.HasStateAuthority)
            return;

        CurrentHealth = maxHealth;
        OnHealthChanged();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage)
    {
        if (CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);

        Debug.Log($"[Health] Player {Object.InputAuthority.PlayerId}: {CurrentHealth}/{maxHealth}");

        OnHealthChanged();

        if (CurrentHealth <= 0)
            Died?.Invoke();
    }

    private void OnHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}