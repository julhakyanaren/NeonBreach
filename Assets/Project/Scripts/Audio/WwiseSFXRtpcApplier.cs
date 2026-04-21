using UnityEngine;

public class WwiseSFXRtpcApplier : MonoBehaviour
{
    [Header("Wwise")]
    [Tooltip("RTPC name used to control SFX volume.")]
    [SerializeField] private string sfxRtpc = "SFXVolume";

    public void ApplySfxVolume()
    {
        float value = RuntimeOptions.AudioRuntimeEffectiveSFX * 100f;
        AkUnitySoundEngine.SetRTPCValue(sfxRtpc, value);
    }

    private void Start()
    {
        ApplySfxVolume();
    }
}