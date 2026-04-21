using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyDeathNotifier))]
[RequireComponent(typeof(HotshotController))]
[RequireComponent(typeof(HotshotShooter))]
public class HotshotHealth : MonoBehaviour, IDamageable, IWaveScalable
{
    [Header("Config source")]
    [Tooltip("Hotshot config with health settings.")]
    [SerializeField] private HotshotConfig configHotshotSO;

    [Header("Health Settings")]
    [Tooltip("Maximum health value.")]
    [Range(1f, 1000f)]
    [SerializeField] private float maxHealth = 30f;

    [Tooltip("Current health value.")]
    [SerializeField] private float currentHealth;

    [Header("Score Settings")]
    [Tooltip("Fallback base score reward used when config is missing.")]
    [SerializeField] private float baseScoreReward = 100f;

    [Tooltip("Fallback score multiplier used when config is missing.")]
    [SerializeField] private float scoreMultiplier = 1f;

    [Header("Wave Scaling")]
    [Tooltip("Runtime health multiplier applied from the current wave.")]
    [SerializeField] private float healthMultiplier = 1f;

    [Tooltip("Runtime score multiplier applied from the current wave.")]
    [SerializeField] private float waveScoreMultiplier = 1f;

    [Header("References")]
    [Tooltip("Reference to the Hotshot controller.")]
    [SerializeField] private HotshotController hotshotController;

    [Tooltip("Reference to the Hotshot shooter.")]
    [SerializeField] private HotshotShooter hotshotShooter;

    [Tooltip("Reference to the Hotshot destroy.")]
    [SerializeField] private HotshotDestroy hotshotDestroy;

    [Tooltip("Reference to the EnemyDeathNotifier.")]
    [SerializeField] private EnemyDeathNotifier deathNotifier;

    [Header("Debug")]
    [Tooltip("Log health changes to Console.")]
    [SerializeField] private bool logChanges = true;

    private bool isDead = false;

    public bool IsDead
    {
        get
        {
            return isDead;
        }
        private set
        {
            isDead = value;
        }
    }

    public float MaxHealth
    {
        get
        {
            return maxHealth;
        }
        private set
        {
            if (value <= 0f)
            {
                maxHealth = 100f;
            }
            else
            {
                maxHealth = value;
            }
        }
    }

    public float CurrentHealth
    {
        get
        {
            return currentHealth;
        }
        private set
        {
            if (value < 0f)
            {
                currentHealth = 0f;
            }
            else
            {
                currentHealth = value;
            }
        }
    }

    private void Reset()
    {
        if (hotshotController == null)
        {
            hotshotController = GetComponent<HotshotController>();
        }

        if (hotshotShooter == null)
        {
            hotshotShooter = GetComponent<HotshotShooter>();
        }

        if (deathNotifier == null)
        {
            deathNotifier = GetComponent<EnemyDeathNotifier>();
        }
    }

    private void Awake()
    {
        if (hotshotController == null)
        {
            hotshotController = GetComponent<HotshotController>();
        }

        if (hotshotShooter == null)
        {
            hotshotShooter = GetComponent<HotshotShooter>();
        }

        if (deathNotifier == null)
        {
            deathNotifier = GetComponent<EnemyDeathNotifier>();
        }

        if (hotshotDestroy == null)
        {
            hotshotDestroy = GetComponent<HotshotDestroy>();
        }

        RebuildRuntimeHealth();
    }

    private void RebuildRuntimeHealth()
    {
        float baseHealth = 30f;

        if (configHotshotSO != null)
        {
            baseHealth = configHotshotSO.maxHealth;
        }

        MaxHealth = baseHealth * healthMultiplier;
        CurrentHealth = MaxHealth;
    }

    public void ApplyWaveScaling(WaveScalingData scalingData)
    {
        healthMultiplier = scalingData.HealthMultiplier;
        waveScoreMultiplier = scalingData.ScoreMultiplier;
        RebuildRuntimeHealth();
    }

    public void ApplyDamage(float damage)
    {
        ApplyDamage(damage, null);
    }

    public void ApplyDamage(float damage, GameObject damageDealer)
    {
        if (IsDead)
        {
            return;
        }

        if (damage <= 0f)
        {
            return;
        }

        float damageToApply = Mathf.Min(damage, CurrentHealth);
        CurrentHealth -= damageToApply;

        TryAwardScore(damageToApply, damageDealer);

        if (logChanges)
        {
            Debug.Log($"{gameObject.name} took {damageToApply} damage. HP: {CurrentHealth}/{MaxHealth}");
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    private void TryAwardScore(float damageDealt, GameObject damageDealer)
    {
        if (damageDealer == null)
        {
            return;
        }

        PlayerScoreOwner scoreOwner = damageDealer.GetComponent<PlayerScoreOwner>();

        if (scoreOwner == null)
        {
            return;
        }

        float scoreGain = CalculateScoreGain(damageDealt);
        scoreOwner.AddScore(scoreGain);
    }

    private float CalculateScoreGain(float damageDealt)
    {
        if (damageDealt <= 0f)
        {
            return 0f;
        }

        if (MaxHealth <= 0f)
        {
            return 0f;
        }

        float finalScoreReward = baseScoreReward;
        float rewardMultiplier = scoreMultiplier;

        if (configHotshotSO != null)
        {
            finalScoreReward = configHotshotSO.baseScoreReward;
            rewardMultiplier = configHotshotSO.scoreMultiplier;
        }

        return (damageDealt / MaxHealth) * finalScoreReward * rewardMultiplier * waveScoreMultiplier;
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (deathNotifier != null)
        {
            deathNotifier.NotifyDeath();
        }

        if (hotshotController != null)
        {
            hotshotController.SetDeadState(true);
        }

        if (hotshotShooter != null)
        {
            hotshotShooter.SetDeadState(true);
        }

        if (hotshotDestroy != null)
        {
            hotshotDestroy.StartDestroy(logChanges, hotshotController.CurrentWaveIndex);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}