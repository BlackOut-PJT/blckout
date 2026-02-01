using UnityEngine;
using System;
using Photon.Pun;
using Photon.Realtime;

public class InventoryModel : MonoBehaviourPun
{
    public static InventoryModel instance;

    //플레이어 인벤토리 모델이 생서된 것을 알리는 정적 이벤트
    //매개변수로 자기자신(InventoryModel)을 넘겨줌
    public static event Action<InventoryModel> OnPlayerSpawned;

    public ItemData item;
    public Action OnInventoryChanged;

    void Awake()
    {
        if (photonView.IsMine)  {
            instance = this;
            //플레이어 생서되면 => InventoryModel을 InventoryUIController 쪽으로 전달.
            OnPlayerSpawned?.Invoke(this);
        }
    }

    public void AddItem (ItemData item)
    {
        this.item = item;
        OnInventoryChanged?.Invoke();
    }

    public void RemoveItem()
    {
        this.item = null;
        OnInventoryChanged?.Invoke();
    }

    void RefreshUI ()
    {
        
    }
}
