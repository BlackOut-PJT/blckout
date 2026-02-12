using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ReloadBtnController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button reloadButton; // 재시작 버튼

    void Start()
    {
        // 비활성화되어 있지만 코드상에서도 비활성화해주기
        reloadButton.gameObject.SetActive(false);
        reloadButton.onClick.AddListener(OnClickReloadButton); // 이벤트 함수 연결

        if (GameStateManager.instance != null)
        {
            // ShowButton()을 게임 종료 이벤트 구독 추가
            GameStateManager.instance.OnGameEnded += ShowButton;
        }
    }

    void Update()
    {
        // 버튼이 비활성화되어 있다면 Update()문 실행x
        if (!reloadButton.gameObject.activeSelf) return;

        // R키 누르면 돌아가기 버튼 함수 호출
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("R키 눌림!!");
            OnClickReloadButton();
        }
    }

    private void ShowButton()
    {
        // 돌아가기 버튼 활성화
        reloadButton.gameObject.SetActive(true);
        reloadButton.interactable = true;
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴될 때 구독 해제
        if (GameStateManager.instance != null)
        {
            GameStateManager.instance.OnGameEnded -= ShowButton;
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
