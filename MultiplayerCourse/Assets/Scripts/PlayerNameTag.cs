using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNameTag : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI nameLabel;

    public override void Spawned()
    {
        ApplyName();
        PlayerManager.OnAnyNicknameChanged += HandleNicknameChanged;
        PlayerManager.OnAnyPlayerColorChanged += HandlePlayerColorChanged;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerManager.OnAnyNicknameChanged -= HandleNicknameChanged;
    }

    private void HandleNicknameChanged(PlayerRef player, string nickname)
    {
        //only react to the player who owns this character.
        if (player != Object.InputAuthority) return;
        SetLabel(nickname);
    }
    private void HandlePlayerColorChanged(PlayerRef player, Color color)
    {
        if (player != Object.InputAuthority) return;
        SetLabelColor(color);
    }

    private void ApplyName()
    {
        if (PlayerManager.Registry.TryGetValue(Object.InputAuthority, out var pm))
        {
            SetLabel(pm.Nickname.ToString());
            SetLabelColor(pm.PlayerColor);
        }
    }

    private void SetLabel(string nickname)
    {
        if (nameLabel != null)
            nameLabel.text = nickname;
    }
    
    private void SetLabelColor(Color color)
    {
        if (nameLabel != null)
            nameLabel.color = color;
    }
}
