using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerShooter))]
[RequireComponent(typeof(PlayerBuffReceiver))]
[RequireComponent(typeof(PlayerInputBlocker))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PhotonView))]
public class PlayerMultiplayerRespawnController : MonoBehaviour
{
    private static PlayerMultiplayerRespawnController localInstance;

    [Header("Respawn Settings")]
    [Tooltip("Small vertical offset applied to respawn position.")]
    [Range(0f, 2f)]
    [SerializeField] private float respawnHeightOffset = 0.25f;

    [Header("References")]
    [Tooltip("Player health component.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Player shooter component.")]
    [SerializeField] private PlayerShooter playerShooter;

    [Tooltip("Player buff receiver component.")]
    [SerializeField] private PlayerBuffReceiver playerBuffReceiver;

    [Tooltip("Local input blocker.")]
    [SerializeField] private PlayerInputBlocker inputBlocker;

    [Tooltip("Player rigidbody.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Tooltip("PhotonView used for ownership check.")]
    [SerializeField] private PhotonView photonView;

    [Tooltip("Reference of the score manager script.")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Respawn")]
    [Tooltip("Reference of the GameplayPlayerSpawner script")]
    [SerializeField] private GameplayPlayerSpawner gameplayPlayerSpawner;

    [Tooltip("Player Layer mask")]
    [SerializeField] private LayerMask playerLayerMask;

    [Tooltip("Respawn platform player safe radius")]
    [Range(0f, 10f)]
    [SerializeField] private float spawnCheckRadius = 4f;

    [Header("Debug")]
    [Tooltip("Log respawn flow.")]
    [SerializeField] private bool logRespawn;

    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private bool hasInitialSpawnPoint;

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerShooter = GetComponent<PlayerShooter>();
        playerBuffReceiver = GetComponent<PlayerBuffReceiver>();
        inputBlocker = GetComponent<PlayerInputBlocker>();
        playerRigidbody = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerShooter>();
        }

        if (playerBuffReceiver == null)
        {
            playerBuffReceiver = GetComponent<PlayerBuffReceiver>();
        }

        if (inputBlocker == null)
        {
            inputBlocker = GetComponent<PlayerInputBlocker>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }

        if (gameplayPlayerSpawner == null)
        {
            gameplayPlayerSpawner = FindFirstObjectByType<GameplayPlayerSpawner>();
        }

        initialSpawnPosition = transform.position;
        initialSpawnRotation = transform.rotation;
        hasInitialSpawnPoint = true;
    }

    private void OnEnable()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (photonView == null)
        {
            return;
        }

        if (!photonView.IsMine)
        {
            return;
        }

        localInstance = this;
    }

    private void OnDisable()
    {
        if (localInstance == this)
        {
            localInstance = null;
        }
    }

    private void OnDestroy()
    {
        if (localInstance == this)
        {
            localInstance = null;
        }
    }

    public static void RespawnLocalPlayerFromUI()
    {
        if (localInstance == null)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("PlayerMultiplayerRespawnController: Local instance is missing. Cannot respawn.");
            }

            return;
        }

        localInstance.RespawnLocalPlayer();
    }

    private Transform FindEmptySpawnpoint(out bool foundEmpty)
    {
        foundEmpty = false;

        if (gameplayPlayerSpawner == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("FindEmptySpawnpoint: GameplayPlayerSpawner is missing.", this);
            }

            return null;
        }

        IReadOnlyList<SpawnPlatformController> spawnPlatforms = gameplayPlayerSpawner.SpawnPlatforms;

        for (int i = 0; i < spawnPlatforms.Count; i++)
        {
            SpawnPlatformController platform = spawnPlatforms[i];

            if (platform == null)
            {
                continue;
            }

            Transform point = platform.SpawnPoint;

            if (point == null)
            {
                continue;
            }

            Collider[] hits = Physics.OverlapSphere(
                point.position,
                spawnCheckRadius,
                playerLayerMask);

            bool blocked = false;

            for (int j = 0; j < hits.Length; j++)
            {
                if (hits[j] == null)
                {
                    continue;
                }

                Transform root = hits[j].transform.root;

                if (root == transform.root)
                {
                    continue;
                }

                blocked = true;
                break;
            }

            if (!blocked)
            {
                foundEmpty = true;
                return point;
            }
        }

        return null;
    }

    public void RespawnLocalPlayer()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (photonView == null)
        {
            return;
        }

        if (!photonView.IsMine)
        {
            return;
        }

        if (playerHealth == null)
        {
            return;
        }

        if (!playerHealth.IsDead)
        {
            return;
        }

        bool foundEmpty;
        Transform point = FindEmptySpawnpoint(out foundEmpty);

        if (foundEmpty)
        {
            TeleportToPoint(point);
        }
        else
        {
            TeleportToRespawnPoint();
        }

        if (playerBuffReceiver != null)
        {
            playerBuffReceiver.ResetAllBuffs();
        }

        if (playerShooter != null)
        {
            playerShooter.RespawnResetShooter();
        }

        playerHealth.RespawnFullHealth();

        if (inputBlocker != null)
        {
            inputBlocker.UnblockInput();
        }

        RuntimeOptions.InputBlocked = false;
        Time.timeScale = 1f;

        if (scoreManager != null)
        {
            scoreManager.ApplyScorePenaltyPercent(25f);
        }

        DeathScreenController deathScreenController = FindFirstObjectByType<DeathScreenController>();

        if (deathScreenController != null)
        {
            deathScreenController.CloseAfterRespawn();
        }

        if (logRespawn)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log("PlayerMultiplayerRespawnController: Local player respawned.", this);
            }
        }

    }

    private void TeleportToRespawnPoint()
    {
        if (!hasInitialSpawnPoint)
        {
            return;
        }

        Vector3 targetPosition = initialSpawnPosition + Vector3.up * respawnHeightOffset;
        Quaternion targetRotation = initialSpawnRotation;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.position = targetPosition;
            playerRigidbody.rotation = targetRotation;
            return;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private void TeleportToPoint(Transform targetPoint)
    {
        if (targetPoint == null)
        {
            TeleportToRespawnPoint();
            return;
        }

        Vector3 targetPosition =
            targetPoint.position + Vector3.up * respawnHeightOffset;

        Quaternion targetRotation =
            targetPoint.rotation;

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;

            playerRigidbody.position = targetPosition;
            playerRigidbody.rotation = targetRotation;

            return;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}