using UnityEngine;

public enum ItemType
{
    Material, // 재료
    Tool      // 도구
}

[CreateAssetMenu(menuName = "Game/Item Data", fileName = "Item_")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public ItemType itemType;

    [Header("Visual")]
    public Sprite icon; // 임시 아이콘(색깔 스프라이트)

    [Header("Rule")]
    public bool killerOnly; // 칼 같은 전용템
}

