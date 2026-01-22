using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

//아이템에 붙이는 스크립트
//플레이어가 E로 상호작용하면 Interact()가 호출됨
//인터페이스 implements
public class ItemBox : MonoBehaviourPun, IInteractable
{
    //디버그용 콘솔에 띄우는 아이템 이름
    public string debugItemName = "Test Item";

    [Header("UI")]
    [SerializeField]
    //ItemBOX 아래 Canvas 넣기
    private GameObject interactUI;

    //중복 상호작용 방지 플래그(이미 먹힌 상자인지)
    private bool used;

    private void Awake()
    {
        //interactUI null 아닌 거 확인하고
        if(interactUI != null)
            //기본으로 줍기(E) 안보이도록
            interactUI.SetActive(false);
    }

    //PlayerInteraction이 보면 켜고, 안 보면 끔
    public void ShowUI(bool show)
    {
        Debug.Log($"[ItemBox] ShowUI({show}) on {name}");
        //interactUI 존재 && 아직 사용 안한 상자일 때
        if(interactUI != null && !used)
            interactUI.SetActive(show);
    }

    //플레이어가 E키 상호작용 성공했을 때 호출될 함수
    public void Interact(Player interactor)
    {
        //이미 누가 먹었으면(used=true) 추가 실행 금지
        if (used) return;

        //방장이 열림 확정->모두에게 전파->사용됨 상태로 만들어야 함

        //오픈한사람의 넘버 초기화
        int openerActorNumber = -1;

        if(interactor != null)
        {
            //연 사람의 고유 넘버 저장
            openerActorNumber = interactor.ActorNumber;
        }

        //내가 방장일 경우, 전체에게 '열림' 적용
        if (PhotonNetwork.IsMasterClient)
        {
            //이 박스에 붙은 PhotonView 이용,
            //방 안 모두에게 RpcOpen 함수 원격 호출(누가 열었는지도 전달)
            photonView.RPC("RpcOpen", RpcTarget.All, openerActorNumber);
        }
        else
        {
            //방장 아닐 경우, 방장에게 open request
            photonView.RPC("RequestOpen", RpcTarget.MasterClient, openerActorNumber);
        }
    }
    
    [PunRPC]
    //방장에게 열기 요청
    private void RequestOpen(int openerActorNumber)
    {
        if(!PhotonNetwork.IsMasterClient) return;
        //방장이 전체에게 open 적용
        photonView.RPC(nameof(RpcOpen), RpcTarget.All, openerActorNumber);
    }

    //모두에게 열림 적용하는 함수
    [PunRPC]
    private void RpcOpen(int openerActorNumber)
    {
        if(used) return;
        //열린 상태 확정
        used = true;
        //안내 ui 끄기
        ShowUI(false);

        //누가 열었는지 ActorNumber로 찾아서 로그 찍기
        Player openerPlayer = null;
        
        if(PhotonNetwork.CurrentRoom != null)
        {
            openerPlayer = PhotonNetwork.CurrentRoom.GetPlayer(openerActorNumber);
        }

        //"누가 뭘 열었습니다." 공지 필요할 경우
        string openerName = "Unknown";
        if(openerPlayer != null)
        {
            openerName = openerPlayer.NickName;
        }

        Debug.Log("[ItemBox] Opened by " + openerName + " : " + debugItemName);

        //!!!!여기부터가 '연 사람만 받는 개인 처리' 자리
        //예) 팝업 띄우기, 인벤토리에 넣기, 개인 효과 등
        if(PhotonNetwork.LocalPlayer != null)
        {
            if(PhotonNetwork.LocalPlayer.ActorNumber == openerActorNumber)
            {
                // 여기 코드는 "연 사람"에게만 실행됨
                Debug.Log("[ItemBox] This client opened the box -> show personal popup / give item");
                // TODO: UIManager.ShowItemPopup(...)
                // TODO: Inventory.AddRandomItem(...)
            }
        }

        //가구 열림 스프라이트 관련해서 추가..?
        
    }
}
