using UnityEngine;

public sealed class LocalHUDRegistry : MonoBehaviour
{
    public static LocalHUDRegistry Instance { get; private set; }

    [SerializeField] private LocalPlayerHealthHUD healthHUD;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void BindHealth(PlayerHealth playerHealth)
    {
        if (Instance == null)
            return;

        Instance.healthHUD.Bind(playerHealth);
    }
}