using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class ConquestCapturePoint : NetworkBehaviour
{
    [Header("Capture")]
    [SerializeField] private string pointName = "A";
    [SerializeField] private float captureRadius = 5f;
    [SerializeField] private float captureSpeed = 0.2f;
    [SerializeField] private LayerMask playerLayers = ~0;

    [Header("Visual")]
    [SerializeField] private Renderer indicatorRenderer;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color redColor = Color.red;
    [SerializeField] private Color blueColor = Color.blue;
    [SerializeField] private Color contestedColor = Color.yellow;

    [Header("Smoke")]
    [SerializeField] private ParticleSystem smokeParticles;

    [Networked, OnChangedRender(nameof(RefreshVisual))]
    public ConquestTeam Owner { get; private set; }

    [Networked, OnChangedRender(nameof(RefreshVisual))]
    public float CaptureProgress { get; private set; }

    [Networked, OnChangedRender(nameof(RefreshVisual))]
    public NetworkBool IsContested { get; private set; }

    public string PointName => pointName;
    public bool IsReady { get; private set; }

    private readonly Collider[] _overlapResults = new Collider[32];
    private readonly HashSet<PlayerRef> _redPlayers = new();
    private readonly HashSet<PlayerRef> _bluePlayers = new();

    private ConquestTeam _lastSmokeOwner = ConquestTeam.None;

    public override void Spawned()
    {
        IsReady = true;
        RefreshVisual();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsReady = false;

        if (smokeParticles != null)
        {
            smokeParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        ConquestManager manager = ConquestManager.Instance;

        if (manager == null || !manager.IsReady || manager.MatchEnded)
            return;

        CountPlayers(manager);

        if (_redPlayers.Count > 0 && _bluePlayers.Count > 0)
        {
            IsContested = true;
            return;
        }

        IsContested = false;

        if (_redPlayers.Count == 0 && _bluePlayers.Count == 0)
            return;

        float direction = _redPlayers.Count > 0 ? 1f : -1f;

        int playerCount = _redPlayers.Count > 0
            ? _redPlayers.Count
            : _bluePlayers.Count;

        CaptureProgress = Mathf.Clamp(
            CaptureProgress +
            direction * captureSpeed * playerCount * Runner.DeltaTime,
            -1f,
            1f);

        UpdateOwner();
    }

    private void CountPlayers(ConquestManager manager)
    {
        _redPlayers.Clear();
        _bluePlayers.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            captureRadius,
            _overlapResults,
            playerLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapResults[i];

            if (hit == null)
                continue;

            PlayerDamageReceiver receiver =
                hit.GetComponentInParent<PlayerDamageReceiver>();

            PlayerHealth health =
                hit.GetComponentInParent<PlayerHealth>();

            if (receiver == null || health == null || health.IsDead)
                continue;

            PlayerRef player = receiver.Owner;
            ConquestTeam team = manager.GetTeam(player);

            if (team == ConquestTeam.Red)
                _redPlayers.Add(player);
            else if (team == ConquestTeam.Blue)
                _bluePlayers.Add(player);
        }
    }

    private void UpdateOwner()
    {
        if (CaptureProgress >= 1f)
        {
            Owner = ConquestTeam.Red;
            return;
        }

        if (CaptureProgress <= -1f)
        {
            Owner = ConquestTeam.Blue;
            return;
        }

        if (Owner == ConquestTeam.Red && CaptureProgress <= 0f)
            Owner = ConquestTeam.None;
        else if (Owner == ConquestTeam.Blue && CaptureProgress >= 0f)
            Owner = ConquestTeam.None;
    }

    private void RefreshVisual()
    {
        Color visualColor = GetVisualColor();

        if (indicatorRenderer != null)
            indicatorRenderer.material.color = visualColor;

        RefreshSmoke();
    }

    private Color GetVisualColor()
    {
        if (IsContested)
            return contestedColor;

        if (Owner == ConquestTeam.Red)
            return redColor;

        if (Owner == ConquestTeam.Blue)
            return blueColor;

        if (CaptureProgress > 0f)
            return Color.Lerp(neutralColor, redColor, CaptureProgress);

        if (CaptureProgress < 0f)
            return Color.Lerp(neutralColor, blueColor, -CaptureProgress);

        return neutralColor;
    }

    private void RefreshSmoke()
    {
        if (smokeParticles == null)
            return;

        if (Owner == ConquestTeam.None)
        {
            smokeParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            _lastSmokeOwner = ConquestTeam.None;
            return;
        }

        Color smokeColor = Owner == ConquestTeam.Red
            ? redColor
            : blueColor;

        ParticleSystem.MainModule main = smokeParticles.main;
        main.startColor = smokeColor;

        if (_lastSmokeOwner != Owner)
        {
            smokeParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            smokeParticles.Play();
            _lastSmokeOwner = Owner;
            return;
        }

        if (!smokeParticles.isPlaying)
            smokeParticles.Play();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}