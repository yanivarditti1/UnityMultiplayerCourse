using Fusion;
using UnityEngine;

public sealed class PlayerChairInventory : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform chairHoldPoint;
    [SerializeField] private GameObject heldChairVisual;

    [Networked, OnChangedRender(nameof(OnChairStateChanged))]
    public bool HasChair { get; private set; }

    public Transform ChairHoldPoint => chairHoldPoint;

    public override void Spawned()
    {
        OnChairStateChanged();
    }

    public bool CanReceiveChair()
    {
        return !HasChair;
    }

    public void RequestReceiveChair()
    {
        RPC_RequestReceiveChair();
    }

    public void RequestConsumeChair()
    {
        RPC_RequestConsumeChair();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestReceiveChair()
    {
        if (HasChair)
            return;

        HasChair = true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestConsumeChair()
    {
        if (!HasChair)
            return;

        HasChair = false;
    }

    private void OnChairStateChanged()
    {
        if (heldChairVisual != null)
            heldChairVisual.SetActive(HasChair);
    }
}