using UnityEngine;

public interface IProjectilePool
{
    public int ObjectsCount { get; set; }
    public GameObject CreateProjectile(int id);
    public bool GetProjectile(out GameObject projectile);
}
