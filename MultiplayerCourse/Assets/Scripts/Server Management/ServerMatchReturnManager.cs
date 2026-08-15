using Fusion;
using UnityEngine;

public class ServerMatchReturnManager : NetworkBehaviour
{
    [SerializeField] private SceneDataSO sceneData;

    public override void Spawned()
    {
        if (MatchScoreManager.Instance != null)
            MatchScoreManager.Instance.ReturnToLobbyRequested += HandleReturnToLobbyRequested;
        else
            Debug.LogError("[ServerMatchReturnManager] MatchScoreManager not found");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (MatchScoreManager.Instance != null)
            MatchScoreManager.Instance.ReturnToLobbyRequested -= HandleReturnToLobbyRequested;
    }

    private async void HandleReturnToLobbyRequested()
    {
        if (!Object.HasStateAuthority)
            return;
        
        Debug.Log($"[ServerMatchReturnManager] Returning to lobby requested, loading lobby...");

        await Runner.LoadScene(sceneData.lobbySceneName);
    }
}
