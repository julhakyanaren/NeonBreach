using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerRotation))]
[RequireComponent(typeof(PlayerShooter))]
public class PlayerBuffReceiver : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the player health component.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Reference to the player movement component.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Tooltip("Reference to the player rotation component.")]
    [SerializeField] private PlayerRotation playerRotation;

    [Tooltip("Reference to the player shooter component.")]
    [SerializeField] private PlayerShooter playerShooter;

    [Tooltip("Reference to the player projectile pool.")]
    [SerializeField] private PlayerProjectilePool playerProjectilePool;

    [Header("Damage State")]
    [Tooltip("Is damage buff currently active.")]
    [SerializeField] private bool isDamageBoostActive;

    private Coroutine defenseCoroutine;
    private float defenseRemainingTime = 0f;
    private float defenseDuration = 0f;
    private float currentDefenseMultiplier = 1f;

    private Coroutine speedCoroutine;
    private float speedRemainingTime = 0f;
    private float speedDuration = 0f;
    private float currentSpeedMultiplier = 1f;

    private Coroutine damageCoroutine;
    private float damageRemainingTime = 0f;
    private float damageDuration = 0f;
    private float currentDamageMultiplier = 1f;

    private Coroutine fireRateCoroutine;
    private float fireRateRemainingTime = 0f;
    private float fireRateDuration = 0f;
    private float currentFireRateMultiplier = 1f;

    public float DefenseBuffFill
    {
        get
        {
            if (defenseDuration <= 0f)
            {
                return 0f;
            }

            float value = defenseRemainingTime / defenseDuration;
            return Mathf.Clamp01(value);
        }
    }

    public float SpeedBuffFill
    {
        get
        {
            if (speedDuration <= 0f)
            {
                return 0f;
            }

            float value = speedRemainingTime / speedDuration;
            return Mathf.Clamp01(value);
        }
    }

    public float DamageBuffFill
    {
        get
        {
            if (damageDuration <= 0f)
            {
                return 0f;
            }

            float value = damageRemainingTime / damageDuration;
            return Mathf.Clamp01(value);
        }
    }

    public float FireRateBuffFill
    {
        get
        {
            if (fireRateDuration <= 0f)
            {
                return 0f;
            }

            float value = fireRateRemainingTime / fireRateDuration;
            return Mathf.Clamp01(value);
        }
    }

    public bool IsDamageBoostActive
    {
        get
        {
            return isDamageBoostActive;
        }
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        if (playerRotation == null)
        {
            playerRotation = GetComponent<PlayerRotation>();
        }

        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerShooter>();
        }

        if (playerProjectilePool == null)
        {
            playerProjectilePool = FindFirstObjectByType<PlayerProjectilePool>();
        }
    }

    private void Reset()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        playerRotation = GetComponent<PlayerRotation>();
        playerShooter = GetComponent<PlayerShooter>();
        playerProjectilePool = FindFirstObjectByType<PlayerProjectilePool>();
    }

    public void ApplyHealth(float amount)
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.RestoreHealth(amount);
    }

    public void ApplyDefense(float multiplier, float duration)
    {
        if (playerHealth == null)
        {
            return;
        }

        if (duration <= 0f)
        {
            return;
        }

        currentDefenseMultiplier = multiplier;
        defenseRemainingTime += duration;
        defenseDuration = defenseRemainingTime;

        playerHealth.SetDamageMultiplier(currentDefenseMultiplier);

        if (defenseCoroutine == null)
        {
            defenseCoroutine = StartCoroutine(DefenseRoutine());
        }
    }

    private IEnumerator DefenseRoutine()
    {
        while (defenseRemainingTime > 0f)
        {
            defenseRemainingTime -= Time.deltaTime;
            yield return null;
        }

        defenseRemainingTime = 0f;
        defenseDuration = 0f;
        currentDefenseMultiplier = 1f;

        if (playerHealth != null)
        {
            playerHealth.ResetDamageMultiplier();
        }

        defenseCoroutine = null;
    }

    public void ApplySpeed(float multiplier, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        currentSpeedMultiplier = multiplier;
        speedRemainingTime += duration;
        speedDuration = speedRemainingTime;

        if (playerMovement != null)
        {
            playerMovement.SetSpeedMultiplier(currentSpeedMultiplier);
        }

        if (playerRotation != null)
        {
            playerRotation.SetRotationMultiplier(currentSpeedMultiplier);
        }

        if (speedCoroutine == null)
        {
            speedCoroutine = StartCoroutine(SpeedRoutine());
        }
    }

    private IEnumerator SpeedRoutine()
    {
        while (speedRemainingTime > 0f)
        {
            speedRemainingTime -= Time.deltaTime;
            yield return null;
        }

        speedRemainingTime = 0f;
        speedDuration = 0f;
        currentSpeedMultiplier = 1f;

        if (playerMovement != null)
        {
            playerMovement.ResetSpeedMultiplier();
        }

        if (playerRotation != null)
        {
            playerRotation.ResetRotationMultiplier();
        }

        speedCoroutine = null;
    }

    public void ApplyDamage(float multiplier, float duration)
    {
        if (playerProjectilePool == null)
        {
            return;
        }

        if (duration <= 0f)
        {
            return;
        }

        currentDamageMultiplier = multiplier;
        damageRemainingTime += duration;
        damageDuration = damageRemainingTime;

        isDamageBoostActive = true;

        playerProjectilePool.SetProjectileDamageMultiplier(currentDamageMultiplier);
        playerProjectilePool.SetBoostedTrailState(true);

        if (damageCoroutine == null)
        {
            damageCoroutine = StartCoroutine(DamageRoutine());
        }
    }

    private IEnumerator DamageRoutine()
    {
        while (damageRemainingTime > 0f)
        {
            damageRemainingTime -= Time.deltaTime;
            yield return null;
        }

        damageRemainingTime = 0f;
        damageDuration = 0f;
        currentDamageMultiplier = 1f;
        isDamageBoostActive = false;

        if (playerProjectilePool != null)
        {
            playerProjectilePool.ResetProjectileDamage();
            playerProjectilePool.SetBoostedTrailState(false);
        }

        damageCoroutine = null;
    }

    public void ApplyFireRate(float multiplier, float duration)
    {
        if (playerShooter == null)
        {
            return;
        }

        if (duration <= 0f)
        {
            return;
        }

        currentFireRateMultiplier = multiplier;
        fireRateRemainingTime += duration;
        fireRateDuration = fireRateRemainingTime;

        playerShooter.SetFireRateMultiplier(currentFireRateMultiplier);
        playerShooter.SetReloadMultiplier(currentFireRateMultiplier);

        if (fireRateCoroutine == null)
        {
            fireRateCoroutine = StartCoroutine(FireRateRoutine());
        }
    }

    private IEnumerator FireRateRoutine()
    {
        while (fireRateRemainingTime > 0f)
        {
            fireRateRemainingTime -= Time.deltaTime;
            yield return null;
        }

        fireRateRemainingTime = 0f;
        fireRateDuration = 0f;
        currentFireRateMultiplier = 1f;

        if (playerShooter != null)
        {
            playerShooter.ResetFireRateMultiplier();
            playerShooter.ResetReloadMultiplier();
        }

        fireRateCoroutine = null;
    }
}