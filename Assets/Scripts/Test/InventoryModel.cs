using UnityEngine;
using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class InventoryModel : MonoBehaviourPunCallbacks
{
    public static InventoryModel instance;

    //플레이어 인벤토리 모델이 생서된 것을 알리는 정적 이벤트
    //매개변수로 자기자신(InventoryModel)을 넘겨줌
    public static event Action<InventoryModel> OnPlayerSpawned;

    public List<ItemData> items = new List<ItemData>();
    public int maxSlots = 1;

    // 하위호환용 프로퍼티: 기존 코드에서 inventoryModel.item 접근하는 곳 유지
    public ItemData item => items.Count > 0 ? items[0] : null;

    public bool IsFull => items.Count >= maxSlots;

    public Action OnInventoryChanged;
    public Action OnInventoryFull;

    void Awake()
    {
        if (photonView.IsMine)
        {
            instance = this;
            UpdateMaxSlots();
            //플레이어 생서되면 => InventoryModel을 InventoryUIController 쪽으로 전달.
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
    }

    public bool AddItem(ItemData item)
    {
        if (IsFull)
        {
            OnInventoryFull?.Invoke();
            return false;
        }
        items.Add(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void RemoveItem()
    {
        if (items.Count > 0)
        {
            items.RemoveAt(0);
            OnInventoryChanged?.Invoke();
        }
    }

    public void RemoveItem(ItemData target)
    {
        if (items.Remove(target))
        {
            OnInventoryChanged?.Invoke();
        }
    }

    ////useitem 추가
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
}
