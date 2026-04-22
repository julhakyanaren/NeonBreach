using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GameplayPhotonLauncher : MonoBehaviourPunCallbacks
{
    [Header("Photon Settings")]
    [Tooltip("Photon game version used to separate incompatible clients.")]
    [SerializeField] private string gameVersion = "NeonBreach_Stage1";

    [Tooltip("Room name used for MVP multiplayer testing.")]
    [SerializeField] private string roomName = "dev_room";

    [Tooltip("Maximum number of players allowed in the room.")]
    [Range(0, 3)]
    [SerializeField] private byte maxPlayers = 3;

    [Header("Debug")]
    [Tooltip("If true, Photon connection starts automatically on scene load.")]
    [SerializeField] private bool connectOnStart = true;

    private bool isConnecting = false;

    private void Start()
    {
        if (connectOnStart == false)
        {
            return;
        }

        ConnectToPhoton();
    }

    public void ConnectToPhoton()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("GameplayPhotonLauncher: Already connected to Photon. Joining room...");
            JoinRoom();
            return;
        }

        isConnecting = true;
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.AutomaticallySyncScene = false;

        Debug.Log("GameplayPhotonLauncher: Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("GameplayPhotonLauncher: Connected to Master Server.");

        if (isConnecting == false)
        {
            return;
        }

        JoinRoom();
    }

    private void JoinRoom()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = maxPlayers;

        Debug.Log("GameplayPhotonLauncher: Joining or creating room: " + roomName);
        PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            "GameplayPhotonLauncher: Joined room successfully. " +
            "ActorNumber = " + PhotonNetwork.LocalPlayer.ActorNumber +
            ", PlayerCount = " + PhotonNetwork.CurrentRoom.PlayerCount);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError(
            "GameplayPhotonLauncher: Failed to join room. " +
            "Code = " + returnCode +
            ", Message = " + message);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("GameplayPhotonLauncher: Disconnected from Photon. Cause = " + cause);
    }
}