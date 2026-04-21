using UnityEngine;

public interface IProjectile
{
    public float Damage { get; set; }
    public float Speed { get; set; }
    public float LifeTime { get; set; }
    public void Launch(Vector3 direction);
    public void DisableProjectile();
}
