using Photon.Pun;
using UnityEngine;

public class PlayerHudBindRpcBridge : MonoBehaviourPun
{
    [Header("References")]
    [Tooltip("PhotonView on this player.")]
    [SerializeField] private PhotonView photonView;

    [Tooltip("Player health component.")]
    [SerializeField] private PlayerHealth playerHealth;

    [Tooltip("Player shooter component.")]
    [SerializeField] private PlayerShooter playerShooter;

    [Tooltip("Player buff receiver component.")]
    [SerializeField] private PlayerBuffReceiver playerBuffReceiver;

    [Header("Timing")]
    [Tooltip("Delay before HUD bind RPC is sent.")]
    [Range(0f, 1f)]
    [SerializeField] private float bindDelay = 0.15f;

    private void Reset()
    {
        photonView = GetComponent<PhotonView>();
        playerHealth = GetComponent<PlayerHealth>();
        playerShooter = GetComponent<PlayerShooter>();
        playerBuffReceiver = GetComponent<PlayerBuffReceiver>();
    }

    private void Awake()
    {
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerShooter>();
        }

        if (playerBuffReceiver == null)
        {
            playerBuffReceiver = GetComponent<PlayerBuffReceiver>();
        }
    }

    private void Start()
    {
        if (RuntimeOptions.MultiplayerMode == false)
        {
            return;
        }

        if (photonView == null)
        {
            return;
        }

        if (photonView.IsMine == false)
        {
            return;
        }

        Invoke(nameof(SendHudBindRpc), bindDelay);
    }

    private void SendHudBindRpc()
    {
        if (RuntimeOptions.MultiplayerMode == false)
        {
            return;
        }

        if (photonView == null)
        {
            return;
        }

        if (photonView.IsMine == false)
        {
            return;
        }

        photonView.RPC(nameof(RpcTryBindHud), RpcTarget.All);
    }

    [PunRPC]
    private void RpcTryBindHud()
    {
        if (RuntimeOptions.MultiplayerMode == false)
        {
            return;
        }

        if (photonView == null)
        {
            return;
        }

        if (photonView.IsMine == false)
        {
            return;
        }

        PlayerHudBinder binder = FindFirstObjectByType<PlayerHudBinder>();

        if (binder == null)
        {
            Debug.LogWarning("PlayerHudBindRpcBridge: PlayerHudBinder was not found.", this);
            return;
        }

        binder.BindLocalPlayer(playerHealth, playerShooter, playerBuffReceiver);
    }
}