using Fusion;
using UnityEngine;

public sealed class ChairPickup : NetworkBehaviour
{
    private bool _wasPickedUp;

    private void OnTriggerEnter(Collider other)
    {
        if (_wasPickedUp)
            return;

        if (!other.TryGetComponent(out PlayerChairInventory inventory))
            return;

        if (!inventory.Object.HasInputAuthority)
            return;

        if (!inventory.CanReceiveChair())
            return;

        inventory.RequestReceiveChair();

        RPC_RequestDespawn();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDespawn()
    {
        if (_wasPickedUp)
            return;

        _wasPickedUp = true;
        Runner.Despawn(Object);
    }
}