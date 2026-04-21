using System;
using UnityEngine;

public class WwiseMusicRtpcApplier : MonoBehaviour
{
    [Header("Wwise")]
    [Tooltip("RTPC name used to control music volume.")]
    [SerializeField] private string musicRtpc = "MusicVolume";

    private float fadInOutDuration;

    public void ApplyMusicVolume()
    {
        float value = RuntimeOptions.AudioRuntimeEffectiveMusic * 100f;
        AkUnitySoundEngine.SetRTPCValue(musicRtpc, value);
    }

    public void FadeOutMusic()
    {
        FadeOutMusic(fadInOutDuration);
        FadeOutMusic();
    }

    public void FadeOutMusic(float fadeOut)
    {
        AkUnitySoundEngine.SetRTPCValue(musicRtpc, 0f, null, Convert.ToInt32(fadeOut * 1000));
    }

    public void FadeInMusic()
    {
        FadeInMusic(fadInOutDuration);
    }

    public void FadeInMusic(float fadeInOutMs)
    {
        float value = RuntimeOptions.AudioRuntimeEffectiveMusic * 100f;
        AkUnitySoundEngine.SetRTPCValue(musicRtpc, value, null, Convert.ToInt32(fadeInOutMs * 1000));
    }

    private void Start()
    {
        ApplyMusicVolume();
    }
}