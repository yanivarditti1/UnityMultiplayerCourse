using System.Text;
using TMPro;
using UnityEngine;

public sealed class ConquestHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ConquestManager manager;
    [SerializeField] private ConquestCapturePoint[] capturePoints;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI redTicketsText;
    [SerializeField] private TextMeshProUGUI blueTicketsText;
    [SerializeField] private TextMeshProUGUI capturePointsText;
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

        if (redTicketsText )
        {
            redTicketsText.text =
                $"Red: {manager.RedTickets}";
        }

        if (blueTicketsText )
        {
            blueTicketsText.text =
                $"Blue: {manager.BlueTickets}";
        }

        RefreshCapturePointText();

        if (!winnerPanel  ||
            !winnerText)
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

    private void RefreshCapturePointText()
    {
        if (!capturePointsText  ||
            capturePoints == null)
        {
            return;
        }

        StringBuilder builder = new();

        foreach (ConquestCapturePoint capturePoint
                 in capturePoints)
        {
            if (!capturePoint  ||
                !capturePoint.IsReady)
            {
                continue;
            }

            if (builder.Length > 0)
                builder.Append("   ");

            builder.Append(capturePoint.PointName);
            builder.Append(": ");

            builder.Append(
                capturePoint.IsContested
                    ? "Contested"
                    : capturePoint.Owner.ToString());
        }

        capturePointsText.text =
            builder.ToString();
    }
}