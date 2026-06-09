using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;


public struct NetworkInputData : INetworkInput
{
    public Vector2 moveInput;
    public Vector2 lookInput;
    public NetworkBool jumpRequested;
    public NetworkBool sprintRequested;
}