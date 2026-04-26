using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Header("Singleplayer Enemy")]
        [Tooltip("Enemy prefab used in singleplayer mode.")]
        public GameObject enemyPrefab;

        [Header("Multiplayer Enemy")]
        [Tooltip("Prefab name in Resources used by Photon room instantiation.")]
        public string multiplayerPrefabName;

        [Header("Spawn Weight")]
        [Tooltip("Relative weight used for random selection.")]
        [Min(0)]
        public int spawnWeight = 1;
    }

    [Header("Enemy Prefabs")]
    [Tooltip("Available enemy prefabs for spawning.")]
    [SerializeField] private List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();

    public GameObject SpawnEnemyAtPosition(Vector3 spawnPosition, WaveScalingData scalingData, int waveIndex)
    {
        EnemySpawnEntry selectedEntry = GetRandomEnemyEntry(waveIndex);

        if (selectedEntry == null)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.LogWarning($"{name}: EnemySpawner could not resolve enemy entry.");
            }
            return null;
        }

        GameObject spawnedEnemy = null;

        if (RuntimeOptions.MultiplayerMode)
        {
            if (!PhotonNetwork.InRoom)
            {
                if (RuntimeOptions.LoggingWarning)
                {
                    Debug.LogWarning($"{name}: MultiplayerMode is enabled but client is not in a Photon room.");
                }
                return null;
            }

            if (!PhotonNetwork.IsMasterClient)
            {
                if (RuntimeOptions.LoggingWarning)
                {
                    Debug.LogWarning($"{name}: Only MasterClient can spawn room enemies.");
                }
                return null;
            }

            if (string.IsNullOrWhiteSpace(selectedEntry.multiplayerPrefabName))
            {
                if (RuntimeOptions.LoggingWarning)
                {
                    Debug.LogWarning($"{name}: Multiplayer prefab name is missing for enemy entry.");
                }
                return null;
            }

            spawnedEnemy = PhotonNetwork.InstantiateRoomObject(
                selectedEntry.multiplayerPrefabName,
                spawnPosition,
                Quaternion.identity,
                0,
                new object[]
                {
                    waveIndex
                });
        }
        else
        {
            if (selectedEntry.enemyPrefab == null)
            {
                if (RuntimeOptions.LoggingWarning)
                {
                    Debug.LogWarning($"{name}: Singleplayer enemy prefab is missing.");
                }                
                return null;
            }

            spawnedEnemy = Instantiate(selectedEntry.enemyPrefab, spawnPosition, Quaternion.identity);
        }

        if (spawnedEnemy == null)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning($"{name}: EnemySpawner failed to spawn enemy.");
            }
            return null;
        }

        ApplyWaveIndex(spawnedEnemy, waveIndex);
        ApplyWaveScaling(spawnedEnemy, scalingData);

        return spawnedEnemy;
    }

    private void ApplyWaveIndex(GameObject spawnedEnemy, int waveIndex)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        SaberwingController saberwing = spawnedEnemy.GetComponent<SaberwingController>();

        if (saberwing != null)
        {
            saberwing.SetWaveIndex(waveIndex);
        }

        JaggernautController jaggernaut = spawnedEnemy.GetComponent<JaggernautController>();

        if (jaggernaut != null)
        {
            jaggernaut.SetWaveIndex(waveIndex);
        }

        HotshotController hotshot = spawnedEnemy.GetComponent<HotshotController>();

        if (hotshot != null)
        {
            hotshot.SetWaveIndex(waveIndex);
        }
    }

    private void ApplyWaveScaling(GameObject spawnedEnemy, WaveScalingData scalingData)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        IWaveScalable[] scalableComponents = spawnedEnemy.GetComponentsInChildren<IWaveScalable>();

        for (int i = 0; i < scalableComponents.Length; i++)
        {
            IWaveScalable scalable = scalableComponents[i];

            if (scalable == null)
            {
                continue;
            }

            scalable.ApplyWaveScaling(scalingData);
        }
    }

    private EnemySpawnEntry GetRandomEnemyEntry(int waveIndex)
    {
        if (enemyEntries == null || enemyEntries.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0; i < enemyEntries.Count; i++)
        {
            EnemySpawnEntry entry = enemyEntries[i];

            if (!IsEntryValid(entry))
            {
                continue;
            }

            int scaledWeight = GetScaledWeight(entry, waveIndex);
            totalWeight += scaledWeight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        for (int i = 0; i < enemyEntries.Count; i++)
        {
            EnemySpawnEntry entry = enemyEntries[i];

            if (!IsEntryValid(entry))
            {
                continue;
            }

            int scaledWeight = GetScaledWeight(entry, waveIndex);
            currentWeight += scaledWeight;

            if (randomValue < currentWeight)
            {
                return entry;
            }
        }

        return null;
    }

    private bool IsEntryValid(EnemySpawnEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        if (entry.spawnWeight <= 0)
        {
            return false;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (string.IsNullOrWhiteSpace(entry.multiplayerPrefabName))
            {
                return false;
            }

            return true;
        }

        if (entry.enemyPrefab == null)
        {
            return false;
        }

        return true;
    }

    private int GetScaledWeight(EnemySpawnEntry entry, int waveIndex)
    {
        if (entry == null)
        {
            return 0;
        }

        int scaledWeight = entry.spawnWeight;
        string enemyName = GetEnemyName(entry);

        if (enemyName.Contains("Saberwing"))
        {
            scaledWeight = Mathf.Max(1, scaledWeight - waveIndex);
        }
        else if (enemyName.Contains("Jaggernaut"))
        {
            scaledWeight += waveIndex;
        }
        else if (enemyName.Contains("Hotshot"))
        {
            scaledWeight += waveIndex * 2;
        }

        return Mathf.Max(1, scaledWeight);
    }

    private string GetEnemyName(EnemySpawnEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (!string.IsNullOrWhiteSpace(entry.multiplayerPrefabName))
            {
                return entry.multiplayerPrefabName;
            }
        }

        if (entry.enemyPrefab != null)
        {
            return entry.enemyPrefab.name;
        }

        return string.Empty;
    }
}