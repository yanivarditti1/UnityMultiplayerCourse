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

    [Networked, Capacity(16)]
    private NetworkDictionary<PlayerRef, int> PlayerKills => default;

    [Networked]
    private TickTimer MatchTimer { get; set; }

    [Networked]
    private NetworkBool MatchEnded { get; set; }

    [Networked]
    private PlayerRef Winner { get; set; }

    public event Action<int, int> LocalScoreChanged;
    public event Action<float> TimerChanged;
    public event Action<PlayerRef, int> MatchFinished;

    private int lastDisplayedSecond = -1;
    private int lastLocalKills = -1;
    private bool matchFinishedEventRaised;

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

        MatchTimer = TickTimer.CreateFromSeconds(
            Runner,
            matchDurationSeconds);

        Debug.Log(
            $"[Match] Started. " +
            $"Kills to win: {killsToWin}, " +
            $"Duration: {matchDurationSeconds} seconds");
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

        PlayerKills.Set(
            killer,
            currentKills);

        Debug.Log(
            $"[Match] Player {killer.PlayerId} " +
            $"now has {currentKills}/{killsToWin} kills.");

        if (currentKills >= killsToWin)
        {
            FinishMatch(
                killer,
                currentKills);
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
        if (PlayerKills.TryGet(player, out int kills))
        {
            return kills;
        }

        return 0;
    }

    private void FinishMatch(
        PlayerRef winner,
        int winnerKills)
    {
        if (!Object.HasStateAuthority)
            return;

        if (MatchEnded)
            return;

        MatchEnded = true;
        Winner = winner;

        Debug.Log(
            $"[Match] Player {winner.PlayerId} won " +
            $"with {winnerKills} kills.");

        RPC_MatchFinished(
            winner,
            winnerKills);
    }

    private void FinishMatchFromTimer()
    {
        if (!Object.HasStateAuthority)
            return;

        PlayerRef highestPlayer = PlayerRef.None;
        int highestKills = -1;

        foreach (KeyValuePair<PlayerRef, int> entry in PlayerKills)
        {
            if (entry.Value <= highestKills)
                continue;

            highestKills = entry.Value;
            highestPlayer = entry.Key;
        }

        if (highestPlayer == PlayerRef.None)
        {
            Debug.LogWarning(
                "[Match] Timer expired, but no players were found.");

            return;
        }

        FinishMatch(
            highestPlayer,
            highestKills);
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All)]
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

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (MatchEnded)
            return;

        if (MatchTimer.Expired(Runner))
        {
            FinishMatchFromTimer();
        }
    }

    public override void Render()
    {
        UpdateTimerDisplay();
        RefreshLocalScore();
    }

    private void UpdateTimerDisplay()
    {
        if (Runner == null)
            return;

        float remainingTime =
            MatchTimer.RemainingTime(Runner) ?? 0f;

        int displayedSecond =
            Mathf.CeilToInt(remainingTime);

        if (displayedSecond == lastDisplayedSecond)
            return;

        lastDisplayedSecond = displayedSecond;

        TimerChanged?.Invoke(
            remainingTime);
    }

    private void RefreshLocalScore()
    {
        if (Runner == null)
            return;

        PlayerRef localPlayer =
            Runner.LocalPlayer;

        if (localPlayer == PlayerRef.None)
            return;

        int kills = GetKills(localPlayer);

        if (kills == lastLocalKills)
            return;

        lastLocalKills = kills;

        LocalScoreChanged?.Invoke(
            kills,
            killsToWin);
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