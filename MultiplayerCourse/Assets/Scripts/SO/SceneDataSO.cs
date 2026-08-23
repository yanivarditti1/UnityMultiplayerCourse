using UnityEngine;

[CreateAssetMenu(fileName = "SceneDataSO", menuName = "Scriptable Objects/SceneDataSO")]
public class SceneDataSO : ScriptableObject
{
    public string lobbySceneName;
    public string gameSceneName;
    
    [SerializeField] private string freeForAllSceneName = "GameScene";
    [SerializeField] private string conquestSceneName = "ConquestScene";
    [SerializeField] private string captureTheFlagSceneName = "CaptureTheFlagScene";

    public string GetSceneName(GameModeType gameMode)
    {
        return gameMode switch
        {
            GameModeType.FreeForAll => freeForAllSceneName,
            GameModeType.Conquest => conquestSceneName,
            GameModeType.CaptureTheFlag => captureTheFlagSceneName,
            _ => gameSceneName
        };
    }
}
