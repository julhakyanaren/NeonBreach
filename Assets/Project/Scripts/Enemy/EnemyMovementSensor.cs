using System.Collections.Generic;
using UnityEngine;

public class EnemyMovementSensor : MonoBehaviour
{
    [Header("Check Points")]
    [Tooltip("Points from which movement blockage checks will be performed.")]
    [SerializeField] private List<Transform> movementCheckPoints = new List<Transform>();

    [Header("Check Settings")]
    [Tooltip("Maximum distance used to check whether movement is blocked.")]
    [SerializeField] private float checkDistance = 0.75f;

    [Tooltip("Radius of each sphere cast used for movement blockage checks.")]
    [SerializeField] private float checkRadius = 0.2f;

    [Tooltip("Small offset applied along movement direction so the cast does not start inside the body.")]
    [SerializeField] private float castStartOffset = 0.05f;

    [Header("Obstacle Detection")]
    [Tooltip("Layers that should block enemy movement.")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Debug")]
    [Tooltip("Draw debug rays and spheres for movement checks.")]
    [SerializeField] private bool drawDebug = true;

    public bool CanMoveInDirection(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        if (movementCheckPoints == null || movementCheckPoints.Count == 0)
        {
            return true;
        }

        Vector3 normalizedDirection = moveDirection.normalized;

        for (int i = 0; i < movementCheckPoints.Count; i++)
        {
            Transform checkPoint = movementCheckPoints[i];

            if (checkPoint == null)
            {
                continue;
            }

            if (IsPointBlocked(checkPoint.position, normalizedDirection))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsPointBlocked(Vector3 pointPosition, Vector3 moveDirection)
    {
        Vector3 castOrigin = pointPosition + moveDirection * castStartOffset;

        bool isBlocked = Physics.SphereCast(
            castOrigin,
            checkRadius,
            moveDirection,
            out RaycastHit hit,
            checkDistance,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );

        return isBlocked;
    }

    private void Awake()
    {
        checkDistance = Mathf.Max(0.05f, checkDistance);
        checkRadius = Mathf.Max(0.01f, checkRadius);
        castStartOffset = Mathf.Max(0f, castStartOffset);

        if (movementCheckPoints == null || movementCheckPoints.Count == 0)
        {
            Debug.LogWarning($"{name}: HotshotMovementSensor has no movement check points assigned.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        if (movementCheckPoints == null || movementCheckPoints.Count == 0)
        {
            return;
        }

        Vector3 debugDirection = transform.forward;

        Gizmos.color = new Color(0f, 1f, 0.4f, 1f);

        for (int i = 0; i < movementCheckPoints.Count; i++)
        {
            Transform checkPoint = movementCheckPoints[i];

            if (checkPoint == null)
            {
                continue;
            }

            Vector3 startPosition = checkPoint.position + debugDirection * castStartOffset;
            Vector3 endPosition = startPosition + debugDirection * checkDistance;

            Gizmos.DrawWireSphere(startPosition, checkRadius);
            Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawWireSphere(endPosition, checkRadius);
        }
    }
}