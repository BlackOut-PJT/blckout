using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager instance;

    [Header("ItemDatabase")]
    public ItemDatabase itemDatabase;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public ItemData GetItem (int id)
    {
        return itemDatabase.GetItemByID(id);
    }
}
