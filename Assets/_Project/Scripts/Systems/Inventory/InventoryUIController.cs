using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIController : MonoBehaviour, IClickHandler
{
    public static InventoryUIController instance;
    public InventoryModel inventoryModel;

    [Header("인벤토리 UI")]
    [SerializeField] private Image itemImage;       // Slot_0 안의 아이콘
    [SerializeField] private TextMeshProUGUI itemText;
    private List<Button> slotUseButtons = new List<Button>();//사용하기 버튼

    [Header("인벤토리 꽉 참 메시지")]
    [SerializeField] private TextMeshProUGUI fullMessageText;

    public IInteractable interactTarget;

    // 멀티슬롯 관리
    private List<GameObject> slotRoots = new List<GameObject>();   // Slot_0, Slot_1, ...
    private List<Image> slotIcons = new List<Image>();             // 각 슬롯의 ItemIcon

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // fullMessageText 초기 숨김
        if (fullMessageText != null) fullMessageText.gameObject.SetActive(false);

        //인벤토리 모델이 있으면 바로 연결, 그렇지 않으면 OnPlayerSpawned라는 Action이 발생 시 실행되도록 해당 함수 실행 저장 (옵저버 패턴)
        if (InventoryModel.instance != null) ConnectToModel(InventoryModel.instance);
        else InventoryModel.OnPlayerSpawned += ConnectToModel;
    }

    void ConnectToModel(InventoryModel model)
    {
        inventoryModel = InventoryModel.instance;
        inventoryModel.OnInventoryChanged += UpdateInventoryUI;
        inventoryModel.OnInventoryFull += ShowFullMessage;

        // Slot_0 등록
        if (itemImage != null)
        {
            GameObject slot0 = itemImage.transform.parent.gameObject;
            slotRoots.Add(slot0);
            slotIcons.Add(itemImage);

            Button btn0 = slot0.transform.Find("Button")?.GetComponent<Button>();
            if (btn0 == null) btn0 = slot0.GetComponentInChildren<Button>();
            slotUseButtons.Add(btn0);
        }

        EnsureSlotCount(inventoryModel.maxSlots);
        UpdateInventoryUI();
    }

    void EnsureSlotCount(int count)
    {
        while (slotRoots.Count < count)
        {
            GameObject template = slotRoots[0];
            GameObject newSlot = Instantiate(template, template.transform.parent);
            newSlot.name = "Slot_" + slotRoots.Count;

            // 복제된 슬롯에서 InventoryUIController 제거 (무한 증식 방지)
            var dupController = newSlot.GetComponent<InventoryUIController>();
            if (dupController != null) Destroy(dupController);

            // 위치: 이전 슬롯 아래에 배치
            RectTransform prevRT = slotRoots[slotRoots.Count - 1].GetComponent<RectTransform>();
            RectTransform newRT = newSlot.GetComponent<RectTransform>();
            float slotHeight = prevRT.rect.height;
            newRT.anchoredPosition = prevRT.anchoredPosition + new Vector2(0, -(slotHeight + 8f + 40f));

            // 자식에서 아이콘 찾기
            Image icon = newSlot.transform.Find(itemImage.gameObject.name)?.GetComponent<Image>();
            if (icon == null) icon = newSlot.GetComponentInChildren<Image>();

            // 자식에서 버튼 찾기
            Button btn = newSlot.transform.Find("Button")?.GetComponent<Button>();
            if (btn == null) btn = newSlot.GetComponentInChildren<Button>();

            slotRoots.Add(newSlot);
            slotIcons.Add(icon);
            slotUseButtons.Add(btn);
        }

        // 불필요한 슬롯 숨기기
        for (int i = 0; i < slotRoots.Count; i++)
        {
            slotRoots[i].SetActive(i < count);
        }
    }

    void UpdateInventoryUI()
    {
        int slotCount = inventoryModel.maxSlots;
        EnsureSlotCount(slotCount);

        for (int i = 0; i < slotRoots.Count; i++)
        {
            if (i >= slotCount)
            {
                slotRoots[i].SetActive(false);
                continue;
            }

            slotRoots[i].SetActive(true);

            if (i < inventoryModel.items.Count && inventoryModel.items[i] != null)
            {
                ItemData data = inventoryModel.items[i];
                slotIcons[i].sprite = data.icon;
                slotIcons[i].gameObject.SetActive(true);
            }
            else
            {
                slotIcons[i].gameObject.SetActive(false);
            }
        }

        // 텍스트는 첫 번째 아이템 기준 (기존 동작 유지)
        if (inventoryModel.item != null)
        {
            itemText.text = inventoryModel.item.itemName;
            itemText.gameObject.SetActive(true);
        }
        else
        {
            itemText.gameObject.SetActive(false);
        }

        //사용하기 버튼 표시/숨김
        bool isKiller = inventoryModel.maxSlots == 2;

        for (int i = 0; i < slotUseButtons.Count; i++)
        {
            if (slotUseButtons[i] == null) continue;

            bool hasItem = i < inventoryModel.items.Count && inventoryModel.items[i] != null;
            bool showButton = hasItem;

            // 킬러의 2번 슬롯(인덱스 1, 칼 슬롯)은 버튼 숨김
            if (isKiller && i == 1)
                showButton = false;

            slotUseButtons[i].gameObject.SetActive(showButton);
        }
    }

    void ShowFullMessage()
    {
        if (fullMessageText == null) return;
        StopCoroutine(nameof(FullMessageCoroutine));
        StartCoroutine(nameof(FullMessageCoroutine));
    }

    IEnumerator FullMessageCoroutine()
    {
        fullMessageText.text = "손이 꽉\n찼습니다!";
        fullMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        fullMessageText.gameObject.SetActive(false);
    }

    public void OnClickAction()
    {
        if (inventoryModel.item == null) return;

        if (interactTarget is IContainer container)
        {
            Debug.Log("OnClickInventory 실행됨");
            SoundManager.instance.UISoundPlay("ButtonClick");
            container.AddItem(inventoryModel.item);
            inventoryModel.RemoveItem();
        }
        else return;
    }

    public void setInteractTarget(IInteractable interactTarget)
    {
        this.interactTarget = interactTarget;
    }

    ////추가
    public void OnClickUseItem()
    {
        SoundManager.instance.SFXPlay("ButtonClick");
        if (inventoryModel == null) return;
        inventoryModel.UseItem();
    }

    public void HideAllSlots()
    {
        foreach (var slot in slotRoots)
            slot.SetActive(false);
    }

    public void ShowAllSlots()
    {
        UpdateInventoryUI();
    } 

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        InventoryModel.OnPlayerSpawned -= ConnectToModel;

        if (inventoryModel != null)
        {
            inventoryModel.OnInventoryChanged -= UpdateInventoryUI;
            inventoryModel.OnInventoryFull -= ShowFullMessage;
        }
    }
}
