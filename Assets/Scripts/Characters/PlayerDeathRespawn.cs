using Fusion;
using UnityEngine;

public sealed class PlayerDeathRespawn : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private FirstPersonNetworkMovement movement;
    [SerializeField] private GameObject visualRoot;
    [SerializeField] private GameObject redScreenOverlay;
    [SerializeField] private PlayerAnimationController animationController;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;

    private TickTimer _respawnTimer;
    private bool _isDead;
    private Vector3 _spawnPosition;

    public override void Spawned()
    {
        _spawnPosition = transform.position;

        playerHealth.Died.AddListener(HandleDeath);

        if (redScreenOverlay != null)
            redScreenOverlay.SetActive(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        playerHealth.Died.RemoveListener(HandleDeath);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        if (!_isDead)
            return;

        if (!_respawnTimer.Expired(Runner))
            return;

        Respawn();
    }

    private void HandleDeath()
    {
        if (!Object.HasStateAuthority)
            return;

        _isDead = true;

        animationController.SetMovementSpeed(0f);
        animationController.SetDead(true);

        _respawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnDelay);

        RPC_SetDeadState(true);
    }

    private void Respawn()
    {
        _isDead = false;

        transform.position = _spawnPosition;

        playerHealth.RestoreFullHealth();

        animationController.SetDead(false);
        animationController.SetMovementSpeed(0f);

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
    }
}