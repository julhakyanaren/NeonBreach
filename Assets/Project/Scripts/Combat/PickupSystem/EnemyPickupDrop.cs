using UnityEngine;

public class EnemyPickupDrop : MonoBehaviour
{
    [Header("Drop Chance Settings")]
    [Tooltip("Base pickup drop chance on the first wave.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseDropChance = 0.1f;

    [Tooltip("Additional drop chance added for each new wave after the first one.")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChancePerWave = 0.02f;

    [Tooltip("Maximum allowed pickup drop chance.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDropChance = 0.5f;

    [Header("Pickup Pool")]
    [Tooltip("Available pickup prefabs that can be spawned after enemy death.")]
    [SerializeField] private GameObject[] pickupPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Vertical offset used when spawning the pickup so it does not intersect the ground.")]
    [SerializeField] private float spawnHeightOffset = 0.25f;

    [Tooltip("Optional custom spawn point for the pickup. If not assigned, enemy transform position will be used.")]
    [SerializeField] private Transform dropPoint;

    [Header("Debug")]
    [Tooltip("Enables debug logs for drop calculations and spawn result.")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool dropWasProcessed;

    public void TryDrop(int waveIndex)
    {
        if (dropWasProcessed)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"{name}: Pickup drop was already processed.");
            }

            return;
        }

        dropWasProcessed = true;

        if (pickupPrefabs == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"{name}: Pickup prefabs array is null.");
            }

            return;
        }

        if (pickupPrefabs.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"{name}: Pickup prefabs array is empty.");
            }

            return;
        }

        float currentDropChance = CalculateDropChance(waveIndex);
        float randomValue = Random.value;

        if (enableDebugLogs)
        {
            Debug.Log($"{name}: Wave = {waveIndex}, DropChance = {currentDropChance:F2}, Roll = {randomValue:F2}");
        }

        if (randomValue > currentDropChance)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{name}: Pickup drop failed.");
            }

            return;
        }

        GameObject selectedPickupPrefab = GetRandomPickupPrefab();

        if (selectedPickupPrefab == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"{name}: Selected pickup prefab is null.");
            }

            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Instantiate(selectedPickupPrefab, spawnPosition, Quaternion.identity);

        if (enableDebugLogs)
        {
            Debug.Log($"{name}: Spawned pickup {selectedPickupPrefab.name} at {spawnPosition}.");
        }
    }

    private float CalculateDropChance(int waveIndex)
    {
        int safeWaveIndex = waveIndex;

        if (safeWaveIndex < 1)
        {
            safeWaveIndex = 1;
        }

        float calculatedChance = baseDropChance + (safeWaveIndex - 1) * dropChancePerWave;
        float clampedChance = Mathf.Clamp(calculatedChance, 0f, maxDropChance);

        return clampedChance;
    }

    private GameObject GetRandomPickupPrefab()
    {
        int randomIndex = Random.Range(0, pickupPrefabs.Length);
        GameObject selectedPickupPrefab = pickupPrefabs[randomIndex];

        return selectedPickupPrefab;
    }

    private Vector3 GetSpawnPosition()
    {
        Vector3 basePosition = transform.position;

        if (dropPoint != null)
        {
            basePosition = dropPoint.position;
        }

        basePosition.y += spawnHeightOffset;

        return basePosition;
    }

    public void ResetDropState()
    {
        dropWasProcessed = false;
    }
}