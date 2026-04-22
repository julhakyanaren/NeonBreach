using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Player config with base stats.")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Limits")]
    [Tooltip("Minimum allowed reload duration to keep animation and audio stable.")]
    [SerializeField] private float minReloadDuration = 5f;

    [Header("Shoot Settings")]
    [Tooltip("Weapon fire rate in shots per minute.")]
    [Range(30f, 3000f)]
    [SerializeField] private float shotsPerMinute = 300f;

    [Tooltip("Reload duration in seconds.")]
    [Range(5f, 15f)]
    [SerializeField] private float reloadDuration = 5f;

    [Tooltip("Current reload state.")]
    [SerializeField] private bool reloading = false;

    [Tooltip("Magazine size.")]
    [Range(1, 500)]
    [SerializeField] private int magazineSize = 30;

    [Header("Animation Base Durations")]
    [Tooltip("Original duration of the shoot animation clip in seconds.")]
    [Range(0.01f, 5f)]
    [SerializeField] private float baseShootDuration = 0.1f;

    [Tooltip("Original duration of the reload animation clip in seconds.")]
    [Range(0.1f, 20f)]
    [SerializeField] private float baseReloadDuration = 5f;

    [Header("Weapon Animator State Names")]
    [Tooltip("Exact state name of the weapon shoot animation.")]
    [SerializeField] private string shootStateName = "PlayerWeapon_Shoot";

    [Tooltip("Exact state name of the weapon reload animation.")]
    [SerializeField] private string reloadStateName = "PlayerWeapon_Reload";

    [Header("Input")]
    [Tooltip("Reference to the Fire action from the Input System.")]
    [SerializeField] private InputActionReference fireAction;

    [Tooltip("Reference to the Reload action from the Input System.")]
    [SerializeField] private InputActionReference reloadAction;

    [Header("Spawn Settings")]
    [Tooltip("Point where projectiles are spawned.")]
    [SerializeField] private Transform firePoint;

    [Header("Muzzle VFX")]
    [Tooltip("All mapped muzzle VFX prefabs.")]
    [SerializeField] private List<MuzzleVFXEntry> muzzleVfxEntries = new List<MuzzleVFXEntry>();

    [Tooltip("Current muzzle VFX type used by the player.")]
    [SerializeField] private MuzzleVFXType muzzleVfxType = MuzzleVFXType.CyanEnergy;

    [Tooltip("Offset applied forward from fire point when spawning muzzle VFX.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float muzzleOffset = 0.05f;

    [Header("References")]
    [Tooltip("Reference to the projectile pool.")]
    [SerializeField] private PlayerProjectilePool projectilePool;

    [Tooltip("Reference to the player rotation component.")]
    [SerializeField] private PlayerRotation playerRotation;

    [Tooltip("Animator on the weapon visual.")]
    [SerializeField] private Animator weaponAnimator;

    [Tooltip("Reference to the Wwise player SFX controller.")]
    [SerializeField] private WwisePlayerSFXController playerSfxController;

    [Tooltip("Local input blocker for this player.")]
    [SerializeField] private PlayerInputBlocker inputBlocker;

    [Header("Debug / External State")]
    [Tooltip("Set true when player is dead.")]
    [SerializeField] private bool isDead = false;

    private float nextFireTime;
    private int currentAmmo;

    private float baseShotsPerMinute;
    private float fireRateMultiplier = 1f;
    private float reloadMultiplier = 1f;

    private Coroutine reloadCoroutine;
    private Dictionary<MuzzleVFXType, GameObject> muzzleVfxMap;

    private static readonly int CoeffShootHash = Animator.StringToHash("Coeff_Shoot");
    private static readonly int CoeffReloadHash = Animator.StringToHash("Coeff_Reload");

    public event Action<int, int> AmmoChanged;

    public int CurrentAmmo
    {
        get
        {
            return currentAmmo;
        }
        private set
        {
            if (value <= 0)
            {
                currentAmmo = 0;
            }
            else
            {
                currentAmmo = value;
            }
        }
    }

    public int MagazineSize
    {
        get
        {
            return magazineSize;
        }
        private set
        {
            if (value <= 0)
            {
                magazineSize = 30;
            }
            else
            {
                magazineSize = value;
            }
        }
    }

    private void Reset()
    {
        playerRotation = GetComponent<PlayerRotation>();
        inputBlocker = GetComponent<PlayerInputBlocker>();
    }

    private void Awake()
    {
        if (playerRotation == null)
        {
            playerRotation = GetComponent<PlayerRotation>();
        }

        if (inputBlocker == null)
        {
            inputBlocker = GetComponent<PlayerInputBlocker>();
        }

        ApplyConfig();
        baseShotsPerMinute = shotsPerMinute;
        BuildMuzzleVfxMap();
    }

    private void Start()
    {
        CurrentAmmo = MagazineSize;
        UpdateAnimationCoefficients();
        NotifyAmmoChanged();
    }

    private void OnEnable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Enable();
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.Enable();
            reloadAction.action.performed += OnReloadPerformed;
        }

        StaticEvents.PauseOpened += OnPauseOpened;
        StaticEvents.PauseClosed += OnPauseClosed;
    }

    private void OnDisable()
    {
        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Disable();
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.performed -= OnReloadPerformed;
            reloadAction.action.Disable();
        }

        StopReloadRoutineInternal();
        reloading = false;

        if (playerSfxController != null)
        {
            playerSfxController.PostReloadStop();
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetFloat(CoeffShootHash, 1f);
            weaponAnimator.SetFloat(CoeffReloadHash, 1f);
        }

        StaticEvents.PauseOpened -= OnPauseOpened;
        StaticEvents.PauseClosed -= OnPauseClosed;
    }

    private void OnPauseOpened()
    {
        PauseReloadAudio();
    }

    private void OnPauseClosed()
    {
        ResumeReloadAudio();
    }

    private void Update()
    {
        UpdateAnimationCoefficients();

        if (IsInputBlocked())
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        if (fireAction == null || fireAction.action == null)
        {
            return;
        }

        if (firePoint == null)
        {
            return;
        }

        if (projectilePool == null)
        {
            return;
        }

        if (playerRotation == null)
        {
            return;
        }

        if (reloading)
        {
            return;
        }

        if (!fireAction.action.IsPressed())
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        if (CurrentAmmo <= 0)
        {
            return;
        }

        Shoot();
    }

    private bool IsInputBlocked()
    {
        if (RuntimeOptions.InputBlocked)
        {
            return true;
        }

        if (inputBlocker != null && inputBlocker.IsBlocked)
        {
            return true;
        }

        return false;
    }

    private void Shoot()
    {
        if (reloading)
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        if (!projectilePool.GetProjectile(out GameObject projectileObject))
        {
            return;
        }

        Vector3 shootDirection = firePoint.forward;

        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        shootDirection = shootDirection.normalized;

        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();

        if (projectile == null)
        {
            Debug.LogError("PlayerShooter: Projectile component was not found on pooled object.", projectileObject);
            return;
        }

        float fireCooldown = GetFireCooldown();
        nextFireTime = Time.time + fireCooldown;
        CurrentAmmo--;
        NotifyAmmoChanged();

        projectileObject.transform.position = firePoint.position;
        projectileObject.transform.rotation = Quaternion.LookRotation(shootDirection);
        projectile.SetOwner(gameObject);
        projectileObject.SetActive(true);
        projectile.Launch(shootDirection);

        SpawnMuzzleVfx();

        if (weaponAnimator != null)
        {
            weaponAnimator.Play(shootStateName, 0, 0f);
        }

        if (playerSfxController != null)
        {
            playerSfxController.PostShoot();
        }

        GameSessionStats stats = GameSessionStats.Instance;

        if (stats != null)
        {
            stats.AddShot();
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
                Debug.LogWarning("PlayerShooter: Missing muzzle VFX prefab for " + entry.muzzleVfxType, this);
                continue;
            }

            if (!muzzleVfxMap.ContainsKey(entry.muzzleVfxType))
            {
                muzzleVfxMap.Add(entry.muzzleVfxType, entry.muzzleVfxPrefab);
            }
            else
            {
                Debug.LogWarning("PlayerShooter: Duplicate muzzle VFX mapping for " + entry.muzzleVfxType, this);
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
            Debug.LogWarning("PlayerShooter: No muzzle VFX mapped for " + muzzleVfxType, this);
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

    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (IsInputBlocked())
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        if (reloadCoroutine != null)
        {
            return;
        }

        reloadCoroutine = StartCoroutine(ReloadingRoutine());
    }

    private IEnumerator ReloadingRoutine()
    {
        if (reloading)
        {
            reloadCoroutine = null;
            yield break;
        }

        if (CurrentAmmo == MagazineSize)
        {
            reloadCoroutine = null;
            yield break;
        }

        reloading = true;

        if (playerSfxController != null)
        {
            playerSfxController.PostReloadPlay(reloadDuration);
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.Play(reloadStateName, 0, 0f);
        }

        yield return new WaitForSeconds(reloadDuration);

        CurrentAmmo = MagazineSize;
        NotifyAmmoChanged();

        reloading = false;
        reloadCoroutine = null;

        if (playerSfxController != null)
        {
            playerSfxController.PostReloadStop();
        }
    }

    public void PauseReloadAudio()
    {
        if (reloading == false)
        {
            return;
        }

        if (playerSfxController == null)
        {
            return;
        }

        playerSfxController.PauseReload();
    }

    public void ResumeReloadAudio()
    {
        if (reloading == false)
        {
            return;
        }

        if (playerSfxController == null)
        {
            return;
        }

        playerSfxController.ResumeReload();
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

        weaponAnimator.SetFloat(CoeffShootHash, shootCoeff);
        weaponAnimator.SetFloat(CoeffReloadHash, reloadCoeff);
    }

    private float GetFireCooldown()
    {
        if (shotsPerMinute <= 0.001f)
        {
            return 999f;
        }

        return 60f / shotsPerMinute;
    }

    public void SetFireRateMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        fireRateMultiplier = multiplier;
        shotsPerMinute = baseShotsPerMinute * fireRateMultiplier;
    }

    public void ResetFireRateMultiplier()
    {
        fireRateMultiplier = 1f;
        shotsPerMinute = baseShotsPerMinute;
    }

    public void SetReloadMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        reloadMultiplier = multiplier;

        float calculatedReload = baseReloadDuration / reloadMultiplier;

        if (calculatedReload < minReloadDuration)
        {
            reloadDuration = minReloadDuration;
        }
        else
        {
            reloadDuration = calculatedReload;
        }
    }

    public void ResetReloadMultiplier()
    {
        reloadMultiplier = 1f;
        reloadDuration = baseReloadDuration;

        if (reloadDuration < minReloadDuration)
        {
            reloadDuration = minReloadDuration;
        }
    }

    public void SetDeadState(bool value)
    {
        isDead = value;

        if (value)
        {
            StopReloadRoutineInternal();
            reloading = false;

            if (playerSfxController != null)
            {
                playerSfxController.PostReloadStop();
            }
        }
    }

    public int GetCurrentAmmo()
    {
        return CurrentAmmo;
    }

    public int GetMagazineSize()
    {
        return MagazineSize;
    }

    public bool IsReloading()
    {
        return reloading;
    }

    private void ApplyConfig()
    {
        if (playerConfig == null)
        {
            return;
        }

        shotsPerMinute = playerConfig.shotsPerMinute;
        reloadDuration = playerConfig.reloadDuration;

        if (reloadDuration < minReloadDuration)
        {
            reloadDuration = minReloadDuration;
        }

        MagazineSize = playerConfig.magazineSize;

        baseShootDuration = playerConfig.shotAnimationDuration;
        baseReloadDuration = playerConfig.reloadAnimationDuration;

        if (baseReloadDuration < minReloadDuration)
        {
            baseReloadDuration = minReloadDuration;
        }

        shootStateName = playerConfig.shotAnimationStateName;
        reloadStateName = playerConfig.reloadAnimationStateName;
    }

    private void NotifyAmmoChanged()
    {
        if (AmmoChanged == null)
        {
            return;
        }

        AmmoChanged(CurrentAmmo, MagazineSize);
    }

    private void StopReloadRoutineInternal()
    {
        if (reloadCoroutine == null)
        {
            return;
        }

        StopCoroutine(reloadCoroutine);
        reloadCoroutine = null;
    }
}