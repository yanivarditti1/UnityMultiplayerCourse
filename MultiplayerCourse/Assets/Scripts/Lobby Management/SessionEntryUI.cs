using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SessionEntryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI playerCountText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject fullIndicator;
    [SerializeField] private GameObject closedIndicator;

    private SessionInfo session;
    private Action<SessionInfo> selectedCallback;

    public void Setup(
        SessionInfo sessionInfo,
        GameModeType gameMode,
        Action<SessionInfo> onSelected)
    {
        session = sessionInfo;
        selectedCallback = onSelected;

        if (roomNameText != null)
            roomNameText.text = session.Name;

        if (gameModeText != null)
            gameModeText.text = GetDisplayName(gameMode);

        if (playerCountText != null)
        {
            playerCountText.text =
                $"{session.PlayerCount}/{session.MaxPlayers}";
        }

        bool isFull =
            session.PlayerCount >= session.MaxPlayers;

        bool isClosed =
            !session.IsOpen;

        if (fullIndicator != null)
            fullIndicator.SetActive(isFull);

        if (closedIndicator != null)
            closedIndicator.SetActive(isClosed);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(HandleSelected);
            selectButton.onClick.AddListener(HandleSelected);
            selectButton.interactable = !isFull && !isClosed;
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(HandleSelected);
    }

    private void HandleSelected()
    {
        if (session == null)
            return;

        selectedCallback?.Invoke(session);
    }

    private static string GetDisplayName(GameModeType gameMode)
    {
        return gameMode switch
        {
            GameModeType.FreeForAll => "Free For All",
            GameModeType.Conquest => "Conquest",
            GameModeType.CaptureTheFlag => "Capture The Flag",
            _ => "Unknown"
        };
    }
}