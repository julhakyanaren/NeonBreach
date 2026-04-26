using System.Collections.Generic;
using UnityEngine;

public class EnemyLineOfSightSensor : MonoBehaviour
{

    [Header("Ray Origins")]
    [Tooltip("Points from which line of sight rays will be cast.")]
    [SerializeField] private List<Transform> raysOriginPosition = new List<Transform>();

    [Header("Settings")]
    [Tooltip("Maximum distance used for line of sight ray checks.")]
    [SerializeField] private float rayDistance = 10f;

    [Header("Obstacle Detection")]
    [Tooltip("Layers that block visibility to the target.")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Vertical offset applied to the target point.")]
    [SerializeField] private float targetHeightOffset = 1f;

    [Header("Debug")]
    [Tooltip("Draw gizmos")]
    [SerializeField] private bool drawGizmos = true;

    public float RayDistance
    {
        get
        {
            return rayDistance;
        }
        set
        {
            if (value < 1f)
            {
                value = 1f;
            }
            rayDistance = value;
        }
    }

    private void Awake()
    {
        if (raysOriginPosition == null || raysOriginPosition.Count == 0)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("No origins positions for raycast");
            }
        }
    }

    public bool CanSeeTarget(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return false;
        }

        if (raysOriginPosition == null || raysOriginPosition.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < raysOriginPosition.Count; i++)
        {
            Transform rayOrigin = raysOriginPosition[i];

            if (rayOrigin == null)
            {
                return false;
            }

            if (!CheckRay(rayOrigin, targetTransform))
            {
                return false;
            }
        }

        return true;
    }

    private bool CheckRay(Transform rayOrigin, Transform targetTransform)
    {
        if (rayOrigin == null || targetTransform == null)
        {
            return false;
        }

        Vector3 origin = rayOrigin.position;
        Vector3 targetPoint = targetTransform.position + Vector3.up * targetHeightOffset;

        Vector3 directionToTarget = targetPoint - origin;
        float distanceToTarget = directionToTarget.magnitude;

        if (distanceToTarget <= 0.001f)
        {
            return true;
        }

        if (distanceToTarget > RayDistance)
        {
            return false;
        }

        directionToTarget.Normalize();

        if (Physics.Raycast(origin, directionToTarget, distanceToTarget, obstacleMask))
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        if (raysOriginPosition == null || raysOriginPosition.Count == 0)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0.5f, 0f, 1f);

        for (int i = 0; i < raysOriginPosition.Count; i++)
        {
            Transform rayOrigin = raysOriginPosition[i];

            if (rayOrigin == null)
            {
                continue;
            }

            Gizmos.DrawSphere(rayOrigin.position, 0.08f);
            Gizmos.DrawRay(rayOrigin.position, transform.forward * RayDistance);
        }
    }
}