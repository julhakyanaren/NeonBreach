using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioOptionsController : MonoBehaviour
{
    [Header("Audio Apply")]
    [Tooltip("Applies current music RTPC value to Wwise.")]
    [SerializeField] private WwiseMusicRtpcApplier wwiseMusicRtpcApplier;

    [Tooltip("Applies current SFX RTPC value to Wwise.")]
    [SerializeField] private WwiseSFXRtpcApplier wwiseSfxRtpcApplier;

    [Tooltip("SFX level slider.")]
    [SerializeField] private Slider sliderVolumeSFX;

    [Tooltip("Music level slider.")]
    [SerializeField] private Slider sliderVolumeMusic;

    [Tooltip("Overall level slider.")]
    [SerializeField] private Slider sliderVolumeOverall;

    [Tooltip("SFX level text.")]
    [SerializeField] private TMP_Text percentTMP_SFX;

    [Tooltip("Music level text.")]
    [SerializeField] private TMP_Text percentTMP_Music;

    [Tooltip("Overall level text.")]
    [SerializeField] private TMP_Text percentTMP_Overall;

    private bool initialized;

    private void Awake()
    {
        initialized = ValidateReferences();

        if (!initialized)
        {
            return;
        }

        SetupSliders();
        LoadRuntimeValuesToUI();
        RefreshAllTexts();
        SubscribeToSliderEvents();
    }

    private void OnDestroy()
    {
        if (!initialized)
        {
            return;
        }

        UnsubscribeFromSliderEvents();
    }

    private bool ValidateReferences()
    {
        bool loggingError = RuntimeOptions.LoggingError;
        if (wwiseMusicRtpcApplier == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: wwiseMusicRtpcApplier is missing.", this);
            }
            return false;
        }

        if (wwiseSfxRtpcApplier == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: wwiseSfxRtpcApplier is missing.", this);
            }
            return false;
        }

        if (sliderVolumeSFX == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: sliderVolumeSFX is missing.", this);
            }
            return false;
        }

        if (sliderVolumeMusic == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: sliderVolumeMusic is missing.", this);
            }
            return false;
        }

        if (sliderVolumeOverall == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: sliderVolumeOverall is missing.", this);
            }
            
            return false;
        }

        if (percentTMP_SFX == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: percentTMP_SFX is missing.", this);
            }
            return false;
        }

        if (percentTMP_Music == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: percentTMP_Music is missing.", this);
            }
            return false;
        }

        if (percentTMP_Overall == null)
        {
            if (loggingError)
            {
                Debug.LogError("MenuOptionsController: percentTMP_Overall is missing.", this);
            }
            return false;
        }

        return true;
    }

    private void SetupSliders()
    {
        sliderVolumeSFX.minValue = 0f;
        sliderVolumeSFX.maxValue = 1f;
        sliderVolumeSFX.wholeNumbers = false;

        sliderVolumeMusic.minValue = 0f;
        sliderVolumeMusic.maxValue = 1f;
        sliderVolumeMusic.wholeNumbers = false;

        sliderVolumeOverall.minValue = 0f;
        sliderVolumeOverall.maxValue = 1f;
        sliderVolumeOverall.wholeNumbers = false;
    }

    private void LoadRuntimeValuesToUI()
    {
        sliderVolumeSFX.value = RuntimeOptions.AudioRuntimeSFX;
        sliderVolumeMusic.value = RuntimeOptions.AudioRuntimeMusic;
        sliderVolumeOverall.value = RuntimeOptions.AudioRuntimeOverall;
    }

    private void SubscribeToSliderEvents()
    {
        sliderVolumeSFX.onValueChanged.AddListener(OnSfxSliderChanged);
        sliderVolumeMusic.onValueChanged.AddListener(OnMusicSliderChanged);
        sliderVolumeOverall.onValueChanged.AddListener(OnOverallSliderChanged);
    }

    private void UnsubscribeFromSliderEvents()
    {
        sliderVolumeSFX.onValueChanged.RemoveListener(OnSfxSliderChanged);
        sliderVolumeMusic.onValueChanged.RemoveListener(OnMusicSliderChanged);
        sliderVolumeOverall.onValueChanged.RemoveListener(OnOverallSliderChanged);
    }

    private void OnSfxSliderChanged(float value)
    {
        RuntimeOptions.AudioRuntimeSFX = value;
        UpdateSliderText(sliderVolumeSFX, percentTMP_SFX);
        wwiseSfxRtpcApplier.ApplySfxVolume();
    }

    private void OnMusicSliderChanged(float value)
    {
        RuntimeOptions.AudioRuntimeMusic = value;
        UpdateSliderText(sliderVolumeMusic, percentTMP_Music);
        wwiseMusicRtpcApplier.ApplyMusicVolume();
    }

    private void OnOverallSliderChanged(float value)
    {
        RuntimeOptions.AudioRuntimeOverall = value;
        UpdateSliderText(sliderVolumeOverall, percentTMP_Overall);

        wwiseMusicRtpcApplier.ApplyMusicVolume();
        wwiseSfxRtpcApplier.ApplySfxVolume();
    }

    private void RefreshAllTexts()
    {
        UpdateSliderText(sliderVolumeSFX, percentTMP_SFX);
        UpdateSliderText(sliderVolumeMusic, percentTMP_Music);
        UpdateSliderText(sliderVolumeOverall, percentTMP_Overall);
    }

    private void UpdateSliderText(Slider slider, TMP_Text tmpText)
    {
        int percentValue = Mathf.RoundToInt(slider.value * 100f);
        tmpText.text = percentValue.ToString() + " %";
    }
}