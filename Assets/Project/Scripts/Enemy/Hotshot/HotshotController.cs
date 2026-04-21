using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HotshotController : MonoBehaviour, IEnemyController, IWaveScalable
{
    [Header("Config source")]
    [Tooltip("Hotshot config with controller settings.")]
    [SerializeField] private HotshotConfig configHotshotSO;

    [Header("Detection Settings")]
    [Tooltip("Radius in which the enemy can detect player targets.")]
    [SerializeField] private float detectionRadius = 6f;

    [Tooltip("Layer mask used to search for player targets.")]
    [SerializeField] private LayerMask playerLayerMask;

    [Tooltip("Time interval between target searches.")]
    [SerializeField] private float targetRefreshInterval = 0.25f;

    [Header("Movement Settings")]
    [Tooltip("Movement speed of the enemy.")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Tooltip("Rotation speed while turning to the target.")]
    [SerializeField] private float rotationSpeed = 8f;

    [Tooltip("If target is closer than this distance, enemy retreats.")]
    [SerializeField] private float minAttackDistance = 4f;

    [Tooltip("If target is farther than this distance, enemy chases.")]
    [SerializeField] private float maxAttackDistance = 5f;

    [Tooltip("Small threshold to prevent jitter when movement direction is too small.")]
    [SerializeField] private float movementDeadZone = 0.15f;

    [Header("Aim Settings")]
    [Tooltip("Weapon visual object that should rotate on local X for vertical aiming.")]
    [SerializeField] private Transform weaponPitchPivot;

    [Tooltip("Vertical aim rotation speed in degrees per second.")]
    [SerializeField] private float aimRotationSpeed = 120f;

    [Tooltip("Allowed angle difference before the weapon is considered aligned.")]
    [SerializeField] private float aimTolerance = 1.5f;

    [Tooltip("Idle weapon pitch angle when target is not attackable.")]
    [SerializeField] private float idlePitchAngle = 0f;

    [Tooltip("If enabled, weapon pitch returns to idle when Hotshot cannot attack.")]
    [SerializeField] private bool resetPitchWhenCannotAttack = true;

    [Header("Wave Scaling")]
    [Tooltip("Runtime projectile damage multiplier applied from the current wave.")]
    [SerializeField] private float projectileDamageMultiplier = 1f;

    [Header("References")]
    [Tooltip("Rigidbody used for movement.")]
    [SerializeField] private Rigidbody enemyRigidbody;

    [Tooltip("Animator controlling root body states.")]
    [SerializeField] private Animator rootAnimator;

    [Tooltip("Reference to the Hotshot shooter component.")]
    [SerializeField] private HotshotShooter hotshotShooter;

    [Header("Animator Parameters")]
    [Tooltip("Animator bool parameter name that shows whether target is found.")]
    [SerializeField] private string hasTargetParameter = "HasTarget";

    [Header("Sensors")]
    [Tooltip("Enemy movement sensor script.")]
    [SerializeField] private EnemyMovementSensor movementSensor;

    [Tooltip("Hotshot aim sensor script.")]
    [SerializeField] private HotshotAimSensor aimSensor;

    [Header("Runtime")]
    [Tooltip("Wave index this enemy was spawned on.")]
    [SerializeField] private int currentWaveIndex;

    [Header("Debug Draw Gizmos")]
    [Tooltip("Draw detection radius.")]
    [SerializeField] private bool drawDetectionRadius = true;

    [Tooltip("Draw attack min distance.")]
    [SerializeField] private bool drawAttackMinDistance = true;

    [Tooltip("Draw attack max distance.")]
    [SerializeField] private bool drawAttackMaxDistance = true;

    private Transform currentTarget;
    private Vector3 currentMoveDirection;
    private float nextTargetRefreshTime;
    private readonly Collider[] targetResults = new Collider[16];

    private bool isDead;
    private bool hasAimSolution;
    private float targetAimAngle;
    private Transform cachedAimTarget;

    private enum HotshotState
    {
        Idle,
        Chasing,
        Retreating,
        Attacking
    }

    private HotshotState currentState = HotshotState.Idle;

    public float MoveSpeed
    {
        get
        {
            return moveSpeed;
        }
        set
        {
            moveSpeed = Mathf.Max(0.1f, value);
        }
    }

    public int CurrentWaveIndex
    {
        get
        {
            return currentWaveIndex;
        }
    }

    public float RotationSpeed
    {
        get
        {
            return rotationSpeed;
        }
        set
        {
            rotationSpeed = Mathf.Max(0.1f, value);
        }
    }

    public float DetectionRadius
    {
        get
        {
            return detectionRadius;
        }
        set
        {
            detectionRadius = Mathf.Max(0.1f, value);
        }
    }

    public float MaxAttackDistance
    {
        get
        {
            return maxAttackDistance;
        }
    }

    public float MinAttackDistance
    {
        get
        {
            return minAttackDistance;
        }
    }

    public float GetProjectileDamageMultiplier()
    {
        return projectileDamageMultiplier;
    }

    public void ApplyWaveScaling(WaveScalingData scalingData)
    {
        projectileDamageMultiplier = scalingData.DamageMultiplier;
    }

    private void Reset()
    {
        enemyRigidbody = GetComponent<Rigidbody>();
        rootAnimator = GetComponent<Animator>();
        hotshotShooter = GetComponent<HotshotShooter>();
        movementSensor = GetComponent<EnemyMovementSensor>();
        aimSensor = GetComponent<HotshotAimSensor>();
    }

    private void Awake()
    {
        if (enemyRigidbody == null)
        {
            enemyRigidbody = GetComponent<Rigidbody>();
        }

        if (rootAnimator == null)
        {
            rootAnimator = GetComponent<Animator>();
        }

        if (hotshotShooter == null)
        {
            hotshotShooter = GetComponent<HotshotShooter>();
        }

        if (movementSensor == null)
        {
            movementSensor = GetComponent<EnemyMovementSensor>();
        }

        if (aimSensor == null)
        {
            aimSensor = GetComponent<HotshotAimSensor>();
        }

        ApplyConfig();

        MoveSpeed = moveSpeed;
        RotationSpeed = rotationSpeed;
        DetectionRadius = detectionRadius;
        minAttackDistance = Mathf.Max(0.1f, minAttackDistance);
        maxAttackDistance = Mathf.Max(minAttackDistance, maxAttackDistance);
        movementDeadZone = Mathf.Max(0.01f, movementDeadZone);
        targetRefreshInterval = Mathf.Max(0.05f, targetRefreshInterval);
        aimRotationSpeed = Mathf.Max(0.1f, aimRotationSpeed);
        aimTolerance = Mathf.Max(0.05f, aimTolerance);
    }

    private void ApplyConfig()
    {
        if (configHotshotSO == null)
        {
            return;
        }

        DetectionRadius = configHotshotSO.detectionRadius;
        MoveSpeed = configHotshotSO.moveSpeed;
        RotationSpeed = configHotshotSO.rotationSpeed;

        targetRefreshInterval = Mathf.Max(0.05f, configHotshotSO.targetRefreshInterval);
        minAttackDistance = Mathf.Max(0.1f, configHotshotSO.minAttackDistance);
        maxAttackDistance = Mathf.Max(minAttackDistance, configHotshotSO.maxAttackDistance);
        movementDeadZone = Mathf.Max(0.01f, configHotshotSO.movementDeadZone);

        aimRotationSpeed = Mathf.Max(0.1f, configHotshotSO.aimRotationSpeed);
        aimTolerance = Mathf.Max(0.05f, configHotshotSO.aimTolerance);
        idlePitchAngle = configHotshotSO.idlePitchAngle;
        resetPitchWhenCannotAttack = configHotshotSO.resetPitchWhenCannotAttack;
    }

    private void Update()
    {
        if (isDead)
        {
            UpdateAnimator();
            return;
        }

        RefreshTargetIfNeeded();
        UpdateMovementDirection();
        RotateToTarget();
        UpdateAimLogic();
        RotateWeaponPitch();
        TryAttackTarget();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            UpdateAnimator();
            return;
        }

        MoveToTarget();
    }

    public void SetDeadState(bool value)
    {
        isDead = value;

        if (!value)
        {
            return;
        }

        currentTarget = null;
        currentMoveDirection = Vector3.zero;
        hasAimSolution = false;
        cachedAimTarget = null;
        targetAimAngle = idlePitchAngle;
    }

    private void RefreshTargetIfNeeded()
    {
        if (Time.time < nextTargetRefreshTime)
        {
            return;
        }

        nextTargetRefreshTime = Time.time + targetRefreshInterval;
        FindTarget();
    }

    public void FindTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            DetectionRadius,
            targetResults,
            playerLayerMask
        );

        if (hitCount <= 0)
        {
            currentTarget = null;
            return;
        }

        Transform closestTarget = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = targetResults[i];

            if (hitCollider == null)
            {
                continue;
            }

            PlayerMovement playerMovement = hitCollider.GetComponentInParent<PlayerMovement>();

            if (playerMovement == null)
            {
                continue;
            }

            PlayerHealth playerHealth = hitCollider.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
            {
                continue;
            }

            if (!playerHealth.IsTargetable)
            {
                continue;
            }

            Transform candidateTarget = playerMovement.transform;
            Vector3 flatDirection = candidateTarget.position - transform.position;
            flatDirection.y = 0f;

            float sqrDistance = flatDirection.sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestTarget = candidateTarget;
            }
        }

        currentTarget = closestTarget;
    }

    public void UpdateMovementDirection()
    {
        currentMoveDirection = Vector3.zero;

        if (currentTarget == null)
        {
            currentState = HotshotState.Idle;
            return;
        }

        Vector3 flatDirectionToTarget = currentTarget.position - transform.position;
        flatDirectionToTarget.y = 0f;

        float distanceToTarget = flatDirectionToTarget.magnitude;

        if (distanceToTarget > DetectionRadius)
        {
            currentTarget = null;
            currentState = HotshotState.Idle;
            return;
        }

        if (distanceToTarget > maxAttackDistance)
        {
            currentState = HotshotState.Chasing;
            currentMoveDirection = flatDirectionToTarget.normalized;
            return;
        }

        if (distanceToTarget < minAttackDistance)
        {
            currentState = HotshotState.Retreating;
            currentMoveDirection = (-flatDirectionToTarget).normalized;
            return;
        }

        currentState = HotshotState.Attacking;
    }

    public void RotateToTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        Vector3 directionToTarget = currentTarget.position - transform.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget.normalized);
        Quaternion newRotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            RotationSpeed * Time.deltaTime
        );

        enemyRigidbody.MoveRotation(newRotation);
    }

    public void MoveToTarget()
    {
        if (currentState != HotshotState.Chasing && currentState != HotshotState.Retreating)
        {
            return;
        }

        if (currentMoveDirection.sqrMagnitude <= movementDeadZone * movementDeadZone)
        {
            return;
        }

        if (movementSensor != null)
        {
            if (!movementSensor.CanMoveInDirection(currentMoveDirection))
            {
                return;
            }
        }

        Vector3 nextPosition = enemyRigidbody.position + currentMoveDirection * MoveSpeed * Time.fixedDeltaTime;
        enemyRigidbody.MovePosition(nextPosition);
    }

    private void UpdateAimLogic()
    {
        if (weaponPitchPivot == null)
        {
            return;
        }

        if (currentTarget == null)
        {
            ClearAimSolution();
            ApplyIdlePitchIfNeeded();
            return;
        }

        if (currentState != HotshotState.Attacking)
        {
            ClearAimSolution();
            ApplyIdlePitchIfNeeded();
            return;
        }

        if (hotshotShooter == null)
        {
            ClearAimSolution();
            ApplyIdlePitchIfNeeded();
            return;
        }

        if (aimSensor == null)
        {
            ClearAimSolution();
            ApplyIdlePitchIfNeeded();
            return;
        }

        if (hotshotShooter.IsReloading)
        {
            ClearAimSolution();
            ApplyIdlePitchIfNeeded();
            return;
        }

        bool canReuseCurrentSolution = false;

        if (hasAimSolution)
        {
            if (cachedAimTarget == currentTarget)
            {
                if (aimSensor.IsAngleStillValid(currentTarget, targetAimAngle))
                {
                    canReuseCurrentSolution = true;
                }
            }
        }

        if (canReuseCurrentSolution)
        {
            return;
        }

        if (aimSensor.TryGetAimAngle(currentTarget, out float foundAimAngle))
        {
            hasAimSolution = true;
            cachedAimTarget = currentTarget;
            targetAimAngle = foundAimAngle;
            return;
        }

        ClearAimSolution();
        ApplyIdlePitchIfNeeded();
    }

    private void RotateWeaponPitch()
    {
        if (weaponPitchPivot == null)
        {
            return;
        }

        float currentPitch = GetCurrentWeaponPitchAngle();
        float nextPitch = Mathf.MoveTowardsAngle(
            currentPitch,
            targetAimAngle,
            aimRotationSpeed * Time.deltaTime
        );

        Vector3 localEulerAngles = weaponPitchPivot.localEulerAngles;
        localEulerAngles.x = nextPitch;
        weaponPitchPivot.localEulerAngles = localEulerAngles;
    }

    private void TryAttackTarget()
    {
        if (currentTarget == null)
        {
            return;
        }

        if (currentState != HotshotState.Attacking)
        {
            return;
        }

        if (hotshotShooter == null)
        {
            return;
        }

        if (hotshotShooter.IsReloading)
        {
            return;
        }

        if (!hasAimSolution)
        {
            return;
        }

        if (!IsWeaponPitchAligned())
        {
            return;
        }

        if (!hotshotShooter.CanShoot())
        {
            return;
        }

        hotshotShooter.TryShoot(currentTarget);
    }

    private bool IsWeaponPitchAligned()
    {
        if (weaponPitchPivot == null)
        {
            return false;
        }

        float currentPitch = GetCurrentWeaponPitchAngle();
        float pitchDelta = Mathf.Abs(Mathf.DeltaAngle(currentPitch, targetAimAngle));

        if (pitchDelta <= aimTolerance)
        {
            return true;
        }

        return false;
    }

    private float GetCurrentWeaponPitchAngle()
    {
        if (weaponPitchPivot == null)
        {
            return 0f;
        }

        float rawX = weaponPitchPivot.localEulerAngles.x;
        return Mathf.DeltaAngle(0f, rawX);
    }

    private void ApplyIdlePitchIfNeeded()
    {
        if (!resetPitchWhenCannotAttack)
        {
            return;
        }

        targetAimAngle = idlePitchAngle;
    }

    private void ClearAimSolution()
    {
        hasAimSolution = false;
        cachedAimTarget = null;
    }

    private void UpdateAnimator()
    {
        if (rootAnimator == null)
        {
            return;
        }

        bool hasTarget = false;

        if (currentTarget != null)
        {
            hasTarget = true;
        }

        if (!string.IsNullOrWhiteSpace(hasTargetParameter))
        {
            rootAnimator.SetBool(hasTargetParameter, hasTarget);
        }
    }

    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }

    public bool HasTarget()
    {
        if (currentTarget == null)
        {
            return false;
        }

        return true;
    }

    public bool IsAttacking()
    {
        if (currentState == HotshotState.Attacking)
        {
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (drawDetectionRadius)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        if (drawAttackMinDistance)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, minAttackDistance);
        }

        if (drawAttackMaxDistance)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, maxAttackDistance);
        }
    }

    public void SetWaveIndex(int waveIndex)
    {
        currentWaveIndex = waveIndex;
    }
}