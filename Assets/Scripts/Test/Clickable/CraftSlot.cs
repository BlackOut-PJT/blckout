using UnityEngine;
using UnityEngine.UI;

public class CraftSlot : MonoBehaviour, IClickHandler
{
    [Header ("설정")]
    public int slotIndex; //0:성냥, 1:종이, 2: 화약

    [Header ("UI 연결")]
    [SerializeField] private Image iconImage;

    private CraftingBox craftingBox;
    private bool hasItem = false;

    public void Initialize (CraftingBox box)
    {
        this.craftingBox = box;
    }

    //상태갱신 CraftingBox가 호출
    public void UpdateSlotState (bool hasItem)
    {
        this.hasItem = hasItem;

        if (hasItem) SetTransparency(1.0f);
        else SetTransparency(0.4f);
        
    }
    public void OnClickAction()
    {
        if (!hasItem) return;

        craftingBox.TryRetrieveItem(slotIndex);
    }

    public void SetTransparency (float alpha)
    {
        if (iconImage == null) return;
        
        Color tempColor = iconImage.color;
        tempColor.a = alpha;
        iconImage.color = tempColor;
    }
}
