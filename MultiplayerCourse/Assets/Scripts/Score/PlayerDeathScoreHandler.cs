using Fusion;
using UnityEngine;

public sealed class PlayerDeathScoreHandler : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.DiedWithAttacker += HandlePlayerDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.DiedWithAttacker -= HandlePlayerDied;
    }

    private void HandlePlayerDied(PlayerRef attacker)
    {
        if (attacker == PlayerRef.None)
            return;

        if (MatchScoreManager.Instance != null)
        {
            MatchScoreManager.Instance.RegisterKill(attacker);
        }
        
        if (PlayerMatchStats.TryGet(
                attacker,
                out PlayerMatchStats attackerStats))
        {
            attackerStats.AddKill();
        }
    }
}