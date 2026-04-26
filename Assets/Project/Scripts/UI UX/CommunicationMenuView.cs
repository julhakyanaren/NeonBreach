using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommunicationMenuView : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Root object of the communication menu.")]
    [SerializeField] private GameObject root;

    [Header("Message")]
    [Tooltip("Text that displays current communication message.")]
    [SerializeField] private TMP_Text messageText;

    [Header("Images")]
    [Tooltip("Previous message image.")]
    [SerializeField] private Image previousImage;

    [Tooltip("Send message image.")]
    [SerializeField] private Image sendImage;

    [Tooltip("Next message image.")]
    [SerializeField] private Image nextImage;

    [Tooltip("Cancel communication menu image.")]
    [SerializeField] private Image cancelImage;

    [Header("Default Colors")]
    [Tooltip("Default color for previous and next images.")]
    [SerializeField] private Color defaultNavigationColor = Color.white;

    [Tooltip("Default color for send image.")]
    [SerializeField] private Color defaultSendColor = Color.white;

    [Tooltip("Default color for cancel image.")]
    [SerializeField] private Color defaultCancelColor = Color.white;

    [Header("Flash Colors")]
    [Tooltip("Flash color for previous and next actions.")]
    [SerializeField] private Color navigationFlashColor = new Color(0.3f, 0.7f, 1f, 1f);

    [Tooltip("Flash color for send action.")]
    [SerializeField] private Color sendFlashColor = new Color(0.3f, 1f, 0.3f, 1f);

    [Tooltip("Flash color for cancel action.")]
    [SerializeField] private Color cancelFlashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Flash Settings")]
    [Tooltip("How long action flash remains visible.")]
    [SerializeField] private float flashDuration = 0.12f;

    private Coroutine previousFlashRoutine;
    private Coroutine sendFlashRoutine;
    private Coroutine nextFlashRoutine;
    private Coroutine cancelFlashRoutine;

    private void Start()
    {
        Hide();
        ResetColors();
    }

    public void Show(string message)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        SetMessage(message);
        ResetColors();
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    public void SetMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = message;
    }

    public void FlashPrevious()
    {
        previousFlashRoutine = StartFlash(previousImage, defaultNavigationColor, navigationFlashColor, previousFlashRoutine);
    }

    public void FlashSend()
    {
        sendFlashRoutine = StartFlash(sendImage, defaultSendColor, sendFlashColor, sendFlashRoutine);
    }

    public void FlashNext()
    {
        nextFlashRoutine = StartFlash(nextImage, defaultNavigationColor, navigationFlashColor, nextFlashRoutine);
    }

    public void FlashCancel()
    {
        cancelFlashRoutine = StartFlash(cancelImage, defaultCancelColor, cancelFlashColor, cancelFlashRoutine);
    }

    private Coroutine StartFlash(Image image, Color defaultColor, Color flashColor, Coroutine currentRoutine)
    {
        if (image == null)
        {
            return null;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        return StartCoroutine(FlashRoutine(image, defaultColor, flashColor));
    }

    private IEnumerator FlashRoutine(Image image, Color defaultColor, Color flashColor)
    {
        image.color = flashColor;

        yield return new WaitForSecondsRealtime(flashDuration);

        image.color = defaultColor;
    }

    private void ResetColors()
    {
        if (previousImage != null)
        {
            previousImage.color = defaultNavigationColor;
        }

        if (nextImage != null)
        {
            nextImage.color = defaultNavigationColor;
        }

        if (sendImage != null)
        {
            sendImage.color = defaultSendColor;
        }

        if (cancelImage != null)
        {
            cancelImage.color = defaultCancelColor;
        }
    }

    private void OnEnable()
    {
        PlayerCommunicationController.OnCommunicationMenuOpened += Show;
        PlayerCommunicationController.OnCommunicationMenuClosed += Hide;
        PlayerCommunicationController.OnCommunicationMessageChanged += SetMessage;

        PlayerCommunicationController.OnCommunicationPreviousFlash += FlashPrevious;
        PlayerCommunicationController.OnCommunicationNextFlash += FlashNext;
        PlayerCommunicationController.OnCommunicationSendFlash += FlashSend;
        PlayerCommunicationController.OnCommunicationCancelFlash += FlashCancel;
    }

    private void OnDisable()
    {
        PlayerCommunicationController.OnCommunicationMenuOpened -= Show;
        PlayerCommunicationController.OnCommunicationMenuClosed -= Hide;
        PlayerCommunicationController.OnCommunicationMessageChanged -= SetMessage;

        PlayerCommunicationController.OnCommunicationPreviousFlash -= FlashPrevious;
        PlayerCommunicationController.OnCommunicationNextFlash -= FlashNext;
        PlayerCommunicationController.OnCommunicationSendFlash -= FlashSend;
        PlayerCommunicationController.OnCommunicationCancelFlash -= FlashCancel;
    }
}
