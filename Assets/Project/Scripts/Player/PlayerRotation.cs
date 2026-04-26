using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotation : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Player config with base stats.")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Gamepad Rotation Settings")]
    [Tooltip("Minimum stick input value required to rotate the player.")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float lookDeadZone = 0.2f;

    [Tooltip("Rotation speed in degrees per second.")]
    [Range(90f, 1080f)]
    [SerializeField] private float rotationSpeed = 540f;

    [Header("Keyboard Rotation Settings")]
    [Tooltip("Reference to Q/E rotation input.")]
    [SerializeField] private InputActionReference rotateAction;

    [Header("Runtime Rotation Modifiers")]
    [Tooltip("Rotation speed multiplier.")]
    [SerializeField] private float rotationMultiplier = 1f;

    [Header("Mouse Aim Settings")]
    [Tooltip("Camera used for mouse aiming raycast.")]
    [SerializeField] private Camera mainCamera;

    [Header("Input")]
    [Tooltip("Reference to the Look action from the Input System.")]
    [SerializeField] private InputActionReference lookAction;

    [Tooltip("Reference to the PointerPosition action from the Input System.")]
    [SerializeField] private InputActionReference pointerPositionAction;

    [Header("References")]
    [Tooltip("Fire point used to define the mouse aim plane height.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Local input blocker for this player.")]
    [SerializeField] private PlayerInputBlocker inputBlocker;

    [Header("Networking")]
    [SerializeField] private PhotonView photonView;

    public Vector3 CurrentAimPoint { get; private set; }

    private float baseRotationSpeed;
    private Vector2 lookInput;
    private Vector2 pointerScreenPosition;

    private bool ownsRotationInput;

    private void Reset()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        inputBlocker = GetComponent<PlayerInputBlocker>();
    }

    private void Awake()
    {
        if (RuntimeOptions.MultiplayerMode && photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (inputBlocker == null)
        {
            inputBlocker = GetComponent<PlayerInputBlocker>();
        }

        ApplyConfig();
        baseRotationSpeed = rotationSpeed;
        CurrentAimPoint = transform.position + transform.forward * 5f;
    }

    private void ApplyConfig()
    {
        if (playerConfig == null)
        {
            return;
        }

        rotationSpeed = playerConfig.rotationSpeed;
        lookDeadZone = playerConfig.lookDeadZone;
    }

    private void OnEnable()
    {
        ownsRotationInput = false;

        if (RuntimeOptions.MultiplayerMode && photonView != null && photonView.IsMine == false)
        {
            return;
        }

        if (lookAction != null && lookAction.action != null)
        {
            lookAction.action.Enable();
        }

        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            pointerPositionAction.action.Enable();
        }

        if (rotateAction != null && rotateAction.action != null)
        {
            rotateAction.action.Enable();
        }

        ownsRotationInput = true;
    }

    private void OnDisable()
    {
        if (ownsRotationInput == false)
        {
            return;
        }

        if (lookAction != null && lookAction.action != null)
        {
            lookAction.action.Disable();
        }

        if (pointerPositionAction != null && pointerPositionAction.action != null)
        {
            pointerPositionAction.action.Disable();
        }

        if (rotateAction != null && rotateAction.action != null)
        {
            rotateAction.action.Disable();
        }

        ownsRotationInput = false;
    }

    private void Update()
    {
        if (RuntimeOptions.MultiplayerMode && photonView != null && photonView.IsMine == false)
        {
            return;
        }

        if (IsInputBlocked())
        {
            return;
        }

        if (RuntimeOptions.ConfirmedCameraView == CameraViewType.TopDown)
        {
            RotateTopDown();
            return;
        }

        RotateThirdPerson();
    }

    private bool IsInputBlocked()
    {
        if (inputBlocker != null && inputBlocker.IsBlocked)
        {
            return true;
        }

        if (RuntimeOptions.InputBlocked)
        {
            return true;
        }

        return false;
    }

    private void RotateTopDown()
    {
        if (RuntimeOptions.UseGamepad)
        {
            RotateByGamepadTopDown();
            return;
        }

        RotateByMouseTopDown();
    }

    private void RotateThirdPerson()
    {
        if (RuntimeOptions.UseGamepad)
        {
            RotateByGamepadThirdPerson();
            return;
        }

        RotateByKeyboardThirdPerson();
    }

    private void RotateByGamepadTopDown()
    {
        if (lookAction == null)
        {
            return;
        }

        InputAction action = lookAction.action;
        lookInput = action.ReadValue<Vector2>();

        InputControl activeControl = action.activeControl;

        if (activeControl == null)
        {
            return;
        }

        if (activeControl.device is Gamepad == false)
        {
            return;
        }

        Camera gameplayCamera = GetGameplayCamera();

        if (gameplayCamera == null)
        {
            return;
        }

        float inputMagnitude = lookInput.magnitude;

        if (inputMagnitude < lookDeadZone)
        {
            return;
        }

        Vector3 cameraForward = gameplayCamera.transform.forward;
        Vector3 cameraRight = gameplayCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector2 normalizedLookInput = lookInput / inputMagnitude;

        Vector3 lookDirection = cameraForward * normalizedLookInput.y + cameraRight * normalizedLookInput.x;

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float normalizedMagnitude = Mathf.InverseLerp(lookDeadZone, 1f, inputMagnitude);
        float scaledRotationSpeed = rotationSpeed * normalizedMagnitude;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            scaledRotationSpeed * Time.deltaTime);

        CurrentAimPoint = transform.position + lookDirection.normalized * 10f;
    }

    private void RotateByGamepadThirdPerson()
    {
        if (lookAction == null)
        {
            return;
        }

        InputAction action = lookAction.action;
        lookInput = action.ReadValue<Vector2>();

        InputControl activeControl = action.activeControl;

        if (activeControl == null)
        {
            return;
        }

        if (activeControl.device is Gamepad == false)
        {
            return;
        }

        Camera gameplayCamera = GetGameplayCamera();

        if (gameplayCamera == null)
        {
            return;
        }

        float inputMagnitude = lookInput.magnitude;

        if (inputMagnitude < lookDeadZone)
        {
            return;
        }

        Vector3 cameraForward = gameplayCamera.transform.forward;
        Vector3 cameraRight = gameplayCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector2 normalizedLookInput = lookInput / inputMagnitude;

        Vector3 lookDirection = cameraForward * normalizedLookInput.y + cameraRight * normalizedLookInput.x;

        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float normalizedMagnitude = Mathf.InverseLerp(lookDeadZone, 1f, inputMagnitude);
        float scaledRotationSpeed = rotationSpeed * normalizedMagnitude;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            scaledRotationSpeed * Time.deltaTime);

        CurrentAimPoint = transform.position + lookDirection.normalized * 10f;
    }

    private void RotateByMouseTopDown()
    {
        if (pointerPositionAction == null)
        {
            return;
        }

        InputAction action = pointerPositionAction.action;
        pointerScreenPosition = action.ReadValue<Vector2>();

        InputControl activeControl = action.activeControl;

        if (activeControl == null)
        {
            return;
        }

        InputDevice device = activeControl.device;

        if (device is Keyboard == false && device is Mouse == false)
        {
            return;
        }

        Camera gameplayCamera = GetGameplayCamera();

        if (gameplayCamera == null)
        {
            return;
        }

        Ray ray = gameplayCamera.ScreenPointToRay(pointerScreenPosition);

        float planeHeight = transform.position.y;

        if (firePoint != null)
        {
            planeHeight = firePoint.position.y;
        }

        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));

        if (aimPlane.Raycast(ray, out float enter) == false)
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector3 lookDirection = hitPoint - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.25f)
        {
            return;
        }

        CurrentAimPoint = hitPoint;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = targetRotation;
    }

    private void RotateByKeyboardThirdPerson()
    {
        if (rotateAction == null)
        {
            return;
        }

        InputAction action = rotateAction.action;
        float rotateInput = action.ReadValue<float>();

        InputControl activeControl = action.activeControl;

        if (activeControl == null)
        {
            return;
        }

        InputDevice device = activeControl.device;

        if (device is Keyboard == false && device is Mouse == false)
        {
            return;
        }

        if (Mathf.Abs(rotateInput) <= 0.01f)
        {
            return;
        }

        float rotationAmount = rotateInput * rotationSpeed * Time.deltaTime;

        transform.Rotate(0f, rotationAmount, 0f);

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
        {
            CurrentAimPoint = transform.position + forward.normalized * 10f;
        }
    }

    private Camera GetGameplayCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return null;
        }

        if (mainCamera.isActiveAndEnabled == false)
        {
            mainCamera = Camera.main;
        }

        return mainCamera;
    }

    public void SetRotationMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        rotationMultiplier = multiplier;
        rotationSpeed = baseRotationSpeed * rotationMultiplier;
    }

    public void ResetRotationMultiplier()
    {
        rotationMultiplier = 1f;
        rotationSpeed = baseRotationSpeed;
    }
}