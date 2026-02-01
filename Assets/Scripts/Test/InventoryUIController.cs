using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUIController : MonoBehaviour, IClickHandler
{
    public static InventoryUIController instance;
    public InventoryModel inventoryModel;

    [Header ("인벤토리 UI")]
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI itemText;

    public IInteractable interactTarget;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        //인벤토리 모델이 있으면 바로 연결, 그렇지 않으면 OnPlayerSpawned라는 Action이 발생 시 실행되도록 해당 함수 실행 저장 (옵저버 패턴)
        if (InventoryModel.instance != null) ConnectToModel (InventoryModel.instance);
        else InventoryModel.OnPlayerSpawned += ConnectToModel;
        
    }

    void ConnectToModel(InventoryModel mode)
    {
        inventoryModel = InventoryModel.instance;
        inventoryModel.OnInventoryChanged += UpdateInventoryUI;
        UpdateInventoryUI();
    }

    void UpdateInventoryUI()
    {
        if (inventoryModel.item != null)
        {
            ItemData item = inventoryModel.item;
            itemImage.sprite = item.icon;
            itemText.text = item.itemName;
            itemImage.gameObject.SetActive(true);
            itemText.gameObject.SetActive(true);
        }
        else
        {
            itemImage.gameObject.SetActive(false);
            itemText.gameObject.SetActive(false);
        }
    }

    public void OnClickAction ()
    {
        
        if (inventoryModel.item == null) return;

        if (interactTarget is IContainer container)
        {
            Debug.Log("OnClickInventory 실행됨");
            container.AddItem(inventoryModel.item);
            inventoryModel.RemoveItem();
        }
        else return;
        
    }

    public void setInteractTarget(IInteractable interactTarget)
    {
        this.interactTarget = interactTarget;
    }
}
