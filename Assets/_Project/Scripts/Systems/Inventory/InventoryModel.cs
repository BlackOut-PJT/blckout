using UnityEngine;
using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class InventoryModel : MonoBehaviourPunCallbacks
{
    public static InventoryModel instance;

    public static event Action<InventoryModel> OnPlayerSpawned;

    public List<ItemData> items = new List<ItemData>();
    public int maxSlots = 1;

    public ItemData item => items.Count > 0 ? items[0] : null;

    // 💡 수정됨: 리스트 전체 크기가 아니라, '0번 손'에 물건이 있는지만 검사합니다.
    public bool IsFull => items.Count > 0 && items[0] != null;

    public Action OnInventoryChanged;
    public Action OnInventoryFull;

    void Awake()
    {
        if (photonView.IsMine)
        {
            instance = this;
            UpdateMaxSlots();
            OnPlayerSpawned?.Invoke(this);
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer != PhotonNetwork.LocalPlayer) return;
        if (!photonView.IsMine) return;

        if (changedProps.ContainsKey("Job"))
        {
            UpdateMaxSlots();
            OnInventoryChanged?.Invoke();

            if ((string)changedProps["Job"] == "Killer")
                GiveKnifeToKiller();
        }
    }

    private void UpdateMaxSlots()
    {
        object jobObj;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Job", out jobObj))
        {
            string job = (string)jobObj;
            maxSlots = (job == "Killer") ? 2 : 1;
        }
        else
        {
            maxSlots = 1;
        }

        // 💡 핵심: maxSlots 개수만큼 미리 빈칸(null)을 만들어 배열처럼 고정시킵니다.
        // 크루원은 1칸, 킬러는 2칸의 null이 미리 생깁니다.
        while (items.Count < maxSlots)
        {
            items.Add(null);
        }
    }

    public bool AddItem(ItemData item)
    {
        if (IsFull)
        {
            OnInventoryFull?.Invoke();
            return false;
        }
        
        // 💡 수정됨: items.Add()로 리스트를 늘리는 대신, 0번 자리에 덮어씌웁니다.
        items[0] = item; 
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem()
    {
        if (items.Count > 0 && items[0] != null)
        {
            // 💡 수정됨: RemoveAt(0)으로 칼을 당겨오지 않고, 0번 자리만 깔끔하게 비웁니다.
            items[0] = null; 
            OnInventoryChanged?.Invoke();
        }
    }

    public ItemData DropItem()
    {
        if (items.Count <= 0 || items[0] == null) return null;

        ItemData dropItem = items[0];
        // 💡 수정됨: 버릴 때도 0번 자리만 비웁니다.
        items[0] = null; 
        OnInventoryChanged?.Invoke();

        return dropItem;
    }

    private void GiveKnifeToKiller()
    {
        ItemData knife = ItemManager.instance?.GetItem(4);
        if (knife == null) return;
        if (items.Exists(i => i != null && i.itemID == 4)) return;
        
        // 💡 수정됨: UpdateMaxSlots에서 1번 방(null)을 미리 만들어 뒀으니, 바로 덮어씁니다.
        if (items.Count > 1)
        {
            items[1] = knife;
            OnInventoryChanged?.Invoke();
        }
    }

    public void UseItem()
    {
        if (!photonView.IsMine) return;
        if (item == null) return;

        if (item.itemID == 3)
        {
            FireworkRpcRelay.Instance?.UseFirework(3f);
            RemoveItem();
            return;
        }

        Debug.Log($"[InventoryModel] UseItem not implemented for itemID={item.itemID}");
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}