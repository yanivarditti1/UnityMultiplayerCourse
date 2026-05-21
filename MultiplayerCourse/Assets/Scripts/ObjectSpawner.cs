using UnityEngine;
using Fusion;


public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject _prefab;
    
    
    [ContextMenu("Spawn from prefab")]
    private void SpawnFromPrefab()
    {
        if (Object.HasStateAuthority)
        {
            var spawnPosition = Vector3.zero;
            var spawnRotation = Quaternion.identity;
            
            //var spawnedObject = Instantiate(_prefab, spawnPosition, spawnRotation);
            Runner.Spawn(_prefab, spawnPosition, spawnRotation);
        }
    }
}
