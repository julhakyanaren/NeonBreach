using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuChooseController : MonoBehaviour
{
    [Header("Options")]
    [Tooltip("Character selected when the menu opens.")]
    [SerializeField] private CharacterType selectedCharacter = CharacterType.CharacterCyan;

    [Tooltip("Game Scene name")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Animation")]
    [Tooltip("Animator that controls character transition animations.")]
    [SerializeField] private Animator characterChangeAnimator;

    [Tooltip("Trigger name for transition of start choose.")]
    [SerializeField] private string startChooseParameter = "StartChoose";

    [Tooltip("Trigger name for transition from Cyan to Lavender.")]
    [SerializeField] private string cyanToLavenderParameter = "CyanToLavender";

    [Tooltip("Trigger name for transition from Lavender to Cyan.")]
    [SerializeField] private string lavenderToCyanParameter = "LavenderToCyan";

    [Tooltip("Animator float parameter that controls transition speed.")]
    [SerializeField] private string characterChangeSpeedParameter = "CharacterChangeSpeed";

    [Tooltip("Playback speed multiplier for character transition animation.")]
    [Range(0.1f, 5f)]
    [SerializeField] private float characterChangeSpeed = 1f;

    [Tooltip("Base duration of transition from Lavender to Cyan at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float lavenderToCyanBaseDuration = 5f;

    [Tooltip("Base duration of transition from Cyan to Lavender at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float cyanToLavenderBaseDuration = 5f;

    [Tooltip("Character change animation exit time duration.")]
    [Range(0f, 3f)]
    [SerializeField] private float exitTimeDuration = 1f;

    [Tooltip("Trigger name for transition represent Cyan character.")]
    [SerializeField] private string representCyanParameter = "FirstSelectCyan";

    [Tooltip("Trigger name for transition represent Lavender character.")]
    [SerializeField] private string representLavenderParameter = "FirstSelectLavender";

    [Tooltip("Base duration of represent Cyan animation at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float representCyanBaseDuration = 4f;

    [Tooltip("Base duration of represent Lavender animation at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float representLavenderBaseDuration = 4f;

    [Tooltip("Trigger name for transition choose Cyan character as player.")]
    [SerializeField] private string chooseCyanParameter = "ChooseCyan";

    [Tooltip("Trigger name for transition choose Lavender character as player.")]
    [SerializeField] private string chooseLavenderParameter = "ChooseLavender";

    [Tooltip("Trigger name for transition choose Cyan character as player.")]
    [SerializeField] private string cyanLeaveParameter = "CyanLeave";

    [Tooltip("Trigger name for transition choose Lavender character as player.")]
    [SerializeField] private string lavenderLeaveParameter = "LavenderLeave";

    [Tooltip("Base duration of Cyan leave scene animation at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float cyanLeaveBaseDuration = 4f;

    [Tooltip("Base duration of Lavender leave scene animation at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float lavenderLeaveBaseDuration = 4f;

    [Tooltip("Base duration of choose Cyan animation at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float chooseCyanBaseDuration = 4f;

    [Tooltip("Base duration of choose Lavender animation at speed 1.")]
    [Range(0.1f, 60f)]
    [SerializeField] private float chooseLavenderBaseDuration = 4f;

    [Header("UI")]
    [Tooltip("UI object with Cyan character name.")]
    [SerializeField] private GameObject cyanCharacterName;

    [Tooltip("UI object with Lavender character name.")]
    [SerializeField] private GameObject lavenderCharacterName;

    [Tooltip("Button that switches to the next character.")]
    [SerializeField] private Button toRightButton;

    [Tooltip("Start Game button.")]
    [SerializeField] private Button startGameButton;

    [Tooltip("Back To Main Menu button.")]
    [SerializeField] private Button backToMenu;

    [Header("Character Config Sources")]
    [Tooltip("Cyan player config source.")]
    [SerializeField] private PlayerConfig playerConfigCyan;

    [Tooltip("Lavender player config source.")]
    [SerializeField] private PlayerConfig playerConfigLavender;

    [Header("Config Output UI Text")]
    [Tooltip("Character start health output.")]
    [SerializeField] private TMP_Text outTextHealthStart;

    [Tooltip("Character health limit output.")]
    [SerializeField] private TMP_Text outTextHealthLimit;

    [Tooltip("Character move speed output.")]
    [SerializeField] private TMP_Text outTextMoveSpeed;

    [Tooltip("Character rotation speed output.")]
    [SerializeField] private TMP_Text outTextRotationSpeed;

    [Tooltip("Character shot per minute output.")]
    [SerializeField] private TMP_Text outTextShotPerMinute;

    [Tooltip("Character one magazine size.")]
    [SerializeField] private TMP_Text outTextAmmunition;

    [Tooltip("Character reload duration output.")]
    [SerializeField] private TMP_Text outTextReloadDuration;

    [Tooltip("Character projectile damage output.")]
    [SerializeField] private TMP_Text outTextProjectileDamage;

    [Tooltip("Character projectile speed output.")]
    [SerializeField] private TMP_Text outTextProjectileSpeed;


    [Header("References")]
    [Tooltip("References of the MainMenu Canvases group game object")]
    [SerializeField] private GameObject mainMenuCanvasGroup;

    [Header("Music")]
    [Tooltip("Reference of the WwiseMusicRtpcApplier script")]
    [SerializeField] private WwiseMusicRtpcApplier musicApplier;

    [Tooltip("Duration of music fade out")]
    [Range(1f, 5f)]
    [SerializeField] private float fadeOutDuration = 1.5f;

    private CharacterType currentCharacter;
    private CharacterType chosenType;

    private bool characterChanging;
    private bool characterSelecting;
    private bool initialized;
    private bool characterSelected;
    private bool representing;
    private bool returnToMainMenu;

    private ProjectileConfig projectileConfigCyan;
    private ProjectileConfig projectileConfigLavender;

    private PlayerConfig currentPlayerConfig;
    private ProjectileConfig currentProjectileConfig;

    public CharacterType ChoosenType
    {
        get
        {
            return chosenType;
        }
        private set
        {
            chosenType = value;
        }
    }

    public bool CharacterSelected
    {
        get
        {
            return characterSelected;
        }
        private set
        {
            characterSelected = value;
        }
    }

    private void Awake()
    {
        initialized = ValidateReferences();

        if (!initialized)
        {
            enabled = false;
            return;
        }

        projectileConfigCyan = playerConfigCyan.projectileConfig;
        projectileConfigLavender = playerConfigLavender.projectileConfig;

        if (projectileConfigCyan == null)
        {
            Debug.LogError("ChooseMenuController: Cyan character projectile config source missing.", this);
            enabled = false;
            return;
        }

        if (projectileConfigLavender == null)
        {
            Debug.LogError("ChooseMenuController: Lavender character projectile config source missing.", this);
            enabled = false;
            return;
        }

        characterChangeAnimator.SetFloat(characterChangeSpeedParameter, characterChangeSpeed);
        characterChangeAnimator.SetTrigger(startChooseParameter);
    }

    private void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        selectedCharacter = RuntimeOptions.ConfirmedCharacter;

        characterChanging = false;
        characterSelecting = false;
        characterSelected = false;
        representing = false;

        currentCharacter = selectedCharacter;
        ChoosenType = currentCharacter;

        characterChangeAnimator.SetTrigger(startChooseParameter);
        characterChangeAnimator.SetFloat(characterChangeSpeedParameter, characterChangeSpeed);

        ResetPreviewState();

        StartCoroutine(RepresentCharacter());
    }

    public void SelectNextCharacter()
    {
        if (!initialized)
        {
            return;
        }

        if (characterChanging)
        {
            return;
        }

        if (characterSelecting)
        {
            return;
        }

        if (CharacterSelected)
        {
            return;
        }

        switch (currentCharacter)
        {
            case CharacterType.CharacterCyan:
                {
                    StartCoroutine(ChangeCharacter(
                        CharacterType.CharacterLavender,
                        cyanToLavenderParameter,
                        cyanToLavenderBaseDuration));
                    break;
                }
            case CharacterType.CharacterLavender:
                {
                    StartCoroutine(ChangeCharacter(
                        CharacterType.CharacterCyan,
                        lavenderToCyanParameter,
                        lavenderToCyanBaseDuration));
                    break;
                }
        }
    }

    public void StartGame()
    {
        if (!initialized)
        {
            return;
        }

        if (characterChanging)
        {
            return;
        }

        if (characterSelecting)
        {
            return;
        }

        if (CharacterSelected)
        {
            return;
        }
        float[] durations = new float[]
        {
            GetAdjustedDuration(chooseLavenderBaseDuration) + exitTimeDuration,
            GetAdjustedDuration(chooseCyanBaseDuration) + exitTimeDuration,
            fadeOutDuration
            
        };
        fadeOutDuration = Mathf.Max(durations);
        StartCoroutine(StartGameRoutine());
        StartCoroutine(ChooseCharacter());
    }

    public void GoToMainMenu()
    {
        StartCoroutine(BackToMainMenu());
    }

    private IEnumerator ChangeCharacter(CharacterType targetCharacter, string triggerParameter, float baseDuration)
    {
        if (currentCharacter == targetCharacter)
        {
            yield break;
        }

        if (characterChangeAnimator == null)
        {
            yield break;
        }

        characterChanging = true;
        SetButtonInteractable(false);

        characterChangeAnimator.SetFloat(characterChangeSpeedParameter, characterChangeSpeed);
        characterChangeAnimator.SetTrigger(triggerParameter);

        float waitDuration = GetAdjustedDuration(baseDuration);
        yield return new WaitForSeconds(waitDuration);

        currentCharacter = targetCharacter;
        ApplyCharacterData(currentCharacter);

        if (exitTimeDuration > 0f)
        {
            yield return new WaitForSeconds(exitTimeDuration);
        }

        characterChanging = false;
        SetButtonInteractable(true);
    }
    private IEnumerator RepresentCharacter()
    {
        if (representing)
        {
            yield break;
        }

        representing = true;

        SetButtonInteractable(false);
        ApplyCharacterData(currentCharacter);

        switch (currentCharacter)
        {
            case CharacterType.CharacterCyan:
                {
                    characterChangeAnimator.SetTrigger(representCyanParameter);
                    yield return null;
                    yield return new WaitForSeconds(GetAdjustedDuration(representCyanBaseDuration));
                    break;
                }
            case CharacterType.CharacterLavender:
                {
                    characterChangeAnimator.SetTrigger(representLavenderParameter);
                    yield return null;
                    yield return new WaitForSeconds(GetAdjustedDuration(representLavenderBaseDuration));
                    break;
                }
        }

        SetButtonInteractable(true);
    }
    private IEnumerator ChooseCharacter()
    {
        if (characterSelecting)
        {
            yield break;
        }

        if (CharacterSelected)
        {
            yield break;
        }

        characterSelecting = true;
        SetButtonInteractable(false);

        switch (currentCharacter)
        {
            case CharacterType.CharacterCyan:
                {
                    characterChangeAnimator.SetTrigger(chooseCyanParameter);
                    yield return null;
                    yield return new WaitForSeconds(GetAdjustedDuration(chooseCyanBaseDuration));
                    ChoosenType = CharacterType.CharacterCyan;
                    break;
                }
            case CharacterType.CharacterLavender:
                {
                    characterChangeAnimator.SetTrigger(chooseLavenderParameter);
                    yield return null;
                    yield return new WaitForSeconds(GetAdjustedDuration(chooseLavenderBaseDuration));
                    ChoosenType = CharacterType.CharacterLavender;
                    break;
                }
        }

        if (exitTimeDuration > 0f)
        {
            yield return new WaitForSeconds(exitTimeDuration);
        }

        CharacterSelected = true;
        characterSelecting = false;
    }
    private IEnumerator BackToMainMenu()
    {
        if (returnToMainMenu)
        {
            yield break;
        }

        returnToMainMenu = true;
        SetButtonInteractable(false);

        switch (currentCharacter)
        {
            case CharacterType.CharacterCyan:
                {
                    characterChangeAnimator.SetTrigger(cyanLeaveParameter);
                    yield return null;
                    yield return new WaitForSeconds(GetAdjustedDuration(cyanLeaveBaseDuration));
                    break;
                }
            case CharacterType.CharacterLavender:
                {
                    characterChangeAnimator.SetTrigger(lavenderLeaveParameter);
                    yield return null;
                    yield return new WaitForSeconds(GetAdjustedDuration(lavenderLeaveBaseDuration));
                    break;
                }
        }
        mainMenuCanvasGroup.SetActive(true);
        gameObject.SetActive(false);
    }
    private IEnumerator StartGameRoutine()
    {
        musicApplier.FadeOutMusic(fadeOutDuration);
        yield return new WaitForSeconds(fadeOutDuration);

        RuntimeOptions.ConfirmedCharacter = ChoosenType;

        try
        {
            SceneManager.LoadSceneAsync(gameSceneName);
        }
        catch
        {
            Debug.LogError("ChooseMenuController: Game Scene failed!", this);
        }
    }
    private void ApplyCharacterData(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.CharacterCyan:
                {
                    currentPlayerConfig = playerConfigCyan;
                    currentProjectileConfig = projectileConfigCyan;
                    ChangeCharacterName(CharacterType.CharacterCyan);
                    OutputCharacterConfigs();
                    break;
                }
            case CharacterType.CharacterLavender:
                {
                    currentPlayerConfig = playerConfigLavender;
                    currentProjectileConfig = projectileConfigLavender;
                    ChangeCharacterName(CharacterType.CharacterLavender);
                    OutputCharacterConfigs();
                    break;
                }
        }
    }

    private void ResetPreviewState()
    {
        if (cyanCharacterName != null)
        {
            cyanCharacterName.SetActive(false);
        }

        if (lavenderCharacterName != null)
        {
            lavenderCharacterName.SetActive(false);
        }
    }

    private void ChangeCharacterName(CharacterType chosenCharacter)
    {
        if (cyanCharacterName == null)
        {
            return;
        }

        if (lavenderCharacterName == null)
        {
            return;
        }

        switch (chosenCharacter)
        {
            case CharacterType.CharacterCyan:
                {
                    cyanCharacterName.SetActive(true);
                    lavenderCharacterName.SetActive(false);
                    break;
                }
            case CharacterType.CharacterLavender:
                {
                    cyanCharacterName.SetActive(false);
                    lavenderCharacterName.SetActive(true);
                    break;
                }
        }
    }

    private void OutputCharacterConfigs()
    {
        if (currentPlayerConfig == null)
        {
            Debug.LogError("ChooseMenuController: Current player config is missing.", this);
            return;
        }

        if (currentProjectileConfig == null)
        {
            Debug.LogError("ChooseMenuController: Current projectile config is missing.", this);
            return;
        }

        SetTextValue(outTextHealthStart, currentPlayerConfig.maxHealth);
        SetTextValue(outTextHealthLimit, currentPlayerConfig.maxHealthLimit);

        SetTextValue(outTextMoveSpeed, currentPlayerConfig.moveSpeed);
        SetTextValue(outTextRotationSpeed, currentPlayerConfig.rotationSpeed);

        SetTextValue(outTextShotPerMinute, currentPlayerConfig.shotsPerMinute);
        SetTextValue(outTextAmmunition, Convert.ToSingle(currentPlayerConfig.magazineSize));
        SetTextValue(outTextReloadDuration, currentPlayerConfig.reloadDuration);

        SetTextValue(outTextProjectileDamage, currentProjectileConfig.damage);
        SetTextValue(outTextProjectileSpeed, currentProjectileConfig.speed);
    }

    private void SetTextValue(TMP_Text tmpText, float value)
    {
        if (tmpText == null)
        {
            return;
        }

        tmpText.text = $"{value}";
    }

    private float GetAdjustedDuration(float baseDuration)
    {
        if (characterChangeSpeed <= 0f)
        {
            return baseDuration;
        }

        return baseDuration / characterChangeSpeed;
    }

    private void SetButtonInteractable(bool interactable)
    {
        if (toRightButton != null)
        {
            toRightButton.interactable = interactable;
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = interactable;
        }
        if (backToMenu != null)
        {
            backToMenu.interactable = interactable;
        }
    }

    private bool ValidateReferences()
    {
        if (characterChangeAnimator == null)
        {
            Debug.LogError("ChooseMenuController: CharacterChangeAnimator is missing.", this);
            return false;
        }

        if (cyanCharacterName == null)
        {
            Debug.LogError("ChooseMenuController: Cyan character name object is missing.", this);
            return false;
        }

        if (lavenderCharacterName == null)
        {
            Debug.LogError("ChooseMenuController: Lavender character name object is missing.", this);
            return false;
        }

        if (toRightButton == null)
        {
            Debug.LogError("ChooseMenuController: ToRight button is missing.", this);
            return false;
        }

        if (startGameButton == null)
        {
            Debug.LogError("ChooseMenuController: StartGame button is missing.", this);
            return false;
        }

        if (backToMenu == null)
        {
            Debug.LogError("ChooseMenuController: BackToMainMenu button is missing.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(startChooseParameter))
        {
            Debug.LogError("ChooseMenuController: StartChoose trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(cyanToLavenderParameter))
        {
            Debug.LogError("ChooseMenuController: CyanToLavender trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(lavenderToCyanParameter))
        {
            Debug.LogError("ChooseMenuController: LavenderToCyan trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(characterChangeSpeedParameter))
        {
            Debug.LogError("ChooseMenuController: CharacterChangeSpeed parameter name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(representCyanParameter))
        {
            Debug.LogError("ChooseMenuController: RepresentCyan trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(representLavenderParameter))
        {
            Debug.LogError("ChooseMenuController: RepresentLavender trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(chooseCyanParameter))
        {
            Debug.LogError("ChooseMenuController: ChooseCyan trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(chooseLavenderParameter))
        {
            Debug.LogError("ChooseMenuController: ChooseLavender trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrEmpty(cyanLeaveParameter))
        {
            Debug.LogError("ChooseMenuController: CyanLeave trigger name is empty.", this);
            return false;
        }

        if (string.IsNullOrEmpty(lavenderLeaveParameter))
        {
            Debug.LogError("ChooseMenuController: LavenderLeave trigger name is empty.", this);
            return false;
        }

        if (characterChangeSpeed <= 0f)
        {
            Debug.LogError("ChooseMenuController: CharacterChangeSpeed must be greater than zero.", this);
            return false;
        }

        if (cyanToLavenderBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: CyanToLavenderBaseDuration must be greater than zero.", this);
            return false;
        }

        if (lavenderToCyanBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: LavenderToCyanBaseDuration must be greater than zero.", this);
            return false;
        }

        if (representCyanBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: RepresentCyanBaseDuration must be greater than zero.", this);
            return false;
        }

        if (representLavenderBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: RepresentLavenderBaseDuration must be greater than zero.", this);
            return false;
        }

        if (chooseCyanBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: ChooseCyanBaseDuration must be greater than zero.", this);
            return false;
        }

        if (chooseLavenderBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: ChooseLavenderBaseDuration must be greater than zero.", this);
            return false;
        }

        if (cyanLeaveBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: CyanLeave must be greater than zero.", this);
            return false;
        }

        if (lavenderLeaveBaseDuration <= 0f)
        {
            Debug.LogError("ChooseMenuController: LavenderLeave must be greater than zero.", this);
            return false;
        }

        if (playerConfigCyan == null)
        {
            Debug.LogError("ChooseMenuController: Cyan character config source missing.", this);
            return false;
        }

        if (playerConfigLavender == null)
        {
            Debug.LogError("ChooseMenuController: Lavender character config source missing.", this);
            return false;
        }

        if (outTextHealthStart == null)
        {
            Debug.LogError("ChooseMenuController: OutTextHealthStart is missing.", this);
            return false;
        }

        if (outTextHealthLimit == null)
        {
            Debug.LogError("ChooseMenuController: OutTextHealthLimit is missing.", this);
            return false;
        }

        if (outTextMoveSpeed == null)
        {
            Debug.LogError("ChooseMenuController: OutTextMoveSpeed is missing.", this);
            return false;
        }

        if (outTextRotationSpeed == null)
        {
            Debug.LogError("ChooseMenuController: OutTextRotationSpeed is missing.", this);
            return false;
        }

        if (outTextShotPerMinute == null)
        {
            Debug.LogError("ChooseMenuController: OutTextShotPerMinute is missing.", this);
            return false;
        }

        if (outTextAmmunition == null)
        {
            Debug.LogError("ChooseMenuController: OutTextAmmunition is missing.", this);
            return false;
        }

        if (outTextReloadDuration == null)
        {
            Debug.LogError("ChooseMenuController: OutTextReloadDuration is missing.", this);
            return false;
        }

        if (outTextProjectileDamage == null)
        {
            Debug.LogError("ChooseMenuController: OutTextProjectileDamage is missing.", this);
            return false;
        }

        if (outTextProjectileSpeed == null)
        {
            Debug.LogError("ChooseMenuController: OutTextProjectileSpeed is missing.", this);
            return false;
        }

        if (mainMenuCanvasGroup == null)
        {
            Debug.LogError("ChooseMenuController: MainMenu canvases group game object is missing.", this);
            return false;
        }
        
        if (SceneManager.GetSceneByName(gameSceneName) == null)
        {
            Debug.LogError($"ChooseMenuController: {gameSceneName} is incorrect scene name.", this);
            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        characterChanging = false;
        characterSelecting = false;
        characterSelected = false;
        representing = false;
        returnToMainMenu = false;

        SetButtonInteractable(true);
    }
}