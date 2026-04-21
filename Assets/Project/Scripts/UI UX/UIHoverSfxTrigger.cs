using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIHoverSfxTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("Reference to Wwise SFX controller.")]
    [SerializeField] private WwiseSFXController wwiseSfxController;

    [Tooltip("Button that owns this hover trigger.")]
    [SerializeField] private Button targetButton;

    [Header("Settings")]
    [Tooltip("Minimum time between hover sound plays in seconds.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float hoverCooldown = 0.15f;

    private bool pointerInside;
    private float nextAllowedPlayTime;

    private void Reset()
    {
        targetButton = GetComponent<Button>();
    }

    private void Awake()
    {
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (wwiseSfxController == null)
        {
            return;
        }

        if (targetButton == null)
        {
            return;
        }

        if (!targetButton.interactable)
        {
            return;
        }

        if (!targetButton.IsActive())
        {
            return;
        }

        if (pointerInside && Time.unscaledTime < nextAllowedPlayTime)
        {
            return;
        }

        pointerInside = true;
        nextAllowedPlayTime = Time.unscaledTime + hoverCooldown;

        wwiseSfxController.Play(UISfxType.Hover, gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
    }

    private void OnDisable()
    {
        pointerInside = false;
        nextAllowedPlayTime = 0f;
    }
}