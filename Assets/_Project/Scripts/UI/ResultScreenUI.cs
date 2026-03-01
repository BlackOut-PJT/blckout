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
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.Log("방장만 로비로 돌아가기를 누를 수 있습니다!");
            return;
        }

        reloadButton.interactable = false;

        // 모두에게 방을 버리고 초기 화면으로 도망가라고 명령!
        photonView.RPC("RPC_DestroyRoomAndLeave", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_DestroyRoomAndLeave()
    {
        // 1. 혹시 모를 망령(유령 스폰 버그 등)을 막기 위해 내 개인 데이터 싹 삭제!
        Hashtable clearProps = new Hashtable();
        foreach (var key in PhotonNetwork.LocalPlayer.CustomProperties.Keys)
        {
            clearProps[key] = null;
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(clearProps);

        // 2. 현재 꼬인 방을 가차 없이 버리고 나갑니다. (LeaveRoom)
        PhotonNetwork.LeaveRoom();
    }

    // 3. 방을 완벽하게 빠져나왔을 때 자동으로 실행되는 콜백 함수
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();

        // 4. 포톤 네트워크 로딩이 아니라, 유니티의 기본 씬 이동을 사용해서 완전히 새 출발!
        // ⚠️ 주의: 괄호 안에 유저님의 진짜 첫 접속 씬 이름을 정확히 적어주세요!
        SceneManager.LoadScene("Scene_Connect");
    }
}
