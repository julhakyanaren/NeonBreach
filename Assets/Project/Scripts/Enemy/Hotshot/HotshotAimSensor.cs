using UnityEngine;

public class HotshotAimSensor : MonoBehaviour
{
    [Header("Config source")]
    [Tooltip("Hotshot config with controller settings.")]
    [SerializeField] private HotshotConfig configHotshotSO;

    [Header("References")]
    [Tooltip("Fire point from which aim rays are cast.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Weapon visual pivot that is actually rotated on local X.")]
    [SerializeField] private Transform weaponPitchPivot;

    [Tooltip("Optional Hotshot controller reference used to read attack distance.")]
    [SerializeField] private HotshotController hotshotController;

    [Header("Aim Angles")]
    [Tooltip("If enabled, center angle becomes 0, upper angle becomes -angle, and lower angle becomes +angle.")]
    [SerializeField] private bool useSymmetricalValues = true;

    [Tooltip("Extreme angle value used for symmetrical mode.")]
    [Range(0f, 45f)]
    [SerializeField] private float angle = 10f;

    [Header("Custom Aim Angles")]
    [Tooltip("Local X angle used to pitch weapon upward for the upper ray.")]
    [Range(-45f, 0f)]
    [SerializeField] private float upAngle = -10f;

    [Tooltip("Center local pitch angle used for the middle ray.")]
    [Range(-45f, 45f)]
    [SerializeField] private float centerAngle = 0f;

    [Tooltip("Local X angle used to pitch weapon downward for the lower ray.")]
    [Range(0f, 45f)]
    [SerializeField] private float downAngle = 10f;

    [Header("Ray Settings")]
    [Tooltip("If enabled, ray distance is taken from HotshotController max attack distance with fire point forward offset compensation.")]
    [SerializeField] private bool useControllerAttackDistance = true;

    [Tooltip("Maximum distance used for aim raycasts when local ray distance mode is active.")]
    [SerializeField] private float rayDistance = 20f;

    [Tooltip("Layer mask used to detect valid targets for aim checks.")]
    [SerializeField] private LayerMask targetMask;

    [Header("Debug")]
    [Tooltip("Draw debug aim rays in play mode and gizmos when object is selected.")]
    [SerializeField] private bool drawDebug = true;

    [Tooltip("Color used for debug aim rays.")]
    [SerializeField] private Color debugRayColor = new Color(0f, 0.25f, 0f, 1f);

    private void Reset()
    {
        if (hotshotController == null)
        {
            hotshotController = GetComponent<HotshotController>();
        }
    }

    private void Awake()
    {
        if (configHotshotSO != null)
        {
            useSymmetricalValues = configHotshotSO.useSymmetricalValues;

            angle = Mathf.Clamp(configHotshotSO.angle, 0f, 45f);
            upAngle = Mathf.Clamp(configHotshotSO.upAngle, -45f, 0f);
            centerAngle = Mathf.Clamp(configHotshotSO.centerAngle, -45f, 45f);
            downAngle = Mathf.Clamp(configHotshotSO.downAngle, 0f, 45f);

            useControllerAttackDistance = configHotshotSO.useControllerAttackDistance;
            rayDistance = Mathf.Max(0.1f, configHotshotSO.aimRayDistance);
            targetMask = configHotshotSO.aimTargetMask;
        }
        else
        {
            angle = Mathf.Clamp(angle, 0f, 45f);
            upAngle = Mathf.Clamp(upAngle, -45f, 0f);
            centerAngle = Mathf.Clamp(centerAngle, -45f, 45f);
            downAngle = Mathf.Clamp(downAngle, 0f, 45f);
            rayDistance = Mathf.Max(0.1f, rayDistance);
        }

        if (hotshotController == null)
        {
            hotshotController = GetComponent<HotshotController>();
        }

        if (firePoint == null)
        {
            Debug.LogWarning($"{name}: HotshotAimSensor has no FirePoint assigned.");
        }

        if (weaponPitchPivot == null)
        {
            Debug.LogWarning($"{name}: HotshotAimSensor has no Weapon Pitch Pivot assigned.");
        }
    }

    public bool TryGetAimAngle(Transform targetTransform, out float aimAngle)
    {
        aimAngle = 0f;

        if (firePoint == null)
        {
            return false;
        }

        if (weaponPitchPivot == null)
        {
            return false;
        }

        if (targetTransform == null)
        {
            return false;
        }

        float resolvedCenterAngle;
        float resolvedUpAngle;
        float resolvedDownAngle;
        ResolveAngles(out resolvedCenterAngle, out resolvedUpAngle, out resolvedDownAngle);

        if (CheckRayForAngle(targetTransform, resolvedCenterAngle))
        {
            aimAngle = resolvedCenterAngle;
            return true;
        }

        if (CheckRayForAngle(targetTransform, resolvedUpAngle))
        {
            aimAngle = resolvedUpAngle;
            return true;
        }

        if (CheckRayForAngle(targetTransform, resolvedDownAngle))
        {
            aimAngle = resolvedDownAngle;
            return true;
        }

        return false;
    }

    public bool IsAngleStillValid(Transform targetTransform, float angleValue)
    {
        if (firePoint == null)
        {
            return false;
        }

        if (weaponPitchPivot == null)
        {
            return false;
        }

        if (targetTransform == null)
        {
            return false;
        }

        return CheckRayForAngle(targetTransform, angleValue);
    }

    private void ResolveAngles(out float resolvedCenterAngle, out float resolvedUpAngle, out float resolvedDownAngle)
    {
        if (useSymmetricalValues)
        {
            resolvedCenterAngle = 0f;
            resolvedUpAngle = -angle;
            resolvedDownAngle = angle;
            return;
        }

        resolvedCenterAngle = centerAngle;
        resolvedUpAngle = upAngle;
        resolvedDownAngle = downAngle;
    }

    private float GetResolvedRayDistance()
    {
        if (useControllerAttackDistance)
        {
            if (hotshotController != null)
            {
                Vector3 fireOffset = firePoint.position - hotshotController.transform.position;
                float forwardOffset = Vector3.Dot(fireOffset, hotshotController.transform.forward);
                forwardOffset = Mathf.Max(0f, forwardOffset);

                float resolvedDistance = hotshotController.MaxAttackDistance - forwardOffset;
                return Mathf.Max(0.1f, resolvedDistance);
            }
        }

        return Mathf.Max(0.1f, rayDistance);
    }

    private bool CheckRayForAngle(Transform targetTransform, float angleValue)
    {
        Vector3 rayOrigin = firePoint.position;
        Vector3 rayDirection = GetRayDirection(angleValue);
        float resolvedRayDistance = GetResolvedRayDistance();

        bool hasHit = Physics.Raycast(
            rayOrigin,
            rayDirection,
            out RaycastHit hit,
            resolvedRayDistance,
            targetMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawDebug)
        {
            Debug.DrawRay(rayOrigin, rayDirection * resolvedRayDistance, debugRayColor);
        }

        if (!hasHit)
        {
            return false;
        }

        return IsTargetHit(hit.transform, targetTransform);
    }

    private Vector3 GetRayDirection(float angleValue)
    {
        if (weaponPitchPivot == null)
        {
            return Vector3.forward;
        }

        Transform pitchParent = weaponPitchPivot.parent;

        Quaternion parentWorldRotation = Quaternion.identity;

        if (pitchParent != null)
        {
            parentWorldRotation = pitchParent.rotation;
        }

        Vector3 pivotLocalEuler = weaponPitchPivot.localEulerAngles;
        pivotLocalEuler.x = angleValue;

        Quaternion simulatedPivotWorldRotation = parentWorldRotation * Quaternion.Euler(pivotLocalEuler);
        Vector3 firePointLocalForward = firePoint.localRotation * Vector3.forward;
        Vector3 rayDirection = simulatedPivotWorldRotation * firePointLocalForward;

        return rayDirection.normalized;
    }

    private bool IsTargetHit(Transform hitTransform, Transform targetTransform)
    {
        if (hitTransform == null || targetTransform == null)
        {
            return false;
        }

        if (hitTransform == targetTransform)
        {
            return true;
        }

        if (hitTransform.IsChildOf(targetTransform))
        {
            return true;
        }

        if (targetTransform.IsChildOf(hitTransform))
        {
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        if (firePoint == null)
        {
            return;
        }

        if (weaponPitchPivot == null)
        {
            return;
        }

        float resolvedCenterAngle;
        float resolvedUpAngle;
        float resolvedDownAngle;
        ResolveAngles(out resolvedCenterAngle, out resolvedUpAngle, out resolvedDownAngle);

        float resolvedRayDistance = GetResolvedRayDistance();

        Gizmos.color = debugRayColor;

        DrawGizmoRay(resolvedCenterAngle, resolvedRayDistance);
        DrawGizmoRay(resolvedUpAngle, resolvedRayDistance);
        DrawGizmoRay(resolvedDownAngle, resolvedRayDistance);
    }

    private void DrawGizmoRay(float angleValue, float distance)
    {
        Vector3 rayOrigin = firePoint.position;
        Vector3 rayDirection = GetRayDirection(angleValue);
        Vector3 rayEnd = rayOrigin + rayDirection * distance;

        Gizmos.DrawLine(rayOrigin, rayEnd);
        Gizmos.DrawWireSphere(rayEnd, 0.06f);
    }
}