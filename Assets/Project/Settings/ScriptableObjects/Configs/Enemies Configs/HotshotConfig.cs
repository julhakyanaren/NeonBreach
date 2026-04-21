using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Hotshot Config")]
public class HotshotConfig : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 30f;

    [Header("Detection")]
    public float detectionRadius = 6f;
    public float targetRefreshInterval = 0.25f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 8f;
    public float minAttackDistance = 4f;
    public float maxAttackDistance = 5f;
    public float movementDeadZone = 0.15f;

    [Header("Aim")]
    public float aimRotationSpeed = 120f;
    public float aimTolerance = 1.5f;
    public float idlePitchAngle = 0f;
    public bool resetPitchWhenCannotAttack = true;

    [Header("Shooting")]
    public float shotsPerMinute = 120f;
    public float shootFacingThreshold = 0.9f;
    public int magazineSize = 6;

    [Header("Animation Durations")]
    public float baseShootDuration = 1f;
    public float baseReloadDuration = 5f;
    public float reloadDuration = 5f;

    [Header("Aim Sensor")]
    public bool useSymmetricalValues = true;
    public float angle = 10f;
    public float upAngle = -10f;
    public float centerAngle = 0f;
    public float downAngle = 10f;

    public bool useControllerAttackDistance = true;
    public float aimRayDistance = 20f;
    public LayerMask aimTargetMask;

    [Header("Score")]
    public float baseScoreReward = 80f;
    public float scoreMultiplier = 1f;
}