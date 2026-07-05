using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerChairCombat : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerChairInventory chairInventory;
    [SerializeField] private PlayerMeleeChair meleeChair;
    [SerializeField] private PlayerAnimationController animationController;

    [SerializeField] private Transform attackOrigin;
    [SerializeField] private NetworkObject thrownChairPrefab;

    [Header("Input")]
    [SerializeField] private InputActionReference primaryAction;

    [Header("Class")]
    [SerializeField] private ChairCombatMode combatMode;

    [Header("Cooldowns")]
    [SerializeField] private float meleeCooldown = 0.45f;
    [SerializeField] private float throwCooldown = 0.7f;

    [Header("Throw")]
    [SerializeField] private int throwDamage = 35;
    [SerializeField] private float throwForce = 16f;

    private float _lastAttackTime;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        primaryAction.action.Enable();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!Object.HasInputAuthority)
            return;

        primaryAction.action.Disable();
    }

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        if (!primaryAction.action.WasPressedThisFrame())
            return;

        if (!chairInventory.HasChair)
            return;

        if (combatMode == ChairCombatMode.Melee)
            TryMeleeAttack();
        else
            TryThrowAttack();
    }

    private void TryMeleeAttack()
    {
        if (Time.time < _lastAttackTime + meleeCooldown)
            return;

        _lastAttackTime = Time.time;

        meleeChair.Attack();
    }

    private void TryThrowAttack()
    {
        if (Time.time < _lastAttackTime + throwCooldown)
            return;

        _lastAttackTime = Time.time;

        RPC_ThrowChair();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ThrowChair()
    {
        animationController.PlayThrow();

        Vector3 spawnPosition =
            attackOrigin.position +
            attackOrigin.forward * 0.8f;

        NetworkObject thrownChair =
            Runner.Spawn(
                thrownChairPrefab,
                spawnPosition,
                attackOrigin.rotation,
                Object.InputAuthority);

        if (thrownChair.TryGetComponent(out ThrownChairProjectile projectile))
        {
            projectile.Launch(
                attackOrigin.forward,
                throwForce,
                throwDamage,
                Object.InputAuthority);
        }

        chairInventory.RequestConsumeChair();
    }
}