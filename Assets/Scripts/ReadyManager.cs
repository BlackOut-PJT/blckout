using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ReadyManager : MonoBehaviourPunCallbacks
{
    [Header("UI 연결")]
    [SerializeField] private Text statusText; // 준비 여부를 표시할 UI 텍스트
    [SerializeField] private Button readyButton;
    [SerializeField] private Text readyButtonText;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startButton;

    void Start()
    {
        // 방장이 씬 로딩하면 나머지 플레이어도 자동으로 따라가게 설정
        PhotonNetwork.AutomaticallySyncScene = true;

        // 버튼 이벤트 리스너 등록
        readyButton.onClick.AddListener(OnClickReadyButton);
        leaveButton.onClick.AddListener(OnClickLeaveButton);
        startButton.onClick.AddListener(StartGame);
    }

    public void OnClickReadyButton()
    {
        // 버튼을 누른 로컬 플레이어 가져오기
        Player localPlayer = PhotonNetwork.LocalPlayer;
        bool isReady = false;
        // 원래 저장값이 있으면 가져오기
        if (localPlayer.CustomProperties.ContainsKey("IsReady"))
        {
            isReady = (bool)localPlayer.CustomProperties["IsReady"];
        }

        isReady = !isReady; // 상태 뒤집기

        // 서버에 변경된 값 보내기
        Hashtable props = new Hashtable();
        props["IsReady"] = isReady;
        localPlayer.SetCustomProperties(props);

        // ready button의 텍스트 갱신
        readyButtonText.text = isReady ? "READY" : "NOT READY";
    }

    public void OnClickLeaveButton()
    {
        PhotonNetwork.LeaveRoom();
    }

    // 방 입장 성공 콜백 이벤트
    public override void OnJoinedRoom()
    {
        Debug.Log("방 입장 성공!");
        Debug.Log($"현재 방의 방장? {PhotonNetwork.IsMasterClient}");

        // 플레이어마다 시작하자마자 본인의 IsReady 상태값을 false로 설정 후
        Hashtable initialProps = new Hashtable() { { "IsReady", false } };
        // 다른 모든 플레이어들에게 본인이 false임을 동기화
        PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

        if (PhotonNetwork.IsMasterClient)
        {
            // 방장이면 시작 버튼은 보이지만 비활성화 상태
            startButton.gameObject.SetActive(true);
            startButton.interactable = false;
        }
        else startButton.gameObject.SetActive(false); // 일반 플레이어는 숨김처리
    }

    // Leave 버튼 누른 후 진짜로 방에서 퇴장 후
    public override void OnLeftRoom()
    {
        // 플레이어를 다시 로비창으로 이동시켜주기
        Debug.Log("방 퇴장 완료. 로비창으로 돌아갑니다.");
        // 로비씬의 이름 Lobby 겠죠??
        SceneManager.LoadScene("Lobby");
    }

    // 내가 아닌 다른 플레이어가 퇴장했을 시
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // UI 갱신
        UpdateStatusText();

        // 방장은 Game Start 버튼 비활성화 조건 확인
        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = CheckGameStartCondition();
        }
    }

    // 프로퍼티 변경 시 콜백
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("IsReady"))
        {
            // UI 업데이트: V 표시 갱신(보류)

            // 방장은 게임 시작 조건 확인(버튼 이벤트 함수에 넣으면 안됨)
            // -> 남이 눌렀을 때도 확인해줘야 하므로
            if (PhotonNetwork.IsMasterClient)
            {
                startButton.interactable = CheckGameStartCondition();
            }
        }

        UpdateStatusText();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"바뀐 방장이 난가? {newMasterClient.IsLocal}");
        
        // 방장이 바뀌면 바뀐 방장에게 start button 보이게 처리.
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = CheckGameStartCondition();
        }
    }

    public void UpdateStatusText()
    {
        int curPlayerCnt = PhotonNetwork.CurrentRoom.PlayerCount;
        int readyPlayerCnt = 0;

        // 현재 준비하고 있는 플레이어 수 세기.
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            object isReadyValue;
            if (p.CustomProperties.TryGetValue("IsReady", out isReadyValue))
            {
                if ((bool)isReadyValue == true) readyPlayerCnt++;
            }
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"현재 {readyPlayerCnt}/{curPlayerCnt}명 준비됨");
        statusText.text = stringBuilder.ToString();
    }

    // 게임 시작 조건 확인 함수
    public bool CheckGameStartCondition()
    {
        int curPlayerCnt = PhotonNetwork.CurrentRoom.PlayerCount; // 현재 인원
        int maxPlayer = PhotonNetwork.CurrentRoom.MaxPlayers; // 방의 최대 인원

        if (curPlayerCnt == maxPlayer) // 6명 있고
        {
            // 모두 준비 상태인지 확인
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object isReadyValue;
                if (p.CustomProperties.TryGetValue("IsReady", out isReadyValue))
                {
                    // IsReady 값이 있는데 하나라도 false라면
                    if ((bool)isReadyValue == false) return false;
                }
                else return false; // IsReady 값이 없으면(아직 로딩중!)
            }

            return true;
        }

        return false;
    }

    public void StartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 게임 시작 시 더 이상 다른 사람이 방으로 못 들어오게 막기
            // 아마 로비창에서 막아주겠지만 제미나이 추천으로 넣었어용
            PhotonNetwork.CurrentRoom.IsOpen = false;

            // 인게임 씬 로드(이름 InGame이겠죠??)
            PhotonNetwork.LoadLevel("TestInGameScene");
        }
    }
}
