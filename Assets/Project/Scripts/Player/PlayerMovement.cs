using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Player config with base stats.")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Movement Runtime Settings")]
    [Tooltip("Movement speed of the player in world space.")]
    [SerializeField] private float moveSpeed = 1f;

    [Tooltip("Current speed multiplier.")]
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Obstacle Block Settings")]
    [Tooltip("Layers that should block player movement, for example Arena and Obstacle.")]
    [SerializeField] private LayerMask obstacleBlockMask;

    [Tooltip("Sphere cast radius used to detect a very close obstacle in front of the player.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float obstacleCheckRadius = 0.3f;

    [Tooltip("Base distance used to check whether movement is blocked by a very close obstacle.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float obstacleCheckDistance = 0.45f;

    [Tooltip("Small vertical offset used if capsule collider is not assigned.")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleCheckHeightOffset = 0.5f;

    [Tooltip("Small safety offset that helps avoid jitter when standing very close to a wall.")]
    [Range(0f, 0.1f)]
    [SerializeField] private float obstacleHitSkin = 0.02f;

    [Header("External Push")]
    [Tooltip("Current external velocity applied by enemies or other gameplay effects.")]
    [SerializeField] private Vector3 externalVelocity;

    [Tooltip("How fast the external velocity fades back to zero.")]
    [Range(0.1f, 50f)]
    [SerializeField] private float externalVelocityDamping = 10f;

    [Tooltip("Maximum magnitude allowed for external velocity.")]
    [Range(0.1f, 50f)]
    [SerializeField] private float maxExternalVelocity = 8f;

    [Header("Input")]
    [Tooltip("Reference to the Move action from the Input System.")]
    [SerializeField] private InputActionReference moveAction;

    [Header("References")]
    [Tooltip("Cached Rigidbody component of the player.")]
    [SerializeField] private Rigidbody playerRigidbody;

    [Tooltip("Cached CapsuleCollider component of the player.")]
    [SerializeField] private CapsuleCollider playerCapsuleCollider;

    [Tooltip("Animator on the visual player model.")]
    [SerializeField] private Animator characterAnimator;

    [Tooltip("Local input blocker for this player.")]
    [SerializeField] private PlayerInputBlocker inputBlocker;

    [Header("Networking")]
    [Tooltip("PhotonView used to detect ownership in multiplayer mode.")]
    [SerializeField] private PhotonView photonView;

    private float baseMoveSpeed;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private Camera cachedMainCamera;
    private float animatorSpeedX;
    private float animatorSpeedY;

    private static readonly int SpeedXHash = Animator.StringToHash("SpeedX");
    private static readonly int SpeedYHash = Animator.StringToHash("SpeedY");

    private void Start()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }
    }

    private void Reset()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerCapsuleCollider = GetComponent<CapsuleCollider>();
        inputBlocker = GetComponent<PlayerInputBlocker>();
    }

    private void Awake()
    {
        if (RuntimeOptions.MultiplayerMode && photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        if (playerCapsuleCollider == null)
        {
            playerCapsuleCollider = GetComponent<CapsuleCollider>();
        }

        if (inputBlocker == null)
        {
            inputBlocker = GetComponent<PlayerInputBlocker>();
        }

        ApplyConfig();
        baseMoveSpeed = moveSpeed;
    }

    private void ApplyConfig()
    {
        if (playerConfig == null)
        {
            return;
        }

        moveSpeed = playerConfig.moveSpeed;
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }

        externalVelocity = Vector3.zero;
        animatorSpeedX = 0f;
        animatorSpeedY = 0f;

        if (characterAnimator != null)
        {
            characterAnimator.SetFloat(SpeedXHash, 0f);
            characterAnimator.SetFloat(SpeedYHash, 0f);
        }
    }

    private void Update()
    {
        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null)
            {
                Debug.Log("[" + gameObject.name + "] IsMine = " + photonView.IsMine, this);
            }
            else
            {
                Debug.Log("[" + gameObject.name + "] PhotonView NULL", this);
            }
        }

        if (RuntimeOptions.MultiplayerMode && photonView != null && photonView.IsMine == false)
        {
            externalVelocity = Vector3.zero;
            return;
        }

        moveInput = ReadMoveInput();

        if (characterAnimator != null)
        {
            characterAnimator.SetFloat(SpeedXHash, animatorSpeedX);
            characterAnimator.SetFloat(SpeedYHash, animatorSpeedY);
        }
    }

    private void FixedUpdate()
    {
        if (RuntimeOptions.MultiplayerMode && photonView != null && photonView.IsMine == false)
        {
            return;
        }

        StabilizeRotation();

        if (IsInputBlocked())
        {
            animatorSpeedX = 0f;
            animatorSpeedY = 0f;
            return;
        }

        moveDirection = GetMoveDirection();

        Vector3 movementVelocity = moveDirection * moveSpeed;
        Vector3 filteredMovementVelocity = FilterVelocityByObstacle(movementVelocity, Time.fixedDeltaTime);
        Vector3 filteredExternalVelocity = FilterVelocityByObstacle(externalVelocity, Time.fixedDeltaTime);

        Vector3 finalVelocity = filteredMovementVelocity + filteredExternalVelocity;

        UpdateAnimatorMovement(filteredMovementVelocity);

        Vector3 nextPosition = playerRigidbody.position + finalVelocity * Time.fixedDeltaTime;
        playerRigidbody.MovePosition(nextPosition);

        externalVelocity = Vector3.Lerp(
            externalVelocity,
            Vector3.zero,
            externalVelocityDamping * Time.fixedDeltaTime);
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

    private Vector2 ReadMoveInput()
    {
        if (RuntimeOptions.MultiplayerMode && photonView != null && photonView.IsMine == false)
        {
            return Vector2.zero;
        }

        if (moveAction == null)
        {
            return Vector2.zero;
        }

        InputAction action = moveAction.action;
        Vector2 value = action.ReadValue<Vector2>();
        InputControl activeControl = action.activeControl;

        if (activeControl == null)
        {
            return Vector2.zero;
        }

        InputDevice device = activeControl.device;

        if (RuntimeOptions.UseGamepad)
        {
            if (device is Gamepad == false)
            {
                return Vector2.zero;
            }

            return value;
        }

        if (device is Keyboard || device is Mouse)
        {
            return value;
        }

        return Vector2.zero;
    }

    private void StabilizeRotation()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        Quaternion currentRotation = playerRigidbody.rotation;
        Vector3 currentEuler = currentRotation.eulerAngles;

        Quaternion stabilizedRotation = Quaternion.Euler(0f, currentEuler.y, 0f);
        playerRigidbody.MoveRotation(stabilizedRotation);
    }

    private Camera GetGameplayCamera()
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }

        if (cachedMainCamera == null)
        {
            return null;
        }

        if (cachedMainCamera.isActiveAndEnabled == false)
        {
            cachedMainCamera = Camera.main;
        }

        return cachedMainCamera;
    }

    private Vector3 GetMoveDirection()
    {
        if (RuntimeOptions.ConfirmedCameraView == CameraViewType.TopDown)
        {
            Camera gameplayCamera = GetGameplayCamera();

            if (gameplayCamera == null)
            {
                return Vector3.zero;
            }

            Vector3 cameraForward = gameplayCamera.transform.forward;
            Vector3 cameraRight = gameplayCamera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 cameraRelativeDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            cameraRelativeDirection = Vector3.ClampMagnitude(cameraRelativeDirection, 1f);

            return cameraRelativeDirection;
        }

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 localDirection = forward * moveInput.y + right * moveInput.x;
        localDirection = Vector3.ClampMagnitude(localDirection, 1f);

        return localDirection;
    }

    private void UpdateAnimatorMovement(Vector3 filteredMovementVelocity)
    {
        if (moveSpeed <= 0.001f)
        {
            animatorSpeedX = 0f;
            animatorSpeedY = 0f;
            return;
        }

        Vector3 localVelocity = transform.InverseTransformDirection(filteredMovementVelocity);

        animatorSpeedX = Mathf.Clamp(localVelocity.x / moveSpeed, -1f, 1f);
        animatorSpeedY = Mathf.Clamp(-localVelocity.z / moveSpeed, -1f, 1f);
    }

    private Vector3 GetObstacleCastOrigin()
    {
        if (playerCapsuleCollider != null)
        {
            return playerRigidbody.position + playerCapsuleCollider.center;
        }

        return playerRigidbody.position + Vector3.up * obstacleCheckHeightOffset;
    }

    private Vector3 FilterVelocityByObstacle(Vector3 velocityToFilter, float deltaTime)
    {
        Vector3 horizontalVelocity = new Vector3(velocityToFilter.x, 0f, velocityToFilter.z);

        if (horizontalVelocity.sqrMagnitude <= 0.0001f)
        {
            return velocityToFilter;
        }

        Vector3 castDirection = horizontalVelocity.normalized;
        Vector3 castOrigin = GetObstacleCastOrigin();
        float dynamicDistance = horizontalVelocity.magnitude * deltaTime;
        float castDistance = obstacleCheckDistance + dynamicDistance;

        RaycastHit hit;

        bool hasHit = Physics.SphereCast(
            castOrigin,
            obstacleCheckRadius,
            castDirection,
            out hit,
            castDistance,
            obstacleBlockMask,
            QueryTriggerInteraction.Ignore);

        if (hasHit == false)
        {
            return velocityToFilter;
        }

        float correctedHitDistance = hit.distance - obstacleHitSkin;

        if (correctedHitDistance > castDistance)
        {
            return velocityToFilter;
        }

        Vector3 projectedHorizontalVelocity = Vector3.ProjectOnPlane(horizontalVelocity, hit.normal);

        Vector3 filteredVelocity = new Vector3(
            projectedHorizontalVelocity.x,
            velocityToFilter.y,
            projectedHorizontalVelocity.z);

        return filteredVelocity;
    }

    public void ApplyExternalPush(Vector3 pushVelocity)
    {
        pushVelocity.y = 0f;
        externalVelocity = Vector3.ClampMagnitude(pushVelocity, maxExternalVelocity);
    }

    public void AddExternalPush(Vector3 pushVelocity)
    {
        pushVelocity.y = 0f;
        externalVelocity += pushVelocity;
        externalVelocity = Vector3.ClampMagnitude(externalVelocity, maxExternalVelocity);
    }

    public void ClearExternalPush()
    {
        externalVelocity = Vector3.zero;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        speedMultiplier = multiplier;
        moveSpeed = baseMoveSpeed * speedMultiplier;
    }

    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1f;
        moveSpeed = baseMoveSpeed;
    }
}