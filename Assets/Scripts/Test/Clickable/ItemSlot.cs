using UnityEngine;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IClickHandler
{
    [Header ("UI 연결")]
    [SerializeField] private Image iconImage;

    private FurnitureBox furnitureBox;
    private ItemData currentItem;

    //가구 박스가 열릴 때 이 함수를 호출해서 정보를 넘겨줌
    public void Initialize (FurnitureBox box, ItemData item)
    {
        this.furnitureBox = box;
        this.currentItem = item;

        if (item != null)
        {
            iconImage.sprite = item.icon;
            iconImage.gameObject.SetActive(true);
        }
        else iconImage.gameObject.SetActive(false);
    }

    public void OnClickAction()
    {
        if (currentItem == null) return;
        furnitureBox.RemoveItem();
    }
}
