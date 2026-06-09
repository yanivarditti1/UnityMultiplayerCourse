using DG.Tweening;
using UnityEngine;

public sealed class ChairSwingAnimation : MonoBehaviour
{
    [SerializeField] private Transform chairVisual;

    [Header("Swing")]
    [SerializeField] private float pullbackDuration = 0.08f;
    [SerializeField] private float swingDuration = 0.12f;
    [SerializeField] private float recoveryDuration = 0.15f;

    [SerializeField] private Vector3 pullbackRotation = new(-40f, 15f, 10f);
    [SerializeField] private Vector3 swingRotation = new(70f, -25f, -15f);

    private Quaternion _idleRotation;
    private Sequence _swingSequence;

    private void Awake()
    {
        _idleRotation = chairVisual.localRotation;
    }

    public void PlaySwing()
    {
        _swingSequence?.Kill();

        chairVisual.localRotation = _idleRotation;

        _swingSequence = DOTween.Sequence();

        _swingSequence.Append(
            chairVisual.DOLocalRotate(
                    pullbackRotation,
                    pullbackDuration,
                    RotateMode.Fast)
                .SetEase(Ease.OutQuad));

        _swingSequence.Append(
            chairVisual.DOLocalRotate(
                    swingRotation,
                    swingDuration,
                    RotateMode.Fast)
                .SetEase(Ease.InQuad));

        _swingSequence.Append(
            chairVisual.DOLocalRotateQuaternion(
                    _idleRotation,
                    recoveryDuration)
                .SetEase(Ease.OutBack));

        _swingSequence.SetLink(gameObject);
    }
}