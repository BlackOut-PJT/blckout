using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class FurnitureBox : MonoBehaviourPun, IInteractable, IContainer
{
    
    [Header ("Item Data")]
    public ItemData itemData;

    [Header ("UI 연결")]
    [SerializeField] private TextMeshProUGUI interactUI; //접근하면 뜨는 상호작용키
    [SerializeField] private GameObject boxPanel; //[E]키 누르면 뜨는 팝업창 패널
    [SerializeField]public ItemSlot slotUI; //팝업창 패널안에 뜰 슬롯(IClickHandler) 제어하는 스크립트 부분


    private int interactPlayerNumber;


    private void Awake()
    {
        slotUI = GetComponentInChildren<ItemSlot>();

        if (slotUI != null)
        {
            slotUI.Initialize(this, itemData);
            boxPanel.SetActive(false);
        }
        if (interactUI != null) interactUI.gameObject.SetActive(false);
    }
    public void ShowUI(bool show)
    {
        if (interactUI != null) interactUI.gameObject.SetActive(show);
    }

    public void Interact(Player interactor)
    {
        PlayerInteraction.instance.SetInteractTarget(this);
        ShowPanel(true);
        interactPlayerNumber = interactor.ActorNumber;
    }

    public void ShowPanel(bool show)
    {
        if (show)
        {
            if (slotUI != null) slotUI.Initialize(this, itemData);
        }
        boxPanel.SetActive(show);
    }

    public void AddItem(ItemData item)
    {
        photonView.RPC("RPC_AddItem", RpcTarget.All, item.itemID);
    }

    public void RemoveItem()
    {
        if (this.itemData == null) return;
        photonView.RPC("RPC_RequestTakeItem", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
    }

    [PunRPC]
    void RPC_RequestTakeItem (int requestingPlayerId)
    {
        if (this.itemData == null) return;
        photonView.RPC("RPC_SuccessTakeItem", RpcTarget.All, requestingPlayerId);
    }

    [PunRPC]
    void RPC_SuccessTakeItem(int playerID)
    {
        if (PhotonNetwork.LocalPlayer.ActorNumber == playerID)
        {
            InventoryModel.instance.AddItem(this.itemData);
        }
        this.itemData = null;

        if (boxPanel.activeSelf && slotUI!= null) slotUI.Initialize(this, null);
    }

    [PunRPC]
    void RPC_AddItem(int itemID)
    {
        ItemData item = ItemManager.instance.GetItem(itemID);
        this.itemData = item;
        if (boxPanel.activeSelf && slotUI != null) slotUI.Initialize(this, itemData);
    }
}
