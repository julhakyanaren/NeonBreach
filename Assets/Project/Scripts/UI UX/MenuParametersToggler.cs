using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuParametersToggler : MonoBehaviour
{
    [Header("TPV Checkbox")]
    [Tooltip("Thirt person view checkbox checkmark")]
    [SerializeField] Image checkmarkTPV;

    [Tooltip("TPV Checkbox")]
    [SerializeField] bool useTPV = false;


    [Header("Use Gamepad Checkbox")]
    [Tooltip("Use gamepad checkbox checkmark")]
    [SerializeField] Image checkmarkUseGamepad;

    [Tooltip("Use gamepad Checkbox")]
    [SerializeField] bool useGamepad = false;

    [Header("Activate multiplayerMode")]
    [Tooltip("Multiplayer mode checkbox checkmark")]
    [SerializeField] Image checkmarkMultiplayerMode;

    [Tooltip("Multiplayer mode Checkbox")]
    [SerializeField] bool useMultiplayerMode = false;

    private void Awake()
    {
        if (useTPV && checkmarkTPV == null)
        {
            Debug.LogError("CameraViewToggler: checkmarkTPV is missing.", this);
            return;
        }
        if (useGamepad && checkmarkUseGamepad == null)
        {
            Debug.LogError("CameraViewToggler: checkmarkUseGamepad is missing.", this);
            return;
        }
        if (useMultiplayerMode && checkmarkMultiplayerMode == null)
        {
            Debug.LogError("CameraViewToggler: checkmarkMultiplayerMode is missing.", this);
            return;
        }
    }

    public void ToggleCameraTPV()
    {
        if (!useTPV)
        {
            return;
        }
        switch (RuntimeOptions.ConfirmedCameraView)
        {
            case CameraViewType.TopDown:
                {
                    RuntimeOptions.ConfirmedCameraView = CameraViewType.ThirdPerson;
                    checkmarkTPV.gameObject.SetActive(true);
                    break;
                }
            case CameraViewType.ThirdPerson:
                {
                    RuntimeOptions.ConfirmedCameraView = CameraViewType.TopDown;
                    checkmarkTPV.gameObject.SetActive(false);
                    break;
                }
        }
    }
    public void ToggleUseGamepad()
    {
        if (!useGamepad)
        {
            return;
        }
        RuntimeOptions.UseGamepad = !RuntimeOptions.UseGamepad;
        checkmarkUseGamepad.gameObject.SetActive(RuntimeOptions.UseGamepad);
    }
    public void ToggleMultiplayerMode()
    {
        if (!useMultiplayerMode)
        {
            return;
        }
        RuntimeOptions.MultiplayerMode = !RuntimeOptions.MultiplayerMode;
        checkmarkMultiplayerMode.gameObject.SetActive(RuntimeOptions.MultiplayerMode);
    }

    private void OnEnable()
    {
        if (useTPV)
        {
            if (RuntimeOptions.ConfirmedCameraView == CameraViewType.ThirdPerson)
            {
                checkmarkTPV.gameObject.SetActive(true);
            }
            else
            {
                checkmarkTPV.gameObject.SetActive(false);
            }
        }
        
        if (useGamepad)
        {
            checkmarkUseGamepad.gameObject.SetActive(RuntimeOptions.UseGamepad);
        }

        if (useMultiplayerMode)
        {
            checkmarkMultiplayerMode.gameObject.SetActive(RuntimeOptions.MultiplayerMode);
        }
    }
}
