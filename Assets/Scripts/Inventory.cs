using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Inventory : MonoBehaviour
{   

    [Header("UI")]
    [SerializeField] private Image slotIcon;
    [SerializeField] private GameObject iconRoot; // slotIcon 부모거나 slotIcon 자체

    [Header("State")]
    public ItemData currentItem;

    private void Awake()
    {
        RefreshUI();
    }

    public bool HasItem => currentItem != null;

    // B가 호출할 함수 (성공하면 true)
    public bool TryAddItem(ItemData item)
    {
        if (item == null) return false;

        // 슬롯 1개니까: 이미 있으면 못 줍게(일단은)
        if (currentItem != null)
        {
            Debug.Log("[Inventory] Slot already occupied.");
            return false;
        }

        currentItem = item;
        RefreshUI();

        Debug.Log($"[Inventory] Picked up: {item.itemName}");
        return true;
    }

    public void Clear()
    {
        currentItem = null;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (iconRoot == null) iconRoot = slotIcon != null ? slotIcon.gameObject : null;

        if (slotIcon == null || iconRoot == null) return;

        if (currentItem == null)
        {
            slotIcon.sprite = null;
            iconRoot.SetActive(false);
        }
        else
        {
            slotIcon.sprite = currentItem.icon;
            iconRoot.SetActive(true);
        }
    }

    public void GetItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[Inventory] GetItem called with null item");
            return;
        }


        currentItem = item;


        if (slotIcon != null)
            slotIcon.sprite = item.icon;


        if (iconRoot != null)
            iconRoot.SetActive(true);


        Debug.Log("[Inventory] GetItem: " + item.itemName);
    }
}

