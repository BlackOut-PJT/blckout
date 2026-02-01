using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class FieldFireWork : MonoBehaviourPun, IInteractable
{
    [Header ("아이템 데이터")]
    public ItemData itemData;

    [Header("UI 연결")]
    public TextMeshProUGUI interactText;

    [Header ("시각효과")]
    public SpriteRenderer spriteRenderer;

    void Start()
    {   
        interactText.gameObject.SetActive(false);
    }

    public void ShowUI(bool show)
    {
        if (this == null || interactText == null) return;
        interactText.gameObject.SetActive(show);
    }

    public void Interact(Player interactor)
    {
        //자신의 인벤토리에 넣기 (로컬) => 줍는 사람이 '나'일 때만
        if (interactor.IsLocal) InventoryModel.instance.AddItem(itemData);

        //필드에서 삭제(모두에게 적용) 방장이 사라지게하거나 스스로 파괴되도록
        if (photonView.IsMine) PhotonNetwork.Destroy(gameObject);
        else photonView.RPC(nameof(RPC_DestroySelf), RpcTarget.MasterClient);
    }

    [PunRPC]
    void RPC_DestroySelf()
    {
        PhotonNetwork.Destroy(gameObject);
    }

    //사용X
    public void ShowPanel (bool show) {}
}
