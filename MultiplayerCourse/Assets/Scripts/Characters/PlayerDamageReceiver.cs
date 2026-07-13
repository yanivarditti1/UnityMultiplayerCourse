using Fusion;
using UnityEngine;

public sealed class PlayerDamageReceiver : NetworkBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    public PlayerRef Owner => Object.InputAuthority;

    public void ReceiveDamage(int damage, PlayerRef attacker)
    {
        if (attacker == Owner)
            return;

        ConquestManager conquestManager =
            ConquestManager.Instance;

        if (conquestManager != null &&
            conquestManager.IsReady &&
            conquestManager.AreTeammates(attacker, Owner))
        {
            return;
        }

        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (captureTheFlagManager != null &&
            captureTheFlagManager.IsReady &&
            captureTheFlagManager.AreTeammates(
                attacker,
                Owner))
        {
            return;
        }

        playerHealth.RequestDamage(damage, attacker);
    }
}