using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(SaberwingDestroy))]
[RequireComponent(typeof(EnemyDeathNotifier))]
[RequireComponent(typeof(SaberwingController))]
public class SaberwingHealth : MonoBehaviour, IDamageable, IWaveScalable
{
    [Header("Config Source")]
    [Tooltip("Saberwing config with health settings.")]
    [SerializeField] private SaberwingConfig configSaberwingSO;

    [Header("Health Settings")]
    [Tooltip("Maximum health of the enemy.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Current health of the enemy.")]
    [SerializeField] private float currentHealth;

    [Tooltip("Enable debug logs for damage.")]
    [SerializeField] private bool debugDamage;

    [Header("Score Settings")]
    [Tooltip("Fallback base score reward used when config is missing.")]
    [SerializeField] private float baseScoreReward = 80f;

    [Tooltip("Fallback score multiplier used when config is missing.")]
    [SerializeField] private float scoreMultiplier = 1f;

    [Header("Wave Scaling")]
    [Tooltip("Runtime health multiplier applied from the current wave.")]
    [SerializeField] private float healthMultiplier = 1f;

    [Tooltip("Runtime score multiplier applied from the current wave.")]
    [SerializeField] private float waveScoreMultiplier = 1f;

    [Header("References")]
    [Tooltip("Reference to the SaberwingController script.")]
    [SerializeField] private SaberwingController saberwingController;

    [Tooltip("Reference to the SaberwingDestroy script.")]
    [SerializeField] private SaberwingDestroy saberwingDestroy;

    [Tooltip("Reference to the EnemyDeathNotifier.")]
    [SerializeField] private EnemyDeathNotifier deathNotifier;

    [Tooltip("PhotonView reference for multiplayer death sync.")]
    [SerializeField] private PhotonView photonView;

    private bool isDead;

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
        if (saberwingDestroy == null)
        {
            saberwingDestroy = GetComponent<SaberwingDestroy>();
        }

        if (deathNotifier == null)
        {
            deathNotifier = GetComponent<EnemyDeathNotifier>();
        }

        if (saberwingController == null)
        {
            saberwingController = GetComponent<SaberwingController>();
        }

        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }
    }

    private void Awake()
    {
        if (saberwingDestroy == null)
        {
            saberwingDestroy = GetComponent<SaberwingDestroy>();
        }

        if (deathNotifier == null)
        {
            deathNotifier = GetComponent<EnemyDeathNotifier>();
        }

        if (saberwingController == null)
        {
            saberwingController = GetComponent<SaberwingController>();
        }

        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        ApplyConfig();
        RebuildRuntimeHealth();
    }

    private void ApplyConfig()
    {
        if (configSaberwingSO == null)
        {
            return;
        }

        MaxHealth = configSaberwingSO.maxHealth;
    }

    private void RebuildRuntimeHealth()
    {
        float baseHealth = 100f;

        if (configSaberwingSO != null)
        {
            baseHealth = configSaberwingSO.maxHealth;
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

        if (debugDamage)
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

        if (configSaberwingSO != null)
        {
            finalScoreReward = configSaberwingSO.baseScoreReward;
            rewardMultiplier = configSaberwingSO.scoreMultiplier;
        }

        return (damageDealt / MaxHealth) * finalScoreReward * rewardMultiplier * waveScoreMultiplier;
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView == null)
            {
                ExecuteDeathSequence();
                return;
            }

            photonView.RPC(
                nameof(Die_RPC),
                RpcTarget.All,
                debugDamage,
                saberwingController.CurrentWaveIndex
            );

            return;
        }

        ExecuteDeathSequence();
    }

    [PunRPC]
    private void Die_RPC(bool rpcDebugDamage, int rpcWaveIndex)
    {
        ExecuteDeathSequence(rpcDebugDamage, rpcWaveIndex);
    }

    private void ExecuteDeathSequence()
    {
        int waveIndex = 0;

        if (saberwingController != null)
        {
            waveIndex = saberwingController.CurrentWaveIndex;
        }

        ExecuteDeathSequence(debugDamage, waveIndex);
    }

    private void ExecuteDeathSequence(bool deathDebugLog, int waveIndex)
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (saberwingController != null)
        {
            saberwingController.SetDeadState(IsDead);
        }

        if (deathNotifier != null)
        {
            deathNotifier.NotifyDeath();
        }

        if (saberwingDestroy != null)
        {
            saberwingDestroy.StartDestroy(deathDebugLog, waveIndex);
        }
    }
}