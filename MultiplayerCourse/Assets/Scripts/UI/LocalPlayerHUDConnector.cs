using Fusion;
using UnityEngine;

public sealed class LocalPlayerHUDConnector : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        LocalHUDRegistry.BindHealth(playerHealth);
    }
}