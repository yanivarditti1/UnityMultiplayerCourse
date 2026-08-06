using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerLobbyPlayerUiEntry : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Toggle readyToggle;

    public void Setup(PlayerRef player, string nickname, bool isReady, bool isLeader, bool isLocal)
    {
        string displayName = string.IsNullOrEmpty(nickname) ?
            $"Player {player.PlayerId}"
            : nickname;
        
        if (isLocal) displayName += " (You)";
        if (isLeader) displayName += " (Leader)";
        
        playerNameText.text = displayName;
        readyToggle.SetIsOnWithoutNotify(isReady);
        readyToggle.interactable = false;
    }
    
    public void SetReady(bool isReady)
    {
        readyToggle.SetIsOnWithoutNotify(isReady);
    }
}
