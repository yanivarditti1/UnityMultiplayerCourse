using Fusion;
using TMPro;
using UnityEngine;

public sealed class ScoreboardRowUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private TMP_Text killsLabel;
    [SerializeField] private TMP_Text deathsLabel;

    [Header("Class Colors")]
    [SerializeField] private Color meleeColor = Color.red;
    [SerializeField] private Color throwerColor = Color.blue;

    public PlayerRef Player { get; private set; }

    public void Setup(
        PlayerRef player,
        string nickname,
        int kills,
        int deaths,
        ChairCombatMode combatMode)
    {
        Player = player;

        UpdateDisplay(
            nickname,
            kills,
            deaths,
            combatMode);
    }

    public void UpdateDisplay(
        string nickname,
        int kills,
        int deaths,
        ChairCombatMode combatMode)
    {
        if (playerNameLabel != null)
        {
            playerNameLabel.text = nickname;

            playerNameLabel.color =
                combatMode == ChairCombatMode.Melee
                    ? meleeColor
                    : throwerColor;
        }

        if (killsLabel != null)
            killsLabel.text = kills.ToString();

        if (deathsLabel != null)
            deathsLabel.text = deaths.ToString();
    }
}