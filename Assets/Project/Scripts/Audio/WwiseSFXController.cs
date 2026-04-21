using System.Collections.Generic;
using UnityEngine;

public class WwiseSFXController : MonoBehaviour
{
    [System.Serializable]
    public class SfxEntry
    {
        [Header("Type")]
        [Tooltip("Type of UI SFX.")]
        public UISfxType type;

        [Header("Event")]
        [Tooltip("Wwise event reference.")]
        public AK.Wwise.Event eventReference;
    }

    [Header("SFX List")]
    [Tooltip("All available SFX mapped by type.")]
    [SerializeField] private List<SfxEntry> sfxEntries = new List<SfxEntry>();

    private Dictionary<UISfxType, AK.Wwise.Event> sfxMap;

    private void Awake()
    {
        BuildMap();
    }

    private void BuildMap()
    {
        sfxMap = new Dictionary<UISfxType, AK.Wwise.Event>();

        for (int i = 0; i < sfxEntries.Count; i++)
        {
            SfxEntry entry = sfxEntries[i];

            if (entry.eventReference == null)
            {
                Debug.LogWarning("WwiseSfxController: Missing eventReference for " + entry.type, this);
                continue;
            }

            if (!sfxMap.ContainsKey(entry.type))
            {
                sfxMap.Add(entry.type, entry.eventReference);
            }
        }
    }

    public void Play(UISfxType type, GameObject source)
    {
        if (!sfxMap.ContainsKey(type))
        {
            Debug.LogWarning("WwiseSfxController: No SFX mapped for " + type, this);
            return;
        }

        if (source == null)
        {
            Debug.LogWarning("WwiseSfxController: Source is null.", this);
            return;
        }

        sfxMap[type].Post(source);
    }

    public void PlayClick(GameObject source)
    {
        Play(UISfxType.Click, source);
    }
    public void PlayClickSelf()
    {
        Play(UISfxType.Click, gameObject);
    }

    public void PlayHover(GameObject source)
    {
        Play(UISfxType.Hover, source);
    }

    public void PlayBack(GameObject source)
    {
        Play(UISfxType.Back, source);
    }
}