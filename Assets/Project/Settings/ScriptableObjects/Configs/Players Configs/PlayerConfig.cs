using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player/Player Config")]
public class PlayerConfig : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float maxHealthLimit = 1000f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 120f;
    public float lookDeadZone = 0.2f;

    [Header("Shooting")]
    public float shotsPerMinute = 450f;
    public float reloadDuration = 5f;
    public int magazineSize = 30;

    [Header("Projectile config")]
    public ProjectileConfig projectileConfig;

    [Header("Aim")]
    public bool verticalAutoAimEnabled = true;
    public bool useSymmetricalAngleValues = true;
    public float symmetricalAngle = 5f;
    public float customAngleUp = -5f;
    public float customAngleDown = 5f;
    public float customAngleCenter = 0f;
    public float rayDistance = 20f;
    public float aimRotationSpeed = 120f;
    public float idlePitchAngle = 0f;
    public LayerMask enemyAimMask;

    [Header("Animation")]
    public float shotAnimationDuration = 0.1f;
    public float reloadAnimationDuration = 5f;
    public string shotAnimationStateName = "PlayerWeapon_Shoot";
    public string reloadAnimationStateName = "PlayerWeapon_Reload";
}