using UnityEngine;

[System.Serializable]
public class CharacterPrefabEntry
{
    [Header("Character")]
    [Tooltip("Character type selected in MainMenu and stored in RuntimeOptions.")]
    [SerializeField] private CharacterType characterType;

    [Header("Prefab")]
    [Tooltip("Player prefab that should be spawned for this character.")]
    [SerializeField] private GameObject playerPrefab;

    public CharacterType CharacterType
    {
        get
        {
            return characterType;
        }
    }

    public GameObject PlayerPrefab
    {
        get
        {
            return playerPrefab;
        }
    }
}