using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(JaggernautController))]
[RequireComponent(typeof(JaggernautDestroy))]
[RequireComponent(typeof(EnemyDeathNotifier))]
[RequireComponent(typeof(PhotonView))]
public class JaggernautHealth : MonoBehaviour, IDamageable, IWaveScalable
{
    [Header("Config Source")]
    [Tooltip("Jaggernaut config with health settings.")]
    [SerializeField] private JaggernautConfig configJaggernautSO;

    [Header("Health Settings")]
    [Tooltip("Maximum health of the enemy.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Current health of the enemy.")]
    [SerializeField] private float currentHealth;

    [Tooltip("Enable debug logs for damage.")]
    [SerializeField] private bool debugDamage;

    [Header("Score Settings")]
    [Tooltip("Fallback base score reward used when config is missing.")]
    [SerializeField] private float baseScoreReward = 200f;

    [Tooltip("Fallback score multiplier used when config is missing.")]
    [SerializeField] private float scoreMultiplier = 1f;

    [Header("Wave Scaling")]
    [Tooltip("Runtime health multiplier applied from the current wave.")]
    [SerializeField] private float healthMultiplier = 1f;

    [Tooltip("Runtime score multiplier applied from the current wave.")]
    [SerializeField] private float waveScoreMultiplier = 1f;

    [Header("References")]
    [Tooltip("Jaggernaut animator.")]
    [SerializeField] private Animator jaggernautAnimator;

    [Tooltip("Jaggernaut Controller script.")]
    [SerializeField] private JaggernautController jaggernautController;

    [Tooltip("Jaggernaut destroy script.")]
    [SerializeField] private JaggernautDestroy jaggernautDestroy;

    [Tooltip("Reference to the EnemyDeathNotifier.")]
    [SerializeField] private EnemyDeathNotifier deathNotifier;

    private PhotonView photonView;
    private bool isDead;

    public bool IsDead
    {
        get { return isDead; }
        private set { isDead = value; }
    }

    public float MaxHealth
    {
        get { return maxHealth; }
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
        get { return currentHealth; }
        private set
        {
            if (value < 0f)
            {
                currentHealth = 0f;
            }
            else if (value > MaxHealth)
            {
                currentHealth = MaxHealth;
            }
            else
            {
                currentHealth = value;
            }
        }
    }

    private void ApplyConfig()
    {
        if (configJaggernautSO == null)
        {
            return;
        }

        MaxHealth = configJaggernautSO.maxHealth;
    }

    private void RebuildRuntimeHealth()
    {
        float baseHealth = 100f;

        if (configJaggernautSO != null)
        {
            baseHealth = configJaggernautSO.maxHealth;
        }

        MaxHealth = baseHealth * healthMultiplier;
        CurrentHealth = MaxHealth;
    }

    private void Reset()
    {
        if (jaggernautController == null)
        {
            jaggernautController = GetComponent<JaggernautController>();
        }

        jaggernautAnimator = GetComponent<Animator>();
    }

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        ApplyConfig();

        if (jaggernautController == null)
        {
            jaggernautController = GetComponent<JaggernautController>();
        }

        if (jaggernautDestroy == null)
        {
            jaggernautDestroy = GetComponent<JaggernautDestroy>();
        }

        if (deathNotifier == null)
        {
            deathNotifier = GetComponent<EnemyDeathNotifier>();
        }

        if (jaggernautAnimator == null)
        {
            jaggernautAnimator = GetComponent<Animator>();
        }

        RebuildRuntimeHealth();
    }

    private void Start()
    {
        NotifyHealthChanged();
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

        NotifyHealthChanged();
        TryAwardScore(damageToApply, damageDealer);

        if (debugDamage)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log($"{gameObject.name} took {damageToApply} damage. HP: {CurrentHealth}/{MaxHealth}");
            }
            
        }

        if (CurrentHealth > 0f)
        {
            return;
        }

        CurrentHealth = 0f;

        if (PhotonNetwork.InRoom == true)
        {
            if (PhotonNetwork.IsMasterClient == false)
            {
                return;
            }

            photonView.RPC(nameof(RPC_Die), RpcTarget.All);
            return;
        }

        DieLocal();
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

        if (configJaggernautSO != null)
        {
            finalScoreReward = configJaggernautSO.baseScoreReward;
            rewardMultiplier = configJaggernautSO.scoreMultiplier;
        }

        return (damageDealt / MaxHealth) * finalScoreReward * rewardMultiplier * waveScoreMultiplier;
    }

    [PunRPC]
    private void RPC_Die()
    {
        if (IsDead)
        {
            return;
        }

        DieLocal();
    }

    private void DieLocal()
    {
        IsDead = true;

        if (jaggernautController != null)
        {
            jaggernautController.SetDeadState(IsDead);
        }

        if (deathNotifier != null)
        {
            deathNotifier.NotifyDeath();
        }

        if (jaggernautDestroy != null)
        {
            jaggernautDestroy.StartDestroy(debugDamage, jaggernautController.CurrentWaveIndex);
        }
    }

    private void NotifyHealthChanged()
    {
        if (jaggernautController == null)
        {
            return;
        }

        jaggernautController.ApplyHealthBasedScaling(CurrentHealth, MaxHealth);
    }

    private void OnValidate()
    {
        MaxHealth = Mathf.Max(1f, maxHealth);
        CurrentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
    }

    public void ApplyWaveScaling(WaveScalingData scalingData)
    {
        healthMultiplier = scalingData.HealthMultiplier;
        waveScoreMultiplier = scalingData.ScoreMultiplier;
        RebuildRuntimeHealth();
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        if (PhotonNetwork.InRoom == true)
        {
            if (PhotonNetwork.IsMasterClient == false)
            {
                return;
            }

            photonView.RPC(nameof(RPC_Die), RpcTarget.All);
            return;
        }

        DieLocal();
    }
}