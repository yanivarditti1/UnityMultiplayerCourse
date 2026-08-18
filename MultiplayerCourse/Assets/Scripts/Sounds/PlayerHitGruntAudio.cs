using Fusion;
using UnityEngine;

public sealed class PlayerHitGruntAudio : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private AudioSource audioSource;

    [Header("Hit Grunts")]
    [SerializeField] private AudioClip[] gruntClips;

    [Header("Death")]
    [SerializeField] private AudioClip deathClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float gruntVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float deathVolume = 1f;

    [SerializeField] private float minPitch = 0.95f;
    [SerializeField] private float maxPitch = 1.05f;

    [SerializeField] private float minimumDelayBetweenGrunts = 0.2f;

    private int _previousHealth = -1;
    private float _lastGruntTime;
    private bool _deathSoundPlayed;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        if (playerHealth == null ||
            audioSource == null)
            return;

        _previousHealth =
            playerHealth.CurrentHealth;

        _deathSoundPlayed = false;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        playerHealth.HealthChanged.AddListener(
            HandleHealthChanged);
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (!Object.HasInputAuthority)
            return;

        if (playerHealth != null)
        {
            playerHealth.HealthChanged.RemoveListener(
                HandleHealthChanged);
        }

        if (audioSource != null)
            audioSource.Stop();
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        if (_previousHealth < 0)
        {
            _previousHealth = currentHealth;
            return;
        }

        bool tookDamage =
            currentHealth < _previousHealth;

        bool died =
            currentHealth <= 0 &&
            _previousHealth > 0;

        _previousHealth =
            currentHealth;

        if (!tookDamage)
        {
            if (currentHealth > 0)
                _deathSoundPlayed = false;

            return;
        }

        if (died)
        {
            PlayDeathSound();
            return;
        }

        PlayRandomGrunt();
    }

    private void PlayRandomGrunt()
    {
        if (gruntClips == null ||
            gruntClips.Length == 0)
            return;

        if (Time.time <
            _lastGruntTime +
            minimumDelayBetweenGrunts)
            return;

        _lastGruntTime =
            Time.time;

        int randomIndex =
            Random.Range(
                0,
                gruntClips.Length);

        AudioClip selectedClip =
            gruntClips[randomIndex];

        if (selectedClip == null)
            return;

        audioSource.pitch =
            Random.Range(
                minPitch,
                maxPitch);

        audioSource.volume =
            gruntVolume;

        audioSource.PlayOneShot(
            selectedClip);
    }

    private void PlayDeathSound()
    {
        if (_deathSoundPlayed)
            return;

        if (deathClip == null)
            return;

        _deathSoundPlayed = true;

        audioSource.Stop();

        audioSource.pitch = 1f;
        audioSource.volume =
            deathVolume;

        audioSource.PlayOneShot(
            deathClip);
    }
}