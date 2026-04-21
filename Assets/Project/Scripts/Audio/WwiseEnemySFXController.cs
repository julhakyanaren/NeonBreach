using System.Collections.Generic;
using UnityEngine;

public class WwiseEnemySFXController : MonoBehaviour
{
    [System.Serializable]
    public class EnemySfxEntry
    {
        [Header("Enemy SFX Type")]
        [Tooltip("Enemy combat SFX group.")]
        public EnemySfxType enemySfxType;

        [Header("Event Type")]
        [Tooltip("Wwise event action type.")]
        public WwiseEventsType eventType;

        [Header("Event Reference")]
        [Tooltip("Mapped Wwise event reference.")]
        public AK.Wwise.Event eventReference;
    }

    [Header("Enemy SFX List")]
    [Tooltip("All mapped enemy combat events.")]
    [SerializeField] private List<EnemySfxEntry> sfxEntries = new List<EnemySfxEntry>();

    private Dictionary<string, AK.Wwise.Event> sfxMap;

    private void Awake()
    {
        BuildMap();
    }

    private void BuildMap()
    {
        sfxMap = new Dictionary<string, AK.Wwise.Event>();

        for (int i = 0; i < sfxEntries.Count; i++)
        {
            EnemySfxEntry entry = sfxEntries[i];

            if (entry.eventReference == null)
            {
                Debug.LogWarning("WwiseEnemySFXController: Missing eventReference for " + entry.enemySfxType + " / " + entry.eventType, this);
                continue;
            }

            string key = GetKey(entry.enemySfxType, entry.eventType);

            if (!sfxMap.ContainsKey(key))
            {
                sfxMap.Add(key, entry.eventReference);
            }
            else
            {
                Debug.LogWarning("WwiseEnemySFXController: Duplicate mapping for " + key, this);
            }
        }
    }

    private string GetKey(EnemySfxType enemySfxType, WwiseEventsType eventType)
    {
        return enemySfxType.ToString() + "_" + eventType.ToString();
    }

    public void Post(EnemySfxType enemySfxType, WwiseEventsType eventType, GameObject source)
    {
        string key = GetKey(enemySfxType, eventType);

        if (!sfxMap.ContainsKey(key))
        {
            Debug.LogWarning("WwiseEnemySFXController: No SFX mapped for " + key, this);
            return;
        }

        if (source == null)
        {
            Debug.LogWarning("WwiseEnemySFXController: Source is null.", this);
            return;
        }

        sfxMap[key].Post(source);
    }
}