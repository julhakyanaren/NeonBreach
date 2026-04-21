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
    [SerializeField] bool useCheckboxTPV = false;


    [Header("Use Gamepad Checkbox")]
    [Tooltip("Thirt person view checkbox checkmark")]
    [SerializeField] Image checkmarkUseGamepad;

    [Tooltip("TPV Checkbox")]
    [SerializeField] bool useChecboxUseGamepad = false;

    private void Awake()
    {
        if (useCheckboxTPV && checkmarkTPV == null)
        {
            Debug.LogError("CameraViewToggler: checkmarkTPV is missing.", this);
            return;
        }
        if (useChecboxUseGamepad && checkmarkUseGamepad == null)
        {
            Debug.LogError("CameraViewToggler: checkmarkUseGamepad is missing.", this);
            return;
        }
    }

    public void ToggleCameraTPV()
    {
        if (!useCheckboxTPV)
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
        if (!useChecboxUseGamepad)
        {
            return;
        }
        RuntimeOptions.UseGamepad = !RuntimeOptions.UseGamepad;
        checkmarkUseGamepad.gameObject.SetActive(RuntimeOptions.UseGamepad);
    }

    private void OnEnable()
    {
        if (useCheckboxTPV)
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
        
        if (useChecboxUseGamepad)
        {
            checkmarkUseGamepad.gameObject.SetActive(RuntimeOptions.UseGamepad);
        }
    }
}
