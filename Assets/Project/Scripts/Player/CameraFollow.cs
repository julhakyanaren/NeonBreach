using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Target the camera follows.")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [Tooltip("Offset from the target position.")]
    [SerializeField] private Vector3 offsetPoition = new Vector3(0f, 10f, -6f);
    //[Tooltip("Offset from the target Rotation.")]
    //[SerializeField] private Vector3 offsetRotation = new Vector3(0f, 10f, -6f);

    [Header("Follow Settings")]
    [Tooltip("How fast the camera follows the target.")]
    [Range(1f, 20f)]
    [SerializeField] private float followSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offsetPoition;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        transform.LookAt(target.position);
    }
}