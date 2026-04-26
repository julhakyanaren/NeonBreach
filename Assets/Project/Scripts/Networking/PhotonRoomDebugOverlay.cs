using Photon.Pun;
using UnityEngine;

public class PhotonRoomDebugOverlay : MonoBehaviour
{
    [Header("Debug")]
    [Tooltip("Show Photon room debug overlay.")]
    [SerializeField] private bool showOverlay = true;

    private void OnGUI()
    {
        if (!showOverlay)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(10f, 120f, 520f, 260f), GUI.skin.box);

        GUILayout.Label("RuntimeOptions.MultiplayerMode: " + RuntimeOptions.MultiplayerMode);
        GUILayout.Label("PhotonNetwork.IsConnected: " + PhotonNetwork.IsConnected);
        GUILayout.Label("PhotonNetwork.IsConnectedAndReady: " + PhotonNetwork.IsConnectedAndReady);
        GUILayout.Label("PhotonNetwork.InRoom: " + PhotonNetwork.InRoom);
        GUILayout.Label("PhotonNetwork.IsMasterClient: " + PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.LocalPlayer != null)
        {
            GUILayout.Label("Local ActorNumber: " + PhotonNetwork.LocalPlayer.ActorNumber);
            GUILayout.Label("Local NickName: " + PhotonNetwork.LocalPlayer.NickName);
        }
        else
        {
            GUILayout.Label("LocalPlayer: NULL");
        }

        if (PhotonNetwork.CurrentRoom != null)
        {
            GUILayout.Label("Room Name: " + PhotonNetwork.CurrentRoom.Name);
            GUILayout.Label("Player Count: " + PhotonNetwork.CurrentRoom.PlayerCount);
        }
        else
        {
            GUILayout.Label("CurrentRoom: NULL");
        }

        GUILayout.EndArea();
    }
}