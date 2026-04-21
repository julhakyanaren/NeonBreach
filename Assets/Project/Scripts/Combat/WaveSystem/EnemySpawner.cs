using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Header("Enemy")]
        [Tooltip("Enemy prefab that can be spawned.")]
        public GameObject enemyPrefab;

        [Tooltip("Relative weight used for random selection.")]
        [Min(0)]
        public int spawnWeight = 1;
    }

    [Header("Enemy Prefabs")]
    [Tooltip("Available enemy prefabs for spawning.")]
    [SerializeField] private List<EnemySpawnEntry> enemyEntries = new List<EnemySpawnEntry>();

    public GameObject SpawnEnemyAtPosition(Vector3 spawnPosition, WaveScalingData scalingData, int waveIndex)
    {
        GameObject enemyPrefab = GetRandomEnemyPrefab(waveIndex);

        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{name}: EnemySpawner could not resolve enemy prefab.");
            return null;
        }

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

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

        return spawnedEnemy;
    }

    private GameObject GetRandomEnemyPrefab(int waveIndex)
    {
        if (enemyEntries == null || enemyEntries.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0; i < enemyEntries.Count; i++)
        {
            EnemySpawnEntry entry = enemyEntries[i];

            if (entry == null)
            {
                continue;
            }

            if (entry.enemyPrefab == null)
            {
                continue;
            }

            if (entry.spawnWeight <= 0)
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

            if (entry == null)
            {
                continue;
            }

            if (entry.enemyPrefab == null)
            {
                continue;
            }

            if (entry.spawnWeight <= 0)
            {
                continue;
            }

            int scaledWeight = GetScaledWeight(entry, waveIndex);
            currentWeight += scaledWeight;

            if (randomValue < currentWeight)
            {
                return entry.enemyPrefab;
            }
        }

        return null;
    }

    private int GetScaledWeight(EnemySpawnEntry entry, int waveIndex)
    {
        if (entry == null)
        {
            return 0;
        }

        int scaledWeight = entry.spawnWeight;
        string enemyName = entry.enemyPrefab.name;

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
}