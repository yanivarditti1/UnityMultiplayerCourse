using Fusion;
using UnityEngine;

public sealed class ThrownChairProjectile : NetworkBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField] private float hitRadius = 0.45f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private Vector3 spinSpeed = new(240f, 0f, 0f);

    [Header("Break SFX")]
    [SerializeField] private NetworkObject chairBreakSFXPrefab;

    private Vector3 _velocity;
    private int _damage;
    private PlayerRef _owner;
    private TickTimer _lifeTimer;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        _lifeTimer = TickTimer.CreateFromSeconds(
            Runner,
            lifetime);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (_lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        _velocity.y +=
            gravity * Runner.DeltaTime;

        Vector3 startPosition =
            transform.position;

        transform.position +=
            _velocity * Runner.DeltaTime;

        transform.Rotate(
            spinSpeed * Runner.DeltaTime,
            Space.Self);

        Vector3 movementDirection =
            transform.position - startPosition;

        float movementDistance =
            movementDirection.magnitude;

        if (movementDistance <= 0f)
            return;

        if (Physics.SphereCast(
                startPosition,
                hitRadius,
                movementDirection.normalized,
                out RaycastHit hit,
                movementDistance))
        {
            PlayerHitbox hitbox =
                hit.collider.GetComponentInParent<PlayerHitbox>();

            if (hitbox != null)
            {
                hitbox.DamageReceiver.ReceiveDamage(
                    _damage,
                    _owner);
            }

            SpawnBreakSound(
                hit.point);

            Runner.Despawn(Object);
        }
    }

    private void SpawnBreakSound(
        Vector3 position)
    {
        if (chairBreakSFXPrefab == null)
            return;

        Runner.Spawn(
            chairBreakSFXPrefab,
            position,
            Quaternion.identity);
    }

    public void Launch(
        Vector3 direction,
        float force,
        int damage,
        PlayerRef owner)
    {
        _velocity =
            direction.normalized * force;

        _damage = damage;
        _owner = owner;
    }
}