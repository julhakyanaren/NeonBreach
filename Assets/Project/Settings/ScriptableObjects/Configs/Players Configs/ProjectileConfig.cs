using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Ammunition/Projectile")]
public class ProjectileConfig : ScriptableObject
{
    [Header("Settings")]
    public float damage = 15f;
    public float speed = 10f;
    public float lifeTime = 3f;
}
