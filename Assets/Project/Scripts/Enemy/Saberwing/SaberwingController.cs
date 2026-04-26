using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyLineOfSightSensor))]
[RequireComponent(typeof(WwiseEnemySFXController))]
public class SaberwingController : MonoBehaviour, IEnemyController, IWaveScalable, IPunInstantiateMagicCallback
{
    [Header("Config Source")]
    [Tooltip("Saberwing config with controller settings.")]
    [SerializeField] private SaberwingConfig configSaberwingSO;

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

    [Tooltip("Distance at which the enemy stops moving closer.")]
    [SerializeField] private float stopDistance = 1.2f;

    [Header("Attack")]
    [Tooltip("Distance at which the enemy can start melee attack.")]
    [SerializeField] private float attackRange = 1.6f;

    [Tooltip("Delay between attacks.")]
    [SerializeField] private float attackCooldown = 0.45f;

    [Tooltip("Damage dealt by melee attack.")]
    [SerializeField] private float attackDamage = 25f;

    [Tooltip("Radius used to detect player colliders during melee hit.")]
    [SerializeField] private float attackHitRadius = 1.8f;

    [Tooltip("Animator trigger name for melee attack.")]
    [SerializeField] private string attackTriggerName = "AttackPrick";

    [Tooltip("The name of the animation speed multiplier parameter for melee attacks.")]
    [SerializeField] private string attackSpeedCoeffName = "AttackSpeedCoeff";

    [Tooltip("Animation speed multiplier for melee attacks.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float attackSpeedCoeff = 1f;

    [Tooltip("Base duration during which Saberwing stays in attack state.")]
    [Range(0.05f, 5f)]
    [SerializeField] private float attackLockBaseDuration = 0.35f;

    [Header("Wave Scaling")]
    [Tooltip("Runtime damage multiplier applied from the current wave.")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("References")]
    [Tooltip("Reference of the EnemyLineOfSightSensor script.")]
    [SerializeField] private EnemyLineOfSightSensor lineOfSightSensor;

    [Tooltip("Cached Rigidbody reference.")]
    [SerializeField] private Rigidbody enemyRigidbody;

    [Tooltip("Animator used for enemy animations.")]
    [SerializeField] private Animator enemyAnimator;

    [Tooltip("Enemy Wwise SFX controller reference.")]
    [SerializeField] private WwiseEnemySFXController enemySfxController;

    [Tooltip("PhotonView reference for multiplayer authority checks.")]
    [SerializeField] private PhotonView photonView;

    [Header("Sensors")]
    [Tooltip("EnemyMovementSensor script reference.")]
    [SerializeField] private EnemyMovementSensor movementSensor;

    [Header("Runtime")]
    [Tooltip("Wave index this enemy was spawned on.")]
    [SerializeField] private int currentWaveIndex;

    [Tooltip("Current target transform.")]
    [SerializeField] private Transform target;

    [Tooltip("Current movement direction.")]
    [SerializeField] private Vector3 moveDirection;

    [Tooltip("Whether the enemy currently has a valid target.")]
    [SerializeField] private bool hasTarget;

    [Tooltip("Whether the enemy is currently in attack state.")]
    [SerializeField] private bool isAttacking;

    [Header("Audio Runtime")]
    [Tooltip("Whether Saberwing attack SFX is currently active.")]
    [SerializeField] private bool isAttackSfxPlaying;

    [Header("Debug")]
    [Tooltip("Draw detection radius gizmos.")]
    [SerializeField] private bool drawGizmosDetectionRadius = true;

    [Tooltip("Draw stop distance gizmos.")]
    [SerializeField] private bool drawGizmosStopDistance = true;

    [Tooltip("Draw attack range gizmos.")]
    [SerializeField] private bool drawGizmosAttackRange = true;

    private float attackTimer;
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

    private void Reset()
    {
        lineOfSightSensor = GetComponentInChildren<EnemyLineOfSightSensor>();
        enemyAnimator = GetComponent<Animator>();
        enemyRigidbody = GetComponent<Rigidbody>();
        enemySfxController = GetComponent<WwiseEnemySFXController>();
        photonView = GetComponent<PhotonView>();
    }

    private void Awake()
    {
        if (lineOfSightSensor == null)
        {
            lineOfSightSensor = GetComponentInChildren<EnemyLineOfSightSensor>();
        }

        if (enemyRigidbody == null)
        {
            enemyRigidbody = GetComponent<Rigidbody>();
        }

        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }

        if (enemySfxController == null)
        {
            enemySfxController = GetComponent<WwiseEnemySFXController>();
        }

        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        ConfigureRigidbody();
        ApplyConfig();
        RebuildRuntimeDamage();
    }

    private void OnEnable()
    {
        StaticEvents.PauseOpened += HandlePauseOpened;
        StaticEvents.PauseClosed += HandlePauseClosed;

        RefreshRigidbodyAuthorityState();
    }

    private void OnDisable()
    {
        StaticEvents.PauseOpened -= HandlePauseOpened;
        StaticEvents.PauseClosed -= HandlePauseClosed;

        StopAllCoroutines();
        isAttacking = false;
        StopAttackSfx();
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
        enemyRigidbody.constraints = RigidbodyConstraints.FreezeRotationX |
                                     RigidbodyConstraints.FreezeRotationY |
                                     RigidbodyConstraints.FreezeRotationZ;
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
        if (configSaberwingSO == null)
        {
            return;
        }

        DetectionRadius = configSaberwingSO.detectionRadius;
        MoveSpeed = configSaberwingSO.moveSpeed;
        RotationSpeed = configSaberwingSO.rotationSpeed;
        stopDistance = configSaberwingSO.stopDistance;

        attackRange = configSaberwingSO.attackRange;
        attackCooldown = configSaberwingSO.attackCooldown;
        attackDamage = configSaberwingSO.attackDamage;
        attackHitRadius = configSaberwingSO.attackHitRadius;
        attackSpeedCoeff = configSaberwingSO.attackSpeedCoeff;
    }

    private void RebuildRuntimeDamage()
    {
        float baseDamage = 25f;

        if (configSaberwingSO != null)
        {
            baseDamage = configSaberwingSO.attackDamage;
        }

        attackDamage = baseDamage * damageMultiplier;
    }

    public void ApplyWaveScaling(WaveScalingData scalingData)
    {
        damageMultiplier = scalingData.DamageMultiplier;
        RebuildRuntimeDamage();
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

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if (!hasTarget || target == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (isAttacking)
        {
            moveDirection = Vector3.zero;
            return;
        }

        UpdateMovementDirection();
        TryAttack();
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

        if (!hasTarget || target == null)
        {
            return;
        }

        RotateToTarget();

        if (isAttacking)
        {
            return;
        }

        MoveToTarget();
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

            if (lineOfSightSensor != null)
            {
                if (!lineOfSightSensor.CanSeeTarget(candidateTarget))
                {
                    continue;
                }
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
            return;
        }

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        float sqrDistanceToTarget = directionToTarget.sqrMagnitude;
        float attackRangeSqr = attackRange * attackRange;
        float stopDistanceSqr = stopDistance * stopDistance;

        if (sqrDistanceToTarget <= attackRangeSqr)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (sqrDistanceToTarget <= stopDistanceSqr)
        {
            moveDirection = Vector3.zero;
            return;
        }

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

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

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
        enemyRigidbody.MovePosition(nextPosition);
    }

    [PunRPC]
    private void Attack_RPC()
    {
        if (enemyAnimator != null)
        {
            enemyAnimator.SetFloat(attackSpeedCoeffName, attackSpeedCoeff);
            enemyAnimator.SetTrigger(attackTriggerName);
        }
    }

    private void TryAttack()
    {
        if (target == null)
        {
            return;
        }

        if (attackTimer > 0f)
        {
            return;
        }

        if (isAttacking)
        {
            return;
        }

        if (!IsTargetInAttackRange())
        {
            return;
        }

        if (enemyAnimator != null)
        {
            if (RuntimeOptions.MultiplayerMode)
            {
                if (photonView != null && PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC(nameof(Attack_RPC), RpcTarget.All);
                }
            }
            else
            {
                enemyAnimator.SetFloat(attackSpeedCoeffName, attackSpeedCoeff);
                enemyAnimator.SetTrigger(attackTriggerName);
            }
        }

        ResetAttackCooldown();
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        moveDirection = Vector3.zero;

        float attackLockDuration = GetAdjustedAttackDuration(attackLockBaseDuration);
        yield return new WaitForSeconds(attackLockDuration);

        isAttacking = false;
    }

    private float GetAdjustedAttackDuration(float baseDuration)
    {
        if (attackSpeedCoeff <= 0f)
        {
            return baseDuration;
        }

        return baseDuration / attackSpeedCoeff;
    }

    public bool IsTargetInAttackRange()
    {
        if (target == null)
        {
            return false;
        }

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        return directionToTarget.sqrMagnitude <= attackRange * attackRange;
    }

    public void PerformAttackHit()
    {
        if (!CanRunAuthorityLogic())
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, attackHitRadius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();

            if (damageable == null)
            {
                continue;
            }

            if (RuntimeOptions.MultiplayerMode)
            {
                TryApplyDamageInMultiplayer(hits[i], attackDamage);
            }
            else
            {
                damageable.ApplyDamage(attackDamage);
            }

            break;
        }
    }

    private void TryApplyDamageInMultiplayer(Collider hit, float damage)
    {
        PhotonView targetPhotonView = hit.GetComponentInParent<PhotonView>();

        if (targetPhotonView == null)
        {
            return;
        }

        if (photonView == null)
        {
            return;
        }

        photonView.RPC(nameof(ApplyDamageToTarget_RPC), RpcTarget.All, targetPhotonView.ViewID, damage);
    }

    [PunRPC]
    private void ApplyDamageToTarget_RPC(int targetViewId, float damage)
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

        IDamageable damageable = targetPhotonView.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        damageable.ApplyDamage(damage);
    }

    public void ResetAttackCooldown()
    {
        attackTimer = attackCooldown;
    }

    public void DisableCollision()
    {
        if (enemyRigidbody == null)
        {
            return;
        }

        enemyRigidbody.isKinematic = true;
    }

    public void EnableCollision()
    {
        if (enemyRigidbody == null)
        {
            return;
        }

        RefreshRigidbodyAuthorityState();
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

    public void PlayAttackSfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.SaberwingAttack,
            WwiseEventsType.Play,
            gameObject);

        isAttackSfxPlaying = true;
    }

    public void StopAttackSfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.SaberwingAttack,
            WwiseEventsType.Stop,
            gameObject);

        isAttackSfxPlaying = false;
    }

    private void HandlePauseOpened()
    {
        if (enemySfxController == null)
        {
            return;
        }

        if (!isAttackSfxPlaying)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.SaberwingAttack,
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

        if (!isAttackSfxPlaying)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.SaberwingAttack,
            WwiseEventsType.Resume,
            gameObject);
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

        if (drawGizmosAttackRange)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}