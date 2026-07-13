using UnityEngine;

public sealed class ConquestSpawnPoint : MonoBehaviour
{
    [SerializeField] private ConquestTeam team;

    public ConquestTeam Team => team;
}
