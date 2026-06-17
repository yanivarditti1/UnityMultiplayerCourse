
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterSelectionButton : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CharacterSelectionManager selectionManager;

    private string _originalText;
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _originalText = label.text;
        button.onClick.AddListener(HandleClicked);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClicked);
    }

    public void SetTakenState(int changedSlotIndex, bool isTaken)
    {
        if (changedSlotIndex != slotIndex)
            return;

        button.interactable = !isTaken;
        label.text = isTaken ? "Taken" : _originalText;
    }

    private void HandleClicked()
    {
        Color playerColor = _image != null ? _image.color : Color.white;
        selectionManager.RequestCharacter(slotIndex, playerColor);
    }
}
