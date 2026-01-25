using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class LobbyPlayerList : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_Text playerCountText;

    [Header("List")]
    public Transform contentRoot;          // ScrollView Content
    public GameObject playerNameItemPrefab; // PlayerNameItem prefab

    private readonly List<GameObject> spawned = new();

    void Start()
    {
        Refresh();
    }

    /* public override void OnConnectedToMaster()
    {
        Debug.Log("[PlayerList] Connected to Master");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PlayerList] Joined Lobby");
        PhotonNetwork.JoinOrCreateRoom(
            "TESTROOM",
            new RoomOptions { MaxPlayers = 6 },
            TypedLobby.Default
        );
    } */


    public override void OnJoinedRoom()
    {
        Refresh();
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Refresh();
    }
    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Refresh();
    }

    void Refresh()
    {       
        Debug.Log($"[PlayerList] Refresh InRoom={PhotonNetwork.InRoom} " +
              $"Room={(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : "null")} " +
              $"Count={(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : -1)}");

        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }
        
        if (!PhotonNetwork.InRoom)
        {
            playerCountText.text = "Players: -/- (Not in room)";
            return;
        }

        // 방 밖이면 아무것도 하지 말기
        if (!PhotonNetwork.InRoom) return;

        // 1) 기존 아이템 삭제
        foreach (var go in spawned) Destroy(go);
        spawned.Clear();

        // 2) 카운트 갱신
        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        playerCountText.text = $"Players: {current}/{max}";

        // 3) 닉네임 목록 생성
        foreach (var p in PhotonNetwork.PlayerList)
        {
            var go = Instantiate(playerNameItemPrefab, contentRoot);
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) tmp = go.GetComponentInChildren<TMP_Text>();
            tmp.text = p.NickName;
            spawned.Add(go);
        }
    }

    bool initialized = false;


    void Update()
    {
        if (!initialized && PhotonNetwork.InRoom)
        {
            initialized = true;
            Refresh();
        }
    }

}
