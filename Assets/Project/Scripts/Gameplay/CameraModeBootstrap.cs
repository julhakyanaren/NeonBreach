using UnityEngine;

public class CameraModeBootstrap : MonoBehaviour, IPlayerSpawnListener
{
    [Header("Camera Rigs")]
    [Tooltip("Top-down camera root.")]
    [SerializeField] private GameObject topDownCameraRig;

    [Tooltip("Third-person camera root.")]
    [SerializeField] private GameObject thirdPersonCameraRig;

    [Header("Camera Follow References")]
    [Tooltip("Top-down follow component.")]
    [SerializeField] private TopDownCameraFollow topDownFollow;

    [Tooltip("Third-person follow component.")]
    [SerializeField] private ThirdPersonCameraFollow thirdPersonFollow;

    [Header("Third Person")]
    [Tooltip("Child anchor name used by the third-person camera.")]
    [SerializeField] private string thirdPersonAnchorName = "ThirdPersonCameraAnchor";

    private GameObject currentPlayerInstance;

    public void OnPlayerSpawned(GameObject playerInstance)
    {
        if (playerInstance == null)
        {
            Debug.LogError("CameraModeBootstrap: playerInstance is null.", this);
            return;
        }

        currentPlayerInstance = playerInstance;
        ApplyCameraMode(playerInstance.transform);
    }

    private void ApplyCameraMode(Transform playerTransform)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("CameraModeBootstrap: playerTransform is null.", this);
            return;
        }

        if (RuntimeOptions.ConfirmedCameraView == CameraViewType.TopDown)
        {
            EnableTopDown(playerTransform);
            return;
        }

        if (RuntimeOptions.ConfirmedCameraView == CameraViewType.ThirdPerson)
        {
            EnableThirdPerson(playerTransform);
            return;
        }

        EnableTopDown(playerTransform);
    }

    private void EnableTopDown(Transform playerTransform)
    {
        if (topDownCameraRig != null)
        {
            topDownCameraRig.SetActive(true);
        }

        if (thirdPersonCameraRig != null)
        {
            thirdPersonCameraRig.SetActive(false);
        }

        if (topDownFollow != null)
        {
            topDownFollow.SetTarget(playerTransform);
        }
    }

    private void EnableThirdPerson(Transform playerTransform)
    {
        if (topDownCameraRig != null)
        {
            topDownCameraRig.SetActive(false);
        }

        if (thirdPersonCameraRig != null)
        {
            thirdPersonCameraRig.SetActive(true);
        }

        Transform anchor = playerTransform.Find(thirdPersonAnchorName);

        if (anchor == null)
        {
            Debug.LogWarning("CameraModeBootstrap: Third-person anchor not found on spawned player.", this);
            return;
        }

        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.SetTarget(anchor);
        }
    }
}