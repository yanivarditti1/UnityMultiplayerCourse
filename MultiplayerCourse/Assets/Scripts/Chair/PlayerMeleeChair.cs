using Fusion;
using UnityEngine;

public sealed class PlayerMeleeChair : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerChairInventory chairInventory;
    [SerializeField] private ChairSwingAnimation swingAnimation;
    [SerializeField] private PlayerMeleeHitbox meleeHitbox;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Settings")]
    [SerializeField] private int damage = 15;
    [SerializeField] private int hitsUntilBreak = 3;
    [SerializeField] private float hitboxActiveTime = 0.15f;

    private int _successfulHits;
    private TickTimer _hitboxTimer;

    public override void Spawned()
    {
        meleeHitbox.Initialize(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (_hitboxTimer.Expired(Runner))
        {
            meleeHitbox.DisableHitbox();
        }
    }

    public void Attack()
    {
        RPC_Attack();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_Attack()
    {
        animationController.PlaySwing();

        meleeHitbox.EnableHitbox();

        _hitboxTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                hitboxActiveTime);

        RPC_PlaySwing();
    }

    public void ProcessHit(Collider other)
    {
        PlayerHitbox hitbox =
            other.GetComponentInParent<PlayerHitbox>();

        if (hitbox == null)
            return;

        if (hitbox.DamageReceiver.Owner ==
            Object.InputAuthority)
            return;

        hitbox.DamageReceiver.ReceiveDamage(
            damage,
            Object.InputAuthority);

        _successfulHits++;

        Debug.Log(
            $"Melee hit for {damage}. Durability {_successfulHits}/{hitsUntilBreak}");

        meleeHitbox.DisableHitbox();

        if (_successfulHits >= hitsUntilBreak)
        {
            _successfulHits = 0;

            chairInventory.RequestConsumeChair();

            Debug.Log("Chair broke");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlaySwing()
    {
        swingAnimation.PlaySwing();
    }
}