using Fusion;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class PlayerHealthBar : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image fillImage;

    public override void Spawned()
    {
        if (!playerHealth)
            return;

        playerHealth.HealthChanged.AddListener(OnHealthChanged);

        OnHealthChanged(
            playerHealth.CurrentHealth,
            playerHealth.MaxHealth);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!playerHealth)
            return;

        playerHealth.HealthChanged.RemoveListener(OnHealthChanged);
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (!fillImage)
            return;

        fillImage.DOFillAmount((float)currentHealth / maxHealth, 0.2f);
    }
}