using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MenuPhotonLauncher : MonoBehaviourPunCallbacks
{
    [Header("Photon Settings")]
    [Tooltip("Photon game version used to separate incompatible clients.")]
    [SerializeField] private string gameVersion = "NeonBreach_Stage1";

    [Tooltip("Maximum number of players allowed in a multiplayer room.")]
    [Range(1f, 3f)]
    [SerializeField] private byte maxPlayers = 3;

    [Header("Photon data send settings")]
    [Tooltip("Defines how many times per second the PhotonHandler should send data, if any is queued")]
    [Range(10, 60)]
    [SerializeField] private int sendRate = 40;

    [Tooltip(" Defines how many times per second OnPhotonSerialize should be called on PhotonViews for controlled objects")]
    [Range(10, 30)]
    [SerializeField] private int serializationRate = 20;

    [Header("Debug")]
    [Tooltip("If true, debug logs will be printed.")]
    [SerializeField] private bool enableLogs = true;

    private string targetSceneName;
    private bool isStartingMultiplayer;

    public void StartMultiplayer(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("MenuPhotonLauncher: Scene name is null or empty.", this);
            return;
        }

        targetSceneName = sceneName;
        isStartingMultiplayer = true;

        if (PhotonNetwork.IsConnected)
        {
            ApplyPhotonNetworkRates();

            if (enableLogs)
            {
                Debug.Log("MenuPhotonLauncher: Already connected. Joining random room...", this);
            }

            JoinRandomRoom();
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = gameVersion;

        ApplyPhotonNetworkRates();

        if (enableLogs)
        {
            Debug.Log("MenuPhotonLauncher: Connecting to Photon...", this);
        }

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (isStartingMultiplayer == false)
        {
            return;
        }

        if (enableLogs)
        {
            Debug.Log("MenuPhotonLauncher: Connected to Master. Joining random room...", this);
        }

        JoinRandomRoom();
    }

    private void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (enableLogs)
        {
            Debug.LogWarning(
                "MenuPhotonLauncher: JoinRandomRoom failed. Creating room instead. " +
                "Code = " + returnCode +
                ", Message = " + message,
                this);
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;

        PhotonNetwork.CreateRoom(null, roomOptions, TypedLobby.Default);
    }

    public override void OnCreatedRoom()
    {
        if (enableLogs)
        {
            Debug.Log("MenuPhotonLauncher: Room created successfully.", this);
        }
    }

    public override void OnJoinedRoom()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogError("MenuPhotonLauncher: Target scene name is empty.", this);
            return;
        }

        if (enableLogs)
        {
            Debug.Log(
                "MenuPhotonLauncher: Joined room successfully. " +
                "ActorNumber = " + PhotonNetwork.LocalPlayer.ActorNumber +
                ", PlayerCount = " + PhotonNetwork.CurrentRoom.PlayerCount +
                ", Loading scene = " + targetSceneName,
                this);
        }

        PhotonNetwork.LoadLevel(targetSceneName);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError(
            "MenuPhotonLauncher: CreateRoom failed. " +
            "Code = " + returnCode +
            ", Message = " + message,
            this);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (enableLogs)
        {
            Debug.LogWarning("MenuPhotonLauncher: Disconnected from Photon. Cause = " + cause, this);
        }

        isStartingMultiplayer = false;
        targetSceneName = string.Empty;
    }

    private void ApplyPhotonNetworkRates()
    {
        PhotonNetwork.SendRate = sendRate;
        PhotonNetwork.SerializationRate = serializationRate;
    }
}