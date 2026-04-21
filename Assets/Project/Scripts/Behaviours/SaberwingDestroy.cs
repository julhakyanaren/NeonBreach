using Photon.Pun.Demo.Cockpit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyPickupDrop))]
public class SaberwingDestroy : MonoBehaviour
{
    [Header("Destroy Settings")]
    [Tooltip("Impuls Power axis X")]
    [Range(0f, 1f)]
    [SerializeField] private float axisXPower = 0.5f;

    [Tooltip("Impuls Power axis Y")]
    [Range(0f, 1f)]
    [SerializeField] private float axisYPower = 0.5f;

    [Tooltip("Impuls Power axis Z")]
    [Range(0f, 10f)]
    [SerializeField] private float axisZPower = 5f;

    [Header("Animation")]
    [Tooltip("Saberwing Animator")]
    [SerializeField] private Animator saberwingAnimator;

    [Tooltip("Saberwing Die parameter")]
    [SerializeField] private string saberwingDieParameter = "Die";

    [Tooltip("Die animation duration")]
    [Range(4f, 10f)]
    [SerializeField] private float dieAnimationDuration = 4f;

    [Tooltip("Destroying after animation duration")]
    [Range(0f, 60f)]
    [SerializeField] private float destroyAfterDie = 4f;

    [Header("References")]
    [Tooltip("Reference of the EnemyPickupDrop script")]
    [SerializeField] private EnemyPickupDrop enemyPickupDrop;

    private bool destroying = false;


    private void Awake()
    {
        if (destroyAfterDie <= 0)
        {
            destroyAfterDie = 1f;
        }
        if (enemyPickupDrop == null)
        {
            enemyPickupDrop = GetComponent<EnemyPickupDrop>();
        }
    }

    private IEnumerator DestroySaberwing(bool debugLog, int currentWaveIndex)
    {
        if (destroying)
        {
            yield break;
        }
        if (saberwingAnimator == null)
        {
            Debug.LogWarning("Saberwing animator missing");
            yield break;
        }
        if (string.IsNullOrEmpty(saberwingDieParameter))
        {
            Debug.LogWarning("Saberwing animator die parameter is null");
            yield break;
        }
        destroying = true;

        if (debugLog)
        {
            Debug.Log($"{gameObject.name} destroyed");
        }

        saberwingAnimator.SetTrigger(saberwingDieParameter);

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionX;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        rb.isKinematic = false;

        rb.drag = 5;
        rb.angularDrag = 5f;
        rb.AddForce(
            Random.Range(axisXPower, axisXPower + 0.1f),
            Random.Range(axisYPower, axisYPower + 0.1f),
            Random.Range(axisZPower, axisZPower + 0.1f) * -gameObject.transform.forward.z,
            ForceMode.Impulse);

        yield return null;

        yield return new WaitForSeconds(dieAnimationDuration);

        enemyPickupDrop.TryDrop(currentWaveIndex);

        Destroy(gameObject, destroyAfterDie);
    }
    public void StartDestroy(bool debugLog, int currentWaveIndex)
    {
        StartCoroutine(DestroySaberwing(debugLog, currentWaveIndex));
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
