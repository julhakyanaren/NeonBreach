using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WwiseGameplaySFXController))]
[RequireComponent(typeof(PhotonView))]
public class HotshotProjectile : MonoBehaviour, IEnemyProjectile, IPunInstantiateMagicCallback
{
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
    [Tooltip("How long the projectile exists before being returned to the pool or destroyed in multiplayer.")]
    [Range(0.5f, 10f)]
    [SerializeField] private float lifeTime = 3f;

    [Header("Wave Scaling")]
    [Tooltip("Runtime damage multiplier applied when projectile is launched.")]
    [SerializeField] private float damageMultiplier = 1f;

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

    [Header("VFX Settings")]
    [Tooltip("Offset applied to impact VFX along hit direction when hitting a player.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float impactOffsetPlayer = 0.05f;

    [Tooltip("Offset applied to impact VFX along hit direction when hitting arena or obstacles.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float impactOffsetArena = 0.05f;

    [Tooltip("All mapped impact VFX prefabs for singleplayer.")]
    [SerializeField] private List<ImpactVfxEntry> impactVfxEntries = new List<ImpactVfxEntry>();

    [Tooltip("Current impact VFX type used by this projectile.")]
    [SerializeField] private ImpactVFXType impactVfxType = ImpactVFXType.HotshotBullet;

    [Header("Trail")]
    [Tooltip("Has projectile trail renderer.")]
    [SerializeField] private bool hasTrail;

    [Tooltip("Projectile Trail Renderer.")]
    [SerializeField] private TrailRenderer trailRenderer;

    [Header("References")]
    [Tooltip("Cached Rigidbody component of the projectile.")]
    [SerializeField] private Rigidbody projectileRigidbody;

    [Tooltip("Reference of the WwiseGameplaySFXController script.")]
    [SerializeField] private WwiseGameplaySFXController gameplaySfxController;

    private PhotonView photonView;
    private Vector3 moveDirection;
    private float currentLifeTimer;
    private Dictionary<ImpactVFXType, GameObject> impactVfxMap;

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

    private void Reset()
    {
        projectileRigidbody = GetComponent<Rigidbody>();
        gameplaySfxController = GetComponent<WwiseGameplaySFXController>();
        photonView = GetComponent<PhotonView>();
    }

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        if (gameplaySfxController == null)
        {
            gameplaySfxController = GetComponent<WwiseGameplaySFXController>();
        }

        if (hasTrail && trailRenderer == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("HotshotProjectile: trail renderer is missing.", this);
            }

            return;
        }

        BuildImpactVfxMap();
        ApplyConfig();
        RebuildRuntimeStats();
    }

    private void OnEnable()
    {
        currentLifeTimer = LifeTime;

        ClearTrail();

        if (projectileRigidbody == null)
        {
            return;
        }

        projectileRigidbody.velocity = Vector3.zero;
        projectileRigidbody.angularVelocity = Vector3.zero;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            projectileRigidbody.velocity = moveDirection * Speed;
        }
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
        if (projectileRigidbody != null)
        {
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
        }

        ClearTrail();
    }

    private void BuildImpactVfxMap()
    {
        impactVfxMap = new Dictionary<ImpactVFXType, GameObject>();

        for (int i = 0; i < impactVfxEntries.Count; i++)
        {
            ImpactVfxEntry entry = impactVfxEntries[i];

            if (entry.impactVfxPrefab == null)
            {
                if (RuntimeOptions.LoggingWarning)
                {
                    Debug.LogWarning("HotshotProjectile: Missing impact VFX prefab for " + entry.impactVfxType, this);
                }

                continue;
            }

            if (!impactVfxMap.ContainsKey(entry.impactVfxType))
            {
                impactVfxMap.Add(entry.impactVfxType, entry.impactVfxPrefab);
            }
            else
            {
                if (RuntimeOptions.LoggingWarning)
                {
                    Debug.LogWarning("HotshotProjectile: Duplicate impact VFX mapping for " + entry.impactVfxType, this);
                }
            }
        }
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

    private void RebuildRuntimeStats()
    {
        float baseDamage = damage;

        if (projectileConfig != null)
        {
            baseDamage = projectileConfig.damage;
        }

        Damage = baseDamage * damageMultiplier;
    }

    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier = Mathf.Max(0.01f, multiplier);
        RebuildRuntimeStats();
    }

    public void Launch(Vector3 direction)
    {
        ClearTrail();

        moveDirection = direction.normalized;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.forward = moveDirection;
        }

        if (projectileRigidbody != null && gameObject.activeInHierarchy)
        {
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
            projectileRigidbody.velocity = moveDirection * Speed;
        }
    }

    private void ClearTrail()
    {
        if (!hasTrail)
        {
            return;
        }

        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
        {
            return;
        }

        if (PhotonNetwork.InRoom == true)
        {
            if (PhotonNetwork.IsMasterClient == false)
            {
                return;
            }
        }

        Vector3 impactForward = -transform.forward;
        Vector3 impactPosition = transform.position + impactForward * impactOffsetArena;

        if (other.CompareTag("Player"))
        {
            TryApplyDamageMultiplayerSafe(other);
            impactPosition = transform.position + impactForward * impactOffsetPlayer;
        }

        PlayImpactSfx();
        SpawnImpactVfx(impactPosition, impactForward);
        DisableProjectile();
    }

    private void TryApplyDamageMultiplayerSafe(Collider other)
    {
        if (PhotonNetwork.InRoom == true)
        {
            PhotonView targetPhotonView = other.GetComponentInParent<PhotonView>();

            if (targetPhotonView == null)
            {
                return;
            }

            if (photonView == null)
            {
                return;
            }

            photonView.RPC(
                nameof(RPC_ApplyDamage),
                RpcTarget.All,
                targetPhotonView.ViewID,
                Damage);

            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
        {
            return;
        }

        damageable.ApplyDamage(Damage);
    }

    [PunRPC]
    private void RPC_ApplyDamage(int targetViewId, float damageValue)
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

        damageable.ApplyDamage(damageValue);
    }

    private void SpawnImpactVfx(Vector3 position, Vector3 forwardDirection)
    {
        if (PhotonNetwork.InRoom == true)
        {
            if (photonView == null)
            {
                return;
            }

            if (PhotonNetwork.IsMasterClient == false)
            {
                return;
            }

            photonView.RPC(
                nameof(RPC_SpawnImpactVfx),
                RpcTarget.All,
                position,
                forwardDirection);

            return;
        }

        SpawnImpactVfxInternal(position, forwardDirection);
    }

    [PunRPC]
    private void RPC_SpawnImpactVfx(Vector3 position, Vector3 forwardDirection)
    {
        SpawnImpactVfxInternal(position, forwardDirection);
    }

    private void SpawnImpactVfxInternal(Vector3 position, Vector3 forwardDirection)
    {
        if (impactVfxMap == null || impactVfxMap.Count == 0)
        {
            return;
        }

        GameObject impactPrefab;

        if (!impactVfxMap.TryGetValue(impactVfxType, out impactPrefab))
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("HotshotProjectile: No impact VFX mapped for " + impactVfxType, this);
            }

            return;
        }

        if (impactPrefab == null)
        {
            return;
        }

        Quaternion impactRotation = Quaternion.LookRotation(forwardDirection);
        Instantiate(impactPrefab, position, impactRotation);
    }

    private void PlayImpactSfx()
    {
        if (gameplaySfxController == null)
        {
            return;
        }

        gameplaySfxController.PlayRandomHit(gameObject);
    }

    public void DisableProjectile()
    {
        if (PhotonNetwork.InRoom == true)
        {
            if (PhotonNetwork.IsMasterClient == true)
            {
                PhotonNetwork.Destroy(gameObject);
            }

            return;
        }

        gameObject.SetActive(false);
        ClearTrail();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        object[] data = info.photonView.InstantiationData;

        if (data == null)
        {
            return;
        }

        if (data.Length < 1)
        {
            return;
        }

        Vector3 direction = (Vector3)data[0];
        Launch(direction);
    }
}