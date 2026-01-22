using UnityEngine;
using Photon.Realtime;
using Photon.Pun;

//시체 오브젝트
//가까이 가면 신고(E) UI 표시
//E 눌러서 한번만 신고
//신고되면 방 전체에서 시체 사라짐
public class DeadBody : MonoBehaviourPun, IInteractable
{
    [Header("UI")]
    [SerializeField] private GameObject reportUI; //신고(E)(report) UI

    // 이미 신고됐는지(중복 신고 방지)
    private bool reported;

    private void Awake()
    {
        // 시작할 때 UI는 꺼두기
        if (reportUI != null)
        {
            reportUI.SetActive(false);
        }
    }

    //가까이 있을 때 UI 키기
    public void ShowUI(bool show)
    {
        if(reported) return;

        if(reportUI!=null)
            reportUI.SetActive(show);
    }

    //E키 눌렀을 때 신고 처리(PlayerInteraction에서 호출)
    public void Interact(Player interactor)
    {
        //이미 신고됐으면 무시
        if(reported) return;

        //신고한 사람 ActorNumber 저장
        int reporterActorNumber = -1;
        if(interactor != null)
        {
            reporterActorNumber = interactor.ActorNumber;
        }

        //방장이면 바로 전체에 신고 처리 적용
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPCReport), RpcTarget.All, reporterActorNumber);
        }
        else
        {
            //방장이 아니면 방장에게 신고 요청
            photonView.RPC(nameof(RequestReport), RpcTarget.MasterClient, reporterActorNumber);
        }
    }

    //방장 실행: 신고 요청 받고 -> 확정 -> 전체 적용 + destroy
    [PunRPC]
    private void RequestReport(int reporterActorNumber)
    {
        if(!PhotonNetwork.IsMasterClient) return;
        if (reported) return;

        //전체에게 신고 처리 적용
        photonView.RPC(nameof(RPCReport), RpcTarget.All, reporterActorNumber);
    }

    //신고 요청
    [PunRPC]
    private void RPCReport(int reporterActorNumber)
    {
        //중복 호출 방지
        if(reported) return;

        //신고 확정
        reported = true;

        //ui 끄기
        ShowUI(false);

        //디버그 로그 : 누가 신고했는지
        Debug.Log("[DeadBody] Reported! reporterActorNumber = " + reporterActorNumber);

        //Destroy는 여기서 방장만
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.Destroy(gameObject);

        //TODO: 여기서 "회의/투표 시작"을 연결하면 됩니다..!
        //주의: 투표는 보통 방장만 시작시키고, 다른 클라는 UI만 열게 만드는 구조가 안정적
        //예) VoteManager.Instance.StartVote();

    }
}
