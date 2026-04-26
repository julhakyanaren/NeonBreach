using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCommunicationBubbleView : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Root object of the communication bubble. Usually Canvas object.")]
    [SerializeField] private GameObject root;

    [Header("Text")]
    [Tooltip("TMP text used to display communication message.")]
    [SerializeField] private TMP_Text messageText;

    [Tooltip("Default text color.")]
    [SerializeField] private Color textColor = Color.white;

    [Header("Background")]
    [Tooltip("Optional background image behind the message.")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("Default background color.")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);

    [Header("Fade Settings")]
    [Tooltip("How long the bubble fades in.")]
    [SerializeField] private float fadeInDuration = 0.15f;

    [Tooltip("How long the bubble stays fully visible.")]
    [SerializeField] private float visibleDuration = 2.5f;

    [Tooltip("How long the bubble fades out.")]
    [SerializeField] private float fadeOutDuration = 0.25f;

    [Header("Billboard")]
    [Tooltip("Optional target camera. If empty, Camera.main will be used.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("Should the bubble rotate toward the camera.")]
    [SerializeField] private bool billboardToCamera = true;

    [Tooltip("If true, billboard rotates only around Y axis.")]
    [SerializeField] private bool yAxisOnly = true;

    [Tooltip("Invert billboard facing if the text appears backwards.")]
    [SerializeField] private bool invertFacing = false;

    private Coroutine showRoutine;

    private void Awake()
    {
        ApplyColors();
        HideImmediate();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (billboardToCamera == false)
        {
            return;
        }

        if (root == null)
        {
            return;
        }

        if (root.activeSelf == false)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 cameraForward = targetCamera.transform.forward;

        if (yAxisOnly)
        {
            cameraForward.y = 0f;
        }

        if (cameraForward.sqrMagnitude <= 0.001f)
        {
            return;
        }

        if (invertFacing)
        {
            cameraForward = -cameraForward;
        }

        root.transform.rotation = Quaternion.LookRotation(cameraForward);
    }

    public void Show(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }

        ApplyColors();

        if (root != null)
        {
            root.SetActive(true);
        }

        StartShowRoutine();
    }

    public void HideImmediate()
    {
        StopShowRoutine();
        SetAlpha(0f);

        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void StartShowRoutine()
    {
        StopShowRoutine();
        showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return FadeRoutine(0f, 1f, fadeInDuration);

        yield return new WaitForSeconds(visibleDuration);

        yield return FadeRoutine(1f, 0f, fadeOutDuration);

        if (root != null)
        {
            root.SetActive(false);
        }

        showRoutine = null;
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(toAlpha);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = timer / duration;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(toAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (messageText != null)
        {
            Color color = textColor;
            color.a *= alpha;
            messageText.color = color;
        }

        if (backgroundImage != null)
        {
            Color color = backgroundColor;
            color.a *= alpha;
            backgroundImage.color = color;
        }
    }

    private void ApplyColors()
    {
        SetAlpha(1f);
    }

    private void StopShowRoutine()
    {
        if (showRoutine == null)
        {
            return;
        }

        StopCoroutine(showRoutine);
        showRoutine = null;
    }
}