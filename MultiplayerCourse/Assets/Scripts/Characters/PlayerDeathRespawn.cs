using Fusion;
using UnityEngine;

public sealed class PlayerDeathRespawn : NetworkBehaviour
{
    [Header("References")] [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField] private FirstPersonNetworkMovement movement;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private GameObject redScreenOverlay;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Respawn")] [SerializeField] private float respawnDelay = 5f;

    private TickTimer _respawnTimer;
    private bool _isDead;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private CharacterController _characterController;

    public override void Spawned()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
        _characterController = GetComponent<CharacterController>();

        if (movement != null)
            movement.enabled = true;

        if (_characterController != null)
            _characterController.enabled = true;

        if (playerHealth != null)
            playerHealth.DiedWithAttacker += HandleDeath;

        if (redScreenOverlay != null)
            redScreenOverlay.SetActive(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (playerHealth != null)
            playerHealth.DiedWithAttacker -= HandleDeath;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !_isDead)
            return;

        if (_respawnTimer.Expired(Runner))
            Respawn();
    }

    private void HandleDeath(PlayerRef attacker)
    {
        if (!Object.HasStateAuthority || _isDead)
            return;

        _isDead = true;

        if (animationController != null)
        {
            animationController.SetMovementSpeed(0f);
            animationController.SetDead(true);
        }

        _respawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnDelay);

        ConquestManager manager = ConquestManager.Instance;

        if (manager != null && manager.IsReady)
            manager.ReportDeath(Object.InputAuthority, attacker);

        CaptureTheFlagManager captureTheFlagManager =
            CaptureTheFlagManager.Instance;

        if (captureTheFlagManager != null &&
            captureTheFlagManager.IsReady)
        {
            captureTheFlagManager.ReportDeath(
                Object.InputAuthority);
        }

        RPC_SetDeadState(true);
    }

    private void Respawn()
    {
        _isDead = false;

        Vector3 respawnPosition = _spawnPosition;
        Quaternion respawnRotation = _spawnRotation;

        ConquestManager manager = ConquestManager.Instance;
        
        CaptureTheFlagManager captureTheFlagManager = CaptureTheFlagManager.Instance;

        if ((manager == null || !manager.IsReady) &&
            captureTheFlagManager != null &&
            captureTheFlagManager.IsReady &&
            captureTheFlagManager.TryGetSpawnPoint(
                Object.InputAuthority,
                out Transform captureTheFlagSpawn))
        {
            respawnPosition = captureTheFlagSpawn.position;
            respawnRotation = captureTheFlagSpawn.rotation;
        }

        bool controllerEnabled =
            _characterController != null &&
            _characterController.enabled;

        if (_characterController != null)
            _characterController.enabled = false;

        transform.SetPositionAndRotation(
            respawnPosition,
            respawnRotation);

        if (_characterController != null)
            _characterController.enabled = controllerEnabled;

        if (playerHealth != null)
            playerHealth.RestoreFullHealth();

        if (animationController != null)
        {
            animationController.SetDead(false);
            animationController.SetMovementSpeed(0f);
        }

        RPC_SetDeadState(false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetDeadState(bool dead)
    {
        if (visualRoot != null)
            visualRoot.SetActive(!dead);

        if (Object.HasInputAuthority &&
            redScreenOverlay != null)
        {
            redScreenOverlay.SetActive(dead);
        }

        if (movement != null)
            movement.enabled = !dead;

        if (!dead && _characterController != null)
            _characterController.enabled = true;
    }
}