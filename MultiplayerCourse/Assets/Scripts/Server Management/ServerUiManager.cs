using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerUiManager : MonoBehaviour
{
    [Header("Panels")] 
    [SerializeField] private GameObject connectPanel;
    [SerializeField] private GameObject preMatchPanel;
    
    [Header("Connection")]
    [SerializeField] private Button joinServerButton;
    [SerializeField] private TextMeshProUGUI connectStatusText;
    
    [Header("Pre-Match")]
    [SerializeField] private TMP_InputField nicknameInputField;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private Button startMatchButton;
    [SerializeField] private TextMeshProUGUI preMatchStatusText;
    
    [Header("References")]
    [SerializeField] private NetworkStartupManager networkStartupManager;
    
    
}
