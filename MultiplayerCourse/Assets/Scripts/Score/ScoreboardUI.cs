using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ScoreboardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform scoreboardPanel;
    [SerializeField] private Transform rowsParent;
    [SerializeField] private ScoreboardRowUI rowPrefab;

    [Header("Input")]
    [SerializeField] private InputActionReference scoreboardAction;

    [Header("Settings")]
    [SerializeField] private int maxDisplayedPlayers = 10;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.2f;
    [SerializeField] private float hideDuration = 0.15f;
    [SerializeField] private float hiddenScale = 0.9f;

    private readonly Dictionary<PlayerRef, ScoreboardRowUI>
        _rows = new();

    private Tween _panelTween;

    private bool _isVisible;

    private void Awake()
    {
        HideInstant();
    }

    private void OnEnable()
    {
        if (scoreboardAction != null)
        {
            scoreboardAction.action.Enable();

            scoreboardAction.action.started +=
                HandleTabPressed;

            scoreboardAction.action.canceled +=
                HandleTabReleased;
        }

        PlayerMatchStats.OnPlayerListChanged +=
            RefreshPlayerList;

        PlayerMatchStats.OnAnyStatsChanged +=
            HandleStatsChanged;

        PlayerManager.OnAnyNicknameChanged +=
            HandleNicknameChanged;
    }

    private void OnDisable()
    {
        if (scoreboardAction != null)
        {
            scoreboardAction.action.started -=
                HandleTabPressed;

            scoreboardAction.action.canceled -=
                HandleTabReleased;

            scoreboardAction.action.Disable();
        }

        PlayerMatchStats.OnPlayerListChanged -=
            RefreshPlayerList;

        PlayerMatchStats.OnAnyStatsChanged -=
            HandleStatsChanged;

        PlayerManager.OnAnyNicknameChanged -=
            HandleNicknameChanged;

        _panelTween?.Kill();
    }

    

    private void HandleTabPressed(
        InputAction.CallbackContext context)
    {
        ShowScoreboard();
    }

    private void HandleTabReleased(
        InputAction.CallbackContext context)
    {
        HideScoreboard();
    }

    

    private void ShowScoreboard()
    {
        if (_isVisible)
            return;

        _isVisible = true;

        RefreshPlayerList();

        _panelTween?.Kill();

        scoreboardPanel.gameObject.SetActive(true);

        scoreboardPanel.localScale =
            Vector3.one * hiddenScale;

        _panelTween =
            scoreboardPanel
                .DOScale(
                    Vector3.one,
                    showDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
    }

    private void HideScoreboard()
    {
        if (!_isVisible)
            return;

        _isVisible = false;

        _panelTween?.Kill();

        _panelTween =
            scoreboardPanel
                .DOScale(
                    Vector3.one * hiddenScale,
                    hideDuration)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (!_isVisible)
                        scoreboardPanel.gameObject.SetActive(false);
                });
    }

    private void HideInstant()
    {
        _isVisible = false;

        if (scoreboardPanel == null)
            return;

        scoreboardPanel.localScale =
            Vector3.one * hiddenScale;

        scoreboardPanel.gameObject.SetActive(false);
    }

   

    private void RefreshPlayerList()
    {
        if (rowPrefab == null ||
            rowsParent == null)
            return;

        List<PlayerMatchStats> players =
            PlayerMatchStats.Registry.Values
                .Where(stats => stats != null)
                .OrderByDescending(stats => stats.Kills)
                .ThenBy(stats => stats.Deaths)
                .Take(maxDisplayedPlayers)
                .ToList();

        HashSet<PlayerRef> activePlayers =
            new HashSet<PlayerRef>();

        foreach (PlayerMatchStats stats in players)
        {
            PlayerRef player =
                stats.Player;

            activePlayers.Add(player);

            if (!_rows.TryGetValue(
                    player,
                    out ScoreboardRowUI row))
            {
                row = Instantiate(
                    rowPrefab,
                    rowsParent);

                _rows.Add(
                    player,
                    row);

                row.transform.localScale =
                    Vector3.zero;

                row.transform
                    .DOScale(Vector3.one, 0.15f)
                    .SetEase(Ease.OutBack);
            }

            UpdateRow(
                player,
                row,
                stats);
        }

        RemoveDisconnectedPlayers(
            activePlayers);

        SortRows(players);
    }
    
    private void UpdateRow(
        PlayerRef player,
        ScoreboardRowUI row,
        PlayerMatchStats stats)
    {
        string playerName =
            GetPlayerName(player);

        row.Setup(
            player,
            playerName,
            stats.Kills,
            stats.Deaths,
            stats.CombatMode);
    }

    private void HandleStatsChanged(
        PlayerRef player)
    {
        if (!PlayerMatchStats.TryGet(
                player,
                out PlayerMatchStats stats))
        {
            return;
        }

        if (!_rows.TryGetValue(
                player,
                out ScoreboardRowUI row))
        {
            RefreshPlayerList();
            return;
        }

        UpdateRow(
            player,
            row,
            stats);

        RefreshPlayerList();
    }

    private void HandleNicknameChanged(
        PlayerRef player,
        string nickname)
    {
        if (!_rows.TryGetValue(
                player,
                out ScoreboardRowUI row))
        {
            return;
        }

        if (!PlayerMatchStats.TryGet(
                player,
                out PlayerMatchStats stats))
        {
            return;
        }

        row.UpdateDisplay(
            nickname,
            stats.Kills,
            stats.Deaths,
            stats.CombatMode);
    }
    

    private void RemoveDisconnectedPlayers(
        HashSet<PlayerRef> activePlayers)
    {
        List<PlayerRef> playersToRemove =
            _rows.Keys
                .Where(player =>
                    !activePlayers.Contains(player))
                .ToList();

        foreach (PlayerRef player
                 in playersToRemove)
        {
            if (_rows.TryGetValue(
                    player,
                    out ScoreboardRowUI row))
            {
                Destroy(row.gameObject);
            }

            _rows.Remove(player);
        }
    }

   

    private void SortRows(
        List<PlayerMatchStats> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            PlayerRef player =
                players[i].Player;

            if (_rows.TryGetValue(
                    player,
                    out ScoreboardRowUI row))
            {
                row.transform.SetSiblingIndex(i);
            }
        }
    }
    

    private string GetPlayerName(
        PlayerRef player)
    {
        if (PlayerManager.Registry.TryGetValue(
                player,
                out PlayerManager playerManager))
        {
            return playerManager
                .Nickname
                .ToString();
        }

        return $"Player {player.PlayerId}";
    }
}