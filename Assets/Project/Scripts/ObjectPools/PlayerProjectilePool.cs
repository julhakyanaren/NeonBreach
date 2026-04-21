using System.Collections.Generic;
using UnityEngine;

public class PlayerProjectilePool : MonoBehaviour, IProjectilePool
{
    [Header("Player Config")]
    [Tooltip("Player config that defines which projectile config should be used.")]
    [SerializeField] private PlayerConfig playerConfig;

    [Header("Pool Settings")]
    [Tooltip("Initial number of projectiles that will be created in the pool.")]
    [SerializeField] private int objectsCount = 30;

    [Tooltip("Additional pooled projectiles above magazine size.")]
    [SerializeField] private int extraProjectiles = 10;

    [Tooltip("Projectile prefab that will be created and stored in the pool.")]
    [SerializeField] private GameObject objectPrefab;

    [Header("Runtime Damage Settings")]
    [Tooltip("Base projectile damage value used for reset.")]
    [SerializeField] private float baseProjectileDamage = 10f;

    [Header("Hierarchy")]
    [Tooltip("Optional root container for pooled projectiles. If empty, it will be created automatically.")]
    [SerializeField] private Transform poolContainer;

    [Tooltip("Name suffix used for the generated projectile container.")]
    [SerializeField] private string containerSuffix = "_ProjectilePoolContainer";

    [Header("Trail State")]
    [Tooltip("Is boosted projectile trail currently active.")]
    [SerializeField] private bool isBoostedTrailActive;

    private List<GameObject> objectsList = new List<GameObject>();

    public int ObjectsCount
    {
        get
        {
            return objectsCount;
        }
        set
        {
            objectsCount = Mathf.Max(10, value);
        }
    }

    private void Awake()
    {
        if (objectPrefab == null)
        {
            Debug.LogError("PlayerProjectilePool: Object Prefab is not assigned.", this);
            return;
        }

        ApplyConfigPoolSize();
        CacheBaseProjectileDamage();
        EnsurePoolContainer();

        for (int i = 0; i < ObjectsCount; i++)
        {
            CreateProjectile(i);
        }
    }

    public GameObject CreateProjectile(int id)
    {
        GameObject newObject = Instantiate(objectPrefab, poolContainer);
        newObject.name = "OP_" + objectPrefab.name + "_" + id;
        newObject.SetActive(false);

        PlayerProjectile projectile = newObject.GetComponent<PlayerProjectile>();

        if (projectile != null)
        {
            if (playerConfig != null && playerConfig.projectileConfig != null)
            {
                projectile.SetConfig(playerConfig.projectileConfig);
            }

            projectile.SetBoostedTrailState(isBoostedTrailActive);
        }

        objectsList.Add(newObject);
        return newObject;
    }

    public bool GetProjectile(out GameObject projectile)
    {
        projectile = null;

        for (int i = 0; i < objectsList.Count; i++)
        {
            GameObject pooledObject = objectsList[i];

            if (pooledObject == null)
            {
                continue;
            }

            if (!pooledObject.activeInHierarchy)
            {
                projectile = pooledObject;

                if (projectile.transform.parent != poolContainer)
                {
                    projectile.transform.SetParent(poolContainer);
                }

                PlayerProjectile playerProjectile = projectile.GetComponent<PlayerProjectile>();

                if (playerProjectile != null)
                {
                    playerProjectile.SetBoostedTrailState(isBoostedTrailActive);
                }

                return true;
            }
        }

        return false;
    }

    public void SetProjectileDamageMultiplier(float multiplier)
    {
        if (multiplier <= 0f)
        {
            return;
        }

        float modifiedDamage = baseProjectileDamage * multiplier;

        for (int i = 0; i < objectsList.Count; i++)
        {
            if (objectsList[i] == null)
            {
                continue;
            }

            PlayerProjectile projectile = objectsList[i].GetComponent<PlayerProjectile>();

            if (projectile == null)
            {
                continue;
            }

            projectile.Damage = modifiedDamage;
        }
    }

    public void ResetProjectileDamage()
    {
        for (int i = 0; i < objectsList.Count; i++)
        {
            if (objectsList[i] == null)
            {
                continue;
            }

            PlayerProjectile projectile = objectsList[i].GetComponent<PlayerProjectile>();

            if (projectile == null)
            {
                continue;
            }

            projectile.Damage = baseProjectileDamage;
        }
    }

    public void SetBoostedTrailState(bool state)
    {
        isBoostedTrailActive = state;
    }

    private void CacheBaseProjectileDamage()
    {
        if (playerConfig != null && playerConfig.projectileConfig != null)
        {
            baseProjectileDamage = playerConfig.projectileConfig.damage;
            return;
        }

        if (objectPrefab == null)
        {
            return;
        }

        PlayerProjectile projectile = objectPrefab.GetComponent<PlayerProjectile>();

        if (projectile == null)
        {
            return;
        }

        if (projectile.ProjectileConfig != null)
        {
            baseProjectileDamage = projectile.ProjectileConfig.damage;
            return;
        }

        baseProjectileDamage = projectile.Damage;
    }

    private void ApplyConfigPoolSize()
    {
        if (playerConfig != null)
        {
            ObjectsCount = playerConfig.magazineSize + extraProjectiles;
            return;
        }

        if (objectsCount <= 0)
        {
            objectsCount = 10;
        }

        ObjectsCount = objectsCount;
    }

    private void EnsurePoolContainer()
    {
        if (poolContainer != null)
        {
            poolContainer.SetParent(null);
            return;
        }

        GameObject containerObject = new GameObject(gameObject.name + containerSuffix);
        poolContainer = containerObject.transform;
        poolContainer.SetParent(null);
    }
}