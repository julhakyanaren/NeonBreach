using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyPickupDrop))]
public class HotshotDestroy : MonoBehaviour
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
    [Tooltip("Hotshot Animator")]
    [SerializeField] private Animator hotshotAnimator;

    [Tooltip("Hotshot Die parameter")]
    [SerializeField] private string hotshotDieParameter = "Die";

    [Tooltip("Hotshot Weapon Animator")]
    [SerializeField] private Animator hotshotWeaponAnimator;

    [Tooltip("Hotshot weapon Die parameter")]
    [SerializeField] private string hotshotWeaponDieParameter = "Die";

    [Tooltip("Die animation duration")]
    [Range(4f, 10f)]
    [SerializeField] private float dieAnimationDuration = 4f;

    [Tooltip("Destroying after animation duration")]
    [Range(0f, 60f)]
    [SerializeField] private float destroyAfterDie = 4f;

    [Header("References")]
    [Tooltip("Hotshot weapon visual")]
    [SerializeField] GameObject hotshotWeaponVisual;
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

    private IEnumerator HotshotDestroying(bool debugLog, int currentWaveIndex)
    {
        if (destroying)
        {
            yield break;
        }
        if (hotshotAnimator == null)
        {
            Debug.LogWarning("Hotshot animator missing");
            yield break;
        }
        if (hotshotWeaponAnimator == null)
        {
            Debug.LogWarning("Hotshot weapon animator missing");
            yield break;
        }
        if (string.IsNullOrEmpty(hotshotDieParameter))
        {
            Debug.LogWarning("Hotshot animator die parameter is null");
            yield break;
        }
        if (string.IsNullOrEmpty(hotshotWeaponDieParameter))
        {
            Debug.LogWarning("Hotshot animator weapon die parameter is null");
            yield break;
        }

        destroying = true;

        Debug.Log($"{gameObject.name} destroyed");

        hotshotAnimator.SetTrigger(hotshotDieParameter);
        hotshotWeaponAnimator.SetTrigger(hotshotWeaponDieParameter);

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionX;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
        rb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;

        yield return null;

        yield return new WaitForSeconds(dieAnimationDuration * 0.3f);

        if (hotshotWeaponVisual != null)
        {
            hotshotWeaponVisual = hotshotWeaponAnimator.gameObject;
            hotshotWeaponVisual.transform.parent = null;

            hotshotWeaponVisual.AddComponent<Rigidbody>();
            Rigidbody rbWeapon = hotshotWeaponVisual.GetComponent<Rigidbody>();

            rbWeapon.drag = 5;
            rbWeapon.angularDrag = 5f;
            rbWeapon.useGravity = false;
            rbWeapon.AddForce(
                Random.Range(axisXPower, axisXPower + 0.1f), 
                Random.Range(axisYPower, axisYPower + 0.1f), 
                Random.Range(axisZPower, axisZPower + 0.1f) * -hotshotWeaponVisual.transform.forward.z, 
                ForceMode.Impulse);

            rbWeapon.useGravity = true;

            enemyPickupDrop.TryDrop(currentWaveIndex);

            Destroy(hotshotWeaponVisual, destroyAfterDie + dieAnimationDuration * 0.7f);
        }

        yield return new WaitForSeconds(dieAnimationDuration * 0.7f);

        yield return null;

        Destroy(gameObject, destroyAfterDie);
    }

    public void StartDestroy(bool debugLog, int currentWaveIndex)
    {
        StartCoroutine(HotshotDestroying(debugLog, currentWaveIndex));
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
