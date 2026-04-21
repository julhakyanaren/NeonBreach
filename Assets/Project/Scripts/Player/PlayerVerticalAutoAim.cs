using UnityEngine;

public class PlayerVerticalAutoAim : MonoBehaviour
{
    [Header("State")]
    [Tooltip("Enables or disables vertical auto aim assist.")]
    [SerializeField] private bool verticalAutoAimEnabled = true;

    [Header("References")]
    [Tooltip("Weapon pivot that rotates on local X.")]
    [SerializeField] private Transform weaponPitchPivot;

    [Tooltip("Fire point used for raycast origin and final projectile direction.")]
    [SerializeField] private Transform firePoint;

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

    [Header("Aim Settings")]
    [Tooltip("Maximum raycast distance used to search for enemy hitboxes.")]
    [SerializeField] private float rayDistance = 20f;

    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float aimRotationSpeed = 120f;

    [Tooltip("Pitch angle used when auto aim is disabled or no target is found.")]
    [SerializeField] private float idlePitchAngle = 0f;

    [Tooltip("Tag used on enemy hit colliders.")]
    [SerializeField] private string enemyHitboxTag = "EnemyHitbox";

    [Tooltip("Layer mask used to detect enemies and world geometry for aim checks.")]
    [SerializeField] private LayerMask aimMask;

    [Header("Debug")]
    [Tooltip("Draw debug rays in play mode and gizmos when selected.")]
    [SerializeField] private bool drawDebug = true;

    [Tooltip("Color used for debug rays.")]
    [SerializeField] private Color debugRayColor = new Color(0f, 0.25f, 0f, 1f);

    private float targetPitchAngle = 0f;
    private bool hasAimSolution = false;
    private float cachedAimAngle = 0f;

    public bool VerticalAutoAimEnabled
    {
        get
        {
            return verticalAutoAimEnabled;
        }
        set
        {
            verticalAutoAimEnabled = value;
        }
    }

    private void Awake()
    {
        angle = Mathf.Clamp(angle, 0f, 45f);
        upAngle = Mathf.Clamp(upAngle, -45f, 0f);
        centerAngle = Mathf.Clamp(centerAngle, -45f, 45f);
        downAngle = Mathf.Clamp(downAngle, 0f, 45f);
        rayDistance = Mathf.Max(0.1f, rayDistance);
        aimRotationSpeed = Mathf.Max(0.1f, aimRotationSpeed);
    }

    private void Update()
    {
        UpdateTargetPitch();
        RotateWeaponPitch();
    }

    private void UpdateTargetPitch()
    {
        if (!verticalAutoAimEnabled)
        {
            ClearAimSolution();
            targetPitchAngle = idlePitchAngle;
            return;
        }

        if (weaponPitchPivot == null)
        {
            ClearAimSolution();
            targetPitchAngle = idlePitchAngle;
            return;
        }

        if (firePoint == null)
        {
            ClearAimSolution();
            targetPitchAngle = idlePitchAngle;
            return;
        }

        if (hasAimSolution)
        {
            if (CheckRayForAngle(cachedAimAngle))
            {
                targetPitchAngle = cachedAimAngle;
                return;
            }
        }

        float resolvedCenterAngle;
        float resolvedUpAngle;
        float resolvedDownAngle;
        ResolveAngles(out resolvedCenterAngle, out resolvedUpAngle, out resolvedDownAngle);

        if (CheckRayForAngle(resolvedCenterAngle))
        {
            SetAimSolution(resolvedCenterAngle);
            return;
        }

        if (CheckRayForAngle(resolvedUpAngle))
        {
            SetAimSolution(resolvedUpAngle);
            return;
        }

        if (CheckRayForAngle(resolvedDownAngle))
        {
            SetAimSolution(resolvedDownAngle);
            return;
        }

        ClearAimSolution();
        targetPitchAngle = idlePitchAngle;
    }

    private void RotateWeaponPitch()
    {
        if (weaponPitchPivot == null)
        {
            return;
        }

        float currentPitch = GetCurrentWeaponPitchAngle();
        float nextPitch = Mathf.MoveTowardsAngle(
            currentPitch,
            targetPitchAngle,
            aimRotationSpeed * Time.deltaTime
        );

        Vector3 localEulerAngles = weaponPitchPivot.localEulerAngles;
        localEulerAngles.x = nextPitch;
        weaponPitchPivot.localEulerAngles = localEulerAngles;
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

    private void SetAimSolution(float angleValue)
    {
        hasAimSolution = true;
        cachedAimAngle = angleValue;
        targetPitchAngle = angleValue;
    }

    private void ClearAimSolution()
    {
        hasAimSolution = false;
        cachedAimAngle = idlePitchAngle;
    }

    private bool CheckRayForAngle(float angleValue)
    {
        if (firePoint == null)
        {
            return false;
        }

        if (weaponPitchPivot == null)
        {
            return false;
        }

        Vector3 rayOrigin = firePoint.position;
        Vector3 rayDirection = GetRayDirection(angleValue);

        bool hasHit = Physics.Raycast(
            rayOrigin,
            rayDirection,
            out RaycastHit hit,
            rayDistance,
            aimMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawDebug)
        {
            Debug.DrawRay(rayOrigin, rayDirection * rayDistance, debugRayColor);
        }

        if (!hasHit)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(enemyHitboxTag))
        {
            if (hit.collider.CompareTag(enemyHitboxTag))
            {
                return true;
            }
        }

        return false;
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

    private float GetCurrentWeaponPitchAngle()
    {
        if (weaponPitchPivot == null)
        {
            return 0f;
        }

        float rawX = weaponPitchPivot.localEulerAngles.x;
        return Mathf.DeltaAngle(0f, rawX);
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

        Gizmos.color = debugRayColor;

        DrawGizmoRay(resolvedCenterAngle);
        DrawGizmoRay(resolvedUpAngle);
        DrawGizmoRay(resolvedDownAngle);
    }

    private void DrawGizmoRay(float angleValue)
    {
        Vector3 rayOrigin = firePoint.position;
        Vector3 rayDirection = GetRayDirection(angleValue);
        Vector3 rayEnd = rayOrigin + rayDirection * rayDistance;

        Gizmos.DrawLine(rayOrigin, rayEnd);
        Gizmos.DrawWireSphere(rayEnd, 0.06f);
    }
}