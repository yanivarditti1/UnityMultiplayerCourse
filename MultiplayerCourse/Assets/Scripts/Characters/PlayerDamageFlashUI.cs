using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDamageFlashUI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image damageFlashImage;

    [Header("Flash")]
    [Range(0f, 1f)]
    [SerializeField] private float flashAlpha = 0.45f;

    [SerializeField] private float fadeDuration = 0.35f;

    private int _previousHealth = -1;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            if (damageFlashImage != null)
                damageFlashImage.gameObject.SetActive(false);

            return;
        }

        _previousHealth =
            playerHealth.CurrentHealth;

        playerHealth.HealthChanged.AddListener(
            HandleHealthChanged);

        HideInstant();
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!Object.HasInputAuthority)
            return;

        if (playerHealth != null)
        {
            playerHealth.HealthChanged.RemoveListener(
                HandleHealthChanged);
        }

        if (damageFlashImage != null)
            damageFlashImage.DOKill();
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        bool tookDamage =
            _previousHealth >= 0 &&
            currentHealth < _previousHealth;

        _previousHealth =
            currentHealth;

        if (!tookDamage)
            return;

        PlayDamageFlash();
    }

    private void PlayDamageFlash()
    {
        if (damageFlashImage == null)
            return;

        damageFlashImage.DOKill();

        damageFlashImage.gameObject.SetActive(true);
        
        Color color =
            damageFlashImage.color;

        color.a = flashAlpha;

        damageFlashImage.color =
            color;
        
        damageFlashImage
            .DOFade(
                0f,
                fadeDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (damageFlashImage != null)
                {
                    damageFlashImage
                        .gameObject
                        .SetActive(false);
                }
            });
    }

    private void HideInstant()
    {
        if (damageFlashImage == null)
            return;

        damageFlashImage.DOKill();

        Color color =
            damageFlashImage.color;

        color.a = 0f;

        damageFlashImage.color =
            color;

        damageFlashImage.gameObject.SetActive(false);
    }
}