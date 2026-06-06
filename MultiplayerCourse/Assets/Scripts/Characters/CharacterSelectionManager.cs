using Fusion;
using UnityEngine;
using UnityEngine.Events;

public sealed class CharacterSelectionManager : NetworkBehaviour
{
    [Header("Character Slots")]
    [SerializeField] private CharacterSlotData[] characterSlots = new CharacterSlotData[10];

    [Header("UI Events")]
    [SerializeField] private UnityEvent<int, bool> onSlotTakenChanged;
    [SerializeField] private UnityEvent<string> onSelectionMessage;
    [SerializeField] private UnityEvent onLocalPlayerSpawned;

    [Networked, Capacity(10)]
    private NetworkArray<PlayerRef> TakenByPlayers => default;

    public override void Spawned()
    {
        Debug.Log("[CharacterSelection] CharacterSelectionManager Spawned");

        RefreshAllSlots();
    }

    public void RequestCharacter(int slotIndex)
    {
        Debug.Log($"[CharacterSelection] RequestCharacter called. Slot: {slotIndex}");

        if (slotIndex < 0 || slotIndex >= characterSlots.Length)
        {
            Debug.LogError($"[CharacterSelection] Invalid slot index: {slotIndex}");
            return;
        }

        Debug.Log($"[CharacterSelection] Sending RPC. LocalPlayer: {Runner.LocalPlayer}");

        RPC_RequestCharacter(Runner.LocalPlayer, slotIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCharacter(PlayerRef requestingPlayer, int slotIndex)
    {
        Debug.Log($"[CharacterSelection] RPC_RequestCharacter CALLED");
        Debug.Log($"[CharacterSelection] Requesting Player: {requestingPlayer}");
        Debug.Log($"[CharacterSelection] Slot Index: {slotIndex}");
        Debug.Log($"[CharacterSelection] Is Master Client: {Runner.IsSharedModeMasterClient}");

        if (!Runner.IsSharedModeMasterClient)
        {
            Debug.LogWarning("[CharacterSelection] Not MasterClient. Returning.");
            return;
        }

        if (slotIndex < 0 || slotIndex >= characterSlots.Length)
        {
            Debug.LogError($"[CharacterSelection] Invalid slot index received: {slotIndex}");
            return;
        }

        Debug.Log($"[CharacterSelection] TakenByPlayers[{slotIndex}] = {TakenByPlayers[slotIndex]}");

        if (TakenByPlayers[slotIndex] != PlayerRef.None)
        {
            Debug.LogWarning($"[CharacterSelection] Slot {slotIndex} already taken.");

            RPC_CharacterDenied(
                requestingPlayer,
                "This character is already taken. Pick another one.");

            return;
        }

        TakenByPlayers.Set(slotIndex, requestingPlayer);

        Debug.Log($"[CharacterSelection] Slot {slotIndex} assigned to player {requestingPlayer}");

        CharacterSlotData slot = characterSlots[slotIndex];

        if (slot == null)
        {
            Debug.LogError($"[CharacterSelection] CharacterSlotData is NULL at index {slotIndex}");
            return;
        }

        Debug.Log("[CharacterSelection] Slot Data Found");
        Debug.Log($"[CharacterSelection] Prefab: {slot.PlayerPrefab}");
        Debug.Log($"[CharacterSelection] Spawn Point: {slot.SpawnPoint}");

        if (slot.PlayerPrefab == null)
        {
            Debug.LogError("[CharacterSelection] PlayerPrefab is NULL");
            return;
        }

        if (slot.SpawnPoint == null)
        {
            Debug.LogError("[CharacterSelection] SpawnPoint is NULL");
            return;
        }

        Debug.Log("[CharacterSelection] Attempting Spawn...");
        Debug.Log($"[CharacterSelection] Position: {slot.SpawnPoint.position}");
        Debug.Log($"[CharacterSelection] Rotation: {slot.SpawnPoint.rotation.eulerAngles}");

        NetworkObject spawnedPlayer = Runner.Spawn(
            slot.PlayerPrefab,
            slot.SpawnPoint.position,
            slot.SpawnPoint.rotation,
            requestingPlayer
        );

        Debug.Log($"[CharacterSelection] Spawn Result: {spawnedPlayer}");

        if (spawnedPlayer == null)
        {
            Debug.LogError("[CharacterSelection] Runner.Spawn returned NULL!");
            return;
        }

        Debug.Log("[CharacterSelection] Spawn Successful");

        RPC_CharacterApproved(requestingPlayer);
        RPC_SlotTakenChanged(slotIndex, true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SlotTakenChanged(int slotIndex, bool isTaken)
    {
        Debug.Log($"[CharacterSelection] Slot Changed. Slot: {slotIndex} Taken: {isTaken}");

        onSlotTakenChanged?.Invoke(slotIndex, isTaken);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CharacterApproved([RpcTarget] PlayerRef targetPlayer)
    {
        Debug.Log($"[CharacterSelection] Character Approved for {targetPlayer}");

        if (Runner.LocalPlayer != targetPlayer)
            return;

        onSelectionMessage?.Invoke("Character selected!");
        onLocalPlayerSpawned?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CharacterDenied([RpcTarget] PlayerRef targetPlayer, string reason)
    {
        Debug.Log($"[CharacterSelection] Character Denied for {targetPlayer}. Reason: {reason}");

        if (Runner.LocalPlayer != targetPlayer)
            return;

        onSelectionMessage?.Invoke(reason);
    }

    private void RefreshAllSlots()
    {
        Debug.Log("[CharacterSelection] Refreshing Slot States");

        for (int i = 0; i < characterSlots.Length; i++)
        {
            bool isTaken = TakenByPlayers[i] != PlayerRef.None;

            Debug.Log($"[CharacterSelection] Slot {i} Taken: {isTaken}");

            onSlotTakenChanged?.Invoke(i, isTaken);
        }
    }
}