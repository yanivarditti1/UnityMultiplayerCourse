using Fusion;
using UnityEngine;

public sealed class PlayerAnimationController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NetworkMecanimAnimator networkAnimator;

    [Header("Settings")]
    [SerializeField] private float speedSmoothTime = 0.08f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int SwingHash = Animator.StringToHash("Swing");
    private static readonly int ThrowHash = Animator.StringToHash("Throw");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private float _currentSpeed;
    private float _speedVelocity;

    public void SetMovementSpeed(float targetSpeed)
    {
        _currentSpeed = Mathf.SmoothDamp(
            _currentSpeed,
            targetSpeed,
            ref _speedVelocity,
            speedSmoothTime);

        animator.SetFloat(SpeedHash, _currentSpeed);
    }

    public void PlaySwing()
    {
        if (Object.HasStateAuthority)
            networkAnimator.SetTrigger(SwingHash);
    }

    public void PlayThrow()
    {
        if (Object.HasStateAuthority)
            networkAnimator.SetTrigger(ThrowHash);
    }

    public void SetDead(bool dead)
    {
        if (Object.HasStateAuthority)
            animator.SetBool(DeadHash, dead);
    }
}