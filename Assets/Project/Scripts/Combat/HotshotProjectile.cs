using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WwiseGameplaySFXController))]
public class HotshotProjectile : MonoBehaviour, IEnemyProjectile
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
    [Tooltip("How long the projectile exists before being returned to the pool.")]
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
    [Tooltip("Offset applied to impact VFX along hit direction, when hit not an player")]
    [Range(0f, 0.5f)]
    [SerializeField] private float impactOffsetPlayer = 0.05f;

    [Tooltip("Offset applied to impact VFX along hit direction, when hit player")]
    [Range(0f, 0.5f)]
    [SerializeField] private float impactOffsetArena = 0.05f;

    [Tooltip("All mapped impact VFX prefabs.")]
    [SerializeField] private List<ImpactVfxEntry> impactVfxEntries = new List<ImpactVfxEntry>();

    [Tooltip("Current impact VFX type used by this projectile.")]
    [SerializeField] private ImpactVFXType impactVfxType = ImpactVFXType.HotshotBullet;

    [Tooltip("Has projectile trail renderer.")]
    [SerializeField] private bool hasTrail;

    [Tooltip("Projectile Trail Renderer.")]
    [SerializeField] private TrailRenderer trailRenderer;

    [Header("References")]
    [Tooltip("Cached Rigidbody component of the projectile.")]
    [SerializeField] private Rigidbody projectileRigidbody;

    [Tooltip("Reference of the WwiseGameplaySFXController script.")]
    [SerializeField] private WwiseGameplaySFXController gameplaySfxController;

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
    }

    private void Awake()
    {
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
            Debug.LogError("HotshotProjectile: trail renderer is missing.", this);
            return;
        }

        BuildImpactVfxMap();
        ApplyConfig();
        RebuildRuntimeStats();
    }

    private void OnEnable()
    {
        currentLifeTimer = LifeTime;

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
        if (projectileRigidbody != null)
        {
            projectileRigidbody.velocity = Vector3.zero;
            projectileRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void BuildImpactVfxMap()
    {
        impactVfxMap = new Dictionary<ImpactVFXType, GameObject>();

        for (int i = 0; i < impactVfxEntries.Count; i++)
        {
            ImpactVfxEntry entry = impactVfxEntries[i];

            if (entry.impactVfxPrefab == null)
            {
                Debug.LogWarning("HotshotProjectile: Missing impact VFX prefab for " + entry.impactVfxType, this);
                continue;
            }

            if (!impactVfxMap.ContainsKey(entry.impactVfxType))
            {
                impactVfxMap.Add(entry.impactVfxType, entry.impactVfxPrefab);
            }
            else
            {
                Debug.LogWarning("HotshotProjectile: Duplicate impact VFX mapping for " + entry.impactVfxType, this);
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
        if (hasTrail)
        {
            trailRenderer.Clear();
        }

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger)
        {
            return;
        }

        Vector3 impactForward = -transform.forward;
        Vector3 impactPosition = transform.position + impactForward * impactOffsetArena;

        if (other.CompareTag("Player"))
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.ApplyDamage(Damage);
            }
        }

        if (gameplaySfxController != null)
        {
            gameplaySfxController.PlayRandomHit(gameObject);
        }

        impactPosition = transform.position + impactForward * impactOffsetPlayer;
        SpawnImpactVfx(impactPosition, impactForward);
        DisableProjectile();
    }

    private void SpawnImpactVfx(Vector3 position, Vector3 forwardDirection)
    {
        if (impactVfxMap == null || impactVfxMap.Count == 0)
        {
            return;
        }

        GameObject impactPrefab;

        if (!impactVfxMap.TryGetValue(impactVfxType, out impactPrefab))
        {
            Debug.LogWarning("HotshotProjectile: No impact VFX mapped for " + impactVfxType, this);
            return;
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
        gameObject.SetActive(false);

        if (hasTrail)
        {
            trailRenderer.Clear();
        }
    }
}