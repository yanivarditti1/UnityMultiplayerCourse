using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class ConquestManager : NetworkBehaviour
{
    public static ConquestManager Instance { get; private set; }

    [Header("Tickets")]
    [SerializeField] private int startingTickets = 100;
    [SerializeField] private int ticketLossPerDeath = 1;
    [SerializeField] private int ticketDrainAmount = 1;
    [SerializeField] private float ticketDrainInterval = 5f;

    [Header("Teams")]
    [SerializeField] private Color redTeamColor = Color.red;
    [SerializeField] private Color blueTeamColor = Color.blue;

    [Networked]
    public int RedTickets { get; private set; }

    [Networked]
    public int BlueTickets { get; private set; }

    [Networked]
    public NetworkBool MatchEnded { get; private set; }

    [Networked]
    public ConquestTeam WinningTeam { get; private set; }

    [Networked]
    private TickTimer TicketDrainTimer { get; set; }

    public bool IsReady { get; private set; }

    private readonly Dictionary<PlayerRef, ConquestTeam> _assignedTeams = new();
    private ConquestCapturePoint[] _capturePoints;
    private ConquestSpawnPoint[] _spawnPoints;
    private float _nextTeamRequestTime;

    public override void Spawned()
    {
        Instance = this;
        IsReady = true;
        RefreshSceneObjects();

        if (!Object.HasStateAuthority)
            return;

        RedTickets = startingTickets;
        BlueTickets = startingTickets;
        MatchEnded = false;
        WinningTeam = ConquestTeam.None;
        TicketDrainTimer = TickTimer.CreateFromSeconds(Runner, ticketDrainInterval);

        foreach (KeyValuePair<PlayerRef, PlayerManager> entry in PlayerManager.Registry)
        {
            if (entry.Value.Team != ConquestTeam.None)
                _assignedTeams[entry.Key] = entry.Value.Team;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        IsReady = false;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsReady ||
            Runner == null ||
            PlayerManager.Local == null ||
            PlayerManager.Local.Team != ConquestTeam.None ||
            Time.unscaledTime < _nextTeamRequestTime)
            return;

        _nextTeamRequestTime = Time.unscaledTime + 1f;
        RequestTeam(Runner.LocalPlayer);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || MatchEnded)
            return;

        if (!TicketDrainTimer.Expired(Runner))
            return;

        DrainTicketsFromObjectiveControl();
        TicketDrainTimer = TickTimer.CreateFromSeconds(Runner, ticketDrainInterval);
    }

    public void RequestTeam(PlayerRef player)
    {
        if (!IsReady)
            return;

        RPC_RequestTeam(player);
    }

    public void ReportDeath(PlayerRef victim, PlayerRef attacker)
    {
        if (!IsReady)
            return;

        RPC_ReportDeath(victim, attacker);
    }

    public ConquestTeam GetTeam(PlayerRef player)
    {
        if (PlayerManager.Registry.TryGetValue(player, out PlayerManager playerManager) &&
            playerManager.Team != ConquestTeam.None)
            return playerManager.Team;

        if (_assignedTeams.TryGetValue(player, out ConquestTeam assignedTeam))
            return assignedTeam;

        return ConquestTeam.None;
    }

    public bool AreTeammates(PlayerRef first, PlayerRef second)
    {
        ConquestTeam firstTeam = GetTeam(first);
        ConquestTeam secondTeam = GetTeam(second);

        return firstTeam != ConquestTeam.None &&
               firstTeam == secondTeam;
    }

    public bool TryGetSpawnPoint(PlayerRef player, out Transform spawnPoint)
    {
        return TryGetSpawnPoint(GetTeam(player), out spawnPoint);
    }

    public bool TryGetSpawnPoint(ConquestTeam team, out Transform spawnPoint)
    {
        spawnPoint = null;

        if (team == ConquestTeam.None)
            return false;

        if (_spawnPoints == null || _spawnPoints.Length == 0)
            RefreshSceneObjects();

        List<ConquestSpawnPoint> validSpawns = new();

        foreach (ConquestSpawnPoint candidate in _spawnPoints)
        {
            if (candidate != null && candidate.Team == team)
                validSpawns.Add(candidate);
        }

        if (validSpawns.Count == 0)
            return false;

        spawnPoint = validSpawns[Random.Range(0, validSpawns.Count)].transform;
        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTeam(PlayerRef player)
    {
        if (!_assignedTeams.TryGetValue(player, out ConquestTeam team))
        {
            int redCount = 0;
            int blueCount = 0;

            foreach (ConquestTeam assignedTeam in _assignedTeams.Values)
            {
                if (assignedTeam == ConquestTeam.Red)
                    redCount++;
                else if (assignedTeam == ConquestTeam.Blue)
                    blueCount++;
            }

            team = redCount <= blueCount
                ? ConquestTeam.Red
                : ConquestTeam.Blue;

            _assignedTeams[player] = team;
        }

        RPC_AssignTeam(player, team);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AssignTeam([RpcTarget] PlayerRef targetPlayer, ConquestTeam team)
    {
        if (Runner.LocalPlayer != targetPlayer || PlayerManager.Local == null)
            return;

        PlayerManager.Local.SetConquestTeam(team);
        PlayerManager.Local.SetNameColor(
            team == ConquestTeam.Red
                ? redTeamColor
                : blueTeamColor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportDeath(PlayerRef victim, PlayerRef attacker)
    {
        if (MatchEnded)
            return;

        ConquestTeam victimTeam = GetTeam(victim);

        if (victimTeam == ConquestTeam.Red)
            RedTickets = Mathf.Max(0, RedTickets - ticketLossPerDeath);
        else if (victimTeam == ConquestTeam.Blue)
            BlueTickets = Mathf.Max(0, BlueTickets - ticketLossPerDeath);

        CheckForWinner();
    }

    private void DrainTicketsFromObjectiveControl()
    {
        if (_capturePoints == null || _capturePoints.Length == 0)
            RefreshSceneObjects();

        int redOwned = 0;
        int blueOwned = 0;

        foreach (ConquestCapturePoint capturePoint in _capturePoints)
        {
            if (capturePoint == null || !capturePoint.IsReady)
                continue;

            if (capturePoint.Owner == ConquestTeam.Red)
                redOwned++;
            else if (capturePoint.Owner == ConquestTeam.Blue)
                blueOwned++;
        }

        if (redOwned > blueOwned)
            BlueTickets = Mathf.Max(0, BlueTickets - ticketDrainAmount);
        else if (blueOwned > redOwned)
            RedTickets = Mathf.Max(0, RedTickets - ticketDrainAmount);

        CheckForWinner();
    }

    private void CheckForWinner()
    {
        if (RedTickets > 0 && BlueTickets > 0)
            return;

        MatchEnded = true;

        if (RedTickets <= 0 && BlueTickets <= 0)
            WinningTeam = ConquestTeam.None;
        else if (RedTickets <= 0)
            WinningTeam = ConquestTeam.Blue;
        else
            WinningTeam = ConquestTeam.Red;
    }

    private void RefreshSceneObjects()
    {
        _capturePoints = FindObjectsByType<ConquestCapturePoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        _spawnPoints = FindObjectsByType<ConquestSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }
}
