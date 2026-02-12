using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Realtime;
using Photon.Pun;
using TMPro;
using System.Linq;

public class CraftingBox : MonoBehaviourPun, IInteractable, IContainer, IHoldInteractable
{
    [Header("설정 (인덱스 연결 세팅)")]
    public ItemData[] requiredIngredients;

    [Header ("조합 결과물")] //조합하면 나오는 폭죽 연결
    public string craftResultPrefabName;

    [Header ("스폰 위치 설정")]
    public Transform spawnPoint;

    [Header("UI 연결")]
    public TextMeshProUGUI text;
    public  GameObject craftPanel;
    public CraftSlot[] slotUIs;
    public Button craftButton; //조합하기 버튼

    //(추가) Hold 설정
    [Header("Hold 설정")]
    [SerializeField] private float holdDuration = 1.5f;
    public float HoldDuration => holdDuration;

    [Header("Hold UI")]
    [SerializeField] private GameObject holdUIRoot;
    [SerializeField] private HoldGaugeUI holdGauge;

    //내부 상태 (동기화 대상)
    private bool[] slotStates;
    private int interactPlayerNumber;

    void Start()
    {
        slotStates = new bool[requiredIngredients.Length];
        
        for (int i=0; i<slotUIs.Length; i++)
        {
            slotUIs[i].slotIndex = i;
            slotUIs[i].Initialize(this);
            UpdateSlotUI(i);
        }

        text.gameObject.SetActive(false);
        craftPanel.SetActive(false);

        //홀드 ui 초기화
        if(holdUIRoot != null) holdUIRoot.SetActive(false);
        if(holdGauge != null) holdGauge.ResetGauge();
    }

    //가까이 있을 때 UI(줍기/신고) 켜거나 끄는 함수
    public void ShowUI(bool show)
    {
        text.gameObject.SetActive(show);
    }

    //플레이어가 E를 눌렀을 때 실행되는 상호작용 함수
    public void Interact(Player interactor)
    {
        if (interactor != null) interactPlayerNumber = interactor.ActorNumber;
        PlayerInteraction.instance.SetInteractTarget(this);
        ShowPanel(true);
        RefreshAllSlots();
        //(추가) 홀드ui 끔
        ShowHoldUI(false);

        // 자동 투입: 들고 있는 아이템이 재료와 일치하면 바로 넣기
        TryAutoDeposit();
    }

    private void TryAutoDeposit()
    {
        if (InventoryModel.instance == null) return;
        ItemData heldItem = InventoryModel.instance.item;
        if (heldItem == null) return;

        for (int i = 0; i < requiredIngredients.Length; i++)
        {
            if (requiredIngredients[i].itemID == heldItem.itemID && slotStates[i] == false)
            {
                photonView.RPC("RPC_UpdateSlot", RpcTarget.All, i, true);
                InventoryModel.instance.RemoveItem();
                return;
            }
        }
    }

    public void ShowPanel (bool show)
    {
        craftPanel.SetActive(show);
        //패널 닫힐 때 락 해제
        if (!show)
        {
            var netLock = GetComponent<PhotonLock>();
            netLock?.ReleaseLock();
        }
    }

    //(추가) 홀드 게이지용
    public void ShowHoldUI(bool show)
    {
        if(holdUIRoot != null) holdUIRoot.SetActive(show);

        if(show && holdGauge != null)
            holdGauge.ResetGauge();
    }

    public void AddItem (ItemData item)
    {
        int foundIndex = -1;
        for (int i=0; i<requiredIngredients.Length; i++)
        {
            if (requiredIngredients[i].itemID == item.itemID)
            {
                foundIndex = i;
                break;
            }
        }

        if (foundIndex == -1 || slotStates[foundIndex] == true) return;
        photonView.RPC("RPC_UpdateSlot", RpcTarget.All, foundIndex, true);
    }

    public void TryRetrieveItem (int slotIndex)
    {
        if (slotStates[slotIndex] == false) return;

        ItemData itemToReturn = requiredIngredients[slotIndex];
        InventoryModel.instance.AddItem(itemToReturn);

        photonView.RPC("RPC_UpdateSlot", RpcTarget.All, slotIndex, false);
    }

    //(추가) 홀드게이지용
    public void SetHoldProgress(float t01)
    {
        if(holdGauge != null) holdGauge.SetProgress(t01);
    }

    //락 이벤트(UnityEvent에서 연결할 함수)
    public void OnLockChanged_FromUnityEvent(bool locked, int byActor)
    {
        bool lockedByMe = locked && PhotonNetwork.LocalPlayer != null && byActor == PhotonNetwork.LocalPlayer.ActorNumber;
        //다른 사람 점유 중일 시
        if(locked && !lockedByMe)
        {
            ShowUI(false);
            ShowHoldUI(false);

            if(craftPanel != null && craftPanel.activeSelf)
                ShowPanel(false);//craftPanel.SetActive(false);
        }
    }

    [PunRPC]
    private void RPC_UpdateSlot (int index, bool hasItem)
    {
        slotStates[index] = hasItem;
        UpdateSlotUI(index);

        CheckButtonCondition(); //슬롯 상태가 변할 때마다 "버튼 활성화 여부" 검사
    }

    void CheckButtonCondition()
    {
        if (craftButton == null) return;

        bool isFull = true;
        foreach (bool state in slotStates) 
        {
            if (!state)
            {
                isFull = false;
                break;
            }
        }

        craftButton.interactable = isFull;
    }

    //조합하기 버튼 클릭시 실행되는 함수
    public void OnClickCraft()
    {
        //버튼 비활성화해서 중복 클릭 방지
        if (craftButton != null) craftButton.interactable = false;

        photonView.RPC(nameof(RPC_TryCraftItem), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_TryCraftItem()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        //재료 확인
        foreach (bool state in slotStates) 
        {
            if (!state) return;
        }

        //1. 아이템 드랍 (방장이 생성 -> RoomObject라 모두에게 보임)
        Vector3 dropPos = spawnPoint.position;
        dropPos += (Vector3)(Random.insideUnitCircle * 0.2f);
        PhotonNetwork.InstantiateRoomObject(craftResultPrefabName, dropPos, Quaternion.identity);

        for (int i=0; i<slotStates.Length; i++)
        {
            photonView.RPC(nameof(RPC_UpdateSlot), RpcTarget.All, i, false);
        }

    }

    void UpdateSlotUI (int index)
    {
        if (index >= 0 && index < slotUIs.Length) 
        {
            slotUIs[index].UpdateSlotState(slotStates[index]);
        }
    }

    void RefreshAllSlots()
    {
        for (int i=0; i < slotUIs.Length; i++) UpdateSlotUI(i);
        CheckButtonCondition();
    }

    //IContainer 인터페이스 요구사항인데..여기서는 사용X
    public void RemoveItem() {}
}