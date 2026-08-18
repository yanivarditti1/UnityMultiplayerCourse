using Fusion;
using UnityEngine;

public sealed class NetworkBoomboxMusicManager : NetworkBehaviour
{
    public static NetworkBoomboxMusicManager Instance { get; private set; }

    [Header("Songs")]
    [SerializeField] private AudioClip[] songs;

    [Networked]
    private int CurrentSongIndex { get; set; }

    [Networked]
    private float SongStartTime { get; set; }

    [Networked]
    private TickTimer SongTimer { get; set; }

    [Networked]
    private int SongVersion { get; set; }

    public bool IsReady { get; private set; }

    private int[] _songOrder;
    private int _orderPosition;

    private void Awake()
    {
        Instance = this;
    }

    public override void Spawned()
    {
        Instance = this;
        IsReady = true;

        if (!Object.HasStateAuthority)
            return;

        if (songs == null || songs.Length == 0)
        {
            Debug.LogError("[BoomboxMusic] No songs assigned.");
            return;
        }

        BuildRandomOrder();

        _orderPosition = 0;

        StartSong(
            _songOrder[_orderPosition]);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!IsReady)
            return;

        if (!SongTimer.Expired(Runner))
            return;

        PlayNextSong();
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        IsReady = false;

        if (Instance == this)
            Instance = null;
    }

    private void PlayNextSong()
    {
        if (_songOrder == null ||
            _songOrder.Length == 0)
        {
            BuildRandomOrder();
        }

        _orderPosition++;

        if (_orderPosition >= _songOrder.Length)
        {
            int previousSong =
                CurrentSongIndex;

            BuildRandomOrder();

            _orderPosition = 0;

            if (_songOrder.Length > 1 &&
                _songOrder[0] == previousSong)
            {
                int swapIndex =
                    Random.Range(
                        1,
                        _songOrder.Length);

                int temp =
                    _songOrder[0];

                _songOrder[0] =
                    _songOrder[swapIndex];

                _songOrder[swapIndex] =
                    temp;
            }
        }

        StartSong(
            _songOrder[_orderPosition]);
    }

    private void StartSong(
        int songIndex)
    {
        if (songs == null ||
            songs.Length == 0)
            return;

        if (songIndex < 0 ||
            songIndex >= songs.Length)
            return;

        AudioClip clip =
            songs[songIndex];

        if (clip == null)
        {
            PlayNextSong();
            return;
        }

        CurrentSongIndex =
            songIndex;

        SongStartTime =
            Runner.SimulationTime;

        SongTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                clip.length);

        SongVersion++;

        Debug.Log(
            $"[BoomboxMusic] Playing {clip.name} " +
            $"for {clip.length:F1} seconds.");
    }

    private void BuildRandomOrder()
    {
        _songOrder =
            new int[songs.Length];

        for (int i = 0;
             i < songs.Length;
             i++)
        {
            _songOrder[i] = i;
        }

        for (int i =
                 _songOrder.Length - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1);

            int temp =
                _songOrder[i];

            _songOrder[i] =
                _songOrder[randomIndex];

            _songOrder[randomIndex] =
                temp;
        }
    }

    public bool TryGetMusicState(
        out AudioClip clip,
        out float playbackTime,
        out int version)
    {
        clip = null;
        playbackTime = 0f;
        version = -1;

        if (!IsReady)
            return false;

        if (Runner == null)
            return false;

        if (songs == null ||
            songs.Length == 0)
            return false;

        int songIndex =
            CurrentSongIndex;

        if (songIndex < 0 ||
            songIndex >= songs.Length)
            return false;

        clip =
            songs[songIndex];

        if (clip == null ||
            clip.length <= 0f)
            return false;

        float elapsed =
            Runner.SimulationTime -
            SongStartTime;

        playbackTime =
            Mathf.Clamp(
                elapsed,
                0f,
                Mathf.Max(
                    0f,
                    clip.length - 0.01f));

        version =
            SongVersion;

        return true;
    }
}