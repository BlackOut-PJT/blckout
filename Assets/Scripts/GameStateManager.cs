using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class GameStateManager : MonoBehaviourPunCallbacks, IPunObservable
{   
    //싱글톤으로 생성
    public static GameStateManager instance;

    #region 인스펙터창 설정 변수
    [Header("UI 연결")]
    public TextMeshProUGUI timerText; //투표 타이머
    public TextMeshProUGUI totalGameTimeText; //전체 게임 타이머
    public GameObject votingPanel;
    public Light2D globalLight;

    [Header("설정")]
    public float gameTime = 1800.0f;
    public float blackoutDelay = 30.0f;
    public float votingTime = 120.0f;
    #endregion

    #region 내부 변수

    //상태변수
    public GameState currentState = GameState.Playing_OnLight;
    private float currentGameTime;

    //투표용 변수
    private double votingEndTime;
    #endregion
    
    //싱글톤으로 생성하기 위한 초기작업
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    void Start()
    {
        #region 테스트용 코드
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Vector2 randomPos = Random.insideUnitCircle * 2.0f;
            PhotonNetwork.Instantiate("Player(Test)", randomPos, Quaternion.identity);
        }
        #endregion

        currentGameTime = gameTime;
        if(votingPanel != null) votingPanel.SetActive(false);
        UpdateLightState();

        
    }

    void Update()
    {   
        #region [방장 ONLY]
        if (PhotonNetwork.IsMasterClient)
        {
            if (currentState == GameState.Playing_OnLight || currentState == GameState.Playing_OffLight)
            {
                currentGameTime -= Time.deltaTime;

                float timeElapsed = gameTime - currentGameTime;
                if (currentState == GameState.Playing_OnLight && timeElapsed >= blackoutDelay)
                {
                    photonView.RPC("RPC_SetGameState", RpcTarget.All, GameState.Playing_OffLight,0.0);
                }

                if (currentGameTime <= 0) currentGameTime = 0;
            }

            if (currentState == GameState.Voting && PhotonNetwork.Time >= votingEndTime)
            {
                EndVoting();
            }
        }
        #endregion

        #region [전체 플레이어]

        //1. 전체 게임 시간 표시
        int min = (int) (currentGameTime / 60);
        int sec = (int) (currentGameTime % 60);
        if (totalGameTimeText != null) totalGameTimeText.text = string.Format("{0:00}:{1:00}", min, sec);

        //2. 투표 남은 시간 표시
        if (currentState == GameState.Voting)
        {
            double timeRemaining = votingEndTime - PhotonNetwork.Time;
            if (timeRemaining > 0) timerText.text = ((int) timeRemaining).ToString();
            else timerText.text = "0";
        }

        //3. 투표 시작 요청(우선은 M키 누르면 시작되게)
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (PhotonNetwork.IsMasterClient) StartMeeting();
            else photonView.RPC("RPC_RequestMeeting", RpcTarget.MasterClient);
        }
        #endregion
        
    }


    [PunRPC]
    public void RPC_RequestMeeting()
    {
        StartMeeting();
    }

    //회의 시작&종료 로직
    public void StartMeeting()
    {
        if (currentState == GameState.Voting) return;
        
        double endTime = PhotonNetwork.Time + votingTime;
        photonView.RPC("RPC_SetGameState", RpcTarget.All, GameState.Voting, endTime);
    }

    [PunRPC]
    void RPC_SetGameState(GameState newState, double endTime)
    {
        currentState = newState;
        votingEndTime = endTime;

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
            globalLight.intensity = 0.2f;
            globalLight.color = Color.darkGray;
        }
    }

    public void EndVoting()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        float timeElapsed = gameTime - currentGameTime;

        GameState nextState;
        if (timeElapsed >= blackoutDelay) nextState = GameState.Playing_OffLight;
        else nextState = GameState.Playing_OnLight;

        photonView.RPC("RPC_SetGameState", RpcTarget.All,nextState,0.0);
    }

    public void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info)
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
                UpdateLightState();
            }
        }
    }
}
