using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerChairCombat : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerChairInventory chairInventory;
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private NetworkObject thrownChairPrefab;
    [SerializeField] private ChairSwingAnimation _swingAnimation;

    [Header("Input")]
    [SerializeField] private InputActionReference primaryAction;

    [Header("Class")]
    [SerializeField] private ChairCombatMode combatMode;

    [Header("Melee")]
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float meleeRadius = 0.8f;
    [SerializeField] private int meleeDamage = 15;
    [SerializeField] private int maxMeleeUses = 4;
    [SerializeField] private float meleeCooldown = 0.45f;

    [Header("Throw")]
    [SerializeField] private int throwDamage = 35;
    [SerializeField] private float throwForce = 16f;
    [SerializeField] private float throwCooldown = 0.7f;

    private int _currentMeleeUses;
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

        if (primaryAction.action.WasPressedThisFrame())
            TryPrimaryAttack();
    }

    private void TryPrimaryAttack()
    {
        if (!chairInventory.HasChair)
            return;

        if (combatMode == ChairCombatMode.Melee)
            TryMeleeAttack();
        else
            TryThrowChair();
    }

    private void TryMeleeAttack()
    {
        if (Time.time < _lastAttackTime + meleeCooldown)
            return;

        _lastAttackTime = Time.time;

        RPC_PerformMeleeAttack();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_PerformMeleeAttack()
    {
        Vector3 origin = attackOrigin.position;
        Vector3 direction = attackOrigin.forward;

        if (Physics.SphereCast(origin, meleeRadius, direction, out RaycastHit hit, meleeRange))
        {
            if (hit.collider.TryGetComponent(out PlayerHealth health))
            {
                if (health != GetComponent<PlayerHealth>())
                    health.RequestDamage(meleeDamage);
            }
        }

        _currentMeleeUses++;

        if (_currentMeleeUses >= maxMeleeUses)
        {
            _currentMeleeUses = 0;
            chairInventory.RequestConsumeChair();
        }

        RPC_PlayMeleeVisual();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayMeleeVisual()
    {
        _swingAnimation.PlaySwing();
    }

    private void TryThrowChair()
    {
        if (Time.time < _lastAttackTime + throwCooldown)
            return;

        _lastAttackTime = Time.time;

        RPC_ThrowChair();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ThrowChair()
    {
        if (thrownChairPrefab == null)
            return;

        Vector3 spawnPosition = attackOrigin.position + attackOrigin.forward * 0.8f;
        Quaternion spawnRotation = attackOrigin.rotation;

        NetworkObject thrownChair = Runner.Spawn(
            thrownChairPrefab,
            spawnPosition,
            spawnRotation,
            Object.InputAuthority
        );

        if (thrownChair.TryGetComponent(out ThrownChairProjectile projectile))
            projectile.Launch(attackOrigin.forward, throwForce, throwDamage, Object.InputAuthority);

        chairInventory.RequestConsumeChair();
    }
}