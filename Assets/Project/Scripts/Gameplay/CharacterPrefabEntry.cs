using UnityEngine;

[System.Serializable]
public class CharacterPrefabEntry
{
    [Header("Character")]
    [Tooltip("Character type for this prefab entry.")]
    [SerializeField] private CharacterType characterType;

    [Header("Singleplayer")]
    [Tooltip("Local prefab used for singleplayer spawn.")]
    [SerializeField] private GameObject singleplayerPrefab;

    [Header("Multiplayer")]
    [Tooltip("Photon prefab name from Resources used for multiplayer spawn.")]
    [SerializeField] private string multiplayerPrefabName;

    public CharacterType CharacterType
    {
        get
        {
            return characterType;
        }
    }

    public GameObject SingleplayerPrefab
    {
        get
        {
            return singleplayerPrefab;
        }
    }

    public string MultiplayerPrefabName
    {
        get
        {
            return multiplayerPrefabName;
        }
    }
}