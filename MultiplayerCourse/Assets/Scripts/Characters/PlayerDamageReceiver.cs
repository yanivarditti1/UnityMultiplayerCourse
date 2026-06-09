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

        playerHealth.RequestDamage(damage);
    }
}