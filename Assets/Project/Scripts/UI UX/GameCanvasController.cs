using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameCanvasController : MonoBehaviour
{
    public static event Action SpawnStarted;
    public static event Action SpawnCompleted;

    [Header("Canvas Targeting")]
    [Tooltip("Children Canvases list")]
    [SerializeField] private List<Canvas> canvases;

    [Tooltip("Target Display ID")]
    [Range(0, 7)]
    [SerializeField] private int targetDisplayID = 1;

    [Header("Script references")]
    [Tooltip("Reference of the DeathScreenController script")]
    [SerializeField] private DeathScreenController deathScreenController;

    [Header("Canvas References")]
    [Tooltip("Root object of the pause menu canvas.")]
    [SerializeField] private GameObject pauseMenuRoot;

    [Tooltip("HUD Canvase Root")]
    [SerializeField] private GameObject hudPlayRoot;

    [Tooltip("HUD Start root")]
    [SerializeField] private GameObject hudStartRoot;

    [Tooltip("HUD Disconnect root")]
    [SerializeField] private GameObject hudDisconnectRoot;

    [Tooltip("HUD Disconnect root")]
    [SerializeField] private GameObject achievmentsMenuRoot;

    [Header("Scene reference")]
    [Tooltip("Menu scene name.")]
    [SerializeField] private string menuScene = "MenuScene";

    [Header("Input")]
    [Tooltip("Reference to the Pause action from the Input System.")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Audio")]
    [Tooltip("Reference to the game music controller.")]
    [SerializeField] private GameMusicController gameMusicController;

    [Header("Animation")]
    [Tooltip("Canvas animator")]
    [SerializeField] private Animator canvasAnimator;

    [Tooltip("Disconnecting animation trigger parameter")]
    [SerializeField] private string disconnectingParameter = "Disconnecting";

    [Tooltip("Spawn animation trigger parameter")]
    [SerializeField] private string spawnParameter = "Spawn";

    [Tooltip("Animation speed parameter")]
    [SerializeField] private string disconnectingSpeedParameter = "AnimationsSpeed";

    [Tooltip("Animations speed multiplier")]
    [Range(0.1f, 2f)]
    [SerializeField] private float animationsSpeed = 1f;

    [Tooltip("Disconnecting animation base duration")]
    [SerializeField] private float disconnectingBaseDuration = 3f;

    [Tooltip("Spawning animation base duration")]
    [SerializeField] private float spawinigBaseDuration = 6f;

    [Tooltip("Disconnecting animation delay")]
    [Range(0f, 4f)]
    [SerializeField] private float disconnectingDelay = 2f;

    [Header("State Debug")]
    [Tooltip("If true, pause menu will be shown on start.")]
    [SerializeField] private bool openPauseOnStart = false;

    private bool isPauseOpen = false;
    private bool initialized = false;
    private bool isDisconnecting = false;
    private bool isSpawning = false;
    private bool spawned = false;

    public bool IsPauseOpen
    {
        get
        {
            return isPauseOpen;
        }
    }

    public static bool IsSpawnCompleted { get; private set; }

    private void Awake()
    {
        initialized = ValidateReferences();

        if (!initialized)
        {
            return;
        }

        SetCanvasDisplays();

        if (openPauseOnStart)
        {
            OpenPauseMenu();
        }
        else
        {
            ClosePauseMenuInstant();
        }

        StartCoroutine(SpawnHUD());
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 1f;
        }

        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 1f;
        }

        StopAllCoroutines();
    }

    public void LoadMenuScene()
    {
        StartCoroutine(ToMenuRoutine());
    }

    public void TogglePauseMenu()
    {
        if (isPauseOpen)
        {
            ClosePauseMenu();
        }
        else
        {
            OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        if (!initialized)
        {
            return;
        }

        if (isPauseOpen)
        {
            return;
        }

        if (deathScreenController.IsDead)
        {
            return;
        }

        if (!spawned)
        {
            return;
        }

        isPauseOpen = true;

        pauseMenuRoot.SetActive(true);

        if (hudPlayRoot != null)
        {
            hudPlayRoot.SetActive(false);
        }

        RuntimeOptions.InputBlocked = true;
        StaticEvents.RaisePauseOpened();

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 0f;
        }

        if (gameMusicController != null)
        {
            gameMusicController.PauseMusic();
        }
    }

    public void ClosePauseMenu()
    {
        if (!initialized)
        {
            return;
        }

        if (!isPauseOpen)
        {
            return;
        }

        isPauseOpen = false;

        pauseMenuRoot.SetActive(false);

        if (hudPlayRoot != null)
        {
            hudPlayRoot.SetActive(true);
        }

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 1f;
        }

        RuntimeOptions.InputBlocked = false;
        StaticEvents.RaisePauseClosed();

        if (gameMusicController != null)
        {
            gameMusicController.ResumeMusic();
        }

        achievmentsMenuRoot.SetActive(false);
    }

    public void OnContinueButtonPressed()
    {
        ClosePauseMenu();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (!initialized)
        {
            return;
        }

        TogglePauseMenu();
    }

    private IEnumerator SpawnHUD()
    {
        if (canvasAnimator == null)
        {
            yield break;
        }

        if (isSpawning)
        {
            yield break;
        }

        if (spawned)
        {
            yield break;
        }

        isSpawning = true;
        IsSpawnCompleted = false;
        SpawnStarted?.Invoke();

        RuntimeOptions.InputBlocked = true;
        hudStartRoot.SetActive(true);

        float duration = spawinigBaseDuration / animationsSpeed;

        canvasAnimator.enabled = true;
        canvasAnimator.Rebind();
        canvasAnimator.Update(0f);
        canvasAnimator.SetFloat(disconnectingSpeedParameter, animationsSpeed);
        canvasAnimator.ResetTrigger(spawnParameter);
        canvasAnimator.SetTrigger(spawnParameter);

        yield return null;
        yield return new WaitForSecondsRealtime(duration);

        hudStartRoot.SetActive(false);
        RuntimeOptions.InputBlocked = false;

        spawned = true;
        isSpawning = false;
        IsSpawnCompleted = true;
        SpawnCompleted?.Invoke();
    }

    private IEnumerator ToMenuRoutine()
    {
        if (hudDisconnectRoot == null)
        {
            yield break;
        }

        if (canvasAnimator == null)
        {
            yield break;
        }

        if (isDisconnecting)
        {
            yield break;
        }

        isDisconnecting = true;

        canvasAnimator.enabled = true;

        pauseMenuRoot.SetActive(false);
        hudPlayRoot.SetActive(true);
        hudDisconnectRoot.SetActive(true);

        yield return null;

        float duration = disconnectingBaseDuration / animationsSpeed;
        canvasAnimator.SetFloat(disconnectingSpeedParameter, animationsSpeed);
        canvasAnimator.SetTrigger(disconnectingParameter);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(menuScene);

        if (loadOperation == null)
        {
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        yield return null;
        yield return new WaitForSecondsRealtime(duration);

        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 1f;
        }

        RuntimeOptions.InputBlocked = false;

        yield return new WaitForSecondsRealtime(disconnectingDelay);
        loadOperation.allowSceneActivation = true;
    }

    private bool ValidateReferences()
    {
        bool loggingError = RuntimeOptions.LoggingError;
        if (deathScreenController == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: deathScreenController is missing.", this);
            }
            
            return false;
        }

        if (hudPlayRoot == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: hudPlayRoot is missing.", this);
            }
            
            return false;
        }

        if (hudStartRoot == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: hudStartRoot is missing.", this);
            }
            return false;
        }

        if (hudDisconnectRoot == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: hudDisconnectRoot is missing.", this);
            }
            return false;
        }

        if (achievmentsMenuRoot == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: achievmentsMenuRoot is missing.", this);
            }
            return false;
        }

        if (canvasAnimator == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: canvasAnimator is missing.", this);
            }
            return false;
        }

        if (gameMusicController == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: gameMusicController is missing.", this);
            }
            return false;
        }

        if (pauseMenuRoot == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: pauseMenuRoot is missing.", this);
            }
            return false;
        }

        if (pauseAction == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: pauseAction is missing.", this);
            }
            return false;
        }

        if (string.IsNullOrEmpty(disconnectingParameter))
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: disconnectingParameter is null or empty.", this);
            }
            return false;
        }

        if (string.IsNullOrEmpty(spawnParameter))
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: spawnParameter is null or empty.", this);
            }
            return false;
        }

        if (string.IsNullOrEmpty(disconnectingSpeedParameter))
        {
            if (loggingError)
            {
                Debug.LogError("GameCanvasController: disconnectingSpeedParameter is null or empty.", this);
            }
            return false;
        }

        if (animationsSpeed <= 0.1f)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning(
                $"GameCanvasController: disconnectingSpeed value {animationsSpeed} is incorrect, changed to 0.1f",
                this);
            }

            animationsSpeed = 0.1f;
        }

        if (disconnectingBaseDuration <= 0f)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning(
                $"GameCanvasController: disconnectingBaseDuration value {disconnectingBaseDuration} is incorrect, changed to 3.5f",
                this);
            }

            disconnectingBaseDuration = 3.5f;
        }

        if (spawinigBaseDuration <= 0f)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning(
                $"GameCanvasController: spawinigBaseDuration value {spawinigBaseDuration} is incorrect, changed to 6f",
                this);
            }

            spawinigBaseDuration = 6f;
        }

        return true;
    }

    private void ClosePauseMenuInstant()
    {
        isPauseOpen = false;
        pauseMenuRoot.SetActive(false);

        if (!RuntimeOptions.MultiplayerMode)
        {
            Time.timeScale = 1f;
        }
    }

    private void SetCanvasDisplays()
    {
        for (int i = 0; i < canvases.Count; i++)
        {
            if (canvases[i] != null)
            {
                canvases[i].targetDisplay = targetDisplayID;
            }
        }
    }
}