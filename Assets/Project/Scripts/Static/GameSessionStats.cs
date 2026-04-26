using UnityEngine;

public class GameSessionStats : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Enable lifecycle logs for current game session stats.")]
    [SerializeField] private bool logLifecycle;

    public static GameSessionStats Instance { get; private set; }

    public int TotalKills { get; private set; }
    public int ShotsFired { get; private set; }
    public int ShotsHit { get; private set; }
    public int WavesSurvived { get; private set; }
    public float TimeSurvived { get; private set; }
    public int HealthPickupsCollected { get; private set; }
    public int DamagePickupsCollected { get; private set; }
    public int DefensePickupsCollected { get; private set; }
    public int FireRatePickupsCollected { get; private set; }
    public int SpeedPickupsCollected { get; private set; }
    public int TotalPickupsCollected { get; private set; }
    public float Score { get; private set; }

    private bool sessionFinished;

    private int lastObservedWaveIndex;
    private bool hasObservedWaveIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("GameSessionStats: Duplicate instance detected. Destroying duplicate.", this);
            }
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResetSession();

        if (logLifecycle)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log("GameSessionStats: Initialized.", this);
            }
        }
    }

    private void Update()
    {
        if (sessionFinished)
        {
            return;
        }

        TimeSurvived += Time.deltaTime;
    }

    public void AddKill()
    {
        TotalKills++;
    }

    public void AddShot()
    {
        ShotsFired++;
    }

    public void AddHit()
    {
        ShotsHit++;
    }

    public void AddPickup(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.Health:
                {
                    HealthPickupsCollected++;
                    break;
                }
            case PickupType.Damage:
                {
                    DamagePickupsCollected++;
                    break;
                }
            case PickupType.Defense:
                {
                    DefensePickupsCollected++;
                    break;
                }
            case PickupType.FireRate:
                {
                    FireRatePickupsCollected++;
                    break;
                }
            case PickupType.Speed:
                {
                    SpeedPickupsCollected++;
                    break;
                }
            default:
                {
                    if (RuntimeOptions.LoggingWarning)
                    {
                        Debug.LogWarning("GameSessionStats: Unknown pickup type received.");
                    }
                    break;
                }
        }

        TotalPickupsCollected++;
    }

    //public void SetWave(int waveNumber)
    //{
    //    if (waveNumber < 0)
    //    {
    //        waveNumber = 0;
    //    }

    //    WavesSurvived = waveNumber;
    //}

    public void RegisterWaveChange(int newWaveIndex)
    {
        if (newWaveIndex <= 0)
        {
            return;
        }

        if (!hasObservedWaveIndex)
        {
            hasObservedWaveIndex = true;
            lastObservedWaveIndex = newWaveIndex;
            return;
        }

        if (newWaveIndex > lastObservedWaveIndex)
        {
            WavesSurvived++;
        }

        lastObservedWaveIndex = newWaveIndex;
    }

    public float GetAccuracy()
    {
        if (ShotsFired <= 0)
        {
            return 0f;
        }

        return (float)ShotsHit / ShotsFired * 100f;
    }

    public void AddScore(float amount)
    {
        Score += amount;
    }

    public void ResetScore()
    {
        Score = 0f;
    }

    public void SetScore(float value)
    {
        if (value < 0f)
        {
            value = 0f;
        }

        Score = value;
    }

    public void FinishSession()
    {
        sessionFinished = true;
    }

    public void ResetSession()
    {
        TotalKills = 0;
        ShotsFired = 0;
        ShotsHit = 0;
        WavesSurvived = 0;
        TotalPickupsCollected = 0;
        HealthPickupsCollected = 0;
        DamagePickupsCollected = 0;
        DefensePickupsCollected = 0;
        FireRatePickupsCollected = 0;
        SpeedPickupsCollected = 0;
        TimeSurvived = 0f;
        lastObservedWaveIndex = 0;
        hasObservedWaveIndex = false;
        sessionFinished = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}