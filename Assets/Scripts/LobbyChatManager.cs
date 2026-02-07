using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LobbyChatManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public ChatUIController chatUI;

    // 중복 방지(가장 확실한 방식)
    private static LobbyChatManager _instance;

    // UI 아직 안 잡혔을 때 메시지 저장
    private readonly Queue<string> _pending = new Queue<string>();

    // 시스템 중복 제거
    private string _lastSystem;
    private float _lastSystemTime;
    private const float SYSTEM_DEDUP_WINDOW = 0.5f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (photonView == null)
            Debug.LogError("[Chat] PhotonView is missing on LobbyChatManager!");
    }

    private void Start()
    {
        Debug.Log($"[Chat] Start Connected={PhotonNetwork.IsConnected} InRoom={PhotonNetwork.InRoom} Nick={PhotonNetwork.NickName}");

        // 혹시 UI가 이미 존재하면 연결 시도 + 밀린 메시지 flush
        EnsureUI();
        FlushPending();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Chat] OnJoinedRoom! IsMaster: {PhotonNetwork.IsMasterClient}");

        string msg = PhotonNetwork.IsMasterClient 
            ? $"[SYSTEM] {PhotonNetwork.NickName} created the room (Master)" 
            : $"[SYSTEM] {PhotonNetwork.NickName} joined";

        EnqueueOrAdd(msg);
    }

    private IEnumerator CoLocalJoinedAndNotifyOthers()
    {
        // UI가 늦게 올라와도 상관없게: 큐에 넣고 나중에 flush
        EnqueueOrAdd($"[SYSTEM] {PhotonNetwork.NickName} joined");

        // 다른 사람들에게만 알림
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            photonView.RPC(nameof(RPC_ReceiveSystem), RpcTarget.Others, $"{PhotonNetwork.NickName} joined");

        yield break;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 오직 방장(MasterClient)만 이 RPC를 발신하도록 제한하여 중복을 막습니다.
        if (PhotonNetwork.IsMasterClient)
        {
            // RpcTarget.All을 통해 새로 들어온 사람 포함 모두에게 메시지 전송
            photonView.RPC(nameof(RPC_ReceiveSystem), RpcTarget.All, $"{newPlayer.NickName} joined");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_ReceiveSystem), RpcTarget.All, $"{otherPlayer.NickName} left");
        }
    }

    public void SendChat(string msg)
    {
        msg = (msg ?? "").Trim();
        if (string.IsNullOrEmpty(msg)) return;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            EnqueueOrAdd("[SYSTEM] Not connected / not in room");
            return;
        }

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[Chat] SendChat => actor={actorNumber} msg={msg} viewID={photonView.ViewID}");

        photonView.RPC(nameof(RPC_ReceiveChat), RpcTarget.All, actorNumber, msg);
    }

    [PunRPC]
    private void RPC_ReceiveChat(int actorNumber, string msg)
    {
        var p = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
        string displayNick = (p != null && !string.IsNullOrEmpty(p.NickName)) ? p.NickName : $"P{actorNumber}";
        string formatted = $"[{displayNick}] {msg}";

        Debug.Log($"[Chat] RPC_ReceiveChat ARRIVED on {PhotonNetwork.NickName} chatUI_null={(chatUI == null)}");

        EnqueueOrAdd(formatted);
    }

    public void SendSystem(string msg)
    {
        msg = (msg ?? "").Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // 송신단 중복 제거(선택)
        if (msg == _lastSystem && Time.time - _lastSystemTime < SYSTEM_DEDUP_WINDOW)
            return;
        _lastSystem = msg;
        _lastSystemTime = Time.time;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            EnqueueOrAdd($"[SYSTEM] {msg}");
            return;
        }

        photonView.RPC(nameof(RPC_ReceiveSystem), RpcTarget.All, msg);
    }

    [PunRPC]
    private void RPC_ReceiveSystem(string msg)
    {
        msg = (msg ?? "").Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // 수신단 중복 제거(강력)
        if (msg == _lastSystem && Time.time - _lastSystemTime < SYSTEM_DEDUP_WINDOW)
            return;
        _lastSystem = msg;
        _lastSystemTime = Time.time;

        EnqueueOrAdd($"[SYSTEM] {msg}");
    }

    private void EnqueueOrAdd(string line)
    {
        EnsureUI();
        if (chatUI == null)
        {
            _pending.Enqueue(line);
            return;
        }

        chatUI.AddMessage(line);
    }

    private void FlushPending()
    {
        if (chatUI == null) return;

        while (_pending.Count > 0)
            chatUI.AddMessage(_pending.Dequeue());
    }

    private void EnsureUI()
    {
        if (chatUI == null)
            chatUI = FindAnyObjectByType<ChatUIController>();
    }

    // BindUI가 호출될 때 즉시 Flush 하도록 보강
    public void BindUI(ChatUIController ui)
    {
        Debug.Log("[Chat] UI Bound to Manager");
        chatUI = ui;
        EnsureUI(); // 한 번 더 체크
        FlushPending();
    }
}



