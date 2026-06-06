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
        RefreshAllSlots();
    }

    public void RequestCharacter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= characterSlots.Length)
            return;

        RPC_RequestCharacter(Runner.LocalPlayer, slotIndex);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCharacter(PlayerRef requestingPlayer, int slotIndex)
    {
        if (!Runner.IsSharedModeMasterClient)
            return;

        if (slotIndex < 0 || slotIndex >= characterSlots.Length)
            return;

        if (TakenByPlayers[slotIndex] != PlayerRef.None)
        {
            RPC_CharacterDenied(requestingPlayer, "This character is already taken. Pick another one.");
            return;
        }

        TakenByPlayers.Set(slotIndex, requestingPlayer);

        CharacterSlotData slot = characterSlots[slotIndex];

        NetworkObject spawnedPlayer = Runner.Spawn(
            slot.PlayerPrefab,
            slot.SpawnPoint.position,
            slot.SpawnPoint.rotation,
            requestingPlayer
        );

        RPC_CharacterApproved(requestingPlayer);
        RPC_SlotTakenChanged(slotIndex, true);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SlotTakenChanged(int slotIndex, bool isTaken)
    {
        onSlotTakenChanged?.Invoke(slotIndex, isTaken);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CharacterApproved([RpcTarget] PlayerRef targetPlayer)
    {
        if (Runner.LocalPlayer != targetPlayer)
            return;

        onSelectionMessage?.Invoke("Character selected!");
        onLocalPlayerSpawned?.Invoke();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_CharacterDenied([RpcTarget] PlayerRef targetPlayer, string reason)
    {
        if (Runner.LocalPlayer != targetPlayer)
            return;

        onSelectionMessage?.Invoke(reason);
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            bool isTaken = TakenByPlayers[i] != PlayerRef.None;
            onSlotTakenChanged?.Invoke(i, isTaken);
        }
    }
}