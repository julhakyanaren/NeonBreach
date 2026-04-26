using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(EnemyPickupDrop))]
[RequireComponent(typeof(PhotonView))]
public class JaggernautDestroy : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Jaggernaut Animator")]
    [SerializeField] private Animator jaggernautAnimator;

    [Tooltip("Jaggernaut Die parameter")]
    [SerializeField] private string jaggernautDieParameter = "Die";

    [Tooltip("Die animation duration")]
    [Range(0.1f, 10f)]
    [SerializeField] private float dieAnimationDuration = 4f;

    [Tooltip("Destroying after animation duration")]
    [Range(0f, 60f)]
    [SerializeField] private float destroyAfterDie = 4f;

    [Header("References")]
    [Tooltip("Reference of the EnemyPickupDrop script")]
    [SerializeField] private EnemyPickupDrop enemyPickupDrop;

    private PhotonView photonView;
    private bool destroying = false;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (destroyAfterDie <= 0f)
        {
            destroyAfterDie = 1f;
        }

        if (enemyPickupDrop == null)
        {
            enemyPickupDrop = GetComponent<EnemyPickupDrop>();
        }

        if (jaggernautAnimator == null)
        {
            jaggernautAnimator = GetComponent<Animator>();
        }
    }

    private IEnumerator DestroyJaggernaut(bool debugLog, int currentWaveIndex)
    {
        if (destroying)
        {
            yield break;
        }

        destroying = true;

        if (debugLog)
        {
            if (RuntimeOptions.Logging)
            {
                Debug.Log(gameObject.name + " destroyed");
            }
        }

        PlayDieAnimation();

        yield return new WaitForSeconds(dieAnimationDuration);

        if (enemyPickupDrop != null)
        {
            if (PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    enemyPickupDrop.TryDrop(currentWaveIndex);
                }
            }
            else
            {
                enemyPickupDrop.TryDrop(currentWaveIndex);
            }
        }

        yield return new WaitForSeconds(destroyAfterDie);

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(gameObject);
            }

            yield break;
        }

        Destroy(gameObject);
    }

    private void PlayDieAnimation()
    {
        if (jaggernautAnimator == null)
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("JaggernautDestroy: Animator missing.", this);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(jaggernautDieParameter))
        {
            if (RuntimeOptions.LoggingWarning)
            {
                Debug.LogWarning("JaggernautDestroy: Die parameter is null or empty.", this);
            }

            return;
        }

        jaggernautAnimator.SetTrigger(jaggernautDieParameter);
    }

    public void StartDestroy(bool debugLog, int currentWaveIndex)
    {
        if (destroying)
        {
            return;
        }

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