using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class LocalPlayerHealthHUD : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image fillImage;

    [Header("Fill Animation")]
    [SerializeField] private float fillDuration = 0.2f;
    [SerializeField] private Ease fillEase = Ease.OutQuad;

    [Header("Color Animation")]
    [SerializeField] private float colorDuration = 0.2f;

    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color criticalHealthColor = Color.red;

    private PlayerHealth currentPlayerHealth;

    public void Bind(PlayerHealth playerHealth)
    {
        Unbind();

        currentPlayerHealth = playerHealth;

        if (currentPlayerHealth == null)
            return;

        currentPlayerHealth.HealthChanged.AddListener(OnHealthChanged);

        OnHealthChanged(
            currentPlayerHealth.CurrentHealth,
            currentPlayerHealth.MaxHealth);
    }

    public void Unbind()
    {
        if (currentPlayerHealth != null)
        {
            currentPlayerHealth.HealthChanged.RemoveListener(OnHealthChanged);
            currentPlayerHealth = null;
        }

        fillImage.DOKill();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0)
            return;

        float normalizedHealth =
            Mathf.Clamp01((float)currentHealth / maxHealth);

        fillImage.DOKill();

        fillImage.DOFillAmount(
                normalizedHealth,
                fillDuration)
            .SetEase(fillEase);

        Color targetColor =
            GetHealthColor(normalizedHealth);

        fillImage.DOColor(
            targetColor,
            colorDuration);
    }

    private Color GetHealthColor(float normalizedHealth)
    {
        float healthPercent =
            normalizedHealth * 100f;

        if (healthPercent >= 100f)
            return fullHealthColor;

        if (healthPercent >= 80f)
            return highHealthColor;

        if (healthPercent >= 60f)
            return mediumHealthColor;

        if (healthPercent >= 30f)
            return lowHealthColor;

        return criticalHealthColor;
    }
}