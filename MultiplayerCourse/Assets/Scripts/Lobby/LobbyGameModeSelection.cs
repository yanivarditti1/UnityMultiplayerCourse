using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class LobbyGameModeSelection : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    public static GameModeType SelectedMode { get; private set; } =
        GameModeType.FreeForAll;

    private void Awake()
    {
        if (!dropdown)
            dropdown = GetComponent<TMP_Dropdown>();

        if (!dropdown )
            return;

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string>
        {
            "Free For All",
            "Conquest",
            "Capture The Flag"
        });

        dropdown.SetValueWithoutNotify((int)SelectedMode);
        dropdown.onValueChanged.AddListener(HandleValueChanged);
    }

    private void OnDestroy()
    {
        if (dropdown )
            dropdown.onValueChanged.RemoveListener(HandleValueChanged);
    }

    private static void HandleValueChanged(int value)
    {
        if (!Enum.IsDefined(typeof(GameModeType), value))
            value = 0;

        SelectedMode = (GameModeType)value;
    }
}