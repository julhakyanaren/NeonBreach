using System.Collections.Generic;
using UnityEngine;

public class GameplayPlayerSpawner : MonoBehaviour
{
    [Header("Spawn Platforms")]
    [Tooltip("All spawn platforms that can be used for player spawn.")]
    [SerializeField] private List<SpawnPlatformController> spawnPlatforms = new List<SpawnPlatformController>();

    [Header("Character Prefabs")]
    [Tooltip("All available character prefab mappings.")]
    [SerializeField] private List<CharacterPrefabEntry> characterPrefabs = new List<CharacterPrefabEntry>();

    [Header("Fallback")]
    [Tooltip("Fallback character type used if selected type is missing.")]
    [SerializeField] private CharacterType fallbackCharacterType;

    [Header("Spawn Listeners")]
    [Tooltip("Objects that should be notified after player spawn, for example camera binder or HUD binder.")]
    [SerializeField] private List<MonoBehaviour> spawnListeners = new List<MonoBehaviour>();

    private GameObject spawnedPlayer;

    private void Start()
    {
        SpawnSelectedPlayer();
    }

    private void SpawnSelectedPlayer()
    {
        GameObject targetPrefab = GetSelectedCharacterPrefab();

        if (targetPrefab == null)
        {
            Debug.LogError("GameplayPlayerSpawner: Target prefab was not found.", this);
            return;
        }

        SpawnPlatformController selectedPlatform = GetRandomAvailablePlatform();

        if (selectedPlatform == null)
        {
            Debug.LogError("GameplayPlayerSpawner: No available spawn platform found.", this);
            return;
        }

        Transform spawnPoint = selectedPlatform.SpawnPoint;

        if (spawnPoint == null)
        {
            Debug.LogError("GameplayPlayerSpawner: Selected platform spawn point is null.", this);
            return;
        }

        spawnedPlayer = Instantiate(
            targetPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        selectedPlatform.DeactivatePlatform();
        NotifySpawnListeners(spawnedPlayer);
    }

    private GameObject GetSelectedCharacterPrefab()
    {
        CharacterType selectedType = RuntimeOptions.ConfirmedCharacter;
        GameObject selectedPrefab = GetPrefabByCharacterType(selectedType);

        if (selectedPrefab != null)
        {
            return selectedPrefab;
        }

        Debug.LogWarning("GameplayPlayerSpawner: Selected character prefab was not found. Fallback will be used.", this);

        GameObject fallbackPrefab = GetPrefabByCharacterType(fallbackCharacterType);

        if (fallbackPrefab == null)
        {
            Debug.LogError("GameplayPlayerSpawner: Fallback character prefab was not found.", this);
            return null;
        }

        return fallbackPrefab;
    }

    private GameObject GetPrefabByCharacterType(CharacterType targetType)
    {
        for (int i = 0; i < characterPrefabs.Count; i++)
        {
            if (characterPrefabs[i] == null)
            {
                continue;
            }

            if (characterPrefabs[i].CharacterType == targetType)
            {
                return characterPrefabs[i].PlayerPrefab;
            }
        }

        return null;
    }

    private SpawnPlatformController GetRandomAvailablePlatform()
    {
        List<SpawnPlatformController> availablePlatforms = new List<SpawnPlatformController>();

        for (int i = 0; i < spawnPlatforms.Count; i++)
        {
            if (spawnPlatforms[i] == null)
            {
                continue;
            }

            if (spawnPlatforms[i].CanBeUsedForSpawn())
            {
                availablePlatforms.Add(spawnPlatforms[i]);
            }
        }

        if (availablePlatforms.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, availablePlatforms.Count);
        return availablePlatforms[randomIndex];
    }

    private void NotifySpawnListeners(GameObject playerInstance)
    {
        for (int i = 0; i < spawnListeners.Count; i++)
        {
            if (spawnListeners[i] == null)
            {
                continue;
            }

            IPlayerSpawnListener listener = spawnListeners[i] as IPlayerSpawnListener;

            if (listener != null)
            {
                listener.OnPlayerSpawned(playerInstance);
            }
        }
    }

    public GameObject GetSpawnedPlayer()
    {
        return spawnedPlayer;
    }
}