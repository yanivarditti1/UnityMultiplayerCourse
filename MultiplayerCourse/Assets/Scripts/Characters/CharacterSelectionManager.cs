using Fusion;
using UnityEngine;
using UnityEngine.Events;

public sealed class CharacterSelectionManager : NetworkBehaviour
{
    [Header("Character Slots")]
    [SerializeField]
    private CharacterSlotData[] characterSlots =
        new CharacterSlotData[10];

    [Header("UI Events")]
    [SerializeField]
    private UnityEvent<int, bool> onSlotTakenChanged;

    [SerializeField]
    private UnityEvent<string> onSelectionMessage;

    [SerializeField]
    private UnityEvent onLocalPlayerSpawned;

    [Networked, Capacity(10)]
    private NetworkArray<PlayerRef> TakenByPlayers => default;

    public override void Spawned()
    {
        RefreshAllSlots();
    }

    public void RequestCharacter(
        int slotIndex,
        Color nameColor)
    {
        if (slotIndex < 0 ||
            slotIndex >= characterSlots.Length)
        {
            return;
        }

        RPC_RequestCharacter(
            Runner.LocalPlayer,
            slotIndex,
            nameColor);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCharacter(
        PlayerRef requestingPlayer,
        int slotIndex,
        Color nameColor = default)
    {
        if (!Runner.IsSharedModeMasterClient)
            return;

        if (slotIndex < 0 ||
            slotIndex >= characterSlots.Length)
        {
            return;
        }

        if (TakenByPlayers[slotIndex] != PlayerRef.None)
        {
            RPC_CharacterDenied(
                requestingPlayer,
                "This character is already taken. Pick another one.");

            return;
        }

        TakenByPlayers.Set(
            slotIndex,
            requestingPlayer);

        RPC_SlotTakenChanged(slotIndex, true);

        RPC_CharacterApproved(
            requestingPlayer,
            slotIndex,
            nameColor);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CharacterApproved(
        [RpcTarget] PlayerRef targetPlayer,
        int slotIndex,
        Color nameColor)
    {
        if (Runner.LocalPlayer != targetPlayer)
            return;

        CharacterSlotData slot =
            characterSlots[slotIndex];

        if (slot.PlayerPrefab == null ||
            slot.SpawnPoint == null)
        {
            Debug.LogError(
                $"[CharacterSelection] Slot {slotIndex} " +
                "is missing its prefab or spawn point.");

            onSelectionMessage?.Invoke(
                "The selected character is not configured correctly.");

            return;
        }

        Vector3 spawnPosition =
            slot.SpawnPoint.position;

        Quaternion spawnRotation =
            slot.SpawnPoint.rotation;

        ConquestManager conquestManager =
            ConquestManager.Instance;

        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (conquestManager != null &&
            conquestManager.IsReady &&
            conquestManager.TryGetSpawnPoint(
                targetPlayer,
                out Transform conquestSpawn))
        {
            spawnPosition = conquestSpawn.position;
            spawnRotation = conquestSpawn.rotation;
        }
        else if (captureTheFlagManager != null &&
                 captureTheFlagManager.IsReady &&
                 captureTheFlagManager.TryGetSpawnPoint(
                     targetPlayer,
                     out Transform captureTheFlagSpawn))
        {
            spawnPosition = captureTheFlagSpawn.position;
            spawnRotation = captureTheFlagSpawn.rotation;
        }

        NetworkObject spawnedPlayer = Runner.Spawn(
            prefab: slot.PlayerPrefab,
            position: spawnPosition,
            rotation: spawnRotation,
            inputAuthority: targetPlayer,
            onBeforeSpawned: null,
            flags:
                NetworkSpawnFlags.SharedModeStateAuthLocalPlayer);

        if (spawnedPlayer == null)
        {
            Debug.LogError(
                $"[CharacterSelection] Failed to spawn " +
                $"slot {slotIndex} for {targetPlayer}.");

            TakenByPlayers.Set(
                slotIndex,
                PlayerRef.None);

            RPC_SlotTakenChanged(slotIndex, false);

            onSelectionMessage?.Invoke(
                "Failed to spawn the selected character.");

            return;
        }

        Runner.SetPlayerObject(
            targetPlayer,
            spawnedPlayer);

        bool teamModeActive =
            conquestManager != null &&
            conquestManager.IsReady ||
            captureTheFlagManager != null &&
            captureTheFlagManager.IsReady;

        if (PlayerManager.Local != null &&
            !teamModeActive)
        {
            PlayerManager.Local.SetNameColor(nameColor);
        }

        onSelectionMessage?.Invoke(
            "Character selected!");

        onLocalPlayerSpawned?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CharacterDenied(
        [RpcTarget] PlayerRef targetPlayer,
        string reason)
    {
        if (Runner.LocalPlayer != targetPlayer)
            return;

        onSelectionMessage?.Invoke(reason);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SlotTakenChanged(
        int slotIndex,
        bool isTaken)
    {
        onSlotTakenChanged?.Invoke(
            slotIndex,
            isTaken);
    }

    private void RefreshAllSlots()
    {
        for (int i = 0;
             i < characterSlots.Length;
             i++)
        {
            bool isTaken =
                TakenByPlayers[i] != PlayerRef.None;

            onSlotTakenChanged?.Invoke(
                i,
                isTaken);
        }
    }
}