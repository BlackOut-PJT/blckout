using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    //싱글톤 객체
    public static NetworkManager Instance;

    // 방 목록 캐시 (어느 씬에서든 접근 가능)
    public Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    //"룸 리스트가 바뀌었다"는 신호(이벤트)
    // UI가 이 이벤트를 구독하므로 네트워크가 UI를 직접 호출하지 않아도 됨
    public System.Action OnRoomListChanged;

    //싱글톤 유지시키는 코드
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시 파괴 방지
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        // (방장이 LoadScene하면 나머지도 따라가게 할 때 유용)
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[NM] Auto ConnectUsingSettings");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    //닉네임 설정 + 포톤 서버 연결 + 로비 입장까지 담당하는 함수
    public void Connect(string nickname)
    {
        // nickname이 null/빈문자/공백이면 연결 진행하지 않음
        if (string.IsNullOrWhiteSpace(nickname))
        {
            Debug.LogWarning("[Connect] nickname empty");
            return;                                      
        }

        PhotonNetwork.NickName = nickname.Trim();

        //아직 서버에 연결이 안 되어 있다면
        if (!PhotonNetwork.IsConnected)
            //서버 연결 먼저
            PhotonNetwork.ConnectUsingSettings();

        //이미 연결되어 있는데 아직 로비에 안 들어간 상태면
        else if (!PhotonNetwork.InLobby)
            //로비에 들어가야 방 리스트를 받을 수 있음
            PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    //UI에서 방 이름 받아서 방 생성하는 함수
    public void CreateRoom(string roomName, byte maxPlayers)
    {
        //서버 연결 완전히 준비되지 않으면
        if(!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[CreateRoom] Not ready yet");
            return;                                   
        }

        //방 이름 비어있으면
        if (string.IsNullOrWhiteSpace(roomName))
            //랜덤 이름으로 자동 생성
            roomName = "Room_" + Random.Range(1000, 9999);

        //방 옵션 설정
        RoomOptions options = new RoomOptions { MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true };
        //방 생성 시도(성공 시 자동으로 그 방에 입장)
        //성공 시 콜백 : OnJoinedRoom()
        //실패 시 콜백 : OnCreateRoomFailed()
        Debug.Log($"[CreateRoom] visible={options.IsVisible} open={options.IsOpen} max={options.MaxPlayers} roomName={roomName}");
        Debug.Log($"CreateRoom 요청: {roomName}");
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[JoinRoom] Not ready yet");
            return;
        }

        if (string.IsNullOrWhiteSpace(roomName))
            return;

        PhotonNetwork.JoinRoom(roomName);
    }


    // --- Photon Callbacks ---

    //포톤 연결 성공해서 마스터 서버에 붙으면 자동호출됨
    public override void OnConnectedToMaster()
    {
        Debug.Log("[Photon] OnConnectedToMaster -> JoinLobby");
        //로비 입장(룸 리스트 받기 위해 필수)(ConnectScene이 포톤에선 Lobby)
        PhotonNetwork.JoinLobby();
    }

    //룸 리스트 받고 UI 갱신
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[Photon] OnRoomListUpdate count={roomList.Count}");
        //포톤이 준 roomList 캐시에 반영
        foreach (var room in roomList)
        {
            Debug.Log($"[RoomDelta] name={room.Name} removed={room.RemovedFromList} " +
                  $"players={room.PlayerCount}/{room.MaxPlayers} open={room.IsOpen} visible={room.IsVisible}");
            //사라진 방이면 캐시에서 제거
            if (room.RemovedFromList) cachedRoomList.Remove(room.Name);
            //이외는 새로 업데이트
            else cachedRoomList[room.Name] = room;
        }

        Debug.Log($"[Cache] cachedRoomList.Count={cachedRoomList.Count}");

        //룸리스트가 바뀌었다고 "구독 중인 UI"에게 알림
        //?. 는 구독자가 없으면(null이면) 그냥 아무것도 안 하고 넘어감
        OnRoomListChanged?.Invoke();
    }

    //CreateRoom 성공하거나 JoinRoom 성공해서 "어떤 방에 들어갔을 때" 자동 호출되는 콜백
    public override void OnJoinedRoom()
    {
        Debug.Log("OnJoinedRoom 호출");
        //SceneManager.LoadScene("[Photon] OnJoinedRoom -> Load Scene_Lobby");
        PhotonNetwork.LoadLevel("Scene_Lobby");
    }

    // CreateRoom이 실패했을 때 자동 호출되는 콜백
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"방 생성 실패: {message} ({returnCode})");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[Photon] JoinRoomFailed: {message} ({returnCode})");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NM] JoinedLobby");
    }
    
}
