using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreenController : MonoBehaviour
{
    public static event Action DeathScreenOpened;

    [Header("Canvas")]
    [Tooltip("Animator that controls switching to the death screen canvas.")]
    [SerializeField] private Animator deathScreenAnimator;

    [Tooltip("DeathScreen canvas")]
    [SerializeField] private GameObject deathScreenCanvas;

    [Tooltip("Animator trigger name used to open death screen.")]
    [SerializeField] private string showTriggerName = "ShowDeathScreen";

    [Tooltip("Optional animator trigger name used to hide death screen.")]
    [SerializeField] private string hideTriggerName = "";

    [Header("Scene Loading")]
    [Tooltip("Main menu scene name.")]
    [SerializeField] private string menuSceneName = "MenuScene";

    [Header("Gameplay Stats Text")]
    [Tooltip("Text field for total kills.")]
    [SerializeField] private TMP_Text totalKillsText;

    [Tooltip("Text field for shots fired.")]
    [SerializeField] private TMP_Text shotsFiredText;

    [Tooltip("Text field for shots hit.")]
    [SerializeField] private TMP_Text shotsHitText;

    [Tooltip("Text field for accuracy percent.")]
    [SerializeField] private TMP_Text accuracyText;

    [Tooltip("Text field for survived waves.")]
    [SerializeField] private TMP_Text wavesSurvivedText;

    [Tooltip("Text field for survived time.")]
    [SerializeField] private TMP_Text timeSurvivedText;

    [Tooltip("Text field for score time.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Pickups collected Stats Text")]
    [Tooltip("Text field for collected health pickups.")]
    [SerializeField] private TMP_Text healthPickupsCollectedText;

    [Tooltip("Text field for collected damage pickups.")]
    [SerializeField] private TMP_Text damagePickupsCollectedText;

    [Tooltip("Text field for collected defense pickups.")]
    [SerializeField] private TMP_Text defensePickupsCollectedText;

    [Tooltip("Text field for collected fire rate pickups.")]
    [SerializeField] private TMP_Text fireRatePickupsCollectedText;

    [Tooltip("Text field for collected speed pickups.")]
    [SerializeField] private TMP_Text speedPickupsCollectedText;

    [Header("Gameplay")]
    [Tooltip("Block gameplay input when death screen is opened.")]
    [SerializeField] private bool blockInputOnDeath = true;

    [Tooltip("Pause gameplay with Time.timeScale when death screen is opened. Only used in singleplayer.")]
    [SerializeField] private bool stopTimeOnDeath = true;

    [Header("Debug")]
    [Tooltip("Log death screen opening.")]
    [SerializeField] private bool logStateChanges;

    private bool isOpened;
    private AsyncOperation menuSceneLoadOperation;
    private bool menuScenePreloadStarted;
    private bool menuSceneReady;

    public bool IsDead { get; private set; }

    public bool IsMenuSceneReady
    {
        get
        {
            return menuSceneReady;
        }
    }

    private void OnEnable()
    {
        PlayerHealth.PlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerHealth.PlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        if (isOpened)
        {
            return;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (!IsLocalPlayerDead())
            {
                return;
            }
        }

        isOpened = true;

        if (blockInputOnDeath)
        {
            RuntimeOptions.InputBlocked = true;
        }

        if (!RuntimeOptions.MultiplayerMode)
        {
            if (GameSessionStats.Instance != null)
            {
                GameSessionStats.Instance.FinishSession();
            }
        }

        if (deathScreenCanvas != null)
        {
            deathScreenCanvas.SetActive(true);
        }

        UpdateStatsView();
        OpenDeathScreen();

        if (!RuntimeOptions.MultiplayerMode)
        {
            PreloadMenuScene();

            if (stopTimeOnDeath)
            {
                Time.timeScale = 0f;
            }
        }

        if (logStateChanges)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log("DeathScreenController: Death screen opened.", this);
            }
        }
    }

    private bool IsLocalPlayerDead()
    {
        PlayerHealth[] playerHealths = FindObjectsOfType<PlayerHealth>(true);

        for (int i = 0; i < playerHealths.Length; i++)
        {
            PlayerHealth playerHealth = playerHealths[i];

            if (playerHealth == null)
            {
                continue;
            }

            PhotonView playerPhotonView = playerHealth.GetComponent<PhotonView>();

            if (playerPhotonView == null)
            {
                continue;
            }

            if (!playerPhotonView.IsMine)
            {
                continue;
            }

            if (playerHealth.IsDead)
            {
                return true;
            }
        }

        return false;
    }

    private void Update()
    {
        if (menuSceneLoadOperation == null)
        {
            return;
        }

        if (menuSceneReady)
        {
            return;
        }

        if (menuSceneLoadOperation.progress >= 0.9f)
        {
            menuSceneReady = true;

            if (logStateChanges)
            {
                if (RuntimeOptions.Logging)
                {
                    Debug.Log("DeathScreenController: Menu scene preloaded and ready.", this);
                }
            }
        }
    }

    private void UpdateStatsView()
    {
        if (GameSessionStats.Instance == null)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("DeathScreenController: GameSessionStats instance is missing.", this);
            }

            return;
        }

        GameSessionStats stats = GameSessionStats.Instance;

        if (totalKillsText != null)
        {
            totalKillsText.text = stats.TotalKills.ToString();
        }

        if (shotsFiredText != null)
        {
            shotsFiredText.text = stats.ShotsFired.ToString();
        }

        if (shotsHitText != null)
        {
            shotsHitText.text = stats.ShotsHit.ToString();
        }

        if (accuracyText != null)
        {
            accuracyText.text = stats.GetAccuracy().ToString("F1") + "%";
        }

        if (wavesSurvivedText != null)
        {
            wavesSurvivedText.text = stats.WavesSurvived.ToString();
        }

        if (healthPickupsCollectedText != null)
        {
            healthPickupsCollectedText.text = stats.HealthPickupsCollected.ToString();
        }

        if (damagePickupsCollectedText != null)
        {
            damagePickupsCollectedText.text = stats.DamagePickupsCollected.ToString();
        }

        if (defensePickupsCollectedText != null)
        {
            defensePickupsCollectedText.text = stats.DefensePickupsCollected.ToString();
        }

        if (fireRatePickupsCollectedText != null)
        {
            fireRatePickupsCollectedText.text = stats.FireRatePickupsCollected.ToString();
        }

        if (speedPickupsCollectedText != null)
        {
            speedPickupsCollectedText.text = stats.SpeedPickupsCollected.ToString();
        }

        if (scoreText != null)
        {
            scoreText.text = Mathf.FloorToInt(stats.Score).ToString();
        }

        if (timeSurvivedText != null)
        {
            int survivedSeconds = Mathf.FloorToInt(stats.TimeSurvived);
            int minutes = survivedSeconds / 60;
            int seconds = survivedSeconds % 60;

            timeSurvivedText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    private void OpenDeathScreen()
    {
        if (deathScreenAnimator != null)
        {
            deathScreenAnimator.ResetTrigger(showTriggerName);
            deathScreenAnimator.SetTrigger(showTriggerName);
        }
        else
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("DeathScreenController: Death screen animator is missing.", this);
            }
        }

        IsDead = true;
        DeathScreenOpened?.Invoke();
    }

    public void CloseAfterRespawn()
    {
        isOpened = false;
        IsDead = false;

        RuntimeOptions.InputBlocked = false;

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 1f;
        }

        if (deathScreenAnimator != null)
        {
            if (!string.IsNullOrWhiteSpace(hideTriggerName))
            {
                deathScreenAnimator.ResetTrigger(hideTriggerName);
                deathScreenAnimator.SetTrigger(hideTriggerName);
            }
        }

        if (deathScreenCanvas != null)
        {
            deathScreenCanvas.SetActive(false);
        }

        if (logStateChanges)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log("DeathScreenController: Death screen closed after respawn.", this);
            }
        }
    }

    private void PreloadMenuScene()
    {
        if (menuScenePreloadStarted)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("DeathScreenController: Menu scene name is empty.", this);
            }

            return;
        }

        menuSceneLoadOperation = SceneManager.LoadSceneAsync(menuSceneName);

        if (menuSceneLoadOperation == null)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("DeathScreenController: Failed to start menu scene preload.", this);
            }

            return;
        }

        menuSceneLoadOperation.allowSceneActivation = false;
        menuScenePreloadStarted = true;
        menuSceneReady = false;

        if (logStateChanges)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log("DeathScreenController: Started preloading menu scene.", this);
            }
        }
    }

    public void LoadMenuScene()
    {
        Time.timeScale = 1f;
        RuntimeOptions.InputBlocked = false;

        if (menuSceneLoadOperation != null && menuScenePreloadStarted)
        {
            menuSceneLoadOperation.allowSceneActivation = true;

            if (logStateChanges)
            {
                if (RuntimeOptions.Logging)
                {
                    Debug.Log("DeathScreenController: Activating preloaded menu scene.", this);
                }
            }

            return;
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private void UnloadMenuScene()
    {
        menuSceneLoadOperation = null;
        menuScenePreloadStarted = false;
        menuSceneReady = false;
    }
}