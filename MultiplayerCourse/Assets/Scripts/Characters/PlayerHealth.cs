using Fusion;
using UnityEngine;
using UnityEngine.Events;

public sealed class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int CurrentHealth { get; private set; }

    public UnityEvent<int> HealthChanged;
    public UnityEvent Died;

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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDamage(int damage)
    {
        if (CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0);

        if (CurrentHealth <= 0)
            Died?.Invoke();
    }

    private void OnHealthChanged()
    {
        HealthChanged?.Invoke(CurrentHealth);
    }
}