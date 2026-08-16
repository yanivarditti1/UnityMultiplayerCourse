using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerChairCombat : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerChairInventory chairInventory;
    [SerializeField] private PlayerMeleeChair meleeChair;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private FirstPersonNetworkMovement movement;

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

    
    [SerializeField] private float throwUpwardBoost = 0.1f;

   
    [SerializeField] private float throwSpawnDistance = 0.8f;

    public ChairCombatMode CombatMode =>
        combatMode;

    private float _lastAttackTime;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        if (primaryAction != null)
        {
            primaryAction.action.Enable();
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!Object.HasInputAuthority)
            return;

        if (primaryAction != null)
        {
            primaryAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        if (primaryAction == null)
            return;

        if (!primaryAction
                .action
                .WasPressedThisFrame())
        {
            return;
        }

        if (chairInventory == null)
            return;

        if (!chairInventory.HasChair)
            return;

        if (combatMode ==
            ChairCombatMode.Melee)
        {
            TryMeleeAttack();
        }
        else
        {
            TryThrowAttack();
        }
    }

  

    private void TryMeleeAttack()
    {
        if (Time.time <
            _lastAttackTime +
            meleeCooldown)
        {
            return;
        }

        _lastAttackTime =
            Time.time;

        if (meleeChair != null)
        {
            meleeChair.Attack();
        }
    }

   

    private void TryThrowAttack()
    {
        if (Time.time <
            _lastAttackTime +
            throwCooldown)
        {
            return;
        }

        _lastAttackTime =
            Time.time;

        RPC_ThrowChair();
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority)]
    private void RPC_ThrowChair()
    {
        if (attackOrigin == null)
            return;

        if (thrownChairPrefab == null)
            return;

        

        if (animationController != null)
        {
            animationController.PlayThrow();
        }

       

        Vector3 throwDirection;

        if (movement != null)
        {
            throwDirection =
                movement.GetAimDirection();
        }
        else
        {
            throwDirection =
                attackOrigin.forward;
        }

        
        throwDirection =
            (throwDirection +
             Vector3.up *
             throwUpwardBoost)
            .normalized;

       

        Vector3 spawnPosition =
            attackOrigin.position +
            throwDirection *
            throwSpawnDistance;

        

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                throwDirection,
                Vector3.up);

        
        NetworkObject thrownChair =
            Runner.Spawn(
                thrownChairPrefab,
                spawnPosition,
                spawnRotation,
                Object.InputAuthority);

        

        if (thrownChair.TryGetComponent(
                out ThrownChairProjectile projectile))
        {
            projectile.Launch(
                throwDirection,
                throwForce,
                throwDamage,
                Object.InputAuthority);
        }

       

        chairInventory
            .RequestConsumeChair();
    }
}