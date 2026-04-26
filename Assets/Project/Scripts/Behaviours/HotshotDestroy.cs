using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(EnemyPickupDrop))]
[RequireComponent(typeof(PhotonView))]
public class HotshotDestroy : MonoBehaviour
{
    [Header("Destroy Settings")]
    [Tooltip("Impuls Power axis X.")]
    [Range(0f, 1f)]
    [SerializeField] private float axisXPower = 0.5f;

    [Tooltip("Impuls Power axis Y.")]
    [Range(0f, 1f)]
    [SerializeField] private float axisYPower = 0.5f;

    [Tooltip("Impuls Power axis Z.")]
    [Range(0f, 10f)]
    [SerializeField] private float axisZPower = 5f;

    [Header("Animation")]
    [Tooltip("Hotshot Animator.")]
    [SerializeField] private Animator hotshotAnimator;

    [Tooltip("Hotshot Die parameter.")]
    [SerializeField] private string hotshotDieParameter = "Die";

    [Tooltip("Hotshot Weapon Animator.")]
    [SerializeField] private Animator hotshotWeaponAnimator;

    [Tooltip("Hotshot weapon Die parameter.")]
    [SerializeField] private string hotshotWeaponDieParameter = "Die";

    [Tooltip("Die animation duration.")]
    [Range(4f, 10f)]
    [SerializeField] private float dieAnimationDuration = 4f;

    [Tooltip("Destroying delay after die animation.")]
    [Range(0f, 60f)]
    [SerializeField] private float destroyAfterDie = 4f;

    [Header("References")]
    [Tooltip("Hotshot weapon visual object.")]
    [SerializeField] private GameObject hotshotWeaponVisual;

    [Tooltip("Reference of the EnemyPickupDrop script.")]
    [SerializeField] private EnemyPickupDrop enemyPickupDrop;

    private PhotonView photonView;
    private bool destroying = false;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

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
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("Hotshot animator missing");
            }
            
            yield break;
        }

        if (hotshotWeaponAnimator == null)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("Hotshot weapon animator missing");
            }
            
            yield break;
        }

        if (string.IsNullOrEmpty(hotshotDieParameter))
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("Hotshot animator die parameter is null");
            }
            yield break;
        }

        if (string.IsNullOrEmpty(hotshotWeaponDieParameter))
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("Hotshot animator weapon die parameter is null");
            }
            
            yield break;
        }

        destroying = true;

        if (debugLog)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log($"{gameObject.name} destroyed");
            }            
        }

        hotshotAnimator.SetTrigger(hotshotDieParameter);
        hotshotWeaponAnimator.SetTrigger(hotshotWeaponDieParameter);

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = true;

            rb.constraints &= ~RigidbodyConstraints.FreezePositionX;
            rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationX;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationY;
            rb.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        }

        yield return null;

        yield return new WaitForSeconds(dieAnimationDuration * 0.3f);

        if (hotshotWeaponVisual != null)
        {
            hotshotWeaponVisual = hotshotWeaponAnimator.gameObject;
            hotshotWeaponVisual.transform.parent = null;

            Rigidbody rbWeapon = hotshotWeaponVisual.GetComponent<Rigidbody>();

            if (rbWeapon == null)
            {
                rbWeapon = hotshotWeaponVisual.AddComponent<Rigidbody>();
            }

            rbWeapon.drag = 5;
            rbWeapon.angularDrag = 5f;
            rbWeapon.useGravity = false;

            rbWeapon.AddForce(
                Random.Range(axisXPower, axisXPower + 0.1f),
                Random.Range(axisYPower, axisYPower + 0.1f),
                Random.Range(axisZPower, axisZPower + 0.1f) * -hotshotWeaponVisual.transform.forward.z,
                ForceMode.Impulse);

            rbWeapon.useGravity = true;

            if (PhotonNetwork.InRoom == false)
            {
                enemyPickupDrop.TryDrop(currentWaveIndex);
                Destroy(hotshotWeaponVisual, destroyAfterDie + dieAnimationDuration * 0.7f);
            }
            else
            {
                if (PhotonNetwork.IsMasterClient == true)
                {
                    enemyPickupDrop.TryDrop(currentWaveIndex);
                }
            }
        }

        yield return new WaitForSeconds(dieAnimationDuration * 0.7f);

        yield return null;

        if (PhotonNetwork.InRoom == true)
        {
            if (PhotonNetwork.IsMasterClient == true)
            {
                yield return new WaitForSeconds(destroyAfterDie);
                PhotonNetwork.Destroy(gameObject);
            }

            yield break;
        }

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