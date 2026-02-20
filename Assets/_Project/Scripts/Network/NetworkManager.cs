using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;
using System.Collections;

using Hashtable = ExitGames.Client.Photon.Hashtable;
public class NetworkManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    //싱글톤 객체
    public static NetworkManager Instance;

    //비공개 방 위한 비번
    public static string pendingPassword = "";

    // 방 목록 캐시 (어느 씬에서든 접근 가능)
    public Dictionary<string, RoomInfo> cachedRoomList = new Dictionary<string, RoomInfo>();

    //"룸 리스트가 바뀌었다"는 신호(이벤트)
    // UI가 이 이벤트를 구독하므로 네트워크가 UI를 직접 호출하지 않아도 됨
    public System.Action OnRoomListChanged;

    //비번 관련 이벤트 코드
    private const byte EVT_PW_CHECK_REQUEST = 10;//참가자->방장
    private const byte EVT_PW_CHECK_RESULT = 11;//방장->참가자(허용/거절)
    private const byte EVT_FORCE_TO_LOBBY = 12;//방장->특정 참가자: 로비로 이동시키기

    private Coroutine moveRoutine = null;
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

    //비번 이벤트
    public override void OnEnable(){base.OnEnable();}

    //비번 이벤트
    public override void OnDisable(){base.OnDisable();}

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
    public void CreateRoom(string roomName, byte maxPlayers, bool isPublic, string password)
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

        //방의 커스텀 속성 테이블
        var props = new Hashtable
        {
            //방 비공개인지 여부
            {"isPrivate", !isPublic},
            //비번 설정
            {"pw", isPublic ? "" : password}
        };

        //방 옵션 설정
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers, IsVisible = true, IsOpen = true, CustomRoomProperties = props,
            CustomRoomPropertiesForLobby = new[] {"isPrivate"}
        };

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

    //비번 포함 버전 JoinRoom 함수(Join 전에 비번 저장해야하므로)
    public void JoinRoom(string roomName, string password)
    {
        pendingPassword = password ?? "";
        JoinRoom(roomName);//기존 함수 재사용
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
        
        bool isPrivate = false;
        if(PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("isPrivate"))
        {
            isPrivate = (bool)PhotonNetwork.CurrentRoom.CustomProperties["isPrivate"];
        }

        //공개방이면 바로 이동
        if (!isPrivate)
        {
            //SceneManager.LoadScene("[Photon] OnJoinedRoom -> Load Scene_Lobby");
            PhotonNetwork.LoadLevel("Scene_Lobby");
            return;
        }

        //비공개방 + 내가 방장이면 바로 이동
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Scene_Lobby");
            return;
        }

        //비공개 참가자면: 방장에게 비번 검증 요청
        SendPasswordCheckRequestToMaster(pendingPassword);
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

    //참가자->방장 비번 검증 요청 보냄
    private void SendPasswordCheckRequestToMaster(string inputPw)
    {
        inputPw ??= "";

        //payload: [요청자 ActorNumber, 입력 비번]
        object[] content = new object[] {PhotonNetwork.LocalPlayer.ActorNumber, inputPw};

        //이벤트 방장만 받음
        var opt = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.MasterClient //방장에게만
        };

        //이벤트 전송
        //요청코드, 데이터, 누가 받을지 설정한 것, sendreliable
        PhotonNetwork.RaiseEvent(EVT_PW_CHECK_REQUEST, content, opt, SendOptions.SendReliable);
        Debug.Log("[PrivateRoom] PW check 요청을 방장에게 보냄");
    }

    //Photon 이벤트 수신 함수
    public void OnEvent(EventData photonEvent)
    {
        //비번 검증 요청 이벤트
        if(photonEvent.Code == EVT_PW_CHECK_REQUEST)
        {
            //방장만 처리
            if(!PhotonNetwork.IsMasterClient) return;
            HandlePwCheckRequest_AsMaster(photonEvent);
        }

        //비번 검증 결과 이벤트
        else if(photonEvent.Code == EVT_PW_CHECK_RESULT)
        {
            //참가자 결과 처리
            HandlePwCheckResult_AsClient(photonEvent);
        }

        else if(photonEvent.Code == EVT_FORCE_TO_LOBBY)
        {
            //참가자만 처리
            if(!PhotonNetwork.IsMasterClient && moveRoutine == null) moveRoutine = StartCoroutine(CoMoveToLobbyWhenReady());
        }
    }
    
    //방장이 비번 검증 요청 받을 시 실행
    private void HandlePwCheckRequest_AsMaster(EventData e)
    {
        //이벤트에서 데이터 꺼내기
        var data = (object[])e.CustomData;

        int requesterActor = (int)data[0];//요청자 ActorNumber
        string inputPw = data[1] as string ?? "";//요청자 입력 비번

        //현재 방 실제 비번
        string realPw = PhotonNetwork.CurrentRoom.CustomProperties["pw"] as string ?? "";

        //입력 비번과 실제 비번 비교
        bool ok = (inputPw == realPw);

        //요청자에게 보낼 비교 결과 데이터
        object[] result = new object[] {ok};

        //요청자에게만 보내기
        var opt = new RaiseEventOptions
        {
            TargetActors = new int[] {requesterActor}
        };

        //결과 이벤트 전송
        PhotonNetwork.RaiseEvent(EVT_PW_CHECK_RESULT, result, opt, SendOptions.SendReliable);
        Debug.Log($"[PrivateRoom] PW 체크 요청 -> actor={requesterActor} ok={ok}");

        if (ok)
        {
            var moveOpt = new RaiseEventOptions {TargetActors = new int[] {requesterActor}};
            PhotonNetwork.RaiseEvent(EVT_FORCE_TO_LOBBY, null, moveOpt, SendOptions.SendReliable);
        }
    }

        //참가자가 비번 비교 결과 받았을 때 실행
        private void HandlePwCheckResult_AsClient(EventData e)
    {
        //이벤트 데이터 꺼내기
        var data = (object[])e.CustomData;
        bool ok = (bool)data[0];//true 통과 false 실패

        pendingPassword = "";//사용 후 초기화

        //결과 따라 처리
        if (ok)
        {
            Debug.Log("[PrivateRoom] PW OK -> LOAD LOBBY");
            //PhotonNetwork.LoadLevel("Scene_Lobby");
        }
        else
        {
            Debug.Log("[PrivateRoom] PW WRONG -> LEAVEROOM");
            PhotonNetwork.LeaveRoom();
        }
    }

    private IEnumerator CoMoveToLobbyWhenReady()
    {
        yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom);

        //이미 로비면 또 안감
        if(SceneManager.GetActiveScene().name != "Scene_Lobby") PhotonNetwork.LoadLevel("Scene_Lobby");

        moveRoutine = null;
    }
}
