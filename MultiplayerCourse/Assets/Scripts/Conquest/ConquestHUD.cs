using System.Text;
using TMPro;
using UnityEngine;

public sealed class ConquestHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI redTicketsText;
    [SerializeField] private TextMeshProUGUI blueTicketsText;
    [SerializeField] private TextMeshProUGUI capturePointsText;
    [SerializeField] private GameObject winnerPanel;
    [SerializeField] private TextMeshProUGUI winnerText;

    private ConquestCapturePoint[] _capturePoints;

    private void Start()
    {
        _capturePoints = FindObjectsByType<ConquestCapturePoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (winnerPanel != null)
            winnerPanel.SetActive(false);
    }

    private void Update()
    {
        ConquestManager manager = ConquestManager.Instance;

        if (manager == null || !manager.IsReady)
            return;

        if (redTicketsText != null)
            redTicketsText.text = $"Red: {manager.RedTickets}";

        if (blueTicketsText != null)
            blueTicketsText.text = $"Blue: {manager.BlueTickets}";

        RefreshCapturePointText();

        if (winnerPanel == null || winnerText == null)
            return;

        winnerPanel.SetActive(manager.MatchEnded);

        if (!manager.MatchEnded)
            return;

        winnerText.text = manager.WinningTeam == ConquestTeam.None
            ? "Draw"
            : $"{manager.WinningTeam} Team Wins";
    }

    private void RefreshCapturePointText()
    {
        if (capturePointsText == null)
            return;

        StringBuilder builder = new();

        foreach (ConquestCapturePoint capturePoint in _capturePoints)
        {
            if (capturePoint == null || !capturePoint.IsReady)
                continue;

            if (builder.Length > 0)
                builder.Append("   ");

            builder.Append(capturePoint.PointName);
            builder.Append(": ");
            builder.Append(capturePoint.IsContested
                ? "Contested"
                : capturePoint.Owner.ToString());
        }

        capturePointsText.text = builder.ToString();
    }
}
