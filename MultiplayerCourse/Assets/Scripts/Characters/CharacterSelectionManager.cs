using Fusion;
using UnityEngine;
using UnityEngine.Events;

public sealed class CharacterSelectionManager : NetworkBehaviour
{
    [Header("Player Classes")]
    [SerializeField] private NetworkObject meleePlayerPrefab;
    [SerializeField] private NetworkObject throwerPlayerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("UI Events")]
    [SerializeField] private UnityEvent<string> onSelectionMessage;
    [SerializeField] private UnityEvent onLocalPlayerSpawned;

    [Networked, Capacity(10)]
    private NetworkArray<PlayerRef> SpawnPointOwners => default;
    

    public void SelectMelee()
    {
        RequestClass(ChairCombatMode.Melee);
    }

    public void SelectThrower()
    {
        RequestClass(ChairCombatMode.Thrower);
    }

  

    private void RequestClass(
        ChairCombatMode combatMode)
    {
        if (Runner == null)
            return;

        string nickname =
            GetLocalNickname();

        RPC_RequestClass(
            combatMode,
            nickname);
    }

   

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority)]
    private void RPC_RequestClass(
        ChairCombatMode requestedClass,
        string nickname,
        RpcInfo info = default)
    {
      

        PlayerRef requestingPlayer =
            info.Source;

        if (requestingPlayer == PlayerRef.None)
            return;

      

        nickname =
            SanitizeNickname(
                nickname,
                requestingPlayer);

       

        if (PlayerAlreadySpawned(
                requestingPlayer))
        {
            RPC_ClassDenied(
                requestingPlayer,
                "You already selected a class.");

            return;
        }

     

        int spawnIndex =
            FindRandomAvailableSpawnPoint();

        if (spawnIndex < 0)
        {
            RPC_ClassDenied(
                requestingPlayer,
                "No spawn points are currently available.");

            return;
        }

      

        NetworkObject playerPrefab =
            requestedClass ==
            ChairCombatMode.Melee
                ? meleePlayerPrefab
                : throwerPlayerPrefab;

        if (playerPrefab == null)
        {
            Debug.LogError(
                $"[CharacterSelection] " +
                $"Missing prefab for {requestedClass}");

            RPC_ClassDenied(
                requestingPlayer,
                "Player prefab is missing.");

            return;
        }

     

        Transform spawnPoint =
            spawnPoints[spawnIndex];

        if (spawnPoint == null)
        {
            RPC_ClassDenied(
                requestingPlayer,
                "Selected spawn point is invalid.");

            return;
        }

      

        SpawnPointOwners.Set(
            spawnIndex,
            requestingPlayer);

      

        NetworkObject spawnedPlayer =
            Runner.Spawn(
                playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                requestingPlayer);

        if (spawnedPlayer == null)
        {
            SpawnPointOwners.Set(
                spawnIndex,
                PlayerRef.None);

            RPC_ClassDenied(
                requestingPlayer,
                "Failed to spawn player.");

            return;
        }

       

        if (spawnedPlayer.TryGetComponent(
                out PlayerMatchStats matchStats))
        {
            matchStats.SetNicknameServer(
                nickname);
        }
        else
        {
            Debug.LogWarning(
                "[CharacterSelection] " +
                "Spawned player has no PlayerMatchStats component.");
        }

        Debug.Log(
            $"[CharacterSelection] " +
            $"Spawned {requestedClass} " +
            $"for {nickname} ({requestingPlayer}) " +
            $"at Spawn Point {spawnIndex}");

       

        RPC_ClassApproved(
            requestingPlayer,
            requestedClass);
    }


    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All)]
    private void RPC_ClassApproved(
        [RpcTarget] PlayerRef targetPlayer,
        ChairCombatMode selectedClass)
    {
        onSelectionMessage?.Invoke(
            $"{selectedClass} selected!");

        onLocalPlayerSpawned?.Invoke();
    }

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All)]
    private void RPC_ClassDenied(
        [RpcTarget] PlayerRef targetPlayer,
        string reason)
    {
        onSelectionMessage?.Invoke(
            reason);
    }


    private string GetLocalNickname()
    {
      
        if (PlayerDataPersistanceManager.Instance != null)
        {
            string nickname =
                PlayerDataPersistanceManager
                    .Instance
                    .Nickname;

            if (!string.IsNullOrWhiteSpace(
                    nickname))
            {
                return nickname.Trim();
            }
        }

      
        if (Runner != null &&
            PlayerManager.Registry.TryGetValue(
                Runner.LocalPlayer,
                out PlayerManager playerManager))
        {
            string nickname =
                playerManager
                    .Nickname
                    .ToString();

            if (!string.IsNullOrWhiteSpace(
                    nickname))
            {
                return nickname.Trim();
            }
        }

        if (Runner != null)
        {
            return
                $"Player {Runner.LocalPlayer.PlayerId}";
        }

        return "Player";
    }

    private string SanitizeNickname(
        string nickname,
        PlayerRef player)
    {
        if (string.IsNullOrWhiteSpace(
                nickname))
        {
            return
                $"Player {player.PlayerId}";
        }

        nickname = nickname.Trim();

        if (nickname.Length > 32)
        {
            nickname =
                nickname.Substring(
                    0,
                    32);
        }

        return nickname;
    }

    

    private bool PlayerAlreadySpawned(
        PlayerRef player)
    {
        int count =
            Mathf.Min(
                spawnPoints.Length,
                SpawnPointOwners.Length);

        for (int i = 0;
             i < count;
             i++)
        {
            if (SpawnPointOwners[i] ==
                player)
            {
                return true;
            }
        }

        return false;
    }

    private int FindRandomAvailableSpawnPoint()
    {
        int count =
            Mathf.Min(
                spawnPoints.Length,
                SpawnPointOwners.Length);

        int availableCount = 0;

       
        for (int i = 0;
             i < count;
             i++)
        {
            if (spawnPoints[i] == null)
                continue;

            if (SpawnPointOwners[i] !=
                PlayerRef.None)
            {
                continue;
            }

            availableCount++;
        }

        if (availableCount == 0)
            return -1;

       
        int randomIndex =
            Random.Range(
                0,
                availableCount);

        for (int i = 0;
             i < count;
             i++)
        {
            if (spawnPoints[i] == null)
                continue;

            if (SpawnPointOwners[i] !=
                PlayerRef.None)
            {
                continue;
            }

            if (randomIndex == 0)
                return i;

            randomIndex--;
        }

        return -1;
    }
    

    public void ReleasePlayerSpawnPoint(
        PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;

        int count =
            Mathf.Min(
                spawnPoints.Length,
                SpawnPointOwners.Length);

        for (int i = 0;
             i < count;
             i++)
        {
            if (SpawnPointOwners[i] !=
                player)
            {
                continue;
            }

            SpawnPointOwners.Set(
                i,
                PlayerRef.None);

            return;
        }
    }
}