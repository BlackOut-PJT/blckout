using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ResultScreenUI : MonoBehaviourPunCallbacks
{
    [Header("UI 연결")]
    [SerializeField] private GameObject resultPanel; // 결과창 패널
    [SerializeField] private TextMeshProUGUI resultText; // 결과 텍스트
    [SerializeField] private Button reloadButton; // 재시작 버튼

    // 내부 변수
    private string nextRoomNameToJoin = "";

    void Start()
    {
        resultText.text = "";
        // 비활성화되어 있지만 코드상에서도 비활성화해주기
        resultPanel.gameObject.SetActive(false);
        reloadButton.onClick.AddListener(OnClickReloadButton); // 이벤트 함수 연결

        if (GameStateManager.instance != null)
        {
            // ShowButton()을 게임 종료 이벤트 구독 추가
            GameStateManager.instance.OnGameEnded += ShowPanel;
        }
    }

    void Update()
    {
        // 결과창 패널이 비활성화되어 있다면 Update()문 실행x
        if (!resultPanel.gameObject.activeSelf) return;

        // R키 누르면 돌아가기 버튼 함수 호출
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R키 눌림!!");
            OnClickReloadButton();
        }
    }

    private void ShowPanel(GameStateManager.WhoWin winner)
    {
        StartCoroutine(ShowPanelDelayed(winner));
    }

    // 킬 모션이 끝날 때까지 기다렸다가 띄워주는 코루틴
    private System.Collections.IEnumerator ShowPanelDelayed(GameStateManager.WhoWin winner)
    {
        // 킬 모션 애니메이션이 끝날 때까지 2.5초 정도 여유롭게 기다려줌
        yield return new WaitForSeconds(2.5f);

        Cursor.visible = true; // 마우스 커서 보이게 하기
        Cursor.lockState = CursorLockMode.None; // 커서 화면 중앙 고정 해제

        resultPanel.gameObject.SetActive(true); // 결과창 패널 활성화

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            if (winner == GameStateManager.WhoWin.SurvivorWin)
            {
                resultText.text = "<color=green>SURVIVOR WIN!</color>";
            }
            else
            {
                resultText.text = "<color=red>KILLER WIN!</color>";
            }
        }

        reloadButton.interactable = true; // 돌아가기 버튼 상호작용 켜기
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴될 때 구독 해제
        if (GameStateManager.instance != null)
        {
            GameStateManager.instance.OnGameEnded -= ShowPanel;
        }
    }

    public void OnClickReloadButton()
    {
        // 방장만 누를 수 있게 차단
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("방장만 재시작을 누를 수 있습니다!");
            return;
        }

        reloadButton.interactable = false;

        string newRoomName = PhotonNetwork.CurrentRoom.Name + "_Retry_" + Random.Range(1000, 9999);
        photonView.RPC("RPC_MigrateToNewRoom", RpcTarget.All, newRoomName);
    }

    [PunRPC]
    public void RPC_MigrateToNewRoom(string newRoomName)
    {
        nextRoomNameToJoin = newRoomName;

        // 개인 데이터 완벽 초기화 (유령 스폰 방지)
        Hashtable clearProps = new Hashtable();
        foreach (var key in PhotonNetwork.LocalPlayer.CustomProperties.Keys)
        {
            clearProps[key] = null;
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);

        PhotonNetwork.LeaveRoom();
    }

    // ★ 여기에 OnConnectedToMaster는 절대 있으면 안 됩니다! ★

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();

        if (!string.IsNullOrEmpty(nextRoomNameToJoin))
        {
            RoomOptions options = new RoomOptions();
            options.MaxPlayers = 8; // 본인 게임 인원수에 맞게 변경

            PhotonNetwork.JoinOrCreateRoom(nextRoomNameToJoin, options, TypedLobby.Default);
        }
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        if (!string.IsNullOrEmpty(nextRoomNameToJoin))
        {
            nextRoomNameToJoin = ""; // 이사 완료! 메모장 초기화

            if (PhotonNetwork.IsMasterClient)
            {
                // [수정] 방을 파자마자 바로 로딩하면 포톤 내부 로직과 충돌하므로,
                // 코루틴을 통해 아주 잠깐 숨을 고른 뒤에 씬을 이동시킵니다!
                StartCoroutine(MigrateLoadRoutine());
            }
        }
    }

    private System.Collections.IEnumerator MigrateLoadRoutine()
    {
        // 포톤이 백그라운드에서 혼자 하던 작업(자동 씬 동기화 등)이 끝날 때까지 잠시 기다려줍니다.
        yield return new WaitForSeconds(0.5f);

        // 혹시라도 이전 로딩 충돌 때문에 멈춰버린 네트워크 통신 큐를 강제로 뚫어줍니다. (안전장치)
        PhotonNetwork.IsMessageQueueRunning = true;

        // 이제 안전하게 다 같이 대기룸으로 이동!!
        PhotonNetwork.LoadLevel("Scene_Lobby");
    }
}
