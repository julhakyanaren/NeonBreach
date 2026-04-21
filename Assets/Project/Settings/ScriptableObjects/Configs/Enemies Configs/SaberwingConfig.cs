using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Saberwing Config")]
public class SaberwingConfig : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Header("Target")]
    public float detectionRadius = 5f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;
    public float stopDistance = 1.2f;

    [Header("Attack")]
    public float attackRange = 1.6f;
    public float attackCooldown = 0.45f;
    public float attackDamage = 25f;
    public float attackHitRadius = 1.8f;
    public float attackSpeedCoeff = 1f;

    [Header("Score")]
    public float baseScoreReward = 80f;
    public float scoreMultiplier = 1f;
}