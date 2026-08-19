using Fusion;
using UnityEngine;

public sealed class ChairBreakSound : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip breakClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private TickTimer _lifeTimer;

    public override void Spawned()
    {
        if (audioSource == null ||
            breakClip == null)
            return;

        audioSource.clip = breakClip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1f;

        audioSource.Play();

        if (Object.HasStateAuthority)
        {
            _lifeTimer =
                TickTimer.CreateFromSeconds(
                    Runner,
                    breakClip.length + 0.2f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!_lifeTimer.Expired(Runner))
            return;

        Runner.Despawn(Object);
    }
}