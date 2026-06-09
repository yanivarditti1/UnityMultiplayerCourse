using UnityEngine;

public sealed class PlayerHitbox : MonoBehaviour
{
    [SerializeField] private PlayerDamageReceiver damageReceiver;

    public PlayerDamageReceiver DamageReceiver => damageReceiver;
}