using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WwiseEnemySFXController))]
public class HotshotShooter : MonoBehaviour, IWaveScalable
{
    [Header("Config source")]
    [Tooltip("Hotshot config with shooting settings.")]
    [SerializeField] private HotshotConfig configHotshotSO;

    [Header("Shoot Settings")]
    [Tooltip("Weapon fire rate in shots per minute.")]
    [Range(30f, 3000f)]
    [SerializeField] private float shotsPerMinute = 120f;

    [Tooltip("Minimum dot product required to allow shooting at the target.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float shootFacingThreshold = 0.9f;

    [Tooltip("If false, the shooter is not allowed to fire.")]
    [SerializeField] private bool canShoot = true;

    [Header("Magazine Settings")]
    [Tooltip("How many shots can be fired before reloading.")]
    [Range(1, 500)]
    [SerializeField] private int magazineSize = 6;

    [Tooltip("Current amount of ammo in magazine.")]
    [SerializeField] private int currentAmmo = 6;

    [Tooltip("Is the weapon currently reloading.")]
    [SerializeField] private bool reloading = false;

    [Header("Animation Base Durations")]
    [Tooltip("Original duration of the shoot animation clip in seconds.")]
    [Range(0.01f, 5f)]
    [SerializeField] private float baseShootDuration = 1f;

    [Tooltip("Original duration of the reload animation clip in seconds.")]
    [Range(0.1f, 20f)]
    [SerializeField] private float baseReloadDuration = 5f;

    [Tooltip("Desired reload duration in seconds.")]
    [Range(0.1f, 20f)]
    [SerializeField] private float reloadDuration = 5f;

    [Header("Projectile Settings")]
    [Tooltip("Projectile pool used to get enemy projectiles.")]
    [SerializeField] private HotshotProjectilePool projectilePool;

    [Tooltip("Transform used as projectile spawn point.")]
    [SerializeField] private Transform firePoint;

    [Header("Muzzle VFX")]
    [Tooltip("All mapped muzzle VFX prefabs.")]
    [SerializeField] private List<MuzzleVFXEntry> muzzleVfxEntries = new List<MuzzleVFXEntry>();

    [Tooltip("Current muzzle VFX type used by Hotshot.")]
    [SerializeField] private MuzzleVFXType muzzleVfxType = MuzzleVFXType.HotshotBullet;

    [Tooltip("Offset applied forward from fire point when spawning muzzle VFX.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float muzzleOffset = 0.05f;

    [Header("Wave Scaling")]
    [Tooltip("Runtime fire rate multiplier applied from the current wave.")]
    [SerializeField] private float shotPerMinuteMultiplier = 1f;

    [Header("References")]
    [Tooltip("Reference to the Hotshot controller.")]
    [SerializeField] private HotshotController hotshotController;

    [Tooltip("Enemy Wwise SFX controller reference.")]
    [SerializeField] private WwiseEnemySFXController enemySfxController;

    [Header("Animation Settings")]
    [Tooltip("Animator that controls weapon animations.")]
    [SerializeField] private Animator weaponAnimator;

    [Tooltip("Name of the weapon shot animation state.")]
    [SerializeField] private string shotStateName = "Hotshot_Weapon_Shot";

    [Tooltip("Name of the weapon reload animation state.")]
    [SerializeField] private string reloadStateName = "Hotshot_Weapon_Reload";

    [Tooltip("Name of the trigger parameter used for weapon death animation.")]
    [SerializeField] private string dieTriggerName = "Die";

    [Tooltip("Name of the animator float parameter for shot animation speed.")]
    [SerializeField] private string coeffShotParameterName = "Coeff_Shoot";

    [Tooltip("Name of the animator float parameter for reload animation speed.")]
    [SerializeField] private string coeffReloadParameterName = "Coeff_Reload";

    [Tooltip("Should the shooter play shot animation when firing.")]
    [SerializeField] private bool playShotAnimation = true;

    [Header("Audio")]
    [Tooltip("GameObject used as Wwise emitter source.")]
    [SerializeField] private GameObject sfxSource;

    [Tooltip("RTPC name used for Hotshot attack pitch.")]
    [SerializeField] private string hotshotAttackPitchRtpcName = "HotshotAttackPitch";

    [Tooltip("Minimum RTPC value.")]
    [Range(0f, 100f)]
    [SerializeField] private float minPitchRtpcValue = 0f;

    [Tooltip("Maximum RTPC value.")]
    [Range(0f, 100f)]
    [SerializeField] private float maxPitchRtpcValue = 100f;

    [Header("Audio Runtime")]
    [Tooltip("Whether reload SFX is currently active.")]
    [SerializeField] private bool isReloadSfxPlaying;

    [Header("Debug")]
    [Tooltip("Enable manual test shot from inspector.")]
    [SerializeField] private bool testShot = false;

    [Tooltip("Target used for manual test shot.")]
    [SerializeField] private Transform testTarget;

    private float nextFireTime;
    private float baseShotsPerMinute;
    private Dictionary<MuzzleVFXType, GameObject> muzzleVfxMap;

    public float ShotsPerMinute
    {
        get
        {
            return shotsPerMinute;
        }
        set
        {
            shotsPerMinute = Mathf.Max(30f, value);
        }
    }

    public float ShootFacingThreshold
    {
        get
        {
            return shootFacingThreshold;
        }
        set
        {
            shootFacingThreshold = Mathf.Clamp(value, 0.5f, 1f);
        }
    }

    public bool CanShootFlag
    {
        get
        {
            return canShoot;
        }
        set
        {
            canShoot = value;
        }
    }

    public int MagazineSize
    {
        get
        {
            return magazineSize;
        }
        set
        {
            magazineSize = Mathf.Max(1, value);
        }
    }

    public int CurrentAmmo
    {
        get
        {
            return currentAmmo;
        }
        private set
        {
            currentAmmo = Mathf.Clamp(value, 0, MagazineSize);
        }
    }

    public bool IsReloading
    {
        get
        {
            return reloading;
        }
    }

    public Transform FirePoint
    {
        get
        {
            return firePoint;
        }
        set
        {
            firePoint = value;
        }
    }

    public HotshotProjectilePool ProjectilePool
    {
        get
        {
            return projectilePool;
        }
        set
        {
            projectilePool = value;
        }
    }

    private void ApplyConfig()
    {
        if (configHotshotSO == null)
        {
            return;
        }

        ShotsPerMinute = configHotshotSO.shotsPerMinute;
        ShootFacingThreshold = configHotshotSO.shootFacingThreshold;
        MagazineSize = configHotshotSO.magazineSize;

        baseShootDuration = Mathf.Max(0.01f, configHotshotSO.baseShootDuration);
        baseReloadDuration = Mathf.Max(0.1f, configHotshotSO.baseReloadDuration);
        reloadDuration = Mathf.Max(0.1f, configHotshotSO.reloadDuration);
    }

    private void Reset()
    {
        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponent<Animator>();
        }

        if (hotshotController == null)
        {
            hotshotController = GetComponent<HotshotController>();
        }

        if (enemySfxController == null)
        {
            enemySfxController = GetComponent<WwiseEnemySFXController>();
        }
    }

    private void Awake()
    {
        if (ProjectilePool == null)
        {
            ProjectilePool = FindAnyObjectByType<HotshotProjectilePool>();
        }

        if (weaponAnimator == null)
        {
            weaponAnimator = GetComponent<Animator>();
        }

        if (hotshotController == null)
        {
            hotshotController = GetComponent<HotshotController>();
        }

        if (enemySfxController == null)
        {
            enemySfxController = GetComponent<WwiseEnemySFXController>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject;
        }

        ApplyConfig();

        baseShotsPerMinute = shotsPerMinute;
        RebuildRuntimeFireRate();

        CurrentAmmo = MagazineSize;
        reloading = false;
        isReloadSfxPlaying = false;

        UpdateAnimationCoefficients();
        UpdateAttackPitchRtpc();
        BuildMuzzleVfxMap();
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

        StopShotSfx();
        StopReloadSfx();
    }

    private void Update()
    {
        if (!canShoot)
        {
            return;
        }

        UpdateAnimationCoefficients();
    }

    public void SetDeadState(bool value)
    {
        canShoot = !value;

        if (!value)
        {
            return;
        }

        reloading = false;
        StopAllCoroutines();

        StopShotSfx();
        StopReloadSfx();
    }

    public bool TryShoot(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (!CanShoot())
        {
            return false;
        }

        if (CurrentAmmo <= 0)
        {
            StartReload();
            return false;
        }

        if (projectilePool == null)
        {
            Debug.LogWarning("HotshotShooter: Projectile Pool is not assigned.", this);
            return false;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("HotshotShooter: Fire Point is not assigned.", this);
            return false;
        }

        if (!IsFacingTarget(target))
        {
            return false;
        }

        if (!projectilePool.GetProjectile(out GameObject projectile))
        {
            return false;
        }

        projectile.transform.position = firePoint.position;
        projectile.transform.rotation = firePoint.rotation;
        projectile.SetActive(true);

        ResetProjectile(projectile);
        SpawnMuzzleVfx();

        CurrentAmmo--;

        if (playShotAnimation)
        {
            PlayShotAnimation();
        }

        PostShotSfx();

        nextFireTime = Time.time + GetFireCooldown();

        if (CurrentAmmo <= 0)
        {
            StartReload();
        }

        return true;
    }

    public void StartReload()
    {
        if (!canShoot)
        {
            return;
        }

        if (reloading)
        {
            return;
        }

        StartCoroutine(ReloadRoutine());
    }

    public void PlayReload()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(reloadStateName))
        {
            weaponAnimator.Play(reloadStateName, 0, 0f);
        }
    }

    public void PlayDie()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(dieTriggerName))
        {
            return;
        }

        weaponAnimator.SetTrigger(dieTriggerName);
    }

    public bool CanShoot()
    {
        if (!canShoot)
        {
            return false;
        }

        if (reloading)
        {
            return false;
        }

        if (Time.time < nextFireTime)
        {
            return false;
        }

        return true;
    }

    public void SetCanShoot(bool value)
    {
        canShoot = value;
    }

    private void PlayShotAnimation()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(shotStateName))
        {
            weaponAnimator.Play(shotStateName, 0, 0f);
        }
    }

    private void UpdateAnimationCoefficients()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        float fireCooldown = GetFireCooldown();
        float shootCoeff = 1f;

        if (fireCooldown > 0.001f)
        {
            shootCoeff = baseShootDuration / fireCooldown;
        }

        float reloadCoeff = 1f;

        if (reloadDuration > 0.001f)
        {
            reloadCoeff = baseReloadDuration / reloadDuration;
        }

        if (!string.IsNullOrWhiteSpace(coeffShotParameterName))
        {
            weaponAnimator.SetFloat(coeffShotParameterName, shootCoeff);
        }

        if (!string.IsNullOrWhiteSpace(coeffReloadParameterName))
        {
            weaponAnimator.SetFloat(coeffReloadParameterName, reloadCoeff);
        }
    }

    private float GetFireCooldown()
    {
        if (shotsPerMinute <= 0.001f)
        {
            return 999f;
        }

        return 60f / shotsPerMinute;
    }

    private bool IsFacingTarget(Transform target)
    {
        if (firePoint == null)
        {
            return false;
        }

        Vector3 directionToTarget = target.position - firePoint.position;
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 fireForward = firePoint.forward;
        fireForward.y = 0f;

        if (fireForward.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        float dot = Vector3.Dot(fireForward.normalized, directionToTarget.normalized);

        if (dot >= ShootFacingThreshold)
        {
            return true;
        }

        return false;
    }

    private void ResetProjectile(GameObject projectile)
    {
        IEnemyProjectile enemyProjectile = projectile.GetComponent<IEnemyProjectile>();

        if (enemyProjectile != null)
        {
            HotshotProjectile hotshotProjectile = projectile.GetComponent<HotshotProjectile>();

            if (hotshotProjectile != null && hotshotController != null)
            {
                hotshotProjectile.SetDamageMultiplier(hotshotController.GetProjectileDamageMultiplier());
            }

            enemyProjectile.Launch(firePoint.forward);
            return;
        }

        Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();

        if (projectileRigidbody != null)
        {
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void BuildMuzzleVfxMap()
    {
        muzzleVfxMap = new Dictionary<MuzzleVFXType, GameObject>();

        for (int i = 0; i < muzzleVfxEntries.Count; i++)
        {
            MuzzleVFXEntry entry = muzzleVfxEntries[i];

            if (entry.muzzleVfxPrefab == null)
            {
                Debug.LogWarning("HotshotShooter: Missing muzzle VFX prefab for " + entry.muzzleVfxType, this);
                continue;
            }

            if (!muzzleVfxMap.ContainsKey(entry.muzzleVfxType))
            {
                muzzleVfxMap.Add(entry.muzzleVfxType, entry.muzzleVfxPrefab);
            }
            else
            {
                Debug.LogWarning("HotshotShooter: Duplicate muzzle VFX mapping for " + entry.muzzleVfxType, this);
            }
        }
    }

    private void SpawnMuzzleVfx()
    {
        if (muzzleVfxMap == null || muzzleVfxMap.Count == 0)
        {
            return;
        }

        if (firePoint == null)
        {
            return;
        }

        GameObject muzzlePrefab;

        if (!muzzleVfxMap.TryGetValue(muzzleVfxType, out muzzlePrefab))
        {
            Debug.LogWarning("HotshotShooter: No muzzle VFX mapped for " + muzzleVfxType, this);
            return;
        }

        if (muzzlePrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = firePoint.position + firePoint.forward * muzzleOffset;
        Quaternion spawnRotation = firePoint.rotation;

        Instantiate(muzzlePrefab, spawnPosition, spawnRotation);
    }

    public void SetMuzzleVfxType(MuzzleVFXType newMuzzleVfxType)
    {
        muzzleVfxType = newMuzzleVfxType;
    }

    private IEnumerator ReloadRoutine()
    {
        reloading = true;

        PostReloadPlaySfx();
        PlayReload();

        yield return new WaitForSeconds(reloadDuration);

        CurrentAmmo = MagazineSize;
        reloading = false;

        StopReloadSfx();
    }

    private IEnumerator TestShoot()
    {
        if (!testShot)
        {
            yield break;
        }

        testShot = false;
        TryShoot(testTarget);
    }

    public void ApplyWaveScaling(WaveScalingData scalingData)
    {
        shotPerMinuteMultiplier = Mathf.Max(0.01f, scalingData.FireRateMultiplier);
        RebuildRuntimeFireRate();
    }

    private void RebuildRuntimeFireRate()
    {
        ShotsPerMinute = baseShotsPerMinute * shotPerMinuteMultiplier;
        UpdateAttackPitchRtpc();
    }

    private void UpdateAttackPitchRtpc()
    {
        if (enemySfxController == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(hotshotAttackPitchRtpcName))
        {
            return;
        }

        float normalized = 0f;

        if (baseShotsPerMinute > 0.001f)
        {
            normalized = Mathf.Clamp01((shotsPerMinute / baseShotsPerMinute) - 1f);
        }

        float rtpcValue = Mathf.Lerp(minPitchRtpcValue, maxPitchRtpcValue, normalized);

        AkUnitySoundEngine.SetRTPCValue(
            hotshotAttackPitchRtpcName,
            rtpcValue,
            sfxSource);
    }

    private void PostShotSfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        UpdateAttackPitchRtpc();

        enemySfxController.Post(
            EnemySfxType.HotshotShot,
            WwiseEventsType.Play,
            sfxSource);
    }

    private void StopShotSfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.HotshotShot,
            WwiseEventsType.Stop,
            sfxSource);
    }

    private void PostReloadPlaySfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        UpdateAttackPitchRtpc();

        enemySfxController.Post(
            EnemySfxType.HotshotReload,
            WwiseEventsType.Play,
            sfxSource);

        isReloadSfxPlaying = true;
    }

    private void StopReloadSfx()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.HotshotReload,
            WwiseEventsType.Stop,
            sfxSource);

        isReloadSfxPlaying = false;
    }

    private void HandlePauseOpened()
    {
        if (enemySfxController == null)
        {
            return;
        }

        if (isReloadSfxPlaying)
        {
            enemySfxController.Post(
                EnemySfxType.HotshotReload,
                WwiseEventsType.Pause,
                sfxSource);
        }

        enemySfxController.Post(
            EnemySfxType.HotshotShot,
            WwiseEventsType.Pause,
            sfxSource);
    }

    private void HandlePauseClosed()
    {
        if (enemySfxController == null)
        {
            return;
        }

        if (!canShoot)
        {
            return;
        }

        if (isReloadSfxPlaying)
        {
            enemySfxController.Post(
                EnemySfxType.HotshotReload,
                WwiseEventsType.Resume,
                sfxSource);
        }

        enemySfxController.Post(
            EnemySfxType.HotshotShot,
            WwiseEventsType.Resume,
            sfxSource);
    }
}