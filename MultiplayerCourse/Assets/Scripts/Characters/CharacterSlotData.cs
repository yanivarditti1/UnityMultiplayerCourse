using Fusion;
using UnityEngine;

[System.Serializable]
public sealed class CharacterSlotData
{
    public CharacterSlotId SlotId;
    public CharacterClassType ClassType;
    public NetworkObject PlayerPrefab;
    public Transform SpawnPoint;
}