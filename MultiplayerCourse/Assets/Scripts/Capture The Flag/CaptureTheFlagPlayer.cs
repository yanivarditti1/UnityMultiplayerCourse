using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CaptureTheFlagPlayer : NetworkBehaviour
{
    private static readonly Dictionary<PlayerRef, CaptureTheFlagPlayer>
        registry = new();

    public static IReadOnlyDictionary<PlayerRef, CaptureTheFlagPlayer>
        Registry => registry;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform flagAnchor;

    public bool IsDead =>
        !playerHealth  ||
        playerHealth.IsDead;

    public Transform FlagAnchor =>
        flagAnchor 
            ? flagAnchor
            : transform;

    public float MovementMultiplier
    {
        get
        {
            CaptureTheFlagManager manager =
                CaptureTheFlagManager.Instance;

            if (!manager  ||
                !manager.IsReady ||
                !manager.IsCarrier(Object.InputAuthority))
            {
                return 1f;
            }

            return manager.CarrierSpeedMultiplier;
        }
    }

    public override void Spawned()
    {
        registry[Object.InputAuthority] = this;

        if (playerHealth )
        {
            playerHealth.DiedWithAttacker +=
                HandleDeath;
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        registry.Remove(Object.InputAuthority);

        if (playerHealth)
        {
            playerHealth.DiedWithAttacker -=
                HandleDeath;
        }
    }

    private void Update()
    {
        bool isLocalPlayer =
            Object.HasInputAuthority ||
            (Runner.GameMode == GameMode.Shared &&
             Object.HasStateAuthority);

        if (!isLocalPlayer ||
            Keyboard.current == null ||
            !Keyboard.current.gKey.wasPressedThisFrame)
        {
            return;
        }

        CaptureTheFlagManager manager =
            CaptureTheFlagManager.Instance;

        if (!manager  ||
            !manager.IsReady)
        {
            return;
        }

        manager.RequestDrop(
            Object.InputAuthority);
    }

    private void HandleDeath(PlayerRef attacker)
    {
        CaptureTheFlagManager manager =
            CaptureTheFlagManager.Instance;

        if (manager == null ||
            !manager.IsReady)
        {
            return;
        }

        manager.ReportDeath(
            Object.InputAuthority);
    }

    public static bool TryGet(
        PlayerRef player,
        out CaptureTheFlagPlayer result)
    {
        return registry.TryGetValue(
            player,
            out result);
    }
}