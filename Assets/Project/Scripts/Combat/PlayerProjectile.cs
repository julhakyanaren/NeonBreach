using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WwiseGameplaySFXController))]
public class PlayerProjectile : MonoBehaviour, IPlayerProjectile, IPunInstantiateMagicCallback
{
    [System.Serializable]
    public class ImpactVfxEntry
    {
        [Header("Impact VFX Type")]
        [Tooltip("Impact VFX group.")]
        public ImpactVFXType impactVfxType;

        [Header("Impact VFX Prefab")]
        [Tooltip("Mapped impact VFX prefab.")]
        public GameObject impactVfxPrefab;
    }

    [Header("Config Source")]
    [Tooltip("Projectile config with base stats.")]
    [SerializeField] private ProjectileConfig projectileConfig;

    [Header("Projectile Settings")]
    [Tooltip("Movement speed of the projectile.")]
    [Range(1f, 30f)]
    [SerializeField] private float speed = 15f;

    [Tooltip("Projectile damage.")]
    [Range(1f, 1000f)]
    [SerializeField] private float damage = 10f;

    [Header("Lifetime Settings")]
    [Tooltip("How long the projectile exists before being returned to the pool.")]
    [Range(0.5f, 10f)]
    [SerializeField] private float lifeTime = 3f;

    [Header("References")]
    [Tooltip("Reference of the WwiseGameplaySFXController script.")]
    [SerializeField] private WwiseGameplaySFXController gameplaySfxController;

    [Tooltip("Cached Rigidbody component of the projectile.")]
    [SerializeField] private Rigidbody projectileRigidbody;

    [Header("Networking")]
    [Tooltip("PhotonView used for multiplayer RPC calls.")]
    [SerializeField] private PhotonView photonView;

    [Header("VFX Settings")]
    [Tooltip("All mapped impact VFX prefabs.")]
    [SerializeField] private List<ImpactVfxEntry> impactVfxEntries = new List<ImpactVfxEntry>();

    [Tooltip("Current impact VFX type used by this projectile.")]
    [SerializeField] private ImpactVFXType impactVfxType = ImpactVFXType.CyanEnergy;

    [Tooltip("Boosted impact VFX type used by this projectile.")]
    [SerializeField] private ImpactVFXType impactBoostedVfxType = ImpactVFXType.BoostedEnergy;

    [Tooltip("Offset applied to impact VFX along hit direction, when hit enemy.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float impactOffsetEnemy = 0.05f;

    [Tooltip("Offset applied to impact VFX along hit direction, when hit not enemy.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float impactOffsetArena = 0.05f;

    [Header("Trail Settings")]
    [Tooltip("Does this projectile use trail renderers.")]
    [SerializeField] private bool hasTrail;

    [Tooltip("Default projectile trail renderer.")]
    [SerializeField] private TrailRenderer trail;

    [Tooltip("Boosted projectile trail renderer.")]
    [SerializeField] private TrailRenderer trailBoosted;

    [Header("Trail State")]
    [Tooltip("Is boosted trail currently active for this projectile.")]
    [SerializeField] private bool isBoostedTrailActive;

    [Header("Runtime")]
    [Tooltip("Prevents duplicate hit processing.")]
    [SerializeField] private bool hasProcessedHit;

    private Vector3 moveDirection;
    private float currentLifeTimer;
    private GameObject owner;
    private Dictionary<ImpactVFXType, GameObject> impactVfxMap;

    public ProjectileConfig ProjectileConfig
    {
        get
        {
            return projectileConfig;
        }
    }

    public float Speed
    {
        get
        {
            return speed;
        }
        set
        {
            speed = Mathf.Max(1f, value);
        }
    }

    public float Damage
    {
        get
        {
            return damage;
        }
        set
        {
            damage = Mathf.Max(1f, value);
        }
    }

    public float LifeTime
    {
        get
        {
            return lifeTime;
        }
        set
        {
            lifeTime = Mathf.Max(0.5f, value);
        }
    }

    public GameObject Owner
    {
        get
        {
            return owner;
        }
    }

    private void Reset()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
        gameplaySfxController = GetComponent<WwiseGameplaySFXController>();
        photonView = GetComponent<PhotonView>();
    }

    private void Awake()
    {
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        if (gameplaySfxController == null)
        {
            gameplaySfxController = GetComponent<WwiseGameplaySFXController>();
        }

        if (hasTrail)
        {
            if (trail == null)
            {
                Debug.LogError("PlayerProjectile: trail renderer is missing.", this);
                return;
            }

            if (trailBoosted == null)
            {
                Debug.LogError("PlayerProjectile: trailBoosted renderer is missing.", this);
                return;
            }
        }

        BuildImpactVfxMap();
        ApplyConfig();
        ApplyTrailVisualState();
    }

    private void OnEnable()
    {
        currentLifeTimer = LifeTime;
        hasProcessedHit = false;

        ClearTrails();
        ApplyTrailVisualState();

        if (projectileRigidbody == null)
        {
            return;
        }

        projectileRigidbody.velocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;
        projectileRigidbody.velocity = moveDirection * Speed;
    }

    private void Update()
    {
        currentLifeTimer -= Time.deltaTime;

        if (currentLifeTimer <= 0f)
        {
            DisableProjectile();
        }
    }

    private void OnDisable()
    {
        hasProcessedHit = false;

        if (projectileRigidbody != null)
        {
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
        }

        ClearTrails();
    }

    private void BuildImpactVfxMap()
    {
        impactVfxMap = new Dictionary<ImpactVFXType, GameObject>();

        for (int i = 0; i < impactVfxEntries.Count; i++)
        {
            ImpactVfxEntry entry = impactVfxEntries[i];

            if (entry.impactVfxPrefab == null)
            {
                Debug.LogWarning("PlayerProjectile: Missing impact VFX prefab for " + entry.impactVfxType, this);
                continue;
            }

            if (!impactVfxMap.ContainsKey(entry.impactVfxType))
            {
                impactVfxMap.Add(entry.impactVfxType, entry.impactVfxPrefab);
            }
            else
            {
                Debug.LogWarning("PlayerProjectile: Duplicate impact VFX mapping for " + entry.impactVfxType, this);
            }
        }
    }

    public void Launch(Vector3 direction)
    {
        ClearTrails();

        moveDirection = direction.normalized;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.forward = moveDirection;
        }

        ApplyTrailVisualState();

        if (projectileRigidbody != null && gameObject.activeInHierarchy)
        {
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
            projectileRigidbody.velocity = moveDirection * Speed;
        }
    }

    public void SetBoostedTrailState(bool boosted)
    {
        isBoostedTrailActive = boosted;

        ClearTrails();
        ApplyTrailVisualState();
    }

    public void SetImpactVfxType(ImpactVFXType newImpactVfxType)
    {
        impactVfxType = newImpactVfxType;
    }

    private void ApplyTrailVisualState()
    {
        if (!hasTrail)
        {
            return;
        }

        if (trail != null)
        {
            trail.enabled = !isBoostedTrailActive;
            trail.emitting = !isBoostedTrailActive;
        }

        if (trailBoosted != null)
        {
            trailBoosted.enabled = isBoostedTrailActive;
            trailBoosted.emitting = isBoostedTrailActive;
        }
    }

    private void ClearTrails()
    {
        if (!hasTrail)
        {
            return;
        }

        if (trail != null)
        {
            trail.emitting = false;
            trail.Clear();
        }

        if (trailBoosted != null)
        {
            trailBoosted.emitting = false;
            trailBoosted.Clear();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasProcessedHit)
        {
            return;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView == null)
            {
                return;
            }

            if (!photonView.IsMine)
            {
                return;
            }
        }

        if (other.isTrigger && !other.CompareTag("EnemyHitbox"))
        {
            return;
        }

        hasProcessedHit = true;

        Vector3 impactForward = -transform.forward;
        Vector3 impactPosition = transform.position + impactForward * impactOffsetArena;

        if (other.CompareTag("EnemyHitbox"))
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                GameSessionStats stats = GameSessionStats.Instance;

                if (stats != null)
                {
                    stats.AddHit();
                }

                if (RuntimeOptions.MultiplayerMode)
                {
                    PhotonView targetView = other.GetComponentInParent<PhotonView>();

                    if (targetView != null && photonView != null && photonView.IsMine)
                    {
                        photonView.RPC(
                            nameof(ApplyDamage_RPC),
                            RpcTarget.All,
                            targetView.ViewID,
                            Damage
                        );
                    }
                }
                else
                {
                    damageable.ApplyDamage(Damage, owner);
                }
            }

            if (gameplaySfxController != null)
            {
                gameplaySfxController.PlayRandomHit(gameObject);
            }

            impactPosition = transform.position + impactForward * impactOffsetEnemy;
            SpawnImpactNetworked(impactPosition, impactForward);
            DisableProjectile();
            return;
        }

        if (gameplaySfxController != null)
        {
            gameplaySfxController.PlayRandomHit(gameObject);
        }

        SpawnImpactNetworked(impactPosition, impactForward);
        DisableProjectile();
    }

    private void SpawnImpactNetworked(Vector3 position, Vector3 forwardDirection)
    {
        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null && photonView.IsMine)
            {
                photonView.RPC(
                    "SpawnImpactVfx_RPC",
                    RpcTarget.All,
                    position,
                    forwardDirection,
                    isBoostedTrailActive);
            }

            return;
        }

        SpawnImpactVfxInternal(position, forwardDirection, isBoostedTrailActive);
    }

    [PunRPC]
    private void SpawnImpactVfx_RPC(Vector3 position, Vector3 forwardDirection, bool boosted)
    {
        SpawnImpactVfxInternal(position, forwardDirection, boosted);
    }

    [PunRPC]
    private void ApplyDamage_RPC(int viewID, float damage)
    {
        PhotonView targetView = PhotonView.Find(viewID);

        if (targetView == null)
        {
            return;
        }

        IDamageable damageable = targetView.GetComponent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        damageable.ApplyDamage(damage, owner);
    }

    private void SpawnImpactVfxInternal(Vector3 position, Vector3 forwardDirection, bool boosted)
    {
        if (impactVfxMap == null || impactVfxMap.Count == 0)
        {
            return;
        }

        GameObject impactPrefab;

        if (boosted)
        {
            if (!impactVfxMap.TryGetValue(impactBoostedVfxType, out impactPrefab))
            {
                Debug.LogWarning("PlayerProjectile: No impact VFX mapped for " + impactBoostedVfxType, this);
                return;
            }
        }
        else
        {
            if (!impactVfxMap.TryGetValue(impactVfxType, out impactPrefab))
            {
                Debug.LogWarning("PlayerProjectile: No impact VFX mapped for " + impactVfxType, this);
                return;
            }
        }

        if (impactPrefab == null)
        {
            return;
        }

        Quaternion impactRotation = Quaternion.LookRotation(forwardDirection);
        Instantiate(impactPrefab, position, impactRotation);
    }

    public void DisableProjectile()
    {
        owner = null;
        hasProcessedHit = false;
        ClearTrails();

        if (RuntimeOptions.MultiplayerMode)
        {
            if (photonView != null)
            {
                if (photonView.IsMine)
                {
                    PhotonNetwork.Destroy(gameObject);
                }
            }

            return;
        }

        gameObject.SetActive(false);
    }

    public void SetOwner(GameObject projectileOwner)
    {
        owner = projectileOwner;
    }

    private void ApplyConfig()
    {
        if (projectileConfig == null)
        {
            return;
        }

        Damage = projectileConfig.damage;
        Speed = projectileConfig.speed;
        LifeTime = projectileConfig.lifeTime;
    }

    public void SetConfig(ProjectileConfig config)
    {
        if (config == null)
        {
            return;
        }

        projectileConfig = config;
        ApplyConfig();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
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

        if (data.Length < 2)
        {
            return;
        }

        Vector3 direction = (Vector3)data[0];
        bool boosted = (bool)data[1];

        SetBoostedTrailState(boosted);
        Launch(direction);
    }
}