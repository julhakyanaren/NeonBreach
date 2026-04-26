using UnityEngine;

public class GameMusicController : MonoBehaviour
{
    [Header("Wwise Source")]
    [Tooltip("Game object used as the music emitter/source for Wwise events.")]
    [SerializeField] private GameObject musicSourceObject;

    [Header("Wwise Event Names")]
    [Tooltip("Wwise event name used to start game music.")]
    [SerializeField] private string playMusicEvent = "Play_GameMusic1";

    [Tooltip("Wwise event name used to stop game music.")]
    [SerializeField] private string stopMusicEvent = "Stop_GameMusic1";

    [Tooltip("Wwise event name used to pause game music.")]
    [SerializeField] private string pauseMusicEvent = "Pause_GameMusic1";

    [Tooltip("Wwise event name used to resume game music.")]
    [SerializeField] private string resumeMusicEvent = "Resume_GameMusic1";

    [Header("Settings")]
    [Tooltip("If true, music will start automatically in Start.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("If true, stop music automatically when this object is disabled.")]
    [SerializeField] private bool stopOnDisable = true;

    private bool initialized = false;
    private bool musicStarted = false;
    private bool musicPaused = false;

    private void Start()
    {
        initialized = ValidateReferences();

        if (!initialized)
        {
            return;
        }

        if (!playOnStart)
        {
            return;
        }

        PlayMusic();
    }

    private bool ValidateReferences()
    {
        bool loggingError = RuntimeOptions.LoggingError;
        if (musicSourceObject == null)
        {
            if (loggingError)
            {
                Debug.LogError("GameMusicController: musicSourceObject is missing.", this);
            }
            return false;
        }

        if (string.IsNullOrWhiteSpace(playMusicEvent))
        {
            if (loggingError)
            {
                Debug.LogError("GameMusicController: playMusicEvent is empty.", this);
            }
            return false;
        }

        if (string.IsNullOrWhiteSpace(stopMusicEvent))
        {
            if (loggingError)
            {
                Debug.LogError("GameMusicController: stopMusicEvent is empty.", this);
            }
            return false;
        }

        if (string.IsNullOrWhiteSpace(pauseMusicEvent))
        {
            if (loggingError)
            {
                Debug.LogError("GameMusicController: pauseMusicEvent is empty.", this);
            }
            return false;
        }

        if (string.IsNullOrWhiteSpace(resumeMusicEvent))
        {
            if (loggingError)
            {
                Debug.LogError("GameMusicController: resumeMusicEvent is empty.", this);
            }
            return false;
        }

        return true;
    }

    public void PlayMusic()
    {
        if (!initialized)
        {
            initialized = ValidateReferences();

            if (!initialized)
            {
                return;
            }
        }

        AkUnitySoundEngine.PostEvent(playMusicEvent, musicSourceObject);
        musicStarted = true;
        musicPaused = false;
    }

    public void StopMusic()
    {
        if (!initialized)
        {
            return;
        }

        if (!musicStarted)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(stopMusicEvent, musicSourceObject);
        musicStarted = false;
        musicPaused = false;
    }

    public void PauseMusic()
    {
        if (!initialized)
        {
            return;
        }

        if (!musicStarted)
        {
            return;
        }

        if (musicPaused)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(pauseMusicEvent, musicSourceObject);
        musicPaused = true;
    }

    public void ResumeMusic()
    {
        if (!initialized)
        {
            return;
        }

        if (!musicStarted)
        {
            return;
        }

        if (!musicPaused)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(resumeMusicEvent, musicSourceObject);
        musicPaused = false;
    }

    public bool IsMusicStarted()
    {
        return musicStarted;
    }

    public bool IsMusicPaused()
    {
        return musicPaused;
    }

    private void OnDisable()
    {
        if (!stopOnDisable)
        {
            return;
        }

        if (!initialized)
        {
            return;
        }

        if (!musicStarted)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(stopMusicEvent, musicSourceObject);
        musicStarted = false;
        musicPaused = false;
    }
}