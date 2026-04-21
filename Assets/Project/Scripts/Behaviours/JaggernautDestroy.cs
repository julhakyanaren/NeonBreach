using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(EnemyPickupDrop))]
public class JaggernautDestroy : MonoBehaviour
{
    [Header("Jaggernaut parts")]
    [Tooltip("Jaggernaut ram game object")]
    [SerializeField] private GameObject jaggernautRam;

    [Tooltip("Jaggernaut body game object")]
    [SerializeField] private GameObject jaggernautBody;

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

    [Tooltip("Blade impuls percent")]
    [Range(1f, 100f)]
    [Serialize] private float bladeImpulsePercent = 75f;

    [Header("Animation")]
    [Tooltip("Jaggernaut Animator")]
    [SerializeField] private Animator jaggernautAnimator;

    [Tooltip("Jaggernaut Die parameter")]
    [SerializeField] private string jaggernautDieParameter = "Die";

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

    private IEnumerator DestroyJaggernaut(bool debugLog, int currentWaveIndex)
    {
        if (destroying)
        {
            yield break;
        }
        if (jaggernautAnimator == null)
        {
            Debug.LogWarning("Jaggernaut animator missing");
            yield break;
        }
        if (string.IsNullOrEmpty(jaggernautDieParameter))
        {
            Debug.LogWarning("Jaggernaut animator die parameter is null");
            yield break;
        }
        destroying = true;

        if (debugLog)
        {
            Debug.Log($"{gameObject.name} destroyed");
        }

        jaggernautAnimator.SetTrigger(jaggernautDieParameter);

        yield return null;

        yield return new WaitForSeconds(dieAnimationDuration);
        jaggernautAnimator.enabled = false;

        Rigidbody rbRam = jaggernautRam.GetComponent<Rigidbody>();

        jaggernautBody.AddComponent<Rigidbody>();
        Rigidbody jaggernautRigidbody = jaggernautBody.GetComponent<Rigidbody>();
        jaggernautRam.AddComponent<BoxCollider>();
        

        jaggernautRigidbody.useGravity = true;
        rbRam.useGravity = true;

        jaggernautRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionX;
        jaggernautRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionY;
        jaggernautRigidbody.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        jaggernautRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationX;
        jaggernautRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        jaggernautRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

        jaggernautRigidbody.isKinematic = false;
        rbRam.isKinematic = false;

        jaggernautRigidbody.drag = 5;
        jaggernautRigidbody.angularDrag = 5f;

        jaggernautRigidbody.AddForce(
            Random.Range(axisXPower, axisXPower + 0.1f),
            Random.Range(axisYPower, axisYPower + 0.1f),
            Random.Range(axisZPower, axisZPower + 0.1f) * -gameObject.transform.forward.z,
            ForceMode.Impulse);

        axisXPower *= rbRam.mass * bladeImpulsePercent / 100;
        axisYPower *= rbRam.mass * bladeImpulsePercent / 100;
        axisZPower *= rbRam.mass * bladeImpulsePercent / 100;

        rbRam.AddForce(
            Random.Range(axisXPower, axisXPower + 0.1f),
            Random.Range(axisYPower, axisYPower + 0.1f),
            Random.Range(axisZPower, axisZPower + 0.1f) * -gameObject.transform.forward.z,
            ForceMode.Impulse);

        yield return null;

        enemyPickupDrop.TryDrop(currentWaveIndex);

        Destroy(gameObject, destroyAfterDie);
    }
    public void StartDestroy(bool debugLog, int currentWaveIndex)
    {
        StartCoroutine(DestroyJaggernaut(debugLog, currentWaveIndex));
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
