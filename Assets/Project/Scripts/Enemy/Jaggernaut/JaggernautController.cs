using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

[RequireComponent(typeof(WwiseEnemySFXController))]
public class JaggernautController : MonoBehaviour, IEnemyController, IWaveScalable
{
    [Header("Config Source")]
    [Tooltip("Jaggernaut config with controller settings.")]
    [SerializeField] private JaggernautConfig configJaggernautSO;

    [Header("Animator")]
    [Tooltip("Reference to the Jaggernaut animator.")]
    [SerializeField] private Animator jaggernautAnimator;

    [Tooltip("Animator bool parameter that shows whether the enemy currently has a target.")]
    [SerializeField] private string hasTargetParameter = "HasTarget";

    [Header("Audio")]
    [Tooltip("Enemy Wwise SFX controller reference.")]
    [SerializeField] private WwiseEnemySFXController enemySfxController;

    [Header("Target")]
    [Tooltip("Layer mask used to detect player targets.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("Maximum distance at which the enemy can detect the player.")]
    [Range(1f, 20f)]
    [SerializeField] private float detectionRadius = 5f;

    [Header("Movement")]
    [Tooltip("Movement speed of the enemy.")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("How fast the enemy rotates toward the target.")]
    [SerializeField] private float rotationSpeed = 10f;

    [Tooltip("Distance at which the enemy stops near the player.")]
    [SerializeField] private float stopDistance = 1f;

    [Tooltip("Extra distance near the target where push-block check starts working.")]
    [SerializeField] private float pushBlockCheckDistanceOffset = 0.4f;

    [Header("Contact Damage")]
    [Tooltip("Damage dealt to the player on contact.")]
    [SerializeField] private float contactDamage = 10f;

    [Tooltip("Delay between damage ticks while the player stays in contact.")]
    [SerializeField] private float damageCooldown = 1f;

    [Header("Health Scaling")]
    [Tooltip("Health percent at which Jaggernaut reaches maximum enraged stats.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float enrageHealthThresholdNormalized = 0.1f;

    [Tooltip("Move speed multiplier at full health.")]
    [SerializeField] private float minMoveSpeedCoeff = 0.75f;

    [Tooltip("Move speed multiplier at low health.")]
    [SerializeField] private float maxMoveSpeedCoeff = 1.35f;

    [Tooltip("Rotation speed multiplier at full health.")]
    [SerializeField] private float minRotationSpeedCoeff = 0.8f;

    [Tooltip("Rotation speed multiplier at low health.")]
    [SerializeField] private float maxRotationSpeedCoeff = 1.5f;

    [Tooltip("Damage multiplier at full health.")]
    [SerializeField] private float minDamageCoeff = 0.75f;

    [Tooltip("Damage multiplier at low health.")]
    [SerializeField] private float maxDamageCoeff = 1.8f;

    [Header("Push Block Check")]
    [Tooltip("Point used as the center reference for obstacle checking before push.")]
    [SerializeField] private Transform pushCheckPoint;

    [Tooltip("Size of the OverlapBox used to detect walls or obstacles that should block push.")]
    [SerializeField] private Vector3 pushBlockedBoxSize = new Vector3(1f, 0.45f, 0.9f);

    [Tooltip("Local offset from pushCheckPoint for fine-tuning the obstacle check box position.")]
    [SerializeField] private Vector3 pushCheckOffset = new Vector3(0f, 0f, 0.5f);

    [Tooltip("Layers that should block player push, such as Arena or Obstacle.")]
    [SerializeField] private LayerMask pushBlockerMask;

    [Tooltip("If enabled, player push will be blocked when an obstacle is detected in the check box.")]
    [SerializeField] private bool blockPushWhenObstacleDetected = true;

    [Header("Wave Scaling")]
    [Tooltip("Runtime damage multiplier applied from the current wave.")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("References")]
    [Tooltip("EnemyLineOfSightSensor script reference.")]
    [SerializeField] private EnemyLineOfSightSensor lineOfSightSensor;

    [Header("Sensors")]
    [Tooltip("EnemyMovementSensor script reference.")]
    [SerializeField] private EnemyMovementSensor movementSensor;

    [Header("Runtime")]
    [Tooltip("Current player target.")]
    [SerializeField] private Transform target;

    [Tooltip("Wave index this enemy was spawned on.")]
    [SerializeField] private int currentWaveIndex;

    [Tooltip("Current movement direction.")]
    [SerializeField] private Vector3 moveDirection;

    [Tooltip("Whether the enemy currently has a valid target.")]
    [SerializeField] private bool hasTarget;

    [Header("Runtime Scaling Debug")]
    [Tooltip("Current move speed multiplier by health.")]
    [SerializeField] private float currentMoveSpeedCoeff = 1f;

    [Tooltip("Current rotation speed multiplier by health.")]
    [SerializeField] private float currentRotationSpeedCoeff = 1f;

    [Tooltip("Current damage multiplier by health.")]
    [SerializeField] private float currentDamageCoeff = 1f;

    [Header("Debug")]
    [Tooltip("Draw Push Check")]
    [SerializeField] private bool drawGizmosPuchCheck = true;

    [Tooltip("Draw Detection Radius")]
    [SerializeField] private bool drawGizmosDetectionRadius = true;

    [Tooltip("Draw Stop Distance")]
    [SerializeField] private bool drawGizmosStopDistance = true;

    private bool previousHasTarget;
    private float lastDamageTime = -999f;
    private int hasTargetParameterHash;
    private bool canPush = true;

    private float baseMoveSpeed;
    private float baseRotationSpeed;
    private float baseContactDamage;

    private bool isDead = false;

    public int CurrentWaveIndex
    {
        get
        {
            return currentWaveIndex;
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
            detectionRadius = Mathf.Max(0f, value);
        }
    }

    public float MoveSpeed
    {
        get
        {
            return moveSpeed;
        }
        set
        {
            moveSpeed = Mathf.Max(0f, value);
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
            rotationSpeed = Mathf.Max(0f, value);
        }
    }

    public float ContactDamage
    {
        get
        {
            return contactDamage;
        }
        set
        {
            contactDamage = Mathf.Max(0f, value);
        }
    }

    private void ApplyConfig()
    {
        if (configJaggernautSO == null)
        {
            return;
        }

        DetectionRadius = configJaggernautSO.detectionRadius;
        MoveSpeed = configJaggernautSO.moveSpeed;
        RotationSpeed = configJaggernautSO.rotationSpeed;
        stopDistance = Mathf.Max(0f, configJaggernautSO.stopDistance);
        pushBlockCheckDistanceOffset = Mathf.Max(0f, configJaggernautSO.pushBlockCheckDistanceOffset);

        ContactDamage = configJaggernautSO.contactDamage;
        damageCooldown = Mathf.Max(0f, configJaggernautSO.damageCooldown);

        enrageHealthThresholdNormalized = Mathf.Clamp(configJaggernautSO.enrageHealthThresholdNormalized, 0.01f, 1f);

        minMoveSpeedCoeff = Mathf.Max(0f, configJaggernautSO.minMoveSpeedCoeff);
        maxMoveSpeedCoeff = Mathf.Max(minMoveSpeedCoeff, configJaggernautSO.maxMoveSpeedCoeff);

        minRotationSpeedCoeff = Mathf.Max(0f, configJaggernautSO.minRotationSpeedCoeff);
        maxRotationSpeedCoeff = Mathf.Max(minRotationSpeedCoeff, configJaggernautSO.maxRotationSpeedCoeff);

        minDamageCoeff = Mathf.Max(0f, configJaggernautSO.minDamageCoeff);
        maxDamageCoeff = Mathf.Max(minDamageCoeff, configJaggernautSO.maxDamageCoeff);

        pushBlockedBoxSize = configJaggernautSO.pushBlockedBoxSize;
        pushCheckOffset = configJaggernautSO.pushCheckOffset;
        blockPushWhenObstacleDetected = configJaggernautSO.blockPushWhenObstacleDetected;

        if (lineOfSightSensor != null)
        {
            lineOfSightSensor.RayDistance = Mathf.Max(configJaggernautSO.sightRayDistance, configJaggernautSO.detectionRadius + 1f);
        }
    }

    private void RebuildRuntimeDamage()
    {
    }

    private void Reset()
    {
        jaggernautAnimator = GetComponent<Animator>();
        enemySfxController = GetComponent<WwiseEnemySFXController>();
    }

    private void Awake()
    {
        if (jaggernautAnimator == null)
        {
            jaggernautAnimator = GetComponent<Animator>();
        }

        if (enemySfxController == null)
        {
            enemySfxController = GetComponent<WwiseEnemySFXController>();
        }

        if (pushCheckPoint == null)
        {
            pushCheckPoint = transform;
        }

        ApplyConfig();

        baseMoveSpeed = moveSpeed;
        baseRotationSpeed = rotationSpeed;
        baseContactDamage = contactDamage;

        hasTargetParameterHash = Animator.StringToHash(hasTargetParameter);
        previousHasTarget = hasTarget;
    }

    private void OnEnable()
    {
        StaticEvents.PauseOpened += HandlePauseOpened;
        StaticEvents.PauseClosed += HandlePauseClosed;
    }

    private void OnDisable()
    {
        StaticEvents.PauseOpened -= HandlePauseOpened;
        StaticEvents.PauseClosed -= HandlePauseClosed;

        StopAttackSfx();
    }

    private void Update()
    {
        if (isDead)
        {
            return;
        }

        FindTarget();
        UpdateAnimatorTargetState();

        if (!hasTarget)
        {
            return;
        }

        UpdateMovementDirection();
        RotateToTarget();
        MoveToTarget();
    }

    public void SetDeadState(bool deadState)
    {
        isDead = deadState;

        if (deadState)
        {
            StopAttackSfx();
        }
    }

    public void ApplyHealthBasedScaling(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            return;
        }

        float normalizedHealth = Mathf.Clamp01(currentHealth / maxHealth);

        float healthFactor = 1f - Mathf.InverseLerp(
            enrageHealthThresholdNormalized,
            1f,
            normalizedHealth);

        currentMoveSpeedCoeff = Mathf.Lerp(
            minMoveSpeedCoeff,
            maxMoveSpeedCoeff,
            healthFactor);

        currentRotationSpeedCoeff = Mathf.Lerp(
            minRotationSpeedCoeff,
            maxRotationSpeedCoeff,
            healthFactor);

        currentDamageCoeff = Mathf.Lerp(
            minDamageCoeff,
            maxDamageCoeff,
            healthFactor);

        MoveSpeed = baseMoveSpeed * currentMoveSpeedCoeff;
        RotationSpeed = baseRotationSpeed * currentRotationSpeedCoeff;
        ContactDamage = baseContactDamage * currentDamageCoeff;
    }

    public void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, DetectionRadius, playerLayer);

        if (hits.Length == 0)
        {
            target = null;
            hasTarget = false;
            return;
        }

        float closestSqrDistance = float.MaxValue;
        Transform closestTarget = null;

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerMovement playerMovement = hits[i].GetComponentInParent<PlayerMovement>();

            if (playerMovement == null)
            {
                continue;
            }

            PlayerHealth playerHealth = hits[i].GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
            {
                continue;
            }

            if (!playerHealth.IsTargetable)
            {
                continue;
            }

            Transform candidateTarget = playerMovement.transform;

            if (lineOfSightSensor != null && !lineOfSightSensor.CanSeeTarget(candidateTarget))
            {
                continue;
            }

            float sqrDistance = (candidateTarget.position - transform.position).sqrMagnitude;

            if (sqrDistance < closestSqrDistance)
            {
                closestSqrDistance = sqrDistance;
                closestTarget = candidateTarget;
            }
        }

        target = closestTarget;
        hasTarget = target != null;
    }

    public void UpdateMovementDirection()
    {
        if (target == null)
        {
            moveDirection = Vector3.zero;
            canPush = true;
            return;
        }

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        float sqrDistanceToTarget = directionToTarget.sqrMagnitude;
        float stopDistanceSqr = stopDistance * stopDistance;

        if (sqrDistanceToTarget <= stopDistanceSqr)
        {
            moveDirection = Vector3.zero;
            canPush = true;
            return;
        }

        float pushBlockCheckDistance = stopDistance + pushBlockCheckDistanceOffset;
        float pushBlockCheckDistanceSqr = pushBlockCheckDistance * pushBlockCheckDistance;

        if (blockPushWhenObstacleDetected && sqrDistanceToTarget <= pushBlockCheckDistanceSqr)
        {
            if (IsPushBlockedByObstacle())
            {
                moveDirection = Vector3.zero;
                return;
            }
        }

        canPush = true;
        moveDirection = directionToTarget.normalized;
    }

    public void RotateToTarget()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookDirection = target.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            RotationSpeed * Time.deltaTime);
    }

    public void MoveToTarget()
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        if (movementSensor != null)
        {
            if (!movementSensor.CanMoveInDirection(moveDirection))
            {
                return;
            }
        }

        transform.position += moveDirection * MoveSpeed * Time.deltaTime;
    }

    public void TryDealContactDamage(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (Time.time < lastDamageTime + damageCooldown)
        {
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
        {
            return;
        }

        if (!playerHealth.IsTargetable)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        damageable.ApplyDamage(ContactDamage);
        lastDamageTime = Time.time;
    }

    private void UpdateAnimatorTargetState()
    {
        if (jaggernautAnimator == null)
        {
            return;
        }

        if (previousHasTarget == hasTarget)
        {
            return;
        }

        jaggernautAnimator.SetBool(hasTargetParameterHash, hasTarget);
        previousHasTarget = hasTarget;
    }

    private bool IsPushBlockedByObstacle()
    {
        if (pushCheckPoint == null)
        {
            canPush = true;
            return false;
        }

        Vector3 center = pushCheckPoint.position + pushCheckOffset;
        Vector3 halfExtents = pushBlockedBoxSize * 0.5f;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, pushBlockerMask);

        if (hits.Length > 0)
        {
            canPush = false;
            return true;
        }

        canPush = true;
        return false;
    }

    private void HandlePauseOpened()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.JaggernautAttack,
            WwiseEventsType.Pause,
            gameObject);
    }

    private void HandlePauseClosed()
    {
        if (enemySfxController == null)
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.JaggernautAttack,
            WwiseEventsType.Resume,
            gameObject);
    }

    private void StopAttackSfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.JaggernautAttack,
            WwiseEventsType.Stop,
            gameObject);
    }

    private void OnValidate()
    {
        detectionRadius = Mathf.Max(0f, detectionRadius);

        moveSpeed = Mathf.Max(0f, moveSpeed);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        contactDamage = Mathf.Max(0f, contactDamage);
        damageCooldown = Mathf.Max(0f, damageCooldown);

        enrageHealthThresholdNormalized = Mathf.Clamp(enrageHealthThresholdNormalized, 0.01f, 1f);

        minMoveSpeedCoeff = Mathf.Max(0f, minMoveSpeedCoeff);
        maxMoveSpeedCoeff = Mathf.Max(minMoveSpeedCoeff, maxMoveSpeedCoeff);

        minRotationSpeedCoeff = Mathf.Max(0f, minRotationSpeedCoeff);
        maxRotationSpeedCoeff = Mathf.Max(minRotationSpeedCoeff, maxRotationSpeedCoeff);

        minDamageCoeff = Mathf.Max(0f, minDamageCoeff);
        maxDamageCoeff = Mathf.Max(minDamageCoeff, maxDamageCoeff);
    }

    private void OnDrawGizmosSelected()
    {
        if (drawGizmosDetectionRadius)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRadius);
        }

        if (drawGizmosStopDistance)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
        }

        if (drawGizmosPuchCheck)
        {
            if (pushCheckPoint != null)
            {
                if (canPush)
                {
                    Gizmos.color = Color.cyan;
                }
                else
                {
                    Gizmos.color = Color.blue;
                }

                Vector3 center = pushCheckPoint.position + pushCheckOffset;

                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, pushBlockedBoxSize);
            }
        }
    }

    public void ApplyWaveScaling(WaveScalingData scalingData)
    {
        damageMultiplier = scalingData.DamageMultiplier;
        RebuildRuntimeDamage();
    }

    public void SetWaveIndex(int waveIndex)
    {
        currentWaveIndex = waveIndex;
    }
}