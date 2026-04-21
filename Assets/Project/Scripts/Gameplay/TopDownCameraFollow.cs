using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target transform that the camera should follow.")]
    [SerializeField] private Transform target;

    [Header("Camera Angle")]
    [Tooltip("Vertical camera angle in degrees.")]
    [SerializeField] private float rotationX = 50f;

    [Tooltip("Horizontal camera angle in degrees.")]
    [SerializeField] private float rotationY = 45f;

    [Header("Camera Distance")]
    [Tooltip("Vertical height of the camera above the target.")]
    [SerializeField] private float height = 7f;

    [Tooltip("Backward distance from the look point.")]
    [SerializeField] private float distance = 8f;

    [Header("Framing")]
    [Tooltip("How far forward on the ground plane the camera should look.")]
    [SerializeField] private float lookAhead = 3f;

    [Header("Follow Settings")]
    [Tooltip("Follow speed (not time). Recommended range: 5 - 12.")]
    [SerializeField] private float followSpeed = 8f;

    [Header("Debug")]
    [Tooltip("If enabled, the camera snaps instantly without smoothing.")]
    [SerializeField] private bool instantFollow = false;

    private Rigidbody targetRigidbody;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = GetTargetPosition();

        Quaternion cameraRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        Vector3 forwardOnPlane = cameraRotation * Vector3.forward;
        forwardOnPlane.y = 0f;
        forwardOnPlane.Normalize();

        Vector3 lookPoint = targetPosition + forwardOnPlane * lookAhead;

        Vector3 desiredPosition = lookPoint;
        desiredPosition -= forwardOnPlane * distance;
        desiredPosition += Vector3.up * height;

        if (instantFollow)
        {
            transform.position = desiredPosition;
        }
        else
        {
            float t = followSpeed * Time.deltaTime;

            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                t);
        }

        transform.rotation = cameraRotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        CacheTargetRigidbody();

        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = GetTargetPosition();

        Quaternion cameraRotation = Quaternion.Euler(rotationX, rotationY, 0f);

        Vector3 forwardOnPlane = cameraRotation * Vector3.forward;
        forwardOnPlane.y = 0f;
        forwardOnPlane.Normalize();

        Vector3 lookPoint = targetPosition + forwardOnPlane * lookAhead;

        Vector3 desiredPosition = lookPoint;
        desiredPosition -= forwardOnPlane * distance;
        desiredPosition += Vector3.up * height;

        transform.position = desiredPosition;
        transform.rotation = cameraRotation;
    }

    private void CacheTargetRigidbody()
    {
        targetRigidbody = null;

        if (target == null)
        {
            return;
        }

        targetRigidbody = target.GetComponent<Rigidbody>();
    }

    private Vector3 GetTargetPosition()
    {
        if (targetRigidbody != null)
        {
            return targetRigidbody.position;
        }

        return target.position;
    }
}