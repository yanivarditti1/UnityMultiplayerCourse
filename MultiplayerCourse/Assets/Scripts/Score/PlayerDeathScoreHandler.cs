using Fusion;
using UnityEngine;

public sealed class PlayerDeathScoreHandler : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private void OnEnable()
    {
        playerHealth.DiedWithAttacker += HandlePlayerDied;
    }

    private void OnDisable()
    {
        playerHealth.DiedWithAttacker -= HandlePlayerDied;
    }

    private void HandlePlayerDied(PlayerRef attacker)
    {
        if (attacker == PlayerRef.None)
            return;

        if (MatchScoreManager.Instance == null)
            return;

        MatchScoreManager.Instance.RegisterKill(attacker);
    }
}