using System.Collections.Generic;
using UnityEngine;

public class WwiseGameplaySFXController : MonoBehaviour
{
    [System.Serializable]
    public class GameplaySfxEntry
    {
        [Header("Gameplay SFX Type")]
        [Tooltip("Gameplay SFX group.")]
        public GameplaySfxType gameplaySfxType;

        [Header("Event Type")]
        [Tooltip("Wwise event action type.")]
        public WwiseEventsType eventType;

        [Header("Event Reference")]
        [Tooltip("Mapped Wwise event reference.")]
        public AK.Wwise.Event eventReference;
    }

    [Header("Gameplay SFX List")]
    [Tooltip("All mapped gameplay events.")]
    [SerializeField] private List<GameplaySfxEntry> sfxEntries = new List<GameplaySfxEntry>();

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
            GameplaySfxEntry entry = sfxEntries[i];

            if (entry.eventReference == null)
            {
                Debug.LogWarning("WwiseGameplaySFXController: Missing eventReference for " + entry.gameplaySfxType + " / " + entry.eventType, this);
                continue;
            }

            string key = GetKey(entry.gameplaySfxType, entry.eventType);

            if (!sfxMap.ContainsKey(key))
            {
                sfxMap.Add(key, entry.eventReference);
            }
            else
            {
                Debug.LogWarning("WwiseGameplaySFXController: Duplicate mapping for " + key, this);
            }
        }
    }

    private string GetKey(GameplaySfxType gameplaySfxType, WwiseEventsType eventType)
    {
        return gameplaySfxType.ToString() + "_" + eventType.ToString();
    }

    public void Post(GameplaySfxType gameplaySfxType, WwiseEventsType eventType, GameObject source)
    {
        string key = GetKey(gameplaySfxType, eventType);

        if (!sfxMap.ContainsKey(key))
        {
            Debug.LogWarning("WwiseGameplaySFXController: No SFX mapped for " + key, this);
            return;
        }

        if (source == null)
        {
            Debug.LogWarning("WwiseGameplaySFXController: Source is null.", this);
            return;
        }

        sfxMap[key].Post(source);
    }

    public void PlayRandomHit(GameObject source)
    {
        if (source == null)
        {
            Debug.LogWarning("WwiseGameplaySFXController: Source is null.", this);
            return;
        }

        GameplaySfxType randomHitType = GameplaySfxType.Hit01;
        int randomIndex = Random.Range(0, 3);

        if (randomIndex == 0)
        {
            randomHitType = GameplaySfxType.Hit01;
        }
        else if (randomIndex == 1)
        {
            randomHitType = GameplaySfxType.Hit02;
        }
        else
        {
            randomHitType = GameplaySfxType.Hit03;
        }

        Post(randomHitType, WwiseEventsType.Play, source);
    }
}