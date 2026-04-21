using UnityEngine;

public class MenuCameraRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed around X axis in degrees per second.")]
    [Range(0f, 30f)]
    [SerializeField] private float rotationSpeedX = 0f;

    [Tooltip("Rotation speed around Y axis in degrees per second.")]
    [Range(0f, 30f)]
    [SerializeField] private float rotationSpeedY = 5f;

    [Tooltip("Rotation speed around Z axis in degrees per second.")]
    [Range(0f, 30f)]
    [SerializeField] private float rotationSpeedZ = 0f;

    [Tooltip("Should the camera pivot rotate automatically.")]
    [SerializeField] private bool rotateAutomatically = true;

    private void Update()
    {
        if (!rotateAutomatically)
        {
            return;
        }

        transform.Rotate(rotationSpeedX * Time.deltaTime, rotationSpeedY * Time.deltaTime, rotationSpeedZ * Time.deltaTime, Space.World);
    }
}