using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public event Action<int> WaveStarted;
    public event Action<int> WaveCompleted;
    public event Action<int> WaveChanged;

    [Header("Wave Settings")]
    [Tooltip("Wave number starts from this value.")]
    [SerializeField] private int startingWaveIndex = 1;

    [Tooltip("Base enemy count used in formula: baseEnemyCount + waveIndex * enemyCountMultiplier.")]
    [SerializeField] private int baseEnemyCount = 3;

    [Tooltip("Enemy count multiplier used in formula: baseEnemyCount + waveIndex * enemyCountMultiplier.")]
    [SerializeField] private int enemyCountMultiplier = 2;

    [Tooltip("Delay before the first wave starts.")]
    [SerializeField] private float startDelay = 1f;

    [Tooltip("Delay between waves.")]
    [SerializeField] private float timeBetweenWaves = 2f;

    [Tooltip("Delay between individual enemy spawns.")]
    [SerializeField] private float spawnInterval = 0.35f;

    [Header("Wave Scaling")]
    [Tooltip("Additional health multiplier added each wave. Example: 0.15 means +15% HP per wave.")]
    [SerializeField] private float healthMultiplierPerWave = 0.15f;

    [Tooltip("Additional damage multiplier added each wave. Example: 0.10 means +10% damage per wave.")]
    [SerializeField] private float damageMultiplierPerWave = 0.10f;

    [Tooltip("Maximum allowed health multiplier.")]
    [SerializeField] private float maxHealthMultiplier = 5f;

    [Tooltip("Maximum allowed damage multiplier.")]
    [SerializeField] private float maxDamageMultiplier = 4f;

    [Tooltip("Additional fire rate multiplier added each wave.")]
    [SerializeField] private float fireRateMultiplierPerWave = 0.08f;

    [Tooltip("Maximum allowed fire rate multiplier.")]
    [SerializeField] private float maxFireRateMultiplier = 3f;

    [Tooltip("Additional score multiplier added each wave.")]
    [SerializeField] private float scoreMultiplierPerWave = 0.1f;

    [Tooltip("Maximum allowed score multiplier.")]
    [SerializeField] private float maxScoreMultiplier = 3f;

    [Header("References")]
    [Tooltip("Reference to the enemy spawner.")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Tooltip("Reference to the spawn position generator.")]
    [SerializeField] private SpawnPositionGenerator spawnPositionGenerator;

    [Header("Runtime Debug")]
    [Tooltip("Starts wave loop automatically on Start.")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("Current active wave index.")]
    [SerializeField] private int currentWaveIndex;

    [Tooltip("Is a wave currently running.")]
    [SerializeField] private bool isWaveRunning;

    [Tooltip("Current alive enemies count.")]
    [SerializeField] private int aliveEnemiesCount;

    private Coroutine waveLoopCoroutine;
    private readonly List<EnemyDeathNotifier> aliveEnemies = new List<EnemyDeathNotifier>();

    public int CurrentWave
    {
        get
        {
            return currentWaveIndex;
        }
    }

    public int CurrentWaveIndex
    {
        get
        {
            return currentWaveIndex;
        }
    }

    public bool IsWaveRunning
    {
        get
        {
            return isWaveRunning;
        }
    }

    public int AliveEnemiesCount
    {
        get
        {
            return aliveEnemiesCount;
        }
    }

    private void Awake()
    {
        startingWaveIndex = Mathf.Max(1, startingWaveIndex);
        baseEnemyCount = Mathf.Max(0, baseEnemyCount);
        enemyCountMultiplier = Mathf.Max(0, enemyCountMultiplier);
        startDelay = Mathf.Max(0f, startDelay);
        timeBetweenWaves = Mathf.Max(0f, timeBetweenWaves);
        spawnInterval = Mathf.Max(0f, spawnInterval);

        healthMultiplierPerWave = Mathf.Max(0f, healthMultiplierPerWave);
        damageMultiplierPerWave = Mathf.Max(0f, damageMultiplierPerWave);
        maxHealthMultiplier = Mathf.Max(1f, maxHealthMultiplier);
        maxDamageMultiplier = Mathf.Max(1f, maxDamageMultiplier);

        fireRateMultiplierPerWave = Mathf.Max(0f, fireRateMultiplierPerWave);
        maxFireRateMultiplier = Mathf.Max(1f, maxFireRateMultiplier);

        currentWaveIndex = startingWaveIndex - 1;
    }

    private void Start()
    {
        if (!autoStart)
        {
            return;
        }
        StartWaveLoop();
    }

    public void StartWaveLoop()
    {
        if (waveLoopCoroutine != null)
        {
            return;
        }

        waveLoopCoroutine = StartCoroutine(WaveLoop());
    }

    public void StopWaveLoop()
    {
        if (waveLoopCoroutine == null)
        {
            return;
        }

        StopCoroutine(waveLoopCoroutine);
        waveLoopCoroutine = null;
        isWaveRunning = false;
    }

    private IEnumerator WaveLoop()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            currentWaveIndex++;
            NotifyWaveChanged();

            yield return StartCoroutine(RunWave(currentWaveIndex));

            if (timeBetweenWaves > 0f)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
    }

    private IEnumerator RunWave(int waveIndex)
    {
        isWaveRunning = true;

        int enemyCount = GetEnemyCountForWave(waveIndex);
        WaveScalingData scalingData = GetScalingForWave(waveIndex);

        if (WaveStarted != null)
        {
            WaveStarted.Invoke(waveIndex);
        }

        Debug.Log($"Wave {waveIndex} started. Enemy count: {enemyCount}");
        Debug.Log($"Wave {waveIndex} scaling: HP x{scalingData.HealthMultiplier}, DMG x{scalingData.DamageMultiplier}");

        List<Vector3> spawnPositions = GenerateSpawnPositionsForWave(enemyCount);

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            Vector3 spawnPosition = spawnPositions[i];

            if (enemySpawner != null)
            {
                GameObject spawnedEnemy = enemySpawner.SpawnEnemyAtPosition(spawnPosition, scalingData, waveIndex);
                RegisterSpawnedEnemy(spawnedEnemy);
            }

            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }

        while (aliveEnemiesCount > 0)
        {
            yield return null;
        }

        isWaveRunning = false;

        GameSessionStats stats = GameSessionStats.Instance;

        if (stats != null)
        {
            stats.SetWave(waveIndex);
        }

        if (WaveCompleted != null)
        {
            WaveCompleted.Invoke(waveIndex);
        }
    }

    public int GetEnemyCountForWave(int waveIndex)
    {
        int result = baseEnemyCount + waveIndex * enemyCountMultiplier;
        return Mathf.Max(0, result);
    }

    public WaveScalingData GetScalingForWave(int waveIndex)
    {
        float healthMultiplier = 1f + waveIndex * healthMultiplierPerWave;
        float damageMultiplier = 1f + waveIndex * damageMultiplierPerWave;
        float fireRateMultiplier = 1f + waveIndex * fireRateMultiplierPerWave;
        float scoreMultiplier = 1f + waveIndex * scoreMultiplierPerWave;

        if (healthMultiplier > maxHealthMultiplier)
        {
            healthMultiplier = maxHealthMultiplier;
        }

        if (damageMultiplier > maxDamageMultiplier)
        {
            damageMultiplier = maxDamageMultiplier;
        }

        if (fireRateMultiplier > maxFireRateMultiplier)
        {
            fireRateMultiplier = maxFireRateMultiplier;
        }

        if (scoreMultiplier > maxScoreMultiplier)
        {
            scoreMultiplier = maxScoreMultiplier;
        }

        WaveScalingData scalingData = new WaveScalingData(
            healthMultiplier,
            damageMultiplier,
            fireRateMultiplier,
            scoreMultiplier);

        return scalingData;
    }

    private List<Vector3> GenerateSpawnPositionsForWave(int enemyCount)
    {
        if (spawnPositionGenerator == null)
        {
            Debug.LogWarning($"{name}: WaveManager has no SpawnPositionGenerator assigned.");
            return new List<Vector3>();
        }

        List<Vector3> positions = spawnPositionGenerator.GenerateSpawnPositions(enemyCount);
        return positions;
    }

    private void RegisterSpawnedEnemy(GameObject spawnedEnemy)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        EnemyDeathNotifier deathNotifier = spawnedEnemy.GetComponent<EnemyDeathNotifier>();

        if (deathNotifier == null)
        {
            Debug.LogWarning($"{name}: Spawned enemy '{spawnedEnemy.name}' has no EnemyDeathNotifier.");
            return;
        }

        deathNotifier.ResetState();

        if (aliveEnemies.Contains(deathNotifier))
        {
            return;
        }

        aliveEnemies.Add(deathNotifier);
        deathNotifier.Died += OnEnemyDied;
        aliveEnemiesCount = aliveEnemies.Count;
    }

    private void OnEnemyDied(EnemyDeathNotifier deathNotifier)
    {
        if (deathNotifier == null)
        {
            return;
        }

        if (!aliveEnemies.Contains(deathNotifier))
        {
            return;
        }

        GameSessionStats stats = GameSessionStats.Instance;

        if (stats != null)
        {
            stats.AddKill();
        }

        deathNotifier.Died -= OnEnemyDied;
        aliveEnemies.Remove(deathNotifier);
        aliveEnemiesCount = aliveEnemies.Count;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyDeathNotifier notifier = aliveEnemies[i];

            if (notifier == null)
            {
                continue;
            }

            notifier.Died -= OnEnemyDied;
        }

        aliveEnemies.Clear();
        aliveEnemiesCount = 0;
    }

    private void NotifyWaveChanged()
    {
        if (WaveChanged == null)
        {
            return;
        }

        WaveChanged(CurrentWave);
    }
}