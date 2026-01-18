using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun; //PhotonNetwork, MonoBehaviourPunCallbacks
using Photon.Realtime; //RoomInfo, RoomOptions
using UnityEngine.SceneManagement;//SceneManager.LoadScene() 사용 위해

//ConnectScene에서 UI를 바인딩(AddListener)하고, NetworkManager를 호출하는 UI 전담 매니저
public class ConnectSceneManager : MonoBehaviour
{
    private const string KEY_NICK = "PLAYER_NICKNAME"; //PlayerPrefs(임시저장용)에 닉네임 저장 시 필요한 키 이름

    [Header("UI Binder")]
    [SerializeField] private ConnectUIBinder ui;

    [Header("Nickname")]
    public InputField NickNameInput;

    [Header("Create Room Popup")]
    public GameObject createRoomPopup; //방만들기 팝업 패널
    public InputField RoomNameInput;//팝업 내 방이름 인풋필드

    [Header("Room List")]
    public Transform roomListParent; //roomItem 담아놓을 부모
    public GameObject roomItemPrefab; //roomItem 프리팹

    [Header("Scene")]
    public byte maxPlayers = 6;

    public static ConnectSceneManager Instance;
    private bool isPublicRoom = true;

    private void Start()
    {
        // 시작하자마자 로비 입장 시도
        string nick = PlayerPrefs.GetString(KEY_NICK, "").Trim();

        if (!string.IsNullOrEmpty(nick))
        {
        // UI에도 표시해주고
        if (ui != null && ui.nickNameInput != null)
            ui.nickNameInput.text = nick;

        NetworkManager.Instance.Connect(nick);
        }

    }
    void Awake()
    {   
        //Inspector로 ui를 안 넣었으면 씬에서 자동으로 ConnectUIBinder를 찾아서 연결
        if (ui == null)
            ui = FindAnyObjectByType<ConnectUIBinder>();
        
        //ui를 못 찾으면 이후 코드가 NullReference 남. 에러 찍고 종료
        if (ui == null)
        {
            Debug.LogError("[ConnectSceneManager] ConnectUIBinder를 찾지 못했습니다.");
            return;
        }

        //시작할 때 방만들기 팝업 꺼놓기
        if(createRoomPopup != null)
            createRoomPopup.SetActive(false);
        
        //여기서 UI 이벤트(버튼 클릭/인풋 변화)를 AddListener로 "코드에서" 연결
        BindUI();
    }

    //오브젝트가 활성화될 때마다 호출(씬 진입, SetActive(true) 등)
    private void OnEnable()
    {
        //NetworkManager가 룸 리스트 바뀜을 알리는 이벤트에 구독(Subscribe)
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnRoomListChanged += RefreshRoomListUI;
    }

    //오브젝트가 비활성화될 때 호출(씬 나감, Destroy되기 전 등)
    private void OnDisable()
    {
        //반드시 구독 해제(Unsubscribe)해야 중복 호출/메모리 누수/버그 방지됨
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnRoomListChanged -= RefreshRoomListUI;
    }

    //UI의 InputField/Button 등에 AddListener를 붙여주는 곳(Inspector 연결 대신 코드로 고정)
    private void BindUI()
    {
        // ---- Nickname Input ----
        //닉네임 InputField가 존재하면
        if (ui.nickNameInput != null)
        {
            //혹시 이전에 붙어있던 리스너가 있으면 제거(중복 호출 방지)
            ui.nickNameInput.onValueChanged.RemoveAllListeners();
            ui.nickNameInput.onEndEdit.RemoveAllListeners();

            //입력 중(한 글자씩 바뀔 때) 호출될 함수 연결
            ui.nickNameInput.onValueChanged.AddListener(OnNicknameValueChanged);
            //입력 완료(엔터/포커스 해제 등 EndEdit) 시 호출될 함수 연결
            ui.nickNameInput.onEndEdit.AddListener(OnNicknameEndEdit);
        }

        // ---- Top Buttons ----
        //뒤로가기 버튼
        if (ui.btnBack != null)
        {
            //중복 방지
            ui.btnBack.onClick.RemoveAllListeners();
            //클릭 시 OnClickBack 실행
            ui.btnBack.onClick.AddListener(OnClickBack);
        }

        //설정 버튼
        if (ui.btnSettings != null)
        {
            ui.btnSettings.onClick.RemoveAllListeners();
            ui.btnSettings.onClick.AddListener(OnClickSettings);
        }

        // ---- Start Button ----
        //Start 버튼(여기서는 최소로 Connect 호출용)
        if (ui.btnStart != null)
        {
            ui.btnStart.onClick.RemoveAllListeners();
            ui.btnStart.onClick.AddListener(OnClickStart);
        }

        // ---- Create Room Buttons (public/private/create) ----
        //Public 버튼: 공개방 상태로 토글
        if (ui.btnPublic != null)
        {
            ui.btnPublic.onClick.RemoveAllListeners();
            ui.btnPublic.onClick.AddListener(() =>
            {
                //공개방 상태로 변경
                isPublicRoom = true;
                //버튼 UI 표시 업데이트
                UpdatePublicPrivateVisual();
            });
        }

        //Private 버튼: 비공개방 상태로 토글
        if (ui.btnPrivate != null)
        {
            ui.btnPrivate.onClick.RemoveAllListeners();
            ui.btnPrivate.onClick.AddListener(() =>
            {
                isPublicRoom = false;
                UpdatePublicPrivateVisual();
            });
        }

        //"방 만들기" 버튼(팝업 열기)
        if (ui.btnCreateRoom != null)
        {
            ui.btnCreateRoom.onClick.RemoveAllListeners();
            ui.btnCreateRoom.onClick.AddListener(OpenCreateRoomPopup);
        }

        // ---- Create Room Popup ----
        //팝업 확인 버튼(방 생성 확정)
        if (ui.btnConfirm != null)
        {
            ui.btnConfirm.onClick.RemoveAllListeners();
            ui.btnConfirm.onClick.AddListener(OnClickCreateRoomConfirm);
        }

        //팝업 취소 버튼(팝업 닫기)
        if (ui.btnCancel != null)
        {
            ui.btnCancel.onClick.RemoveAllListeners();
            ui.btnCancel.onClick.AddListener(CloseCreateRoomPopup);
        }
    }

    //닉네임 관련
    //닉네임 입력칸에서 현재 닉네임 문자열을 가져옴(Trim으로 공백 제거)
    private string GetNick()
    {
        //ui나 input이 없으면 빈 문자열 반환(NullReference 방지)
        if (ui == null || ui.nickNameInput == null) return "";
        //입력값 앞뒤 공백 제거해서 반환
        return ui.nickNameInput.text.Trim();
    }

    //닉네임 저장하기
    //닉네임을 PlayerPrefs에 저장(입력이 비어있으면 실패)
    private bool SaveNickName()
    {
        //현재 입력값 가져오기
        string nick = GetNick();

        //빈 값이면 저장 실패
        if (string.IsNullOrWhiteSpace(nick))
        {
            Debug.Log("닉네임을 입력하세요!");
            return false;
        }

        //로컬 저장소(PlayerPrefs)에 저장
        PlayerPrefs.SetString(KEY_NICK, nick);
        //즉시 저장 반영
        PlayerPrefs.Save();
        //저장 성공
        return true;
    }

    private bool HasNick()
    {
        return !string.IsNullOrWhiteSpace(GetNick());
    }

    //디버깅용
    public void OnNicknameValueChanged(string value)
    {
        Debug.Log("닉네임 입력 중: "+value);
    }

    //디버깅용
    public void OnNicknameEndEdit(string value)
    {
        SaveNickName();
        Debug.Log("닉네임 입력완료: "+value);
    }

    //버튼 관련

    //뒤로가기 버튼 클릭 처리
    private void OnClickBack()
    {
        Debug.Log("[UI] Back clicked");
        SceneManager.LoadScene("Scene_Title");
    }

    //설정 버튼 클릭 처리(현재는 로그만)
    private void OnClickSettings()
    {
        Debug.Log("[UI] Settings clicked");
        //여기서 설정 팝업 열기 처리해야 함
    }

    //Start 버튼 클릭 처리: 닉네임 저장 + NetworkManager.Connect 호출
    private void OnClickStart()
    {
        //닉네임 저장 실패하면(빈 값이면) 진행 중단
        if (!SaveNickName()) return;

        //NetworkManager를 통해 포톤 연결/로비입장 수행
        NetworkManager.Instance.Connect(GetNick());

        Debug.Log("[UI] Start -> Connect called");
    }

    //방 만들기 버튼 클릭: 팝업 열기(+ 연결 보장)
    private void OpenCreateRoomPopup()
    {
        //닉네임 저장 실패하면 진행 중단
        if (!SaveNickName()) return;

        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("[UI] 아직 로비 입장 중입니다.");
            return;
        }

        //팝업 켜기
        if (ui.createRoomPopup != null)
            ui.createRoomPopup.SetActive(true);

        //방 이름 입력칸이 있으면 초기화 셋팅 후
        if (ui.roomNameInput != null)
        {
            ui.roomNameInput.text = "";
            //커서 자동으로 찍어줌
            ui.roomNameInput.ActivateInputField();
        }
    }

    //팝업 닫기
    private void CloseCreateRoomPopup()
    {
        if (ui.createRoomPopup != null)
            ui.createRoomPopup.SetActive(false);
    }

    //팝업 확인 버튼: 방 생성 요청
    public void OnClickCreateRoomConfirm()
    {
        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("[UI] 아직 로비 입장 중입니다.");
            return;
        }

        if (!HasNick())
        {
            Debug.Log("[UI] 닉네임을 먼저 입력하세요!");
            return;
        }

        // 닉네임 적용
        PhotonNetwork.NickName = GetNick();

        string roomName = ui.roomNameInput != null ? ui.roomNameInput.text.Trim() : "";
        NetworkManager.Instance.CreateRoom(roomName, maxPlayers);
        CloseCreateRoomPopup();
    }

    // Room List UI 관련

    //NetworkManager.OnRoomListChanged 이벤트가 오면 호출되어 룸 리스트 UI를 새로 그림
    public void RefreshRoomListUI()
    {
        //Content(부모 Transform)가 연결되어 있어야 RoomItem을 자식으로 생성 가능
        if (ui == null || ui.roomListParent == null)
        {
            Debug.LogError("[UI] roomListParent가 NULL");
            return;
        }

        //RoomItem 프리팹이 없으면 생성 불가
        if (roomItemPrefab == null)
        {
            Debug.LogError("[UI] roomItemPrefab이 NULL (Inspector에 넣어줘야 함)");
            return;
        }

        //기존에 생성돼 있던 RoomItem UI들을 모두 삭제(새로 갱신)
        for (int i = ui.roomListParent.childCount - 1; i >= 0; i--)
            Destroy(ui.roomListParent.GetChild(i).gameObject);

        //NetworkManager에 저장된 최신 룸 캐시를 가져옴
        Dictionary<string, RoomInfo> cache = NetworkManager.Instance.cachedRoomList;

        //캐시에 있는 모든 방 정보를 순회하며 RoomItem을 생성
        foreach (var kv in cache)
        {
            //현재 방의 정보
            RoomInfo info = kv.Value;

            //프리팹을 Content 아래에 생성
            GameObject item = Instantiate(roomItemPrefab, ui.roomListParent);

            //RoomItemUI 컴포넌트가 있으면 Set(info)로 텍스트/버튼 등을 세팅
            RoomItemUI roomItemUI = item.GetComponent<RoomItemUI>();
            if (roomItemUI != null)
                roomItemUI.Set(info);
        }

        //갱신 결과 로그
        Debug.Log($"[UI] RoomList refreshed: {cache.Count}");
    }

    //Public/Private 버튼의 UI 상태를 표시(현재는 interactable로만 표현)
    private void UpdatePublicPrivateVisual()
    {
        //Public이 선택된 상태면 Public 버튼은 비활성(interactable=false 느낌으로), Private 버튼은 활성
        if (ui.btnPublic != null) ui.btnPublic.interactable = !isPublicRoom;
        //Private이 선택된 상태면 반대로
        if (ui.btnPrivate != null) ui.btnPrivate.interactable = isPublicRoom;
    }
    
}
