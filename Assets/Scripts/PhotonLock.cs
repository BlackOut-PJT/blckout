using UnityEngine;
using Photon.Pun;

public class PhotonLock : MonoBehaviourPun
{

    [Header("UnityEvent")]
    //락 상태 변경시 호출되는 이벤트
    public LockStateChangedEvent onLockChanged;

    [Header("락 상태 관련")]
    [SerializeField] private bool isLocked = false;//현재 락 상태, true = 점유 중
    [SerializeField] private int lockedByActor = -1; //현재 락 점유한 플레이어의 ActorNumber

    public bool IsLocked => isLocked;//getter 용 함수 람다식
    public int LockedByActor => lockedByActor;

    //내가 이 락 점유 중인지 여부, 홀드 진행 가능한지 판단 시 사용
    public bool IsLockedByMe
    {
        get
        {
            return isLocked && PhotonNetwork.LocalPlayer != null && lockedByActor == PhotonNetwork.LocalPlayer.ActorNumber;
        }
    }

    //락 요청. 플레이어가 상호작용 시작 시 호출, 실제 승인 여부 MasterClient가 판단
    public void RequestLock()
    {
        if(!PhotonNetwork.InRoom) return;

        photonView.RPC(
            nameof(RPC_TryLock),
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber
        );
    }

    //락 해제 요청. 상호작용 완료/취소 시 호출, 자기 락만 해제 가능
    public void ReleaseLock()
    {
        if(!PhotonNetwork.InRoom) return;
        if(!IsLockedByMe) return;

        photonView.RPC(
            nameof(RPC_ReleaseLock),
            RpcTarget.MasterClient,
            PhotonNetwork.LocalPlayer.ActorNumber
        );
    }

    //MasterClient 전용. 락 요청 받았을 시 판단
    [PunRPC]
    private void RPC_TryLock(int requesterActor)
    {
        //아직 아무도 점유 안 했을 시 승인
        if(!isLocked)
        {
            isLocked = true;
            lockedByActor = requesterActor;

            //모든 클라에 락 상태 동기화
            photonView.RPC(
                nameof(RPC_OnLockChanged),
                RpcTarget.All,
                isLocked, 
                lockedByActor
            );
        }

        else
        {
            //이미 점유 중이면 거절
            photonView.RPC(
                nameof(RPC_LockDenied),
                RpcTarget.All,
                requesterActor,
                lockedByActor
            );
        }
    }

    //MasterClient 전용. 락 해제 요청 처리
    [PunRPC]
    private void RPC_ReleaseLock(int requesterActor)
    {
        if(!isLocked) return;

        //지금 락 잡은 사람이랑 해제 요청한 사람이 다르면 return
        if(lockedByActor != requesterActor) return;

        isLocked = false;
        lockedByActor = -1;

        photonView.RPC(
            nameof(RPC_OnLockChanged),
            RpcTarget.All,
            isLocked,
            lockedByActor
        );
    }

    //모든 클라이언트에서 실행. 실제 락 상태 반영. UnityEvent 호출해서 FurnitureBox, CraftingBox UI 갱신
    [PunRPC]
    private void RPC_OnLockChanged(bool locked, int byActor)
    {
        isLocked = locked;
        lockedByActor = byActor;

        if(onLockChanged != null)
            onLockChanged.Invoke(isLocked, lockedByActor);
    }

    //락 거절 시 호출.. 인데 일단 비워둠
    [PunRPC]
    private void RPC_LockDenied(int requesterActor, int currentTarget)
    {
        
    }


}
