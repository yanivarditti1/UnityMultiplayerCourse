using Fusion;
using UnityEngine;

public struct LobbyPlayerState : INetworkStruct
{
    public NetworkString<_32> Nickname;
    public NetworkBool HasNickname;
    public NetworkBool IsReady;
    public NetworkBool IsInLobby;
}
