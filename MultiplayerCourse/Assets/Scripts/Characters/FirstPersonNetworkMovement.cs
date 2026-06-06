using Fusion;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonNetworkMovement : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = -20f;

    [Header("Look")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float mouseSensitivity = 2f;

    private CharacterController _characterController;
    private Camera _camera;
    private float _verticalVelocity;
    private float _pitch;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        _camera = Camera.main;

        if (_camera != null && cameraRoot != null)
        {
            _camera.transform.SetParent(cameraRoot);
            _camera.transform.localPosition = Vector3.zero;
            _camera.transform.localRotation = Quaternion.identity;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!Object.HasInputAuthority)
            return;

        Move();
        Look();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 move =
            transform.right * horizontal +
            transform.forward * vertical;

        move.Normalize();

        if (_characterController.isGrounded && _verticalVelocity < 0)
            _verticalVelocity = -2f;

        if (_characterController.isGrounded && Input.GetKeyDown(KeyCode.Space))
            _verticalVelocity = jumpForce;

        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalMove = move * moveSpeed;
        finalMove.y = _verticalVelocity;

        _characterController.Move(finalMove * Time.deltaTime);
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -80f, 80f);

        if (cameraRoot != null)
            cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }
}