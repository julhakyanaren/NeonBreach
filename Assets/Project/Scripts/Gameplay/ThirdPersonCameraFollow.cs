using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Anchor transform behind the player.")]
    [SerializeField] private Transform targetAnchor;

    [Header("Rotation")]
    [Tooltip("Should camera follow rotation.")]
    [SerializeField] private bool followRotation = true;

    private void LateUpdate()
    {
        if (targetAnchor == null)
        {
            return;
        }

        transform.position = targetAnchor.position;

        if (followRotation)
        {
            Vector3 euler = targetAnchor.eulerAngles;

            transform.rotation = Quaternion.Euler(
                euler.x,
                euler.y,
                0f);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        targetAnchor = newTarget;

        if (targetAnchor == null)
        {
            return;
        }

        transform.position = targetAnchor.position;

        Vector3 euler = targetAnchor.eulerAngles;

        transform.rotation = Quaternion.Euler(
            euler.x,
            euler.y,
            0f);
    }
}