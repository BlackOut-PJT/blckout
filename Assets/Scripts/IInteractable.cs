using UnityEngine;
using Photon.Realtime;

//상호작용 가능한 대상들이 갖는 기능,약속 적어둔 인터페이스
public interface IInteractable
{
    //가까이 있을 때 UI(줍기/신고) 켜거나 끄는 함수
    void ShowUI(bool show);

    //플레이어가 E를 눌렀을 때 실행되는 상호작용 함수
    void Interact(Player interactor);
}
