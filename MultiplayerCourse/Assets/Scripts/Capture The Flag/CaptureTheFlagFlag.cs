using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class CaptureTheFlagFlag : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CaptureTheFlagManager manager;

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private float pickupCooldown = 0.75f;

    [Header("Dropping")]
    [SerializeField] private float dropHeight = 0.5f;

    [Networked]
    public PlayerRef Carrier { get; private set; }

    [Networked]
    public NetworkBool IsHome { get; private set; }

    [Networked]
    private TickTimer PickupCooldownTimer { get; set; }

    public bool IsReady { get; private set; }

    private Vector3 homePosition;
    private Quaternion homeRotation;

    public override void Spawned()
    {
        IsReady = true;

        homePosition = transform.position;
        homeRotation = transform.rotation;

        if (!Object.HasStateAuthority)
            return;

        Carrier = PlayerRef.None;
        IsHome = true;
        PickupCooldownTimer = default;
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        IsReady = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority ||
            !manager  ||
            !manager.IsReady ||
            manager.MatchEnded)
        {
            return;
        }

        if (Carrier != PlayerRef.None)
        {
            FollowCarrier();
            return;
        }

        if (!PickupCooldownTimer.ExpiredOrNotRunning(Runner))
            return;

        TryCollect();
    }

    public void DropAtPlayer(PlayerRef playerRef)
    {
        if (!Object.HasStateAuthority ||
            Carrier != playerRef)
        {
            return;
        }

        Vector3 dropPosition = transform.position;

        if (CaptureTheFlagPlayer.TryGet(
                playerRef,
                out CaptureTheFlagPlayer player))
        {
            dropPosition = player.transform.position;
        }

        Carrier = PlayerRef.None;
        IsHome = false;

        transform.SetPositionAndRotation(
            dropPosition + Vector3.up * dropHeight,
            homeRotation);

        PickupCooldownTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                pickupCooldown);
    }

    public void ReturnHome()
    {
        if (!Object.HasStateAuthority)
            return;

        Carrier = PlayerRef.None;
        IsHome = true;
        PickupCooldownTimer = default;

        transform.SetPositionAndRotation(
            homePosition,
            homeRotation);
    }

    private void TryCollect()
    {
        float radiusSquared =
            pickupRadius * pickupRadius;

        foreach (KeyValuePair<PlayerRef, CaptureTheFlagPlayer> entry
                 in CaptureTheFlagPlayer.Registry)
        {
            CaptureTheFlagPlayer player = entry.Value;

            if (!player  || player.IsDead)
                continue;

            if (manager.GetTeam(entry.Key) ==
                ConquestTeam.None)
            {
                continue;
            }

            Vector3 difference =
                player.transform.position -
                transform.position;

            if (difference.sqrMagnitude >
                radiusSquared)
            {
                continue;
            }

            Carrier = entry.Key;
            IsHome = false;
            PickupCooldownTimer = default;
            return;
        }
    }

    private void FollowCarrier()
    {
        if (!CaptureTheFlagPlayer.TryGet(
                Carrier,
                out CaptureTheFlagPlayer player))
        {
            ReturnHome();
            return;
        }

        transform.SetPositionAndRotation(
            player.FlagAnchor.position,
            player.FlagAnchor.rotation);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            pickupRadius);
    }
}