using UnityEngine;

public interface IDamageable
{
    public void ApplyDamage(float damage);
    public void ApplyDamage(float damage, GameObject damageDealer);

    public void Die();
}
