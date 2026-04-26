using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCommunicationController : MonoBehaviourPun
{
    public static Action<string> OnCommunicationMenuOpened;
    public static Action OnCommunicationMenuClosed;
    public static Action<string> OnCommunicationMessageChanged;

    public static Action OnCommunicationPreviousFlash;
    public static Action OnCommunicationSendFlash;
    public static Action OnCommunicationNextFlash;
    public static Action OnCommunicationCancelFlash;

    [Header("References")]
    [Tooltip("Bubble view shown above this player.")]
    [SerializeField] private PlayerCommunicationBubbleView bubbleView;

    [Header("Input Actions")]
    [Tooltip("Previous communication action.")]
    [SerializeField] private InputActionReference previousAction;

    [Tooltip("Send communication action.")]
    [SerializeField] private InputActionReference sendAction;

    [Tooltip("Next communication action.")]
    [SerializeField] private InputActionReference nextAction;

    [Tooltip("Close communication action.")]
    [SerializeField] private InputActionReference closeAction;

    [Header("Messages")]
    [Tooltip("Available communication messages.")]
    [SerializeField]
    private PlayerCommunicationMessageType[] availableMessages =
    {
        PlayerCommunicationMessageType.Help,
        PlayerCommunicationMessageType.FollowMe,
        PlayerCommunicationMessageType.Danger,
        PlayerCommunicationMessageType.Thanks,
        PlayerCommunicationMessageType.GoodGame
    };

    [Header("Close Settings")]
    [Tooltip("Delay before menu closes after cancel.")]
    [SerializeField] private float closeDelayAfterCancel = 0.15f;

    [Tooltip("Should the menu close automatically after send.")]
    [SerializeField] private bool closeAfterSend = false;

    [Tooltip("Delay before menu closes after send.")]
    [SerializeField] private float closeDelayAfterSend = 0.15f;

    private bool isMenuOpen;
    private int currentMessageIndex;
    private Coroutine closeRoutine;

    private void Reset()
    {
        bubbleView = GetComponentInChildren<PlayerCommunicationBubbleView>();
    }

    private void Awake()
    {
        if (bubbleView == null)
        {
            bubbleView = GetComponentInChildren<PlayerCommunicationBubbleView>();
        }
    }

    private void OnEnable()
    {
        EnableAction(previousAction);
        EnableAction(sendAction);
        EnableAction(nextAction);
        EnableAction(closeAction);
    }

    private void OnDisable()
    {
        DisableAction(previousAction);
        DisableAction(sendAction);
        DisableAction(nextAction);
        DisableAction(closeAction);
    }

    private void Update()
    {
        if (CanReadLocalInput() == false)
        {
            return;
        }

        HandleInput();
    }

    private bool CanReadLocalInput()
    {
        if (RuntimeOptions.MultiplayerMode == false)
        {
            return true;
        }

        return photonView.IsMine;
    }

    private void HandleInput()
    {
        if (WasPressed(previousAction))
        {
            if (isMenuOpen == false)
            {
                OpenMenu();
                return;
            }

            SelectPreviousMessage();
            return;
        }

        if (WasPressed(sendAction))
        {
            if (isMenuOpen == false)
            {
                OpenMenu();
                return;
            }

            SendCurrentMessage();
            return;
        }

        if (WasPressed(nextAction))
        {
            if (isMenuOpen == false)
            {
                OpenMenu();
                return;
            }

            SelectNextMessage();
            return;
        }

        if (WasPressed(closeAction))
        {
            if (isMenuOpen == false)
            {
                OpenMenu();
                return;
            }

            StartCloseRoutine(closeDelayAfterCancel, true);
        }
    }

    private void OpenMenu()
    {
        StopCloseRoutine();

        isMenuOpen = true;

        string text = GetCurrentMessageText();

        OnCommunicationMenuOpened?.Invoke(text);
    }

    private void CloseMenuImmediate()
    {
        isMenuOpen = false;

        OnCommunicationMenuClosed?.Invoke();
    }

    private void SelectPreviousMessage()
    {
        currentMessageIndex--;

        if (currentMessageIndex < 0)
        {
            currentMessageIndex = availableMessages.Length - 1;
        }

        string text = GetCurrentMessageText();

        OnCommunicationPreviousFlash?.Invoke();
        OnCommunicationMessageChanged?.Invoke(text);
    }

    private void SelectNextMessage()
    {
        currentMessageIndex++;

        if (currentMessageIndex >= availableMessages.Length)
        {
            currentMessageIndex = 0;
        }

        string text = GetCurrentMessageText();

        OnCommunicationNextFlash?.Invoke();
        OnCommunicationMessageChanged?.Invoke(text);
    }

    private void SendCurrentMessage()
    {
        string text = GetCurrentMessageText();

        OnCommunicationSendFlash?.Invoke();

        photonView.RPC(nameof(RPC_ShowCommunicationBubble), RpcTarget.All, text);

        if (closeAfterSend)
        {
            StartCloseRoutine(closeDelayAfterSend, false);
        }
    }

    [PunRPC]
    private void RPC_ShowCommunicationBubble(string message)
    {
        if (bubbleView == null)
        {
            return;
        }

        bubbleView.Show(message);
    }

    private void StartCloseRoutine(float delay, bool playCancelFlash)
    {
        StopCloseRoutine();
        closeRoutine = StartCoroutine(CloseRoutine(delay, playCancelFlash));
    }

    private IEnumerator CloseRoutine(float delay, bool playCancelFlash)
    {
        if (playCancelFlash)
        {
            OnCommunicationCancelFlash?.Invoke();
        }

        yield return new WaitForSecondsRealtime(delay);

        CloseMenuImmediate();

        closeRoutine = null;
    }

    private void StopCloseRoutine()
    {
        if (closeRoutine == null)
        {
            return;
        }

        StopCoroutine(closeRoutine);
        closeRoutine = null;
    }

    private string GetCurrentMessageText()
    {
        return StaticTextLibrary.Get(GetCurrentMessage());
    }

    private PlayerCommunicationMessageType GetCurrentMessage()
    {
        if (availableMessages == null || availableMessages.Length == 0)
        {
            return PlayerCommunicationMessageType.None;
        }

        return availableMessages[currentMessageIndex];
    }

    private bool WasPressed(InputActionReference actionReference)
    {
        if (actionReference == null)
        {
            return false;
        }

        if (actionReference.action == null)
        {
            return false;
        }

        return actionReference.action.WasPressedThisFrame();
    }

    private void EnableAction(InputActionReference actionReference)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return;
        }

        actionReference.action.Enable();
    }

    private void DisableAction(InputActionReference actionReference)
    {
        if (actionReference == null || actionReference.action == null)
        {
            return;
        }

        actionReference.action.Disable();
    }
}