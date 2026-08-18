using Fusion;
using UnityEngine;

public sealed class LobbyMusicController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Music")]
    [SerializeField] private AudioClip musicClip;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    [Header("Synchronization")]
    [SerializeField] private float syncCheckInterval = 2f;
    [SerializeField] private float allowedDrift = 0.15f;

    [Networked]
    private NetworkBool MusicStarted { get; set; }

    [Networked]
    private float MusicStartTime { get; set; }

    private bool _startedLocally;
    private float _nextSyncCheck;

    private void Awake()
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = volume;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            MusicStartTime = Runner.SimulationTime;
            MusicStarted = true;
        }

        TryStartMusic();
    }

    public override void Render()
    {
        if (Runner.IsServer && !Runner.IsClient)
            return;

        if (!_startedLocally)
        {
            TryStartMusic();
            return;
        }

        if (Time.unscaledTime < _nextSyncCheck)
            return;

        _nextSyncCheck =
            Time.unscaledTime +
            syncCheckInterval;

        CorrectDrift();
    }

    private void TryStartMusic()
    {
        if (_startedLocally)
            return;

        if (Runner == null)
            return;

        if (Runner.IsServer && !Runner.IsClient)
            return;

        if (!MusicStarted)
            return;

        if (musicClip == null)
            return;

        if (audioSource == null)
            return;

        float position =
            GetCorrectMusicPosition();

        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.volume = volume;
        audioSource.time = position;

        audioSource.Play();

        _startedLocally = true;

        _nextSyncCheck =
            Time.unscaledTime +
            syncCheckInterval;
    }

    private float GetCorrectMusicPosition()
    {
        if (musicClip == null ||
            musicClip.length <= 0f)
        {
            return 0f;
        }

        float elapsed =
            Runner.SimulationTime -
            MusicStartTime;

        elapsed =
            Mathf.Max(0f, elapsed);

        return Mathf.Repeat(
            elapsed,
            musicClip.length);
    }

    private void CorrectDrift()
    {
        if (audioSource == null)
            return;

        if (!audioSource.isPlaying)
            return;

        if (musicClip == null)
            return;

        float correctPosition =
            GetCorrectMusicPosition();

        float currentPosition =
            audioSource.time;

        float directDifference =
            Mathf.Abs(
                correctPosition -
                currentPosition);

        float wrappedDifference =
            musicClip.length -
            directDifference;

        float difference =
            Mathf.Min(
                directDifference,
                wrappedDifference);

        if (difference <= allowedDrift)
            return;

        audioSource.time =
            correctPosition;
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        StopMusic();
    }

    private void OnDestroy()
    {
        StopMusic();
    }

    private void StopMusic()
    {
        if (audioSource != null)
            audioSource.Stop();

        _startedLocally = false;
    }
}