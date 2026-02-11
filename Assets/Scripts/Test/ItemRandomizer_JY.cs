using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemRandomizer_JY : MonoBehaviour
{
    [Header("필수 아이템 ID (반드시 1개씩 배치)")]
    public int[] guaranteedItemIDs = { 0, 1, 2 }; // 0=화약, 1=성냥, 2=종이

    [Header("남는 가구에 뿌릴 아이템 ID (비우면 빈 상태)")]
    public int[] extraItemIDs = { 0, 1, 2 }; // 랜덤 풀

    void Start()
    {
        if (ItemManager.instance == null || ItemManager.instance.itemDatabase == null)
        {
            Debug.LogWarning("[ItemRandomizer_JY] ItemManager 또는 ItemDatabase 없음");
            return;
        }

        FurnitureBox[] allBoxes = FindObjectsOfType<FurnitureBox>();
        if (allBoxes.Length == 0)
        {
            Debug.LogWarning("[ItemRandomizer_JY] FurnitureBox가 씬에 없습니다.");
            return;
        }

        // 셔플
        List<FurnitureBox> shuffled = allBoxes.OrderBy(x => Random.value).ToList();

        int index = 0;

        // 1단계: 필수 아이템 배치 (화약, 성냥, 종이 각 1개 보장)
        foreach (int itemID in guaranteedItemIDs)
        {
            if (index >= shuffled.Count) break;
            ItemData item = ItemManager.instance.GetItem(itemID);
            if (item == null)
            {
                Debug.LogWarning($"[ItemRandomizer_JY] itemID={itemID} 못 찾음");
                continue;
            }
            shuffled[index].itemData = item;
            Debug.Log($"[ItemRandomizer_JY] {shuffled[index].name} ← {item.itemName} (필수)");
            index++;
        }

        // 2단계: 남은 가구에 랜덤 배치
        for (int i = index; i < shuffled.Count; i++)
        {
            if (extraItemIDs != null && extraItemIDs.Length > 0)
            {
                int randomID = extraItemIDs[Random.Range(0, extraItemIDs.Length)];
                ItemData item = ItemManager.instance.GetItem(randomID);
                shuffled[i].itemData = item;
                Debug.Log($"[ItemRandomizer_JY] {shuffled[i].name} ← {(item != null ? item.itemName : "null")} (랜덤)");
            }
            else
            {
                shuffled[i].itemData = null;
            }
        }

        Debug.Log($"[ItemRandomizer_JY] 총 {allBoxes.Length}개 가구에 아이템 배치 완료");
    }
}
