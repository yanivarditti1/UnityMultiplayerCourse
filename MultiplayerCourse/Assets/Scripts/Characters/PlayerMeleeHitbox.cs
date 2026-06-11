using UnityEngine;

public sealed class PlayerMeleeHitbox : MonoBehaviour
{
    [SerializeField] private Collider hitboxCollider;

    private PlayerMeleeChair _owner;

    public void Initialize(PlayerMeleeChair owner)
    {
        _owner = owner;

        DisableHitbox();
    }

    public void EnableHitbox()
    {
        hitboxCollider.enabled = true;
    }

    public void DisableHitbox()
    {
        hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_owner == null)
            return;

        _owner.ProcessHit(other);
    }
}