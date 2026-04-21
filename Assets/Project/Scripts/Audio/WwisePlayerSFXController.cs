using UnityEngine;

public class WwisePlayerSFXController : MonoBehaviour
{
    [Header("Shoot Event")]
    [Tooltip("Wwise event for player shot.")]
    [SerializeField] private AK.Wwise.Event playerShotPlayEvent;

    [Header("Reload Events")]
    [Tooltip("Wwise event for reload start.")]
    [SerializeField] private AK.Wwise.Event playerReloadPlayEvent;

    [Tooltip("Optional fallback Wwise event for reload stop.")]
    [SerializeField] private AK.Wwise.Event playerReloadStopEvent;

    [Tooltip("Optional fallback Wwise event for reload pause.")]
    [SerializeField] private AK.Wwise.Event playerReloadPauseEvent;

    [Tooltip("Optional fallback Wwise event for reload resume.")]
    [SerializeField] private AK.Wwise.Event playerReloadResumeEvent;

    [Header("RTPC Names")]
    [Tooltip("RTPC name for shoot pitch variation.")]
    [SerializeField] private string shootPitchRtpcName = "PlayerShootPitch";

    [Tooltip("RTPC name for reload pitch variation.")]
    [SerializeField] private string reloadPitchRtpcName = "PlayerReloadPitch";

    [Header("Shoot Pitch Settings")]
    [Tooltip("Minimum random pitch value for shooting.")]
    [SerializeField] private float shootPitchMin = -1.5f;

    [Tooltip("Maximum random pitch value for shooting.")]
    [SerializeField] private float shootPitchMax = 1.5f;

    [Header("Reload Pitch Settings")]
    [Tooltip("Pitch value for the fastest reload.")]
    [SerializeField] private float reloadPitchFastValue = 2f;

    [Tooltip("Pitch value for the slowest reload.")]
    [SerializeField] private float reloadPitchSlowValue = -2f;

    [Tooltip("Minimum reload duration used for RTPC remap.")]
    [SerializeField] private float reloadDurationMin = 0.5f;

    [Tooltip("Maximum reload duration used for RTPC remap.")]
    [SerializeField] private float reloadDurationMax = 3f;

    [Header("Runtime State")]
    [Tooltip("Shows whether reload loop is currently active.")]
    [SerializeField] private bool reloadLoopPlaying;

    [Tooltip("Shows whether reload audio is currently paused.")]
    [SerializeField] private bool reloadPaused;

    private uint reloadPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

    public void PostShoot()
    {
        if (playerShotPlayEvent == null)
        {
            return;
        }

        float randomPitch = Random.Range(shootPitchMin, shootPitchMax);
        AkUnitySoundEngine.SetRTPCValue(shootPitchRtpcName, randomPitch, gameObject);
        playerShotPlayEvent.Post(gameObject);
    }

    public void PostReloadPlay(float reloadDuration)
    {
        if (reloadLoopPlaying)
        {
            return;
        }

        if (playerReloadPlayEvent == null)
        {
            return;
        }

        SetReloadPitchByDuration(reloadDuration);

        reloadPlayingId = playerReloadPlayEvent.Post(gameObject);

        if (reloadPlayingId == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            reloadLoopPlaying = false;
            reloadPaused = false;
            return;
        }

        reloadLoopPlaying = true;
        reloadPaused = false;
    }

    public void PostReloadStop()
    {
        if (reloadLoopPlaying == false)
        {
            return;
        }

        if (reloadPlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                reloadPlayingId,
                0,
                AkCurveInterpolation.AkCurveInterpolation_Linear);
        }
        else
        {
            if (playerReloadStopEvent != null)
            {
                playerReloadStopEvent.Post(gameObject);
            }
        }

        reloadLoopPlaying = false;
        reloadPaused = false;
        reloadPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    }

    public void PauseReload()
    {
        if (reloadLoopPlaying == false)
        {
            return;
        }

        if (reloadPaused)
        {
            return;
        }

        if (reloadPlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Pause,
                reloadPlayingId,
                0,
                AkCurveInterpolation.AkCurveInterpolation_Linear);
        }
        else
        {
            if (playerReloadPauseEvent != null)
            {
                playerReloadPauseEvent.Post(gameObject);
            }
        }

        reloadPaused = true;
    }

    public void ResumeReload()
    {
        if (reloadLoopPlaying == false)
        {
            return;
        }

        if (reloadPaused == false)
        {
            return;
        }

        if (reloadPlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Resume,
                reloadPlayingId,
                0,
                AkCurveInterpolation.AkCurveInterpolation_Linear);
        }
        else
        {
            if (playerReloadResumeEvent != null)
            {
                playerReloadResumeEvent.Post(gameObject);
            }
        }

        reloadPaused = false;
    }

    private void OnDisable()
    {
        ForceReloadStop();
    }

    private void OnDestroy()
    {
        ForceReloadStop();
    }

    private void ForceReloadStop()
    {
        if (reloadLoopPlaying == false)
        {
            return;
        }

        if (reloadPlayingId != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                reloadPlayingId,
                0,
                AkCurveInterpolation.AkCurveInterpolation_Linear);
        }
        else
        {
            if (playerReloadStopEvent != null)
            {
                playerReloadStopEvent.Post(gameObject);
            }
        }

        reloadLoopPlaying = false;
        reloadPaused = false;
        reloadPlayingId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
    }

    private void SetReloadPitchByDuration(float reloadDuration)
    {
        if (reloadDurationMax <= reloadDurationMin)
        {
            AkUnitySoundEngine.SetRTPCValue(reloadPitchRtpcName, reloadPitchSlowValue, gameObject);
            return;
        }

        float clampedDuration = Mathf.Clamp(reloadDuration, reloadDurationMin, reloadDurationMax);
        float normalizedValue = Mathf.InverseLerp(reloadDurationMin, reloadDurationMax, clampedDuration);
        float rtpcValue = Mathf.Lerp(reloadPitchFastValue, reloadPitchSlowValue, normalizedValue);

        AkUnitySoundEngine.SetRTPCValue(reloadPitchRtpcName, rtpcValue, gameObject);
    }
}