using DG.Tweening;
using UnityEngine;

public sealed class ChairPickupVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;

    [Header("Float")]
    [SerializeField] private float floatHeight = 0.25f;
    [SerializeField] private float floatDuration = 1.2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    private Vector3 _startPosition;
    private Tween _floatTween;

    private void Awake()
    {
        if (!visualRoot)
            visualRoot = transform;

        _startPosition = visualRoot.localPosition;
    }

    private void OnEnable()
    {
        visualRoot.localPosition = _startPosition;

        _floatTween = visualRoot
            .DOLocalMoveY(
                _startPosition.y + floatHeight,
                floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void Update()
    {
        visualRoot.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.World);
    }

    private void OnDisable()
    {
        _floatTween?.Kill();
    }
}