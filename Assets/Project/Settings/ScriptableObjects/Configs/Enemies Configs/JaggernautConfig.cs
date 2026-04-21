using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Jaggernaut Config")]
public class JaggernautConfig : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Target")]
    public float detectionRadius = 5f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float stopDistance = 1f;
    public float pushBlockCheckDistanceOffset = 0.4f;

    [Header("Contact Damage")]
    public float contactDamage = 10f;
    public float damageCooldown = 1f;

    [Header("Health Scaling")]
    [Range(0.01f, 1f)]
    public float enrageHealthThresholdNormalized = 0.1f;

    public float minMoveSpeedCoeff = 0.75f;
    public float maxMoveSpeedCoeff = 1.35f;

    public float minRotationSpeedCoeff = 0.8f;
    public float maxRotationSpeedCoeff = 1.5f;

    public float minDamageCoeff = 0.75f;
    public float maxDamageCoeff = 1.8f;

    [Header("Sensor")]
    public float sightRayDistance = 20f;

    [Header("Score")]
    public float baseScoreReward = 80f;
    public float scoreMultiplier = 1f;

    [Header("Push Block Check")]
    public Vector3 pushBlockedBoxSize = new Vector3(1f, 0.45f, 0.9f);
    public Vector3 pushCheckOffset = new Vector3(0f, 0f, 0.5f);
    public bool blockPushWhenObstacleDetected = true;
}