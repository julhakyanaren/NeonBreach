using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WwiseGameplaySFXController))]
public class PickupController : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Type of this pickup.")]
    [SerializeField] private PickupType pickupType;

    [Tooltip("Value of the pickup effect.")]
    [SerializeField] private float value = 10f;

    [Tooltip("Duration of the pickup effect in seconds.")]
    [SerializeField] private float duration = 5f;

    [Header("Animation")]
    [Tooltip("Animator on visual object.")]
    [SerializeField] private Animator pickupAnimator;

    [Tooltip("Trigger name for pickup animation.")]
    [SerializeField] private string pickupTriggerName = "PickUp";

    [Tooltip("Delay before destroying pickup.")]
    [SerializeField] private float destroyDelay = 0.8f;

    [Header("References")]
    [Tooltip("Trigger collider.")]
    [SerializeField] private Collider triggerCollider;

    [Tooltip("Reference to the Wwise gameplay SFX controller.")]
    [SerializeField] private WwiseGameplaySFXController gameplaySfxController;

    [Tooltip("GameObject used as audio source for pickup SFX.")]
    [SerializeField] private GameObject sfxSource;

    [Tooltip("PhotonView used for pickup network synchronization.")]
    [SerializeField] private PhotonView photonView;

    [Header("Runtime")]
    [Tooltip("Whether pickup was already collected.")]
    [SerializeField] private bool isPickedUp = false;

    [Tooltip("Whether PowerUp SFX is currently active.")]
    [SerializeField] private bool isPowerUpSfxPlaying = false;

    private Coroutine destroyCoroutine;

    private void Reset()
    {
        triggerCollider = GetComponent<Collider>();
        gameplaySfxController = GetComponent<WwiseGameplaySFXController>();
        photonView = GetComponent<PhotonView>();

        if (pickupAnimator == null)
        {
            pickupAnimator = GetComponentInChildren<Animator>();
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        if (gameplaySfxController == null)
        {
            gameplaySfxController = GetComponent<WwiseGameplaySFXController>();
        }

        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (pickupAnimator == null)
        {
            pickupAnimator = GetComponentInChildren<Animator>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject;
        }
    }

    private void OnEnable()
    {
        StaticEvents.PauseOpened += HandlePauseOpened;
        StaticEvents.PauseClosed += HandlePauseClosed;

        isPickedUp = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }

    private void OnDisable()
    {
        StaticEvents.PauseOpened -= HandlePauseOpened;
        StaticEvents.PauseClosed -= HandlePauseClosed;

        StopPowerUpSfx();

        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            destroyCoroutine = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPickedUp)
        {
            return;
        }

        PlayerBuffReceiver receiver = other.GetComponentInParent<PlayerBuffReceiver>();

        if (receiver == null)
        {
            return;
        }

        if (RuntimeOptions.MultiplayerMode)
        {
            TryPickupMultiplayer(other, receiver);
            return;
        }

        TryPickupSingleplayer(receiver);
    }

    private void TryPickupSingleplayer(PlayerBuffReceiver receiver)
    {
        isPickedUp = true;

        ApplyPickup(receiver);
        AddPickupStats();

        PlayPowerUpSfx();
        PlayAnimation();
        DisableCollider();

        destroyCoroutine = StartCoroutine(DestroyRoutineSingleplayer());
    }

    private void TryPickupMultiplayer(Collider other, PlayerBuffReceiver receiver)
    {
        PhotonView playerPhotonView = other.GetComponentInParent<PhotonView>();

        if (playerPhotonView == null)
        {
            return;
        }

        if (!playerPhotonView.IsMine)
        {
            return;
        }

        isPickedUp = true;

        ApplyPickup(receiver);
        AddPickupStats();

        if (photonView != null)
        {
            photonView.RPC(nameof(RPC_PlayPickupFeedback), RpcTarget.All);
        }
        else
        {
            PlayPowerUpSfx();
            PlayAnimation();
            DisableCollider();
        }

        if (PhotonNetwork.IsMasterClient)
        {
            destroyCoroutine = StartCoroutine(DestroyRoutineMultiplayer());
        }
        else
        {
            if (photonView != null)
            {
                photonView.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
            }
        }
    }

    [PunRPC]
    private void RPC_PlayPickupFeedback()
    {
        isPickedUp = true;

        PlayPowerUpSfx();
        PlayAnimation();
        DisableCollider();
    }

    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (destroyCoroutine != null)
        {
            return;
        }

        destroyCoroutine = StartCoroutine(DestroyRoutineMultiplayer());
    }

    private void ApplyPickup(PlayerBuffReceiver receiver)
    {
        switch (pickupType)
        {
            case PickupType.Health:
                receiver.ApplyHealth(value);
                break;

            case PickupType.Defense:
                receiver.ApplyDefense(value, duration);
                break;

            case PickupType.Speed:
                receiver.ApplySpeed(value, duration);
                break;

            case PickupType.Damage:
                receiver.ApplyDamage(value, duration);
                break;

            case PickupType.FireRate:
                receiver.ApplyFireRate(value, duration);
                break;
        }
    }

    private void AddPickupStats()
    {
        GameSessionStats stats = GameSessionStats.Instance;

        if (stats == null)
        {
            return;
        }

        stats.AddPickup(pickupType);
    }

    private void PlayAnimation()
    {
        if (pickupAnimator == null)
        {
            return;
        }

        pickupAnimator.SetTrigger(pickupTriggerName);
    }

    private void DisableCollider()
    {
        if (triggerCollider == null)
        {
            return;
        }

        triggerCollider.enabled = false;
    }

    private IEnumerator DestroyRoutineSingleplayer()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private IEnumerator DestroyRoutineMultiplayer()
    {
        yield return new WaitForSeconds(destroyDelay);

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void PlayPowerUpSfx()
    {
        if (gameplaySfxController == null)
        {
            return;
        }

        gameplaySfxController.Post(
            GameplaySfxType.PowerUp,
            WwiseEventsType.Play,
            sfxSource);

        isPowerUpSfxPlaying = true;
    }

    private void StopPowerUpSfx()
    {
        if (gameplaySfxController == null)
        {
            return;
        }

        if (!isPowerUpSfxPlaying)
        {
            return;
        }

        gameplaySfxController.Post(
            GameplaySfxType.PowerUp,
            WwiseEventsType.Stop,
            sfxSource);

        isPowerUpSfxPlaying = false;
    }

    private void HandlePauseOpened()
    {
        if (gameplaySfxController == null)
        {
            return;
        }

        if (!isPowerUpSfxPlaying)
        {
            return;
        }

        gameplaySfxController.Post(
            GameplaySfxType.PowerUp,
            WwiseEventsType.Pause,
            sfxSource);
    }

    private void HandlePauseClosed()
    {
        if (gameplaySfxController == null)
        {
            return;
        }

        if (!isPowerUpSfxPlaying)
        {
            return;
        }

        gameplaySfxController.Post(
            GameplaySfxType.PowerUp,
            WwiseEventsType.Resume,
            sfxSource);
    }
}