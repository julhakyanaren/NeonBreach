using Photon.Pun;
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

    [Header("Singleplayer Pickup Pool")]
    [Tooltip("Available pickup prefabs that can be spawned after enemy death in singleplayer.")]
    [SerializeField] private GameObject[] pickupPrefabs;

    [Header("Multiplayer Pickup Pool")]
    [Tooltip("Resources paths for pickup prefabs spawned through Photon in multiplayer.")]
    [SerializeField]
    private string[] photonPickupPrefabPaths =
    {
        "PhotonPrefabs/Boosters_PUN/BoostDamage_PUN",
        "PhotonPrefabs/Boosters_PUN/BoostDefence_PUN",
        "PhotonPrefabs/Boosters_PUN/BoostHealth_PUN",
        "PhotonPrefabs/Boosters_PUN/BoostShoot_PUN",
        "PhotonPrefabs/Boosters_PUN/BoostSpeed_PUN"
    };

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
            return;
        }

        dropWasProcessed = true;

        if (RuntimeOptions.MultiplayerMode)
        {
            TryDropMultiplayer(waveIndex);
            return;
        }

        TryDropSingleplayer(waveIndex);
    }

    private void TryDropSingleplayer(int waveIndex)
    {
        if (pickupPrefabs == null)
        {
            return;
        }

        if (pickupPrefabs.Length == 0)
        {
            return;
        }

        if (!RollDrop(waveIndex))
        {
            return;
        }

        GameObject selectedPickupPrefab = GetRandomPickupPrefab();

        if (selectedPickupPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();
        Instantiate(selectedPickupPrefab, spawnPosition, Quaternion.identity);
    }

    private void TryDropMultiplayer(int waveIndex)
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (photonPickupPrefabPaths == null)
        {
            return;
        }

        if (photonPickupPrefabPaths.Length == 0)
        {
            return;
        }

        if (!RollDrop(waveIndex))
        {
            return;
        }

        string selectedPath = GetRandomPhotonPickupPath();

        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition();

        PhotonNetwork.InstantiateRoomObject(
            selectedPath,
            spawnPosition,
            Quaternion.identity);
    }

    private bool RollDrop(int waveIndex)
    {
        float currentDropChance = CalculateDropChance(waveIndex);
        float randomValue = Random.value;

        if (enableDebugLogs)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log(name + ": Wave = " + waveIndex + ", DropChance = " + currentDropChance.ToString("F2") + ", Roll = " + randomValue.ToString("F2"));
            }
        }

        if (randomValue > currentDropChance)
        {
            return false;
        }

        return true;
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

    private string GetRandomPhotonPickupPath()
    {
        int randomIndex = Random.Range(0, photonPickupPrefabPaths.Length);
        string selectedPath = photonPickupPrefabPaths[randomIndex];

        return selectedPath;
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