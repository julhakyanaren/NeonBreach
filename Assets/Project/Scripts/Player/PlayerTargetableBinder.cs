using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerTargetableBinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to player health that stores targetable state.")]
    [SerializeField] private PlayerHealth playerHealth;

    private void Reset()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.DisableTargeting();
        }
    }

    private void OnEnable()
    {
        GameCanvasController.SpawnStarted += HandleSpawnStarted;
        GameCanvasController.SpawnCompleted += HandleSpawnCompleted;
        PlayerHealth.PlayerDied += HandlePlayerDied;

        if (GameCanvasController.IsSpawnCompleted)
        {
            HandleSpawnCompleted();
        }
    }

    private void OnDisable()
    {
        GameCanvasController.SpawnStarted -= HandleSpawnStarted;
        GameCanvasController.SpawnCompleted -= HandleSpawnCompleted;
        PlayerHealth.PlayerDied -= HandlePlayerDied;
    }

    private void HandleSpawnStarted()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.DisableTargeting();
    }

    private void HandleSpawnCompleted()
    {
        if (playerHealth == null)
        {
            return;
        }

        if (playerHealth.IsDead)
        {
            return;
        }

        playerHealth.EnableTargeting();
    }

    private void HandlePlayerDied()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.DisableTargeting();
    }
}