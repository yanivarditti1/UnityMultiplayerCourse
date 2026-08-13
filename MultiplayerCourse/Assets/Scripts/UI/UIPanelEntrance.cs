using DG.Tweening;
using UnityEngine;

public sealed class UIPanelEntrance : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private RectTransform target;

    [Header("Animation")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float startYOffset = -40f;

    private Vector3 originalScale;
    private Vector2 originalPosition;

    private void Awake()
    {
        originalScale = target.localScale;
        originalPosition = target.anchoredPosition;
    }

    private void OnEnable()
    {
        PlayEntrance();
    }

    private void OnDisable()
    {
        target.DOKill();

        target.localScale = originalScale;
        target.anchoredPosition = originalPosition;
    }

    private void PlayEntrance()
    {
        target.DOKill();

        target.localScale =
            originalScale * startScale;

        target.anchoredPosition =
            originalPosition + Vector2.up * startYOffset;

        Sequence sequence = DOTween.Sequence();

        sequence.Join(
            target.DOScale(
                    originalScale,
                    duration)
                .SetEase(Ease.OutBack));

        sequence.Join(
            target.DOAnchorPos(
                    originalPosition,
                    duration)
                .SetEase(Ease.OutCubic));

        sequence.SetUpdate(true);
    }
}