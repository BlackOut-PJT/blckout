using System;
using Random = UnityEngine.Random;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon; //Hashtable 사용 위해서
using HashTable = ExitGames.Client.Photon.Hashtable;

public class GameStateManager : MonoBehaviourPunCallbacks, IPunObservable
{
    //싱글톤으로 생성
    public static GameStateManager instance;

    //상태 변경 알림 이벤트(빛/사운드/ 등이 구독해서 반응)
    public System.Action<GameState> OnGameStateChanged;


    #region 인스펙터창 설정 변수
    [Header("UI 연결")]
    public TextMeshProUGUI timerText; //투표 타이머
    public TextMeshProUGUI totalGameTimeText; //전체 게임 타이머
    public GameObject votingPanel;
    public Light2D globalLight;
    public TextMeshProUGUI resultText; //결과출력창 (임시)
    public NotifyUI notificationSystem; //메시지 출력 UI

    [Header("설정")]
    public const float gameTime = 1800.0f; // 수정 못하게 상수로 변경
    public float blackoutDelay;
    public float votingTime = 120.0f;
    public int maxMeetingCount = 1; //플레이어당 최대 회의 소집 횟수 
    private const string MEETING = "MeetingCount"; //상수 키값 (CustomProperties)
    #endregion

    #region 내부 변수

    //상태변수
    public GameState currentState = GameState.Playing_OnLight;
    private float currentGameTime;
    private int loadedPlayerCnt = 0;
    public bool isGameStart = false;
    public bool isGameEnded = false;
    [HideInInspector] public bool skipWinCondition = false;

    //투표용 변수
    private double votingEndTime;

    // 승리 조건 체크용 변수
    public enum WhoWin { None = 0, SurvivorWin = 10, KillerWin = 20 }

    // 게임 종료 이벤트
    public event Action<WhoWin> OnGameEnded;

    #endregion

    //싱글톤으로 생성하기 위한 초기작업
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        //isGameStart = true;
        #region 테스트용 코드
        if (PhotonNetwork.IsConnectedAndReady)
        {
            resultText.text = "";

            // 1. 인게임씬에 캐릭터 생성하기 위한 코드
            Vector2 randomPos = Random.insideUnitCircle * 2.0f;
            PhotonNetwork.Instantiate("Player(main)", randomPos, Quaternion.identity);

            // 2. 상태 초기화
            Hashtable props = new Hashtable();
            props.Add("IsDead", false);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        #endregion

        blackoutDelay = Random.Range(30f, 60f); // 30초->30~60초 랜덤으로 변경
        currentGameTime = gameTime;
        if (votingPanel != null) votingPanel.SetActive(false);
        UpdateLightState();

        if (PhotonNetwork.IsConnectedAndReady)
        {
            photonView.RPC("RPC_ReportLoadingComplete", RpcTarget.MasterClient);
        }
    }

    void Update()
    {
        if (isGameStart == false) return;

        switch (currentState)
        {
            case GameState.Playing_OnLight:
            case GameState.Playing_OffLight:
                UpdatePlayLogic();
                break;
            case GameState.Voting:
                UpdateVoteLogic();
                break;
            case GameState.Result:
                break;
        }

        UpdateGlobalTimer();
    }


    #region [게임 로직 관련]

    public void UpdatePlayLogic()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            currentGameTime -= Time.deltaTime;

            float timeElapsed = gameTime - currentGameTime;
            if (currentState == GameState.Playing_OnLight && timeElapsed >= blackoutDelay)
            {
                photonView.RPC("RPC_SetGameState", RpcTarget.All, GameState.Playing_OffLight, 0.0);
            }

            if (currentGameTime <= 0) currentGameTime = 0;

            if(isGameEnded) return; // RPC 중복 방지용
            
            WhoWin result = CheckWinCondition();
            if (result != WhoWin.None) // 게임 종료되었다면
            {
                Debug.Log($"[게임 종료] 승리: {result}");
                photonView.RPC("RPC_EndGame", RpcTarget.All, result);
            }
        }

        if(GameUtils.IsMyPlayerDead) return; // 투표 소집 전 생존여부 파악

        // 3. 투표 시작 요청(우선은 M키 누르면 시작되게)
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (PhotonNetwork.IsMasterClient) StartMeeting();
            else photonView.RPC("RPC_RequestMeeting", RpcTarget.MasterClient);
        }
    }

    public void AssignJobs()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        Player[] allPlayers = PhotonNetwork.PlayerList;
        int killerIndex = Random.Range(0, allPlayers.Length);

        for (int i = 0; i < allPlayers.Length; i++)
        {
            HashTable props = new HashTable();

            if (i == killerIndex)
            {
                props.Add("Job", "Killer");
                Debug.Log($"[직업배정] 킬러: {allPlayers[i].NickName}");
            }
            else
            {
                props.Add("Job", "Survivor");
            }

            allPlayers[i].SetCustomProperties(props);
        }
    }

    public WhoWin CheckWinCondition()
    {
        if (skipWinCondition) return WhoWin.None;

        // 생존자 승리: 게임 시작 다 지나면 or 살인마 검거
        // 살인마 승리: 생존자 전멸

        int survivorCount = 0;
        int killerCount = 0;
        int notYetCnt = 0; // 직업 로딩 전 승리 조건 체크 시

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isDeadValue;
            if (p.CustomProperties.TryGetValue("IsDead", out isDeadValue))
            {
                if ((bool)isDeadValue == true) continue; // 죽었으면 카운트x
            }

            if (p.CustomProperties.TryGetValue("Job", out object jobObject))
            {
                string job = (string)jobObject;
                if (job == "Survivor") survivorCount++;
                else if (job == "Killer") killerCount++;
                else notYetCnt++; // 직업이 "None"인 플레이어는 여기로 카운트
            }
            else notYetCnt++;
        }

        if(notYetCnt>0) return WhoWin.None; // 아직 로딩 다 안 됐으면 승리 조건 체크x
        if (survivorCount == 0) 
        {
            isGameEnded = true;
            return WhoWin.KillerWin;
        }
        if (killerCount == 0) 
        {
            isGameEnded = true;
            return WhoWin.SurvivorWin;
        }
        if (currentGameTime <= 0) 
        {
            isGameEnded = true;
            return WhoWin.SurvivorWin;
        }

        return WhoWin.None;
    }

    #endregion


    #region [투표 관련 로직]
    public void UpdateVoteLogic()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.Time >= votingEndTime)
            {
                EndVoting();
            }
        }

        // 2. 투표 남은 시간 표시
        double timeRemaining = votingEndTime - PhotonNetwork.Time;
        if (timeRemaining > 0) timerText.text = ((int)timeRemaining).ToString();
        else timerText.text = "0";
    }

    public void MeetingButtonPressed(Player interacter)
    {   
        if (currentState != GameState.Playing_OnLight && currentState != GameState.Playing_OffLight) return;
        
        int currentCount = GetPlayerMeetingCount(interacter);
        if (currentCount >= maxMeetingCount) {
            notificationSystem.ShowMessage("투표 소집은 플레이어당 한 번만 가능합니다.");
            return;
        }
        
        photonView.RPC("RPC_RequestMeeting", RpcTarget.MasterClient, interacter);
    }

    [PunRPC]
    public void RPC_RequestMeeting(Player interacter)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (currentState == GameState.Voting) return;

        //2차 검사
        int currentCount = GetPlayerMeetingCount(interacter);
        if (currentCount >= maxMeetingCount) return;

        //회의 소집 가능 횟수 관련 CustomProperties 업데이트
        Hashtable props = new Hashtable {{MEETING, currentCount + 1}};
        interacter.SetCustomProperties(props);

        StartMeeting();
    }

    //플레이어의 현재 투표 소집 사용횟수 가져오는 함수
    private int GetPlayerMeetingCount (Player player)
    {
        if (player.CustomProperties.ContainsKey(MEETING))
            return (int) player.CustomProperties[MEETING];
        return 0; //키가 없으면 0번 사용한 것.
    }

    // 투표 회의 시작&종료 로직
    public void StartMeeting()
    {
        Debug.Log("회의 소집 시작");
        if (currentState == GameState.Voting) return;

        double endTime = PhotonNetwork.Time + votingTime;
        photonView.RPC("RPC_SetGameState", RpcTarget.All, GameState.Voting, endTime);
    }

    public void EndVoting()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        float timeElapsed = gameTime - currentGameTime;

        GameState nextState;
        if (timeElapsed >= blackoutDelay) nextState = GameState.Playing_OffLight;
        else nextState = GameState.Playing_OnLight;

        photonView.RPC("RPC_SetGameState", RpcTarget.All, nextState, 0.0);
    }

    //투표 횟수 초기화용 함수
    public void ResetMeetingCounts()
    {
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            Hashtable props = new HashTable {{MEETING, 0}};
            p.SetCustomProperties(props);
        }
    }

    #endregion


    #region [UI 관련]
    public void UpdateGlobalTimer()
    {
        // 1. 전체 게임 시간 표시
        int min = (int)(currentGameTime / 60);
        int sec = (int)(currentGameTime % 60);
        if (totalGameTimeText != null) totalGameTimeText.text = string.Format("{0:00}:{1:00}", min, sec);
    }

    void UpdateLightState()
    {
        if (globalLight == null) return;

        if (currentState == GameState.Playing_OnLight)
        {
            globalLight.intensity = 1.0f;
            globalLight.color = Color.white;
        }
        else if (currentState == GameState.Playing_OffLight)
        {
            globalLight.intensity = 0f;
            globalLight.color = Color.darkGray;
        }
    }

    #endregion


    #region [네트워크 관련]
    [PunRPC]
    public void RPC_ReportLoadingComplete()
    {
        // 방장만 인원 체크
        if (!PhotonNetwork.IsMasterClient) return;

        loadedPlayerCnt++;
        Debug.Log($"[로딩체크] {loadedPlayerCnt} / {PhotonNetwork.PlayerList.Length} 명 로딩 완료");

        if (loadedPlayerCnt == PhotonNetwork.PlayerList.Length)
        {
            Debug.Log("[로딩체크] 모든 플레이어 로딩 완료");
            AssignJobs();
            photonView.RPC("RPC_StartGame", RpcTarget.All); // 게임 시작 신호 방송
        }
    }

    [PunRPC]
    void RPC_StartGame()
    {
        isGameStart = true; // 각 플레이어들의 Update문 실행 시작
        Debug.Log("게임 로직 가동 시작");
    }

    [PunRPC]
    void RPC_SetGameState(GameState newState, double endTime)
    {
        currentState = newState;
        votingEndTime = endTime;
        //상태 변경 알림 발사 (SightSystemController가 여기에 반응)
        OnGameStateChanged?.Invoke(currentState);

        switch (newState)
        {
            case GameState.Playing_OnLight:
            case GameState.Playing_OffLight:
                if (votingPanel != null) votingPanel.SetActive(false);
                timerText.text = "";
                UpdateLightState();
                break;
            case GameState.Voting:
                if (votingPanel != null) votingPanel.SetActive(true);
                globalLight.intensity = 1.0f;
                break;
        }
    }

    [PunRPC]
    public void RPC_EndGame(WhoWin winner)
    {
        currentState = GameState.Result;
        isGameStart = false; // 플레이어 움직임 봉쇄

        // 매개변수로 winner 정보 넘겨주기
        OnGameEnded?.Invoke(winner); // 게임 종료 이벤트 구독 대상들에게 이벤트 알림

        // 결과 텍스트 띄우기는 삭제
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentGameTime);
            stream.SendNext(currentState);
        }
        else
        {
            currentGameTime = (float)stream.ReceiveNext();
            GameState receivedState = (GameState)stream.ReceiveNext();

            if (currentState != receivedState)
            {
                currentState = receivedState;
                //네트워크 동기화로 상태가 바뀐 경우도 알림
                OnGameStateChanged?.Invoke(currentState);
                UpdateLightState();
            }
        }
    }
    #endregion

}