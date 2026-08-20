using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public sealed class MatchScoreManager : NetworkBehaviour
{
    public static MatchScoreManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private int killsToWin = 3;
    [SerializeField] private float matchDurationSeconds = 300f;

    [Header("End Match Settings")]
    [SerializeField] private float endScreenDuration = 3f;

    [Networked, Capacity(16)]
    private NetworkDictionary<PlayerRef, int> PlayerKills => default;

    [Networked]
    private TickTimer MatchTimer { get; set; }

    [Networked]
    private TickTimer EndScreenTimer { get; set; }

    [Networked]
    private NetworkBool MatchEnded { get; set; }

    [Networked]
    private PlayerRef Winner { get; set; }

    public event Action<int, int> LocalScoreChanged;
    public event Action<float> TimerChanged;
    public event Action<PlayerRef, int> MatchFinished;
    public event Action<float> EndCountdownChanged;

    // This is the event pls help
    public event Action ReturnToLobbyRequested;

    private int lastDisplayedMatchSecond = -1;
    private int lastDisplayedEndSecond = -1;
    private int lastLocalKills = -1;

    private bool matchFinishedEventRaised;
    private bool returnToLobbyEventRaised;

    public int KillsToWin => killsToWin;
    public bool HasMatchEnded => MatchEnded;

    public override void Spawned()
    {
        Instance = this;

        if (Object.HasStateAuthority)
        {
            StartMatch();
        }

        RefreshLocalScore();
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void StartMatch()
    {
        PlayerKills.Clear();

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            PlayerKills.Set(player, 0);
        }

        Winner = PlayerRef.None;
        MatchEnded = false;

        returnToLobbyEventRaised = false;
        matchFinishedEventRaised = false;

        MatchTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                matchDurationSeconds);

        EndScreenTimer = TickTimer.None;

        Debug.Log(
            $"[Match] Started. " +
            $"Kills to win: {killsToWin}. " +
            $"Duration: {matchDurationSeconds}s.");
    }

    public void RegisterKill(PlayerRef killer)
    {
        if (!Object.HasStateAuthority)
            return;

        if (MatchEnded)
            return;

        if (killer == PlayerRef.None)
            return;

        int currentKills = 0;

        if (PlayerKills.TryGet(killer, out int existingKills))
        {
            currentKills = existingKills;
        }

        currentKills++;

        PlayerKills.Set(killer, currentKills);

        Debug.Log(
            $"[Match] Player {killer.PlayerId} " +
            $"has {currentKills}/{killsToWin} kills.");

        if (currentKills >= killsToWin)
        {
            FinishMatch(killer, currentKills);
        }
    }

    public void RegisterPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (player == PlayerRef.None)
            return;

        if (PlayerKills.ContainsKey(player))
            return;

        PlayerKills.Set(player, 0);

        Debug.Log(
            $"[Match] Registered Player {player.PlayerId}");
    }

    public void RemovePlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        if (!PlayerKills.ContainsKey(player))
            return;

        PlayerKills.Remove(player);

        Debug.Log(
            $"[Match] Removed Player {player.PlayerId}");
    }

    public int GetKills(PlayerRef player)
    {
        if (PlayerKills.TryGet(
            player,
            out int kills))
        {
            return kills;
        }

        return 0;
    }

    private void FinishMatch(PlayerRef winner, int winnerKills)
    {
        if (!Object.HasStateAuthority)
            return;

        if (MatchEnded)
            return;

        MatchEnded = true;
        Winner = winner;

        EndScreenTimer = TickTimer.CreateFromSeconds(Runner, endScreenDuration);
        
        var summaryJson = BuildMatchSummaryJson(winner, winnerKills);
        
        var currentGameMode = NetworkMatchManager.Instance != null 
            ? NetworkMatchManager.Instance.SelectedGameMode.ToString()
            : "Unknown";

        Debug.Log(
            $"[Match] Player {winner.PlayerId} won a {currentGameMode} in {matchDurationSeconds}s. " +
            $"with {winnerKills} kills.");

        RPC_MatchFinished(winner, winnerKills);
        RPC_MatchSummaryReceived(summaryJson);
    }

    private void FinishMatchFromTimer()
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerRef highestPlayer = PlayerRef.None;
        int highestKills = -1;

        foreach (
            KeyValuePair<PlayerRef, int> entry
            in PlayerKills)
        {
            if (entry.Value <= highestKills)
                continue;

            highestKills = entry.Value;
            highestPlayer = entry.Key;
        }

        if (highestPlayer == PlayerRef.None)
        {
            Debug.LogWarning(
                "[Match] Timer ended but no players were found.");

            return;
        }

        FinishMatch(
            highestPlayer,
            highestKills);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MatchFinished(
        PlayerRef winner,
        int winnerKills)
    {
        if (matchFinishedEventRaised)
            return;

        matchFinishedEventRaised = true;

        MatchFinished?.Invoke(
            winner,
            winnerKills);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MatchSummaryReceived(string summaryJson)
    {
        MatchSummaryData matchSummary = JsonUtility.FromJson<MatchSummaryData>(summaryJson);
        
        Debug.Log(
            $"[Match] Match summary received: " +
            $"{matchSummary.WinnerNickname} " +
            $"won a {matchSummary.GameMode} in {matchSummary.MatchDurationSeconds}s. " +
            $"with {matchSummary.WinnerKills} kills.");
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!MatchEnded)
        {
            if (MatchTimer.Expired(Runner))
            {
                FinishMatchFromTimer();
            }

            return;
        }

        if (!EndScreenTimer.Expired(Runner))
            return;

        if (returnToLobbyEventRaised)
            return;

        returnToLobbyEventRaised = true;

        Debug.Log(
            "[Match] End countdown finished. " +
            "Requesting return to lobby.");

        ReturnToLobbyRequested?.Invoke();
    }

    public override void Render()
    {
        UpdateMatchTimerDisplay();
        UpdateEndCountdownDisplay();
        RefreshLocalScore();
    }

    private void UpdateMatchTimerDisplay()
    {
        if (Runner == null)
            return;

        if (MatchEnded)
            return;

        float remainingTime =
            MatchTimer.RemainingTime(Runner) ?? 0f;

        int displayedSecond =
            Mathf.CeilToInt(remainingTime);

        if (displayedSecond ==
            lastDisplayedMatchSecond)
        {
            return;
        }

        lastDisplayedMatchSecond =
            displayedSecond;

        TimerChanged?.Invoke(
            remainingTime);
    }

    private void UpdateEndCountdownDisplay()
    {
        if (!MatchEnded)
            return;

        if (Runner == null)
            return;

        float remainingTime =
            EndScreenTimer.RemainingTime(Runner) ?? 0f;

        int displayedSecond = Mathf.CeilToInt(remainingTime);

        if (displayedSecond == lastDisplayedEndSecond)
        {
            return;
        }

        lastDisplayedEndSecond =
            displayedSecond;

        EndCountdownChanged?.Invoke(remainingTime);
    }

    private void RefreshLocalScore()
    {
        if (Runner == null)
            return;

        PlayerRef localPlayer = Runner.LocalPlayer;

        if (localPlayer == PlayerRef.None)
            return;

        int kills =
            GetKills(localPlayer);

        if (kills == lastLocalKills)
            return;

        lastLocalKills = kills;

        LocalScoreChanged?.Invoke(kills, killsToWin);
    }

    private MatchSummaryData BuildMatchSummary(PlayerRef winner, int winnerKills)
    {
        var currentGameMode = NetworkMatchManager.Instance != null 
            ? NetworkMatchManager.Instance.SelectedGameMode.ToString()
                : "Unknown";

        return new MatchSummaryData
        {
            WinnerNickname = PlayerNicknameUtility.GetNickname(winner),
            WinnerKills = winnerKills,
            MatchDurationSeconds = matchDurationSeconds,
            GameMode = currentGameMode
        };
    }

    private string BuildMatchSummaryJson(PlayerRef winner, int winnerKills)
    {
        MatchSummaryData matchSummary = BuildMatchSummary(winner, winnerKills);
        return JsonUtility.ToJson(matchSummary);   
    }

    [ContextMenu("DEBUG - Give Local Player Kill")]
    private void DebugGiveLocalPlayerKill()
    {
        if (!Application.isPlaying)
            return;

        if (!Object.HasStateAuthority)
            return;

        RegisterKill(
            Runner.LocalPlayer);
    }
}