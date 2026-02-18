using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class FurnitureBox : MonoBehaviourPun, IInteractable, IContainer, IHoldInteractable
{
    
    [Header ("Item Data")]
    public ItemData itemData;

    [Header ("UI 연결")]
    [SerializeField] private TextMeshProUGUI interactUI; //접근하면 뜨는 상호작용키
    [SerializeField] private GameObject boxPanel; //[E]키 누르면 뜨는 팝업창 패널
    [SerializeField]public ItemSlot slotUI; //팝업창 패널안에 뜰 슬롯(IClickHandler) 제어하는 스크립트 부분

    [Header("Hold 설정")]
    [SerializeField] private float holdDuration = 1.5f;
    public float HoldDuration => holdDuration;

    [Header("Hold UI")]
    [SerializeField] private GameObject holdUIRoot;
    [SerializeField] private HoldGaugeUI holdGauge;

    //사운드 설정
    private string audioClipName = "OpeningDrawer";

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

        //(추가) 홀드 ui 초기화
        if(holdUIRoot != null)
            holdUIRoot.SetActive(false);
        if(holdGauge != null)
            holdGauge.ResetGauge();
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
            SoundManager.instance.SFXPlay(audioClipName);
        }
        boxPanel.SetActive(show);

        if(!show)
            GetComponent<PhotonLock>()?.ReleaseLock();
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

    public void ShowHoldUI(bool show)
    {
        if(holdUIRoot != null)
            holdUIRoot.SetActive(show);
        
        if(show && holdGauge != null)
            holdGauge.ResetGauge();
    }

    public void SetHoldProgress(float t01)
    {
        if(holdGauge != null)
            holdGauge.SetProgress(t01);
    }

    public void OnLockChanged_FromUnityEvent(bool locked, int byActor)
    {
        bool lockedByMe = locked && PhotonNetwork.LocalPlayer != null && byActor == PhotonNetwork.LocalPlayer.ActorNumber;
        //다른 사람 점유 중이면 ui 닫기
        if(locked && !lockedByMe)
        {
            ShowUI(false);
            ShowHoldUI(false);

            if(boxPanel != null && boxPanel.activeSelf)
                ShowPanel(false);//boxPanel.SetActive(false);
        }
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
