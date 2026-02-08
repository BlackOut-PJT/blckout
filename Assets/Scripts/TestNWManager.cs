using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Test_NW_Manager : MonoBehaviourPunCallbacks
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 5;
        PhotonNetwork.JoinOrCreateRoom("TestRoom",roomOptions,null);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.Instantiate("Player(main)",Vector3.zero,Quaternion.identity);
    }
}
