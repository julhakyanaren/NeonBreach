using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public static event Action PlayerDied;

    [Header("Config")]
    [Tooltip("Player config with base stats.")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Health Runtime Settings")]
    [Tooltip("Base maximum health value loaded from config.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("Absolute health limit value loaded from config.")]
    [SerializeField] private float maxHealthLimit = 1000f;

    [Header("Defense Settings")]
    [Tooltip("Current incoming damage multiplier. 1 = normal damage, lower values reduce damage.")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Debug")]
    [Tooltip("Log health changes to Console.")]
    [SerializeField] private bool logChanges = true;

    [Tooltip("Make player invincible.")]
    [SerializeField] private bool makeInvincible = false;

    private float currentHealth;
    private bool isDead = false;

    public event Action<float, float> HealthChanged;

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

    public bool IsTargetable { get; private set; }

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

    public float MaxHealthLimit
    {
        get
        {
            return maxHealthLimit;
        }
        private set
        {
            if (value <= 0f)
            {
                maxHealthLimit = 1000f;
            }
            else if (value < MaxHealth)
            {
                maxHealthLimit = MaxHealth;
            }
            else
            {
                maxHealthLimit = value;
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
            else if (value > MaxHealthLimit)
            {
                currentHealth = MaxHealthLimit;
            }
            else
            {
                currentHealth = value;
            }
        }
    }

    private void Awake()
    {
        ApplyConfig();
        currentHealth = maxHealth;
        NotifyHealthChanged();
    }

    public void EnableTargeting()
    {
        IsTargetable = true;
    }

    public void DisableTargeting()
    {
        IsTargetable = false;
    }

    private void ApplyConfig()
    {
        if (playerConfig == null)
        {
            return;
        }

        MaxHealth = playerConfig.maxHealth;
        MaxHealthLimit = playerConfig.maxHealthLimit;
        DisableTargeting();
    }

    public void ApplyDamage(float damage, GameObject damageDealer)
    {
        if (makeInvincible)
        {
            return;
        }

        if (IsDead)
        {
            return;
        }

        if (damage <= 0f)
        {
            return;
        }

        float finalDamage = damage * damageMultiplier;

        CurrentHealth -= finalDamage;
        NotifyHealthChanged();

        if (logChanges)
        {
            Debug.Log($"{gameObject.name} HP: {CurrentHealth}/{MaxHealth} (Limit: {MaxHealthLimit})");
        }

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }
    public void ApplyDamage(float damage)
    {
        ApplyDamage(damage, null);
    }

    public void RestoreHealth(float amount)
    {
        if (IsDead)
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        if (CurrentHealth >= MaxHealthLimit)
        {
            return;
        }

        CurrentHealth += amount;
        NotifyHealthChanged();

        if (logChanges)
        {
            Debug.Log($"{gameObject.name} healed: {CurrentHealth}/{MaxHealth} (Limit: {MaxHealthLimit})");
        }
    }

    public void SetDamageMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        damageMultiplier = multiplier;
    }

    public void ResetDamageMultiplier()
    {
        damageMultiplier = 1f;
    }

    public void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        CurrentHealth = 0f;
        NotifyHealthChanged();

        PlayerDied?.Invoke();

        Debug.Log($"{gameObject.name} destroyed");
        DisableTargeting();
    }

    private void NotifyHealthChanged()
    {
        if (HealthChanged == null)
        {
            return;
        }

        HealthChanged(CurrentHealth, MaxHealth);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}