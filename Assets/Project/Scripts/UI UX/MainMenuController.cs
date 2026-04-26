using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Reference to Wwise music RTPC applier.")]
    [SerializeField] private WwiseMusicRtpcApplier wwiseMusicRtpcApplier;

    [Header("Transition")]
    [Tooltip("Music fade out duration before scene load in seconds.")]
    [SerializeField] private float musicFadeOutSeconds = 1.5f;

    [Header("Wwise Settings")]
    [Tooltip("Wwise ambient music game object")]
    [SerializeField] private GameObject ambientSourceObject;

    [Tooltip("Wwise music play event name")]
    [SerializeField] private string playMusicEvent = "Play_MenuMusic";

    private bool initialized = false;

    private void Start()
    {
        initialized = ValidateReferences();

        if (!initialized)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(playMusicEvent, ambientSourceObject);
        wwiseMusicRtpcApplier.FadeOutMusic(Mathf.RoundToInt(musicFadeOutSeconds * 1000f));
    }

    private bool ValidateReferences()
    {
        if (ambientSourceObject == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("MainMenuController: ambientSourceObject is missing.", this);
            }
            return false;
        }
        if (string.IsNullOrWhiteSpace(playMusicEvent))
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("MainMenuController: playMusicEvent Wwise event name is empty.", this);
            }
            return false;
        }
        return true;
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
