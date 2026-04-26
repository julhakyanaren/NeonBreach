using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
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
    [Tooltip("Objects that should be notified after local player spawn, for example camera binder or HUD binder.")]
    [SerializeField] private List<MonoBehaviour> spawnListeners = new List<MonoBehaviour>();

    [Header("Multiplayer Wait")]
    [Tooltip("How long to wait for Photon room before giving up multiplayer spawn.")]
    [Range(1f, 15f)]
    [SerializeField] private float multiplayerRoomWaitTimeout = 8f;

    [Header("Debug")]
    [Tooltip("Log spawn flow details.")]
    [SerializeField] private bool logSpawnFlow = false;

    private GameObject spawnedPlayer;

    public IReadOnlyList<SpawnPlatformController> SpawnPlatforms
    {
        get
        {
            return spawnPlatforms;
        }
    }

    private void Start()
    {
        if (RuntimeOptions.MultiplayerMode == false)
        {
            SpawnSingleplayerPlayer();
            return;
        }

        StartCoroutine(WaitForMultiplayerRoomAndSpawn());
    }

    private IEnumerator WaitForMultiplayerRoomAndSpawn()
    {
        float elapsed = 0f;

        while (PhotonNetwork.InRoom == false || PhotonNetwork.IsConnectedAndReady == false)
        {
            elapsed += Time.unscaledDeltaTime;

            if (elapsed >= multiplayerRoomWaitTimeout)
            {
                if (RuntimeOptions.LoggingError)
                {
                    Debug.LogError("GameplayPlayerSpawner: Timed out while waiting for Photon room before multiplayer spawn.", this);
                }

                yield break;
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.5f);

        SpawnMultiplayerPlayer();
    }

    private void SpawnSingleplayerPlayer()
    {
        CharacterPrefabEntry targetEntry = GetSelectedCharacterEntry();

        if (targetEntry == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Target character entry was not found.", this);
            }
            
            return;
        }

        GameObject targetPrefab = targetEntry.SingleplayerPrefab;

        if (targetPrefab == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Singleplayer prefab was not found.", this);
            }
            
            return;
        }

        SpawnPlatformController selectedPlatform = GetRandomAvailablePlatform();

        if (selectedPlatform == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: No available spawn platform found for singleplayer.", this);
            }
            return;
        }

        Transform spawnPoint = selectedPlatform.SpawnPoint;

        if (spawnPoint == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Selected singleplayer spawn point is null.", this);
            }
            return;
        }

        spawnedPlayer = Instantiate(
            targetPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        selectedPlatform.DeactivatePlatform();

        if (logSpawnFlow)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log(
                "GameplayPlayerSpawner: Spawned singleplayer character " +
                RuntimeOptions.ConfirmedCharacter +
                " on platform " + selectedPlatform.name,
                this);
            }
            
        }

        NotifySpawnListeners(spawnedPlayer);
    }

    private void SpawnMultiplayerPlayer()
    {
        if (PhotonNetwork.InRoom == false)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Multiplayer spawn requested, but client is not in a Photon room.", this);
            }
            return;
        }

        CharacterPrefabEntry targetEntry = GetSelectedCharacterEntry();

        if (targetEntry == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Target character entry was not found.", this);
            }
            
            return;
        }

        string prefabName = targetEntry.MultiplayerPrefabName;

        if (string.IsNullOrWhiteSpace(prefabName))
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Multiplayer prefab name is missing.", this);
            }
            
            return;
        }

        SpawnPlatformController selectedPlatform = GetPlatformForActorNumber(PhotonNetwork.LocalPlayer.ActorNumber);

        if (selectedPlatform == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: No available spawn platform found for multiplayer.", this);
            }
            return;
        }

        Transform spawnPoint = selectedPlatform.SpawnPoint;

        if (spawnPoint == null)
        {
            if (RuntimeOptions.LoggingError)
            {

                Debug.LogError("GameplayPlayerSpawner: Selected multiplayer spawn point is null.", this);
            }
            return;
        }

        object[] instantiationData = new object[]
        {
        (int)RuntimeOptions.ConfirmedCharacter
        };

        spawnedPlayer = PhotonNetwork.Instantiate(
            prefabName,
            spawnPoint.position,
            spawnPoint.rotation,
            0,
            instantiationData);

        if (spawnedPlayer == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: PhotonNetwork.Instantiate returned null.", this);
            }
            return;
        }

        PhotonView photonView = spawnedPlayer.GetComponent<PhotonView>();

        if (photonView == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: PhotonView was not found on spawned multiplayer player.", spawnedPlayer);
            }
            return;
        }

        if (photonView.IsMine)
        {
            NotifySpawnListeners(spawnedPlayer);
        }

        if (logSpawnFlow)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log(
                "GameplayPlayerSpawner: Spawned multiplayer character " +
                RuntimeOptions.ConfirmedCharacter +
                " with ActorNumber " + PhotonNetwork.LocalPlayer.ActorNumber +
                " using prefab " + prefabName,
                this);
            }

        }
    }

    private CharacterPrefabEntry GetSelectedCharacterEntry()
    {
        CharacterType selectedType = RuntimeOptions.ConfirmedCharacter;
        CharacterPrefabEntry selectedEntry = GetEntryByCharacterType(selectedType);

        if (selectedEntry != null)
        {
            return selectedEntry;
        }

        if (RuntimeOptions.LoggingWarning)
        {
            Debug.LogWarning("GameplayPlayerSpawner: Selected character entry was not found. Fallback will be used.", this);
        }

        CharacterPrefabEntry fallbackEntry = GetEntryByCharacterType(fallbackCharacterType);

        if (fallbackEntry == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Fallback character entry was not found.", this);
            }

            return null;
        }

        return fallbackEntry;
    }

    private CharacterPrefabEntry GetEntryByCharacterType(CharacterType targetType)
    {
        for (int i = 0; i < characterPrefabs.Count; i++)
        {
            CharacterPrefabEntry entry = characterPrefabs[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.CharacterType == targetType)
            {
                return entry;
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

    private SpawnPlatformController GetPlatformForActorNumber(int actorNumber)
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

        int platformIndex = actorNumber - 1;

        if (platformIndex < 0)
        {
            platformIndex = 0;
        }

        platformIndex = platformIndex % availablePlatforms.Count;
        return availablePlatforms[platformIndex];
    }

    private void NotifySpawnListeners(GameObject playerInstance)
    {
        if (playerInstance == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("GameplayPlayerSpawner: Cannot notify spawn listeners because player instance is null.", this);
            }
            return;
        }

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