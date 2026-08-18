using UnityEngine;

public sealed class NetworkBoomboxSpeaker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    [SerializeField] private float syncCheckInterval = 1f;
    [SerializeField] private float allowedDrift = 0.15f;

    private AudioClip _currentClip;
    private int _currentSongVersion = -1;
    private float _nextSyncCheck;

    private void Awake()
    {
        if (audioSource == null)
            return;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume = volume;
    }

    private void Update()
    {
        NetworkBoomboxMusicManager manager =
            NetworkBoomboxMusicManager.Instance;

        if (manager == null)
            return;

        if (!manager.IsReady)
            return;

        if (!manager.TryGetMusicState(
                out AudioClip clip,
                out float correctTime,
                out int version))
        {
            return;
        }

        if (_currentSongVersion != version ||
            _currentClip != clip)
        {
            StartSong(
                clip,
                correctTime,
                version);

            return;
        }

        if (Time.unscaledTime <
            _nextSyncCheck)
            return;

        _nextSyncCheck =
            Time.unscaledTime +
            syncCheckInterval;

        CorrectDrift(
            correctTime);
    }

    private void StartSong(
        AudioClip clip,
        float playbackTime,
        int version)
    {
        if (audioSource == null)
            return;

        audioSource.Stop();

        _currentClip =
            clip;

        _currentSongVersion =
            version;

        audioSource.clip =
            clip;

        audioSource.volume =
            volume;

        audioSource.loop =
            false;

        audioSource.time =
            Mathf.Clamp(
                playbackTime,
                0f,
                Mathf.Max(
                    0f,
                    clip.length - 0.01f));

        audioSource.Play();

        _nextSyncCheck =
            Time.unscaledTime +
            syncCheckInterval;
    }

    private void CorrectDrift(
        float correctTime)
    {
        if (audioSource == null)
            return;

        if (_currentClip == null)
            return;

        if (!audioSource.isPlaying)
        {
            audioSource.time =
                correctTime;

            audioSource.Play();

            return;
        }

        float difference =
            Mathf.Abs(
                audioSource.time -
                correctTime);

        if (difference <=
            allowedDrift)
            return;

        audioSource.time =
            correctTime;
    }
}