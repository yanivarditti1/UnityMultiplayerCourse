using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSelectionButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private CharacterSelectionManager selectionManager;
    [SerializeField] private ChairCombatMode combatMode;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(SelectClass);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(SelectClass);
    }

    private void SelectClass()
    {
        if (selectionManager == null)
            return;

        if (combatMode == ChairCombatMode.Melee)
            selectionManager.SelectMelee();
        else
            selectionManager.SelectThrower();
    }
}