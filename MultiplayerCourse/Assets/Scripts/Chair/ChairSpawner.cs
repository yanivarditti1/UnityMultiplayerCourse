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
    private bool _waitingForRespawn;

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

        if (!_waitingForRespawn)
            return;

        if (!_respawnTimer.Expired(Runner))
            return;

        SpawnChair();
    }

    private void SpawnChair()
    {
        if (chairPrefab == null)
        {
            Debug.LogError("[ChairSpawner] Chair prefab is missing.");
            return;
        }

        Vector3 position =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        Quaternion rotation =
            spawnPoint != null
                ? spawnPoint.rotation
                : transform.rotation;

        _currentChair = Runner.Spawn(
            chairPrefab,
            position,
            rotation,
            onBeforeSpawned: (runner, spawnedObject) =>
            {
                if (spawnedObject.TryGetComponent(
                        out ChairPickup pickup))
                {
                    pickup.SetSpawner(this);
                }
            });

        _waitingForRespawn = false;

        Debug.Log("[ChairSpawner] Chair spawned.");
    }

    public void NotifyChairPickedUp(
        NetworkObject chair)
    {
        if (!Object.HasStateAuthority)
            return;

        if (_currentChair != chair)
            return;

        _currentChair = null;

        _waitingForRespawn = true;

        _respawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnDelay);

        Debug.Log(
            $"[ChairSpawner] Chair picked up. " +
            $"Respawn in {respawnDelay} seconds.");
    }
}