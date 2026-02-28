using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

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

        // 방 이름을 시드로 사용 → 모든 클라이언트가 동일한 셔플 결과
        int seed = PhotonNetwork.CurrentRoom != null
            ? PhotonNetwork.CurrentRoom.Name.GetHashCode()
            : 0;
        System.Random rng = new System.Random(seed);

        // 이름순 정렬 (FindObjectsOfType 순서가 클라이언트마다 다를 수 있음)
        List<FurnitureBox> sorted = allBoxes.OrderBy(x => x.gameObject.name).ToList();

        // Fisher-Yates 셔플 (결정론적 — 같은 시드면 같은 결과)
        for (int j = sorted.Count - 1; j > 0; j--)
        {
            int k = rng.Next(j + 1);
            var temp = sorted[j];
            sorted[j] = sorted[k];
            sorted[k] = temp;
        }

        int index = 0;

        // 1단계: 필수 아이템 배치 (화약, 성냥, 종이 각 1개 보장)
        foreach (int itemID in guaranteedItemIDs)
        {
            if (index >= sorted.Count) break;
            ItemData item = ItemManager.instance.GetItem(itemID);
            if (item == null)
            {
                Debug.LogWarning($"[ItemRandomizer_JY] itemID={itemID} 못 찾음");
                continue;
            }
            sorted[index].itemData = item;
            Debug.Log($"[ItemRandomizer_JY] {sorted[index].name} ← {item.itemName} (필수)");
            index++;
        }

        // 2단계: 남은 가구에 랜덤 배치
        for (int i = index; i < sorted.Count; i++)
        {
            if (extraItemIDs != null && extraItemIDs.Length > 0)
            {
                int randomID = extraItemIDs[rng.Next(0, extraItemIDs.Length)];
                ItemData item = ItemManager.instance.GetItem(randomID);
                sorted[i].itemData = item;
                Debug.Log($"[ItemRandomizer_JY] {sorted[i].name} ← {(item != null ? item.itemName : "null")} (랜덤)");
            }
            else
            {
                sorted[i].itemData = null;
            }
        }

        Debug.Log($"[ItemRandomizer_JY] 총 {allBoxes.Length}개 가구에 아이템 배치 완료 (seed={seed})");
    }
}
