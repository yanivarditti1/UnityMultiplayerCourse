using Fusion;
using TMPro;
using UnityEngine;

public sealed class MatchHUDController : MonoBehaviour
{
    [SerializeField]
    private MatchScoreManager scoreManager;

    [Header("Match HUD")]
    [SerializeField]
    private TMP_Text timerText;

    [SerializeField]
    private TMP_Text localKillsText;

    [Header("End Game")]
    [SerializeField]
    private GameObject endGamePanel;

    [SerializeField]
    private TMP_Text winnerNameText;

    [SerializeField]
    private TMP_Text winnerKillsText;

    private void Awake()
    {
        endGamePanel.SetActive(false);

        timerText.text = "00:00";
        localKillsText.text = "Kills: 0";
    }

    private void OnEnable()
    {
        scoreManager.TimerChanged +=
            UpdateTimer;

        scoreManager.LocalScoreChanged +=
            UpdateLocalScore;

        scoreManager.MatchFinished +=
            ShowWinner;
    }

    private void OnDisable()
    {
        scoreManager.TimerChanged -=
            UpdateTimer;

        scoreManager.LocalScoreChanged -=
            UpdateLocalScore;

        scoreManager.MatchFinished -=
            ShowWinner;
    }

    private void UpdateTimer(float remainingSeconds)
    {
        int totalSeconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(remainingSeconds));

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        timerText.text =
            $"{minutes:00}:{seconds:00}";
    }

    private void UpdateLocalScore(
        int kills,
        int killsToWin)
    {
        localKillsText.text =
            $"Kills: {kills} / {killsToWin}";
    }

    private void ShowWinner(
        PlayerRef winner,
        int kills)
    {
        endGamePanel.SetActive(true);

        winnerNameText.text =
            $"Winner: Player {winner.PlayerId}";

        winnerKillsText.text =
            $"Kills: {kills}";
    }
}