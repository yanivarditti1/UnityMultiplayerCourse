using Fusion;
using TMPro;
using UnityEngine;

public sealed class CaptureTheFlagHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CaptureTheFlagManager manager;
    [SerializeField] private CaptureTheFlagFlag flag;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI redScoreText;
    [SerializeField] private TextMeshProUGUI blueScoreText;
    [SerializeField] private TextMeshProUGUI flagStatusText;
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    private void Start()
    {
        if (winnerPanel )
            winnerPanel.SetActive(false);
    }

    private void Update()
    {
        if (!manager  ||
            !manager.IsReady)
        {
            return;
        }

        if (redScoreText )
            redScoreText.text = $"Red: {manager.RedScore}";

        if (blueScoreText )
            blueScoreText.text = $"Blue: {manager.BlueScore}";

        RefreshFlagStatus();

        if (!winnerPanel  ||
            !winnerText )
        {
            return;
        }

        winnerPanel.SetActive(
            manager.MatchEnded);

        if (!manager.MatchEnded)
            return;

        winnerText.text =
            manager.WinningTeam == ConquestTeam.None
                ? "Draw"
                : $"{manager.WinningTeam} Team Wins";
    }

    private void RefreshFlagStatus()
    {
        if (!flagStatusText  ||
           !flag  ||
            !flag.IsReady)
        {
            return;
        }

        if (flag.Carrier != PlayerRef.None)
        {
            if (PlayerManager.Registry.TryGetValue(
                    flag.Carrier,
                    out PlayerManager playerManager))
            {
                flagStatusText.text =
                    $"Flag carried by {playerManager.Nickname}";
            }
            else
            {
                flagStatusText.text =
                    "Flag is being carried";
            }

            return;
        }

        flagStatusText.text =
            flag.IsHome
                ? "Flag: Center"
                : "Flag: Dropped";
    }
}