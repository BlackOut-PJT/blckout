using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class FieldItem : MonoBehaviourPun, IInteractable, IPunInstantiateMagicCallback
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    //내부데이터
    private int itemID;
    private ItemData itemData;
    private bool isCollected;

    [Header("UI 연결")]
    public TextMeshProUGUI interactText;

    void Start()
    {
        if (interactText == null) gameObject.GetComponent<TextMeshProUGUI>();
        interactText.gameObject.SetActive(false);
    }

    //포톤 Instantiate로 생성될 때 데이터(ID)를 받는 함수
    public void OnPhotonInstantiate (PhotonMessageInfo info)
    {   
        //InstantiationData는 object[] 형태
        object[] data = info.photonView.InstantiationData;

        if (data != null && data.Length > 0)
        {
            //넘어온 ID 받기
            this.itemID = (int)data[0];

            //ID 기반으로 이미지 채우기 -> ItemDatabase 사용해서 매핑
            ItemData dataObj = ItemManager.instance.GetItem(this.itemID);

            if (dataObj != null)
            {
                this.itemData = dataObj;
                this.spriteRenderer.sprite = dataObj.icon;
            }
        }
    }

    public void ShowUI(bool show)
    {   
        if (this == null || interactText == null) return;
        interactText.gameObject.SetActive(show);
    }

    public void Interact(Player interactor)
    {
        photonView.RPC(nameof(RequestGetItem), RpcTarget.MasterClient, interactor);
    }

    

    [PunRPC]
    public void RequestGetItem(Player interactor)
    {   
        if (this.itemData == null || isCollected) return;

        isCollected = true;

        photonView.RPC(nameof(GetItem), interactor, this.itemID);
        PhotonNetwork.Destroy(gameObject);
    }
    
    [PunRPC]
    public void GetItem(int id)
    {
        ItemData item = ItemManager.instance.GetItem(id);
        InventoryModel.instance.AddItem(item);
    }

    //사용X
    public void ShowPanel(bool show){}
}
