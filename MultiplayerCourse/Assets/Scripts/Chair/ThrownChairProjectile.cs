using Fusion;
using UnityEngine;

public sealed class ThrownChairProjectile : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Lifetime")]
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

        if (collision.collider.TryGetComponent(out PlayerHitbox hitbox))
        {
            hitbox.DamageReceiver.ReceiveDamage(_damage, _owner);
        }

        Runner.Despawn(Object);
    }
}