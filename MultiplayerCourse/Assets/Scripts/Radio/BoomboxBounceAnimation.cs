using DG.Tweening;
using UnityEngine;

public sealed class BoomboxBounceAnimation : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] private float jumpHeight = 0.08f;
    [SerializeField] private float jumpDuration = 0.12f;

    [Header("Punch")]
    [SerializeField] private float scalePunch = 0.06f;
    [SerializeField] private float rotationAmount = 2f;

    [Header("Timing")]
    [SerializeField] private float pauseBetweenBounces = 0.04f;

    private Vector3 _startPosition;
    private Vector3 _startScale;
    private Quaternion _startRotation;

    private Sequence _sequence;

    private void Awake()
    {
        _startPosition = transform.localPosition;
        _startScale = transform.localScale;
        _startRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        StartBounce();
    }

    private void StartBounce()
    {
        _sequence?.Kill();

        transform.DOKill();

        transform.localPosition = _startPosition;
        transform.localScale = _startScale;
        transform.localRotation = _startRotation;

        Vector3 rightRotation =
            _startRotation.eulerAngles +
            new Vector3(0f, 0f, rotationAmount);

        Vector3 leftRotation =
            _startRotation.eulerAngles +
            new Vector3(0f, 0f, -rotationAmount);

        _sequence = DOTween.Sequence();

        _sequence.Append(
            transform
                .DOLocalMoveY(
                    _startPosition.y + jumpHeight,
                    jumpDuration)
                .SetEase(Ease.OutQuad));

        _sequence.Join(
            transform
                .DOLocalRotate(
                    rightRotation,
                    jumpDuration)
                .SetEase(Ease.OutQuad));

        _sequence.Join(
            transform
                .DOScale(
                    _startScale * (1f + scalePunch),
                    jumpDuration)
                .SetEase(Ease.OutQuad));

        _sequence.Append(
            transform
                .DOLocalMoveY(
                    _startPosition.y,
                    jumpDuration)
                .SetEase(Ease.InQuad));

        _sequence.Join(
            transform
                .DOLocalRotate(
                    leftRotation,
                    jumpDuration)
                .SetEase(Ease.InOutQuad));

        _sequence.Join(
            transform
                .DOScale(
                    _startScale,
                    jumpDuration)
                .SetEase(Ease.InQuad));

        _sequence.Append(
            transform
                .DOLocalRotate(
                    _startRotation.eulerAngles,
                    jumpDuration * 0.5f)
                .SetEase(Ease.OutQuad));

        _sequence.AppendInterval(
            pauseBetweenBounces);

        _sequence.SetLoops(
            -1,
            LoopType.Restart);
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        transform.DOKill();

        transform.localPosition = _startPosition;
        transform.localScale = _startScale;
        transform.localRotation = _startRotation;
    }
}