using Fusion;
using UnityEngine;

public sealed class ChairSpawner : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private NetworkObject chairPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Settings")]
    [SerializeField] private float respawnDelay = 5f;

    private NetworkObject _currentChair;
    private TickTimer _respawnTimer;

    public override void Spawned()
    {
        if (!Object.HasStateAuthority)
            return;

        SpawnChair();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (_currentChair != null)
            return;

        if (!_respawnTimer.ExpiredOrNotRunning(Runner))
            return;

        SpawnChair();
    }

    private void SpawnChair()
    {
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        _currentChair = Runner.Spawn(chairPrefab, position, rotation);
        _respawnTimer = TickTimer.CreateFromSeconds(Runner, respawnDelay);
    }
}