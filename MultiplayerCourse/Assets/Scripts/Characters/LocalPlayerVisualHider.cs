using Fusion;
using UnityEngine;

public sealed class LocalPlayerVisualHider : NetworkBehaviour
{
    [SerializeField] private Renderer[] playerRenderers;

    public override void Spawned()
    {
        bool hideForLocalPlayer =
            Object.HasInputAuthority;

        SetRenderersVisible(
            !hideForLocalPlayer);
    }

    private void SetRenderersVisible(
        bool visible)
    {
        if (playerRenderers == null)
            return;

        for (int i = 0;
             i < playerRenderers.Length;
             i++)
        {
            if (playerRenderers[i] == null)
                continue;

            playerRenderers[i].enabled =
                visible;
        }
    }
}