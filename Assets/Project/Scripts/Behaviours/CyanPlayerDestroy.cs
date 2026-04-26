using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CyanPlayerDestroy : MonoBehaviour
{
    [Tooltip("Player detachable parts")]
    [SerializeField] private List<GameObject> playerParts;

    [Header("Destroying Options")]
    [Tooltip("Minimum horizontal explosion force.")]
    [Range(0.1f, 5f)]
    [SerializeField] private float minPower = 1f;

    [Tooltip("Maximum horizontal explosion force.")]
    [Range(0.1f, 10f)]
    [SerializeField] private float maxPower = 3f;

    [Tooltip("Additional multiplier for explosion force.")]
    [Range(1f, 5f)]
    [SerializeField] private float powerCoeff = 2.5f;

    [Tooltip("Minimum upward force.")]
    [Range(0.1f, 10f)]
    [SerializeField] private float minUpwardPower = 1f;

    [Tooltip("Maximum upward force.")]
    [Range(0.1f, 10f)]
    [SerializeField] private float maxUpwardPower = 3f;

    [Header("Animations")]
    [Tooltip("Delay before physical destruction starts.")]
    [Range(0.1f, 10f)]
    [SerializeField] private float destroyDelay = 7f;

    [Tooltip("Player animator.")]
    [SerializeField] private Animator playerAnimator;

    [Tooltip("Player die trigger.")]
    [SerializeField] private string playerDieTrigger = "Die";

    [Tooltip("Weapon animator.")]
    [SerializeField] private Animator weaponAnimator;

    [Tooltip("Weapon die trigger.")]
    [SerializeField] private string weaponDieTrigger = "Die";

    [Header("Debug")]
    [Tooltip("Set true to trigger destruction.")]
    [SerializeField] private bool destroy = false;

    private Rigidbody playerRB;

    private bool destroyed = false;

    private void Start()
    {
        playerRB = GetComponent<Rigidbody>();
        if (playerRB == null)
        {
            if (RuntimeOptions.LoggingError)
            {
                Debug.LogError("Player rigidbody missing");
            }
            return;
        }
    }

    public IEnumerator DestroyCyanPlayer()
    {
        destroyed = true;

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(playerDieTrigger);
        }

        yield return null;

        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger(weaponDieTrigger);
        }

        yield return new WaitForSeconds(destroyDelay);

        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = false;
        }

        playerRB.isKinematic = true;

        for (int p = 0; p < playerParts.Count; p++)
        {
            GameObject part = playerParts[p];
            part.AddComponent<Rigidbody>();
            Rigidbody rb = part.GetComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.drag = 1f;
            rb.angularDrag = 0.1f;
            rb.mass = 1f;

            AddForceToRB(rb);

            playerRB.isKinematic = false;
            playerRB.useGravity = true;
            playerRB.freezeRotation = false;
            playerRB.constraints &= ~RigidbodyConstraints.FreezePositionX;
            playerRB.constraints &= ~RigidbodyConstraints.FreezePositionY;
            playerRB.constraints &= ~RigidbodyConstraints.FreezePositionZ;
            playerRB.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            playerRB.drag = 1f;
            playerRB.angularDrag = 0.1f;
            playerRB.mass = 1f;

            AddForceToRB(playerRB);
        }
    }

    private void AddForceToRB(Rigidbody rb)
    {
        if (rb == null)
        {
            return;
        }
        float forceX = Random.Range(-maxPower, maxPower) * powerCoeff;
        float forceY = Random.Range(minUpwardPower, maxUpwardPower) * powerCoeff;
        float forceZ = Random.Range(-maxPower, maxPower) * powerCoeff;

        if (Mathf.Abs(forceX) < minPower)
        {
            if (forceX < 0f)
            {
                forceX = -minPower;
            }
            else
            {
                forceX = minPower;
            }
        }

        if (Mathf.Abs(forceZ) < minPower)
        {
            if (forceZ < 0f)
            {
                forceZ = -minPower;
            }
            else
            {
                forceZ = minPower;
            }
        }

        Vector3 randomForce = new Vector3(forceX, forceY, forceZ);
        rb.AddForce(randomForce, ForceMode.Impulse);
    }

    private void Update()
    {
        if (!destroy)
        {
            return;
        }

        if (destroyed)
        {
            return;
        }

        StartCoroutine(DestroyCyanPlayer());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

}