using DG.Tweening;
using Fusion;
using TMPro;
using UnityEngine;

public sealed class MatchHUDController : MonoBehaviour
{
    [SerializeField] private MatchScoreManager scoreManager;

    [Header("Match HUD")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform timerTransform;

    [SerializeField] private TMP_Text localKillsText;
    [SerializeField] private RectTransform localKillsTransform;

    [Header("End Game")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text winnerNameText;
    [SerializeField] private TMP_Text winnerKillsText;
    [SerializeField] private TMP_Text returnCountdownText;

    [Header("Kill Animation")]
    [SerializeField] private float killPunchScale = 0.25f;
    [SerializeField] private float killPunchDuration = 0.25f;

    [Header("Timer Animation")]
    [SerializeField] private float timerMinutePunchScale = 0.22f;
    [SerializeField] private float timerCountdownPunchScale = 0.12f;
    [SerializeField] private float timerPunchDuration = 0.25f;

    [Header("End Countdown Animation")]
    [SerializeField] private float endCountdownPunchScale = 0.15f;
    [SerializeField] private float endCountdownPunchDuration = 0.2f;

    private int previousTimerSecond = -1;
    private int previousKills = -1;
    private int previousEndCountdownSecond = -1;

    private void Awake()
    {
        endGamePanel.SetActive(false);

        timerText.text = "00:00";

        localKillsText.text =
            $"Kills: 0 / {scoreManager.KillsToWin}";

        returnCountdownText.text = "";
    }

    private void OnEnable()
    {
        scoreManager.TimerChanged +=
            UpdateTimer;

        scoreManager.LocalScoreChanged +=
            UpdateLocalScore;

        scoreManager.MatchFinished +=
            ShowWinner;

        scoreManager.EndCountdownChanged +=
            UpdateEndCountdown;
    }

    private void OnDisable()
    {
        scoreManager.TimerChanged -=
            UpdateTimer;

        scoreManager.LocalScoreChanged -=
            UpdateLocalScore;

        scoreManager.MatchFinished -=
            ShowWinner;

        scoreManager.EndCountdownChanged -=
            UpdateEndCountdown;

        timerTransform.DOKill();
        localKillsTransform.DOKill();
        returnCountdownText.rectTransform.DOKill();
    }

    private void UpdateLocalScore(
        int kills,
        int killsToWin)
    {
        localKillsText.text =
            $"Kills: {kills} / {killsToWin}";

        bool gainedKill =
            previousKills >= 0 &&
            kills > previousKills;

        previousKills = kills;

        if (!gainedKill)
            return;

        AnimateKillText();
    }

    private void AnimateKillText()
    {
        localKillsTransform.DOKill();

        localKillsTransform.localScale =
            Vector3.one;

        localKillsTransform
            .DOPunchScale(
                Vector3.one * killPunchScale,
                killPunchDuration,
                6,
                0.5f);
    }

    private void UpdateTimer(
        float remainingSeconds)
    {
        int totalSeconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    remainingSeconds));

        int minutes =
            totalSeconds / 60;

        int seconds =
            totalSeconds % 60;

        timerText.text =
            $"{minutes:00}:{seconds:00}";

        if (previousTimerSecond ==
            totalSeconds)
            return;

        bool reachedMinute =
            totalSeconds > 0 &&
            totalSeconds % 60 == 0;

        bool finalCountdown =
            totalSeconds > 0 &&
            totalSeconds <= 10;

        if (finalCountdown)
        {
            AnimateTimer(
                timerCountdownPunchScale);
        }
        else if (reachedMinute)
        {
            AnimateTimer(
                timerMinutePunchScale);
        }

        previousTimerSecond =
            totalSeconds;
    }

    private void AnimateTimer(
        float strength)
    {
        timerTransform.DOKill();

        timerTransform.localScale =
            Vector3.one;

        timerTransform
            .DOPunchScale(
                Vector3.one * strength,
                timerPunchDuration,
                5,
                0.5f);
    }

    private void ShowWinner(
        PlayerRef winner,
        int kills)
    {
        string nickname =
            PlayerNicknameUtility.GetNickname(
                winner);

        winnerNameText.text =
            $"Winner: {nickname}";

        winnerKillsText.text =
            $"Kills: {kills}";

        endGamePanel.SetActive(true);
    }

    private void UpdateEndCountdown(
        float remainingSeconds)
    {
        int seconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    remainingSeconds));

        if (previousEndCountdownSecond ==
            seconds)
            return;

        previousEndCountdownSecond =
            seconds;

        returnCountdownText.text =
            $"Returning to lobby in {seconds}...";

        returnCountdownText
            .rectTransform
            .DOKill();

        returnCountdownText
            .rectTransform
            .localScale =
            Vector3.one;

        returnCountdownText
            .rectTransform
            .DOPunchScale(
                Vector3.one *
                endCountdownPunchScale,
                endCountdownPunchDuration,
                5,
                0.5f);
    }
}