using Photon.Pun;
using UnityEngine;

public class NetworkPlayerBootstrap : MonoBehaviourPun
{
    [Header("Local Only Components")]
    [Tooltip("Components that must stay enabled only for the local player.")]
    [SerializeField] private Behaviour[] localOnlyBehaviours;

    [Header("Remote Only Components")]
    [Tooltip("Components that must stay enabled only for remote players.")]
    [SerializeField] private Behaviour[] remoteOnlyBehaviours;

    [Header("References")]
    [Tooltip("Optional local input blocker on this player.")]
    [SerializeField] private PlayerInputBlocker inputBlocker;

    [Tooltip("Rigidbody on the player root.")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Header("Debug")]
    [Tooltip("Log ownership setup in console.")]
    [SerializeField] private bool logOwnershipSetup = false;

    private void Reset()
    {
        inputBlocker = GetComponent<PlayerInputBlocker>();
        targetRigidbody = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (inputBlocker == null)
        {
            inputBlocker = GetComponent<PlayerInputBlocker>();
        }

        if (targetRigidbody == null)
        {
            targetRigidbody = GetComponent<Rigidbody>();
        }

        ApplyOwnershipState();
    }

    private void ApplyOwnershipState()
    {
        bool isMine = photonView.IsMine;

        SetBehavioursState(localOnlyBehaviours, isMine);
        SetBehavioursState(remoteOnlyBehaviours, !isMine);

        ApplyInputBlockState(isMine);
        ApplyRigidbodyState(isMine);

        if (logOwnershipSetup)
        {
            Debug.Log(
                "NetworkPlayerBootstrap: Ownership applied. IsMine = " + isMine,
                this);
        }
    }

    private void ApplyInputBlockState(bool isMine)
    {
        if (inputBlocker == null)
        {
            return;
        }

        if (isMine)
        {
            inputBlocker.UnblockInput();
        }
        else
        {
            inputBlocker.BlockInput();
        }
    }

    private void ApplyRigidbodyState(bool isMine)
    {
        if (targetRigidbody == null)
        {
            return;
        }

        if (isMine)
        {
            targetRigidbody.isKinematic = false;
            return;
        }

        targetRigidbody.velocity = Vector3.zero;
        targetRigidbody.angularVelocity = Vector3.zero;
        targetRigidbody.isKinematic = true;
    }

    private void SetBehavioursState(Behaviour[] behaviours, bool state)
    {
        if (behaviours == null)
        {
            return;
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null)
            {
                continue;
            }

            behaviours[i].enabled = state;
        }
    }
}