using UnityEngine;

public class EnemyPushDamage : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the root Jaggernaut controller.")]
    [SerializeField] private JaggernautController jaggernautController;

    [Header("Push Settings")]
    [Tooltip("Continuous push force applied to the player while staying inside the push trigger.")]
    [Range(0f, 50f)]
    [SerializeField] private float pushForce = 25f;

    private void Reset()
    {
        jaggernautController = GetComponentInParent<JaggernautController>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (jaggernautController == null)
        {
            return;
        }

        jaggernautController.TryDealContactDamage(other);
        TryApplyPushToPlayer(other);
    }

    private void TryApplyPushToPlayer(Collider other)
    {
        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null)
        {
            return;
        }

        Vector3 pushDirection = playerMovement.transform.position - jaggernautController.transform.position;
        pushDirection.y = 0f;

        if (pushDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        pushDirection.Normalize();

        Vector3 pushStep = pushDirection * pushForce * Time.fixedDeltaTime;
        playerMovement.AddExternalPush(pushStep);
    }
}