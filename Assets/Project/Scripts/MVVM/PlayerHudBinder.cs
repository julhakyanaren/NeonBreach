using System.Collections;
using UnityEngine;

public class PlayerHudBinder : MonoBehaviour
{
    [Header("ViewModel")]
    [Tooltip("Reference to the HUD ViewModel.")]
    [SerializeField] private PlayerHudViewModel viewModel;

    [Header("Gameplay References")]
    [Tooltip("Reference to the player health component.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Reference to the player shooter component.")]
    [SerializeField] private PlayerShooter playerShooter;

    [Tooltip("Reference to the wave manager component.")]
    [SerializeField] private WaveManager waveManager;

    [Tooltip("Reference to the player buff receiver component.")]
    [SerializeField] private PlayerBuffReceiver playerBuffReceiver;

    [Tooltip("Reference to the score manager component.")]
    [SerializeField] private ScoreManager scoreManager;

    [Header("Initialization")]
    [Tooltip("Delay before HUD tries to find runtime-spawned references.")]
    [Range(0f, 2f)]
    [SerializeField] private float bindDelay = 0.2f;

    private Coroutine bindRoutine;
    private bool initialized;

    private void OnEnable()
    {
        bindRoutine = StartCoroutine(BindRoutine());
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        UnsubscribeAll();
        initialized = false;
    }

    private IEnumerator BindRoutine()
    {
        yield return new WaitForSecondsRealtime(bindDelay);

        ResolveReferences();
        SubscribeAll();
        RefreshAll();

        initialized = true;
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        if (playerShooter == null)
        {
            playerShooter = FindFirstObjectByType<PlayerShooter>();
        }

        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }

        if (playerBuffReceiver == null)
        {
            playerBuffReceiver = FindFirstObjectByType<PlayerBuffReceiver>();
        }

        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
    }

    private void SubscribeAll()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            playerHealth.HealthChanged += HandleHealthChanged;
        }

        if (playerShooter != null)
        {
            playerShooter.AmmoChanged -= HandleAmmoChanged;
            playerShooter.AmmoChanged += HandleAmmoChanged;
        }

        if (waveManager != null)
        {
            waveManager.WaveChanged -= HandleWaveChanged;
            waveManager.WaveChanged += HandleWaveChanged;
        }

        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged -= HandleScoreChanged;
            scoreManager.OnScoreChanged += HandleScoreChanged;
        }
    }

    private void UnsubscribeAll()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
        }

        if (playerShooter != null)
        {
            playerShooter.AmmoChanged -= HandleAmmoChanged;
        }

        if (waveManager != null)
        {
            waveManager.WaveChanged -= HandleWaveChanged;
        }

        if (scoreManager != null)
        {
            scoreManager.OnScoreChanged -= HandleScoreChanged;
        }
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        RefreshBuffFills();
    }

    private void RefreshAll()
    {
        if (viewModel == null)
        {
            return;
        }

        if (playerHealth != null)
        {
            HandleHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        if (playerShooter != null)
        {
            HandleAmmoChanged(playerShooter.CurrentAmmo, playerShooter.MagazineSize);
        }

        if (waveManager != null)
        {
            HandleWaveChanged(waveManager.CurrentWave);
        }

        if (scoreManager != null)
        {
            HandleScoreChanged(scoreManager.CurrentScore);
        }

        RefreshBuffFills();
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (viewModel == null)
        {
            return;
        }

        int displayHealth = Mathf.FloorToInt(currentHealth);
        viewModel.HealthText = $"HEALTH: {displayHealth}";
    }

    private void HandleAmmoChanged(int currentAmmo, int magazineSize)
    {
        if (viewModel == null)
        {
            return;
        }

        viewModel.AmmoText = $"AMMO: {currentAmmo} | {magazineSize}";
    }

    private void HandleWaveChanged(int currentWave)
    {
        if (viewModel == null)
        {
            return;
        }

        viewModel.CurrentWaveText = $"{currentWave}";
    }

    private void HandleScoreChanged(int currentScore)
    {
        if (viewModel == null)
        {
            return;
        }

        viewModel.ScoreText = $"{currentScore}";
    }

    private void RefreshBuffFills()
    {
        if (viewModel == null)
        {
            return;
        }

        if (playerBuffReceiver == null)
        {
            viewModel.DamageBuffFill = 0f;
            viewModel.DefenseBuffFill = 0f;
            viewModel.SpeedBuffFill = 0f;
            viewModel.FireRateBuffFill = 0f;
            return;
        }

        viewModel.DamageBuffFill = playerBuffReceiver.DamageBuffFill;
        viewModel.DefenseBuffFill = playerBuffReceiver.DefenseBuffFill;
        viewModel.SpeedBuffFill = playerBuffReceiver.SpeedBuffFill;
        viewModel.FireRateBuffFill = playerBuffReceiver.FireRateBuffFill;
    }
}