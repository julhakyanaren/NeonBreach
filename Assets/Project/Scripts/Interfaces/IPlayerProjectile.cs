using UnityEngine;

public interface IPlayerProjectile : IProjectile
{
    public GameObject Owner { get;}
    void SetOwner(GameObject projectileOwner);
}
