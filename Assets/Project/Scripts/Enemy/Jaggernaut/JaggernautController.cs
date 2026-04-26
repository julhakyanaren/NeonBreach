using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WwiseEnemySFXController))]
[RequireComponent(typeof(PhotonView))]
public class JaggernautController : MonoBehaviour, IEnemyController, IWaveScalable, IPunInstantiateMagicCallback
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

    [Tooltip("Enemy Rigidbody used for physics-based movement.")]
    [SerializeField] private Rigidbody enemyRigidbody;

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

    private PhotonView photonView;

    private bool previousHasTarget;
    private float lastDamageTime = -999f;
    private int hasTargetParameterHash;
    private bool canPush = true;

    private float baseMoveSpeed;
    private float baseRotationSpeed;
    private float baseContactDamage;
    private float lockedYPosition;

    private bool isDead = false;

    public int CurrentWaveIndex
    {
        get { return currentWaveIndex; }
    }

    public float DetectionRadius
    {
        get { return detectionRadius; }
        set { detectionRadius = Mathf.Max(0f, value); }
    }

    public float MoveSpeed
    {
        get { return moveSpeed; }
        set { moveSpeed = Mathf.Max(0f, value); }
    }

    public float RotationSpeed
    {
        get { return rotationSpeed; }
        set { rotationSpeed = Mathf.Max(0f, value); }
    }

    public float ContactDamage
    {
        get { return contactDamage; }
        set { contactDamage = Mathf.Max(0f, value); }
    }

    private void Reset()
    {
        jaggernautAnimator = GetComponent<Animator>();
        enemySfxController = GetComponent<WwiseEnemySFXController>();
        enemyRigidbody = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        lineOfSightSensor = GetComponentInChildren<EnemyLineOfSightSensor>();
        movementSensor = GetComponentInChildren<EnemyMovementSensor>();
    }

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (jaggernautAnimator == null)
        {
            jaggernautAnimator = GetComponent<Animator>();
        }

        if (enemySfxController == null)
        {
            enemySfxController = GetComponent<WwiseEnemySFXController>();
        }

        if (enemyRigidbody == null)
        {
            enemyRigidbody = GetComponent<Rigidbody>();
        }

        if (lineOfSightSensor == null)
        {
            lineOfSightSensor = GetComponentInChildren<EnemyLineOfSightSensor>();
        }

        if (movementSensor == null)
        {
            movementSensor = GetComponentInChildren<EnemyMovementSensor>();
        }

        if (pushCheckPoint == null)
        {
            pushCheckPoint = transform;
        }

        if (enemyRigidbody != null)
        {
            lockedYPosition = enemyRigidbody.position.y;
        }

        ConfigureRigidbody();
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

        if (enemyRigidbody != null)
        {
            lockedYPosition = enemyRigidbody.position.y;
        }

        RefreshRigidbodyAuthorityState();
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

        if (!CanRunAuthorityLogic())
        {
            return;
        }

        FindTarget();
        UpdateAnimatorTargetState();

        if (!hasTarget)
        {
            moveDirection = Vector3.zero;
            return;
        }

        UpdateMovementDirection();
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }

        if (!CanRunAuthorityLogic())
        {
            return;
        }

        LockRigidbodyYPosition();

        if (!hasTarget)
        {
            return;
        }

        RotateToTarget();
        MoveToTarget();
    }

    private void ConfigureRigidbody()
    {
        if (enemyRigidbody == null)
        {
            return;
        }

        enemyRigidbody.useGravity = false;
        enemyRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        enemyRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        enemyRigidbody.constraints =
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;
    }

    private void LockRigidbodyYPosition()
    {
        if (enemyRigidbody == null)
        {
            return;
        }

        Vector3 lockedPosition = enemyRigidbody.position;
        lockedPosition.y = lockedYPosition;
        enemyRigidbody.position = lockedPosition;

        Vector3 velocity = enemyRigidbody.velocity;
        velocity.y = 0f;
        enemyRigidbody.velocity = velocity;
    }

    private void RefreshRigidbodyAuthorityState()
    {
        if (enemyRigidbody == null)
        {
            return;
        }

        if (!RuntimeOptions.MultiplayerMode)
        {
            enemyRigidbody.isKinematic = false;
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            enemyRigidbody.isKinematic = true;
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            enemyRigidbody.isKinematic = false;
            return;
        }

        enemyRigidbody.isKinematic = true;
    }

    private bool CanRunAuthorityLogic()
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return true;
        }

        if (!PhotonNetwork.InRoom)
        {
            return false;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            return false;
        }

        return true;
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
        float baseDamage = baseContactDamage;

        if (configJaggernautSO != null)
        {
            baseDamage = configJaggernautSO.contactDamage;
        }

        ContactDamage = baseDamage * damageMultiplier * currentDamageCoeff;
    }

    public void SetDeadState(bool deadState)
    {
        isDead = deadState;

        if (deadState)
        {
            moveDirection = Vector3.zero;
            StopAttackSfx();

            if (enemyRigidbody != null)
            {
                enemyRigidbody.velocity = Vector3.zero;
                enemyRigidbody.angularVelocity = Vector3.zero;
            }
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
        ContactDamage = baseContactDamage * currentDamageCoeff * damageMultiplier;
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

        if (enemyRigidbody == null)
        {
            return;
        }

        Vector3 lookDirection = target.position - enemyRigidbody.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);

        Quaternion nextRotation = Quaternion.Slerp(
            enemyRigidbody.rotation,
            targetRotation,
            RotationSpeed * Time.fixedDeltaTime);

        enemyRigidbody.MoveRotation(nextRotation);
    }

    public void MoveToTarget()
    {
        if (enemyRigidbody == null)
        {
            return;
        }

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

        Vector3 nextPosition = enemyRigidbody.position + moveDirection * MoveSpeed * Time.fixedDeltaTime;
        nextPosition.y = lockedYPosition;
        enemyRigidbody.MovePosition(nextPosition);
    }

    public void TryDealContactDamage(Collider other)
    {
        if (!CanRunAuthorityLogic())
        {
            return;
        }

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

        if (RuntimeOptions.MultiplayerMode)
        {
            PhotonView targetPhotonView = playerHealth.GetComponentInParent<PhotonView>();

            if (targetPhotonView == null)
            {
                return;
            }

            photonView.RPC(
                nameof(RPC_ApplyContactDamage),
                RpcTarget.All,
                targetPhotonView.ViewID,
                ContactDamage);

            lastDamageTime = Time.time;
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

        previousHasTarget = hasTarget;

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView == null)
            {
                return;
            }

            photonView.RPC(nameof(RPC_SetHasTarget), RpcTarget.All, hasTarget);
            return;
        }

        jaggernautAnimator.SetBool(hasTargetParameterHash, hasTarget);
    }

    [PunRPC]
    private void RPC_SetHasTarget(bool newHasTarget)
    {
        hasTarget = newHasTarget;

        if (jaggernautAnimator == null)
        {
            return;
        }

        jaggernautAnimator.SetBool(hasTargetParameterHash, newHasTarget);
    }

    [PunRPC]
    private void RPC_ApplyContactDamage(int targetViewId, float damage)
    {
        PhotonView targetPhotonView = PhotonView.Find(targetViewId);

        if (targetPhotonView == null)
        {
            return;
        }

        if (!targetPhotonView.IsMine)
        {
            return;
        }

        IDamageable damageable = targetPhotonView.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable = targetPhotonView.GetComponentInChildren<IDamageable>();
        }

        if (damageable == null)
        {
            damageable = targetPhotonView.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
        {
            return;
        }

        damageable.ApplyDamage(damage);
    }

    private bool IsPushBlockedByObstacle()
    {
        if (pushCheckPoint == null)
        {
            canPush = true;
            return false;
        }

        Vector3 center = pushCheckPoint.position + transform.rotation * pushCheckOffset;
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

                Vector3 center = pushCheckPoint.position + transform.rotation * pushCheckOffset;

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

        if (PhotonNetwork.InRoom == true)
        {
            if (PhotonNetwork.IsMasterClient == true)
            {
                if (photonView != null)
                {
                    photonView.RPC(nameof(RPC_SetWaveIndex), RpcTarget.OthersBuffered, waveIndex);
                }
            }
        }
    }

    [PunRPC]
    private void RPC_SetWaveIndex(int waveIndex)
    {
        currentWaveIndex = waveIndex;
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (!RuntimeOptions.MultiplayerMode)
        {
            return;
        }

        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (photonView == null)
        {
            return;
        }

        object[] data = photonView.InstantiationData;

        if (data == null)
        {
            return;
        }

        if (data.Length < 1)
        {
            return;
        }

        if (data[0] is int)
        {
            currentWaveIndex = (int)data[0];
        }

        RefreshRigidbodyAuthorityState();
    }
}