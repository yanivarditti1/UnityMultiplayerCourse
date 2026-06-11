using Fusion;
using UnityEngine;

public sealed class ThrownChairProjectile : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Settings")]
    [SerializeField] private float lifetime = 5f;

    private int _damage;
    private PlayerRef _owner;
    private bool _hasHit;
    private TickTimer _lifeTimer;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            _lifeTimer = TickTimer.CreateFromSeconds(Runner, lifetime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (_lifeTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    public void Launch(Vector3 direction, float force, int damage, PlayerRef owner)
    {
        _damage = damage;
        _owner = owner;

        if (rb)
            rb.linearVelocity = direction.normalized * force;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!Object.HasStateAuthority)
            return;

        if (_hasHit)
            return;

        _hasHit = true;

        PlayerHitbox hitbox = collision.collider.GetComponentInParent<PlayerHitbox>();

        if (hitbox != null)
        {
            hitbox.DamageReceiver.ReceiveDamage(
                _damage,
                _owner);
        }
        else
        {
            return;
        }

        Runner.Despawn(Object);
    }
}