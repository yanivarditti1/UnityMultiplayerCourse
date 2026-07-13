using Fusion;
using UnityEngine;

public sealed class CaptureTheFlagBase : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CaptureTheFlagManager manager;

    [Header("Base")]
    [SerializeField] private ConquestTeam team;
    [SerializeField] private float scoringRadius = 3f;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority ||
            !manager  ||
            !manager.IsReady ||
            manager.MatchEnded)
        {
            return;
        }

        CaptureTheFlagFlag flag = manager.Flag;

        if (!flag  ||
            !flag.IsReady ||
            flag.Carrier == PlayerRef.None)
        {
            return;
        }

        if (!CaptureTheFlagPlayer.TryGet(
                flag.Carrier,
                out CaptureTheFlagPlayer carrier))
        {
            return;
        }

        Vector3 difference =
            carrier.transform.position -
            transform.position;

        if (difference.sqrMagnitude >
            scoringRadius * scoringRadius)
        {
            return;
        }

        manager.TryScore(
            flag.Carrier,
            team);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            scoringRadius);
    }
}