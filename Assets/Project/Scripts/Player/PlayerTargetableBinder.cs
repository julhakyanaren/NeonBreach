using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerTargetableBinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to player health that stores targetable state.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Spawn Protection")]
    [Tooltip("Delay before player becomes targetable after spawn completed.")]
    [SerializeField] private float enableTargetingDelay = 4f;

    private Coroutine enableTargetingCoroutine;

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
            StartEnableTargetingDelay();
        }
    }

    private void OnDisable()
    {
        GameCanvasController.SpawnStarted -= HandleSpawnStarted;
        GameCanvasController.SpawnCompleted -= HandleSpawnCompleted;
        PlayerHealth.PlayerDied -= HandlePlayerDied;

        StopEnableTargetingDelay();
    }

    private void HandleSpawnStarted()
    {
        StopEnableTargetingDelay();

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.DisableTargeting();
    }

    private void HandleSpawnCompleted()
    {
        StartEnableTargetingDelay();
    }

    private void StartEnableTargetingDelay()
    {
        StopEnableTargetingDelay();

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.DisableTargeting();
        enableTargetingCoroutine = StartCoroutine(EnableTargetingAfterDelayRoutine());
    }

    private IEnumerator EnableTargetingAfterDelayRoutine()
    {
        if (enableTargetingDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(enableTargetingDelay);
        }

        if (playerHealth == null)
        {
            yield break;
        }

        if (playerHealth.IsDead)
        {
            yield break;
        }

        playerHealth.EnableTargeting();
        enableTargetingCoroutine = null;
    }

    private void StopEnableTargetingDelay()
    {
        if (enableTargetingCoroutine == null)
        {
            return;
        }

        StopCoroutine(enableTargetingCoroutine);
        enableTargetingCoroutine = null;
    }

    private void HandlePlayerDied()
    {
        StopEnableTargetingDelay();

        if (playerHealth == null)
        {
            return;
        }

        playerHealth.DisableTargeting();
    }
}