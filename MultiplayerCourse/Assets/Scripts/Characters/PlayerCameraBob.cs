using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerCameraBob : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRoot;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Bob Settings")]
    [SerializeField] private float horizontalAmount = 0.025f;
    [SerializeField] private float verticalAmount = 0.04f;
    [SerializeField] private float stepDuration = 0.16f;

    [Header("Return")]
    [SerializeField] private float returnDuration = 0.12f;

    private Vector3 _originalLocalPosition;

    private Sequence _bobSequence;
    private Tween _returnTween;

    private bool _isBobbing;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            enabled = false;
            return;
        }

        if (cameraRoot == null)
        {
            Debug.LogWarning(
                "[PlayerCameraBob] Camera Root is missing.");

            enabled = false;
            return;
        }

        if (moveAction == null)
        {
            Debug.LogWarning(
                "[PlayerCameraBob] Move Action is missing.");

            enabled = false;
            return;
        }

        _originalLocalPosition =
            cameraRoot.localPosition;
        
        moveAction.action.Enable();
    }

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        Vector2 movementInput =
            moveAction.action.ReadValue<Vector2>();

        bool isMoving =
            movementInput.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            if (!_isBobbing)
                StartBob();
        }
        else
        {
            if (_isBobbing)
                StopBob();
        }
    }

    private void StartBob()
    {
        _isBobbing = true;

        KillTweens();

        cameraRoot.localPosition =
            _originalLocalPosition;

        _bobSequence =
            DOTween.Sequence();

       
        _bobSequence.Append(
            cameraRoot
                .DOLocalMove(
                    _originalLocalPosition +
                    new Vector3(
                        horizontalAmount,
                        verticalAmount,
                        0f),
                    stepDuration)
                .SetEase(Ease.InOutSine)
        );

       
        _bobSequence.Append(
            cameraRoot
                .DOLocalMove(
                    _originalLocalPosition,
                    stepDuration)
                .SetEase(Ease.InOutSine)
        );

       
        _bobSequence.Append(
            cameraRoot
                .DOLocalMove(
                    _originalLocalPosition +
                    new Vector3(
                        -horizontalAmount,
                        verticalAmount,
                        0f),
                    stepDuration)
                .SetEase(Ease.InOutSine)
        );
        
        _bobSequence.Append(
            cameraRoot
                .DOLocalMove(
                    _originalLocalPosition,
                    stepDuration)
                .SetEase(Ease.InOutSine)
        );

        _bobSequence.SetLoops(
            -1,
            LoopType.Restart);
    }

    private void StopBob()
    {
        _isBobbing = false;

        KillTweens();

        _returnTween =
            cameraRoot
                .DOLocalMove(
                    _originalLocalPosition,
                    returnDuration)
                .SetEase(Ease.OutSine);
    }

    private void KillTweens()
    {
        _bobSequence?.Kill();
        _returnTween?.Kill();

        cameraRoot?.DOKill();

        _bobSequence = null;
        _returnTween = null;
    }

    private void OnDisable()
    {
        KillTweens();

        if (cameraRoot != null)
        {
            cameraRoot.localPosition =
                _originalLocalPosition;
        }

        _isBobbing = false;
    }
}