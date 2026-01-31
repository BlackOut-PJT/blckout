using UnityEngine;
using System.Collections.Generic;
using System.Linq; //리스트 검색용

[CreateAssetMenu(menuName = "Game/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems;

    public ItemData GetItemByID (int id)
    {
        return allItems.Find(item => item.itemID == id);
    }
}
