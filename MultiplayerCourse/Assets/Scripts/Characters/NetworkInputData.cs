using Fusion;
using UnityEngine;


public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public Vector2 lookInput;
    public NetworkBool jumpRequested;
    public NetworkBool sprintRequested;
}