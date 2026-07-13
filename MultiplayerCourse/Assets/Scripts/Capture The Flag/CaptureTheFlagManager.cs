using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class CaptureTheFlagManager : NetworkBehaviour
{
    public static CaptureTheFlagManager Instance { get; private set; }

    [Header("Match")]
    [SerializeField] private int scoreToWin = 3;
    [SerializeField, Range(0.1f, 1f)]
    private float carrierSpeedMultiplier = 0.65f;

    [Header("Teams")]
    [SerializeField] private Color redTeamColor = Color.red;
    [SerializeField] private Color blueTeamColor = Color.blue;

    [Header("Scene References")]
    [SerializeField] private CaptureTheFlagFlag flag;
    [SerializeField] private ConquestSpawnPoint[] spawnPoints;

    [Networked]
    public int RedScore { get; private set; }

    [Networked]
    public int BlueScore { get; private set; }

    [Networked]
    public NetworkBool MatchEnded { get; private set; }

    [Networked]
    public ConquestTeam WinningTeam { get; private set; }

    public bool IsReady { get; private set; }
    public float CarrierSpeedMultiplier => carrierSpeedMultiplier;
    public CaptureTheFlagFlag Flag => flag;

    private readonly Dictionary<PlayerRef, ConquestTeam> assignedTeams = new();
    private float nextTeamRequestTime;

    public override void Spawned()
    {
        Instance = this;
        IsReady = true;

        if (!Object.HasStateAuthority)
            return;

        RedScore = 0;
        BlueScore = 0;
        MatchEnded = false;
        WinningTeam = ConquestTeam.None;

        foreach (KeyValuePair<PlayerRef, PlayerManager> entry
                 in PlayerManager.Registry)
        {
            if (entry.Value  &&
                entry.Value.Team != ConquestTeam.None)
            {
                assignedTeams[entry.Key] = entry.Value.Team;
            }
        }
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        IsReady = false;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsReady ||
            !Runner  ||
            !PlayerManager.Local ||
            PlayerManager.Local.Team != ConquestTeam.None ||
            Time.unscaledTime < nextTeamRequestTime)
        {
            return;
        }

        nextTeamRequestTime = Time.unscaledTime + 1f;
        RequestTeam(Runner.LocalPlayer);
    }

    private void RequestTeam(PlayerRef player)
    {
        RPC_RequestTeam(player);
    }

    public ConquestTeam GetTeam(PlayerRef player)
    {
        if (PlayerManager.Registry.TryGetValue(
                player,
                out PlayerManager playerManager) &&
            playerManager &&
            playerManager.Team != ConquestTeam.None)
        {
            return playerManager.Team;
        }

        if (assignedTeams.TryGetValue(
                player,
                out ConquestTeam assignedTeam))
        {
            return assignedTeam;
        }

        return ConquestTeam.None;
    }

    public bool AreTeammates(
        PlayerRef first,
        PlayerRef second)
    {
        ConquestTeam firstTeam = GetTeam(first);
        ConquestTeam secondTeam = GetTeam(second);

        return firstTeam != ConquestTeam.None &&
               firstTeam == secondTeam;
    }

    public bool IsCarrier(PlayerRef player)
    {
        return flag &&
               flag.IsReady &&
               flag.Carrier == player;
    }

    public bool TryGetSpawnPoint(
        PlayerRef player,
        out Transform spawnPoint)
    {
        return TryGetSpawnPoint(
            GetTeam(player),
            out spawnPoint);
    }

    public bool TryGetSpawnPoint(
        ConquestTeam team,
        out Transform spawnPoint)
    {
        spawnPoint = null;

        if (team == ConquestTeam.None ||
            spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            return false;
        }

        int matchingSpawnCount = 0;

        foreach (ConquestSpawnPoint candidate in spawnPoints)
        {
            if (candidate != null &&
                candidate.Team == team)
            {
                matchingSpawnCount++;
            }
        }

        if (matchingSpawnCount == 0)
            return false;

        int selectedIndex = Random.Range(
            0,
            matchingSpawnCount);

        foreach (ConquestSpawnPoint candidate in spawnPoints)
        {
            if (!candidate  ||
                candidate.Team != team)
            {
                continue;
            }

            if (selectedIndex == 0)
            {
                spawnPoint = candidate.transform;
                return true;
            }

            selectedIndex--;
        }

        return false;
    }

    public void RequestDrop(PlayerRef player)
    {
        if (!IsReady)
            return;

        RPC_RequestDrop(player);
    }

    public void ReportDeath(PlayerRef player)
    {
        if (!IsReady)
            return;

        RPC_ReportDeath(player);
    }

    public void TryScore(
        PlayerRef carrier,
        ConquestTeam baseTeam)
    {
        if (!IsReady)
            return;

        RPC_TryScore(carrier, baseTeam);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestTeam(PlayerRef player)
    {
        if (!assignedTeams.TryGetValue(
                player,
                out ConquestTeam team))
        {
            int redCount = 0;
            int blueCount = 0;

            foreach (ConquestTeam assignedTeam
                     in assignedTeams.Values)
            {
                if (assignedTeam == ConquestTeam.Red)
                    redCount++;
                else if (assignedTeam == ConquestTeam.Blue)
                    blueCount++;
            }

            team = redCount <= blueCount
                ? ConquestTeam.Red
                : ConquestTeam.Blue;

            assignedTeams[player] = team;
        }

        RPC_AssignTeam(player, team);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_AssignTeam(
        [RpcTarget] PlayerRef targetPlayer,
        ConquestTeam team)
    {
        if (Runner.LocalPlayer != targetPlayer ||
            !PlayerManager.Local )
        {
            return;
        }

        PlayerManager.Local.SetConquestTeam(team);

        PlayerManager.Local.SetNameColor(
            team == ConquestTeam.Red
                ? redTeamColor
                : blueTeamColor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDrop(PlayerRef player)
    {
        if (MatchEnded ||
            !flag  ||
            !flag.IsReady ||
            flag.Carrier != player)
        {
            return;
        }

        flag.DropAtPlayer(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ReportDeath(PlayerRef player)
    {
        if (!flag  ||
            !flag.IsReady ||
            flag.Carrier != player)
        {
            return;
        }

        flag.DropAtPlayer(player);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TryScore(
        PlayerRef carrier,
        ConquestTeam baseTeam)
    {
        if (MatchEnded ||
            !flag  ||
            !flag.IsReady ||
            flag.Carrier != carrier ||
            GetTeam(carrier) != baseTeam)
        {
            return;
        }

        if (baseTeam == ConquestTeam.Red)
        {
            RedScore++;
        }
        else if (baseTeam == ConquestTeam.Blue)
        {
            BlueScore++;
        }
        else
        {
            return;
        }

        flag.ReturnHome();
        CheckForWinner();
    }

    private void CheckForWinner()
    {
        if (RedScore < scoreToWin &&
            BlueScore < scoreToWin)
        {
            return;
        }

        MatchEnded = true;

        if (RedScore >= scoreToWin &&
            BlueScore >= scoreToWin)
        {
            WinningTeam = ConquestTeam.None;
        }
        else if (RedScore >= scoreToWin)
        {
            WinningTeam = ConquestTeam.Red;
        }
        else
        {
            WinningTeam = ConquestTeam.Blue;
        }

        if (flag  && flag.IsReady)
            flag.ReturnHome();
    }
}