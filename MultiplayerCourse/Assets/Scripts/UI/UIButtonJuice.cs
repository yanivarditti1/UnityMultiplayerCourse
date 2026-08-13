using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIButtonJuice : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private RectTransform target;

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float hoverDuration = 0.15f;

    [Header("Click")]
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float clickDuration = 0.08f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = target.localScale;
    }

    private void OnDisable()
    {
        target.DOKill();
        target.localScale = originalScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        target.DOKill();

        target.DOScale(
                originalScale * hoverScale,
                hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target.DOKill();

        target.DOScale(
                originalScale,
                hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        target.DOKill();

        target.DOScale(
                originalScale * pressedScale,
                clickDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        target.DOKill();

        target.DOScale(
                originalScale * hoverScale,
                clickDuration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}