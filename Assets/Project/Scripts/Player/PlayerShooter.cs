using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
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

    [Tooltip("Boosted muzzle VFX type used by the player.")]
    [SerializeField] private MuzzleVFXType muzzleBoostedVfxType = MuzzleVFXType.BoostedEnergy;

    [Tooltip("Offset applied forward from fire point when spawning muzzle VFX.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float muzzleOffset = 0.05f;

    [Header("Projectile Runtime State")]
    [Tooltip("Is boosted projectile trail currently active for spawned projectiles.")]
    [SerializeField] private bool isBoostedProjectileTrailActive;

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

    [Header("Networking")]
    [Tooltip("PhotonView used to detect ownership in multiplayer mode.")]
    [SerializeField] private PhotonView photonView;

    [Tooltip("Resources path used for multiplayer projectile spawn through Photon.")]
    [SerializeField] private string multiplayerProjectileResourcePath = "PhotonPrefabs/Projectiles_PUN/CyanProjectile_PUN";

    [Header("Debug / External State")]
    [Tooltip("Set true when player is dead.")]
    [SerializeField] private bool isDead = false;

    private bool ownsShooterInput;

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
        photonView = GetComponent<PhotonView>();
    }

    private void Awake()
    {
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

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
        ownsShooterInput = false;

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null && photonView.IsMine == false)
            {
                StaticEvents.PauseOpened += OnPauseOpened;
                StaticEvents.PauseClosed += OnPauseClosed;
                return;
            }
        }

        if (fireAction != null && fireAction.action != null)
        {
            fireAction.action.Enable();
        }

        if (reloadAction != null && reloadAction.action != null)
        {
            reloadAction.action.Enable();
            reloadAction.action.performed += OnReloadPerformed;
        }

        ownsShooterInput = true;

        StaticEvents.PauseOpened += OnPauseOpened;
        StaticEvents.PauseClosed += OnPauseClosed;
    }

    private void OnDisable()
    {
        if (ownsShooterInput)
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
        }

        ownsShooterInput = false;

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

    public void RespawnResetShooter()
    {
        if (ShouldIgnoreNetworkInput())
        {
            return;
        }

        StopReloadRoutineInternal();
        reloading = false;

        if (playerSfxController != null)
        {
            playerSfxController.PostReloadStop();
        }

        isDead = false;

        nextFireTime = 0f;

        CurrentAmmo = MagazineSize;
        NotifyAmmoChanged();

        UpdateAnimationCoefficients();
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

        if (ShouldIgnoreNetworkInput())
        {
            return;
        }

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

        if (RuntimeOptions.MultiplayerMode == false && projectilePool == null)
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

    public void SetBoostedProjectileTrailState(bool state)
    {
        isBoostedProjectileTrailActive = state;
    }

    private bool ShouldIgnoreNetworkInput()
    {
        if (RuntimeOptions.MultiplayerMode == false)
        {
            return false;
        }

        if (photonView == null)
        {
            return false;
        }

        if (photonView.IsMine == false)
        {
            return true;
        }

        return false;
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

    private void SpawnSingleplayerProjectile(Vector3 shootDirection)
    {
        if (projectilePool == null)
        {
            return;
        }

        if (!projectilePool.GetProjectile(out GameObject projectileObject))
        {
            return;
        }

        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();

        if (projectile == null)
        {
            Debug.LogError("PlayerShooter: Projectile component was not found on pooled object.", projectileObject);
            return;
        }

        projectileObject.transform.position = firePoint.position;
        projectileObject.transform.rotation = Quaternion.LookRotation(shootDirection);
        projectile.SetOwner(gameObject);
        projectile.SetBoostedTrailState(isBoostedProjectileTrailActive);
        projectileObject.SetActive(true);
        projectile.Launch(shootDirection);
    }

    private void SpawnMultiplayerProjectile(Vector3 shootDirection)
    {
        if (string.IsNullOrWhiteSpace(multiplayerProjectileResourcePath))
        {
            Debug.LogError("PlayerShooter: Multiplayer projectile resource path is empty.", this);
            return;
        }

        GameObject projectileObject = PhotonNetwork.Instantiate(
            multiplayerProjectileResourcePath,
            firePoint.position,
            Quaternion.LookRotation(shootDirection),
            0,
            new object[]
            {
                shootDirection,
                isBoostedProjectileTrailActive
            });

        if (projectileObject == null)
        {
            Debug.LogError("PlayerShooter: Photon projectile spawn failed.", this);
            return;
        }

        PlayerProjectile projectile = projectileObject.GetComponent<PlayerProjectile>();

        if (projectile == null)
        {
            Debug.LogError("PlayerShooter: PlayerProjectile component was not found on multiplayer projectile.", projectileObject);
            return;
        }

        projectile.SetOwner(gameObject);
        projectile.SetBoostedTrailState(isBoostedProjectileTrailActive);
        projectile.Launch(shootDirection);
    }

    private void Shoot()
    {
        if (ShouldIgnoreNetworkInput())
        {
            return;
        }

        if (reloading)
        {
            return;
        }

        if (isDead)
        {
            return;
        }

        if (firePoint == null)
        {
            return;
        }

        Vector3 shootDirection = firePoint.forward;

        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        shootDirection = shootDirection.normalized;

        float fireCooldown = GetFireCooldown();
        nextFireTime = Time.time + fireCooldown;
        CurrentAmmo--;
        NotifyAmmoChanged();

        if (RuntimeOptions.MultiplayerMode)
        {
            SpawnMultiplayerProjectile(shootDirection);
        }
        else
        {
            SpawnSingleplayerProjectile(shootDirection);
        }

        SpawnMuzzleVfx();
        PlayWeaponShootAnimationNetworked();

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

    [PunRPC]
    private void SpawnMuzzleVfx_RPC(Vector3 position, Quaternion rotation, bool isBoosted)
    {
        if (muzzleVfxMap == null || muzzleVfxMap.Count == 0)
        {
            return;
        }

        GameObject prefab;

        if (isBoosted)
        {
            if (!muzzleVfxMap.TryGetValue(muzzleBoostedVfxType, out prefab))
            {
                return;
            }
        }
        else
        {
            if (!muzzleVfxMap.TryGetValue(muzzleVfxType, out prefab))
            {
                return;
            }
        }

        if (prefab == null)
        {
            return;
        }

        Instantiate(prefab, position, rotation);
    }

    private void SpawnMuzzleVfx()
    {
        if (firePoint == null)
        {
            return;
        }

        Vector3 spawnPosition = firePoint.position + firePoint.forward * muzzleOffset;
        Quaternion spawnRotation = firePoint.rotation;

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC(
                    "SpawnMuzzleVfx_RPC",
                    RpcTarget.All,
                    spawnPosition,
                    spawnRotation,
                    isBoostedProjectileTrailActive);
            }
        }
        else
        {
            if (muzzleVfxMap == null || muzzleVfxMap.Count == 0)
            {
                return;
            }

            if (!muzzleVfxMap.TryGetValue(muzzleVfxType, out GameObject muzzlePrefab))
            {
                return;
            }

            if (!isBoostedProjectileTrailActive)
            {
                Instantiate(muzzlePrefab, spawnPosition, spawnRotation);
            }
            else
            {
                if (!muzzleVfxMap.TryGetValue(muzzleBoostedVfxType, out GameObject muzzleBoosted))
                {
                    return;
                }

                Instantiate(muzzleBoosted, spawnPosition, spawnRotation);
            }
        }
    }

    [PunRPC]
    private void PlayWeaponShootAnimation_RPC()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        weaponAnimator.Play(shootStateName, 0, 0f);
    }

    [PunRPC]
    private void PlayWeaponReloadAnimation_RPC()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        weaponAnimator.Play(reloadStateName, 0, 0f);
    }

    private void PlayWeaponShootAnimationNetworked()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("PlayWeaponShootAnimation_RPC", RpcTarget.All);
            }

            return;
        }

        weaponAnimator.Play(shootStateName, 0, 0f);
    }

    private void PlayWeaponReloadAnimationNetworked()
    {
        if (weaponAnimator == null)
        {
            return;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC("PlayWeaponReloadAnimation_RPC", RpcTarget.All);
            }

            return;
        }

        weaponAnimator.Play(reloadStateName, 0, 0f);
    }

    public void SetMuzzleVfxType(MuzzleVFXType newMuzzleVfxType)
    {
        muzzleVfxType = newMuzzleVfxType;
    }

    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (ShouldIgnoreNetworkInput())
        {
            return;
        }

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

        PlayWeaponReloadAnimationNetworked();

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