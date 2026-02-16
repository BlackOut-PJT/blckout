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
        // 결과창 패널 활성화
        resultPanel.gameObject.SetActive(true);

        // 결과 텍스트 띄우기
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

        reloadButton.interactable = true; // 돌아가기 버튼 상호작용 키기
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
        reloadButton.interactable = false;
        photonView.RPC("RPC_MoveToLobby", RpcTarget.MasterClient);
    }

    [PunRPC]
    public void RPC_MoveToLobby()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // 다 같이 로비씬으로 이동
            PhotonNetwork.LoadLevel("Scene_Lobby");
        }
    }
}
