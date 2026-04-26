using System.Collections.Generic;
using UnityEngine;

public class HotshotProjectilePool : MonoBehaviour, IProjectilePool
{
    [Header("Pool Settings")]
    [Tooltip("Initial number of projectiles that will be created in the pool.")]
    [SerializeField] private int objectsCount = 20;

    [Tooltip("Projectile prefab that will be created and stored in the pool.")]
    [SerializeField] private GameObject objectPrefab;

    [Tooltip("If enabled, the pool can automatically create more projectiles when all existing ones are busy.")]
    [SerializeField] private bool canExpand = true;

    [Tooltip("How many new projectiles will be created when the pool expands.")]
    [SerializeField] private int expandStep = 5;

    private readonly List<GameObject> objectsList = new List<GameObject>();
    private int nextProjectileId;

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

    public bool CanExpand
    {
        get
        {
            return canExpand;
        }
        set
        {
            canExpand = value;
        }
    }

    public int ExpandStep
    {
        get
        {
            return expandStep;
        }
        set
        {
            expandStep = Mathf.Max(1, value);
        }
    }

    private void Awake()
    {
        if (objectPrefab == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("HotshotProjectilePool: Object Prefab is not assigned.", this);
            }
            return;
        }

        ObjectsCount = objectsCount;
        ExpandStep = expandStep;

        CreateProjectiles(ObjectsCount);
    }

    public GameObject CreateProjectile(int id)
    {
        GameObject newObject = Instantiate(objectPrefab, transform);
        newObject.name = $"OP_{objectPrefab.name}_{id}";
        newObject.SetActive(false);
        objectsList.Add(newObject);
        return newObject;
    }

    public void CreateProjectiles(int count)
    {
        int safeCount = Mathf.Max(1, count);

        for (int i = 0; i < safeCount; i++)
        {
            CreateProjectile(nextProjectileId);
            nextProjectileId++;
        }
    }

    public bool GetProjectile(out GameObject projectile)
    {
        projectile = null;

        for (int i = 0; i < objectsList.Count; i++)
        {
            if (!objectsList[i].activeInHierarchy)
            {
                projectile = objectsList[i];
                return true;
            }
        }

        if (!CanExpand)
        {
            return false;
        }

        CreateProjectiles(ExpandStep);

        for (int i = 0; i < objectsList.Count; i++)
        {
            if (!objectsList[i].activeInHierarchy)
            {
                projectile = objectsList[i];
                return true;
            }
        }

        return false;
    }
}