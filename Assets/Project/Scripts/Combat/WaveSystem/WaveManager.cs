using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using ExitGames.Client.Photon;

public class WaveManager : MonoBehaviourPunCallbacks
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
    private Coroutine waitForWavePropertyCoroutine;
    private readonly List<EnemyDeathNotifier> aliveEnemies = new List<EnemyDeathNotifier>();

    private const string CurrentWaveIndexRoomKey = "CurrentWaveIndex";

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

        scoreMultiplierPerWave = Mathf.Max(0f, scoreMultiplierPerWave);
        maxScoreMultiplier = Mathf.Max(1f, maxScoreMultiplier);

        currentWaveIndex = startingWaveIndex - 1;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.5f);

        LoadCurrentWaveFromRoom();

        if (!autoStart)
        {
            yield break;
        }

        if (CanRunWaveAuthority())
        {
            StartWaveLoop();
            yield break;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            waitForWavePropertyCoroutine = StartCoroutine(WaitForWaveRoomPropertyRoutine());
        }
    }

    private IEnumerator WaitForWaveRoomPropertyRoutine()
    {
        float timer = 0f;
        float timeout = 5f;

        while (timer < timeout)
        {
            LoadCurrentWaveFromRoom();

            if (currentWaveIndex > 0)
            {
                waitForWavePropertyCoroutine = null;
                yield break;
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        waitForWavePropertyCoroutine = null;

        if (RuntimeOptions.LoggingWarning)
        {
            Debug.LogWarning("WaveManager: Non-master client did not receive CurrentWaveIndex room property in time.", this);
        }
    }

    private void SaveCurrentWaveToRoom()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable();
        roomProperties[CurrentWaveIndexRoomKey] = currentWaveIndex;

        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
    }

    private void LoadCurrentWaveFromRoom()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        if (PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(CurrentWaveIndexRoomKey))
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("WaveManager: Room has no CurrentWaveIndex property yet.", this);
            }

            return;
        }

        object waveValue = PhotonNetwork.CurrentRoom.CustomProperties[CurrentWaveIndexRoomKey];

        if (waveValue is int)
        {
            int newWaveIndex = (int)waveValue;

            if (currentWaveIndex == newWaveIndex)
            {
                return;
            }

            currentWaveIndex = newWaveIndex;
            NotifyWaveChanged();
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (!propertiesThatChanged.ContainsKey(CurrentWaveIndexRoomKey))
        {
            return;
        }

        object waveValue = propertiesThatChanged[CurrentWaveIndexRoomKey];

        if (waveValue is int)
        {
            int newWaveIndex = (int)waveValue;

            if (currentWaveIndex == newWaveIndex)
            {
                return;
            }

            currentWaveIndex = newWaveIndex;
            NotifyWaveChanged();
        }
    }

    public void StartWaveLoop()
    {
        if (waveLoopCoroutine != null)
        {
            return;
        }

        if (!CanRunWaveAuthority())
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

    private bool CanRunWaveAuthority()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return true;
        }

        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        return true;
    }

    private IEnumerator WaveLoop()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (CanRunWaveAuthority())
        {
            currentWaveIndex++;
            SaveCurrentWaveToRoom();
            NotifyWaveChanged();

            yield return StartCoroutine(RunWave(currentWaveIndex));

            if (!CanRunWaveAuthority())
            {
                break;
            }

            if (timeBetweenWaves > 0f)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        waveLoopCoroutine = null;
        isWaveRunning = false;
    }

    private IEnumerator RunWave(int waveIndex)
    {
        if (!CanRunWaveAuthority())
        {
            yield break;
        }

        isWaveRunning = true;

        int enemyCount = GetEnemyCountForWave(waveIndex);
        WaveScalingData scalingData = GetScalingForWave(waveIndex);

        if (WaveStarted != null)
        {
            WaveStarted.Invoke(waveIndex);
        }

        if (RuntimeOptions.Logging)
        {
            Debug.Log($"Wave {waveIndex} started. Enemy count: {enemyCount}");
            Debug.Log($"Wave {waveIndex} scaling: HP x{scalingData.HealthMultiplier}, DMG x{scalingData.DamageMultiplier}");
        }

        List<Vector3> spawnPositions = GenerateSpawnPositionsForWave(enemyCount);

        for (int i = 0; i < spawnPositions.Count; i++)
        {
            if (!CanRunWaveAuthority())
            {
                yield break;
            }

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
            if (!CanRunWaveAuthority())
            {
                yield break;
            }

            yield return null;
        }

        isWaveRunning = false;

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
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning($"{name}: WaveManager has no SpawnPositionGenerator assigned.");
            }

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
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning($"{name}: Spawned enemy '{spawnedEnemy.name}' has no EnemyDeathNotifier.");
            }

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

    private void ClearAuthorityRuntimeState()
    {
        if (waveLoopCoroutine != null)
        {
            StopCoroutine(waveLoopCoroutine);
            waveLoopCoroutine = null;
        }

        if (waitForWavePropertyCoroutine != null)
        {
            StopCoroutine(waitForWavePropertyCoroutine);
            waitForWavePropertyCoroutine = null;
        }

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
        isWaveRunning = false;
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (PhotonNetwork.LocalPlayer == null)
        {
            return;
        }

        if (newMasterClient == null)
        {
            return;
        }

        if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
        {
            ClearAuthorityRuntimeState();
            LoadCurrentWaveFromRoom();

            if (autoStart)
            {
                StartWaveLoop();
            }

            if (RuntimeOptions.Logging)
            {
                Debug.Log("WaveManager: Local client became new MasterClient. Wave loop restarted.", this);
            }

            return;
        }

        StopWaveLoop();

        if (waitForWavePropertyCoroutine == null)
        {
            waitForWavePropertyCoroutine = StartCoroutine(WaitForWaveRoomPropertyRoutine());
        }
    }

    public override void OnLeftRoom()
    {
        ClearAuthorityRuntimeState();
    }

    private void OnDestroy()
    {
        ClearAuthorityRuntimeState();
    }

    private void NotifyWaveChanged()
    {
        GameSessionStats stats = GameSessionStats.Instance;

        if (stats != null)
        {
            stats.RegisterWaveChange(CurrentWave);
        }

        if (WaveChanged == null)
        {
            return;
        }

        WaveChanged(CurrentWave);
    }
}