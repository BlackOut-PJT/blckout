using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LobbyChatManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public ChatUIController chatUI;

    private void Awake()
    {
        // 씬에 2개 생기는 경우 방지(특히 DontDestroyOnLoad 썼을 때)
        var all = FindObjectsOfType<LobbyChatManager>(true);
        if (all.Length > 1)
        {
            Debug.LogWarning("[Chat] Duplicate LobbyChatManager detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }

        // PhotonView 필수 확인
        if (photonView == null)
            Debug.LogError("[Chat] PhotonView is missing on LobbyChatManager!");

        // chatUI가 비어있으면 자동으로 찾아보기
        if (chatUI == null)
            chatUI = FindAnyObjectByType<ChatUIController>();
    }

    private void Start()
    {
        Debug.Log($"[Chat] Start Connected={PhotonNetwork.IsConnected} InRoom={PhotonNetwork.InRoom} Nick={PhotonNetwork.NickName}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SendSystem($"{newPlayer.NickName} joined");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SendSystem($"{otherPlayer.NickName} left");
    }

    public void SendChat(string msg)
    {
        msg = (msg ?? "").Trim();
        if (string.IsNullOrEmpty(msg)) return;

        // 룸 밖이면 RPC 못 보냄 → UI에 안내만
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[Chat] Not connected / not in room. Printing local only.");
            EnsureUI();
            chatUI?.AddMessage($"[SYSTEM] Not connected / not in room");
            return;
        }

        string nick = string.IsNullOrEmpty(PhotonNetwork.NickName) ? "Unknown" : PhotonNetwork.NickName;

        Debug.Log($"[Chat] SendChat => {nick} : {msg}");
        photonView.RPC(nameof(RPC_ReceiveChat), RpcTarget.All, nick, msg);
    }

    [PunRPC]
    private void RPC_ReceiveChat(string nick, string msg)
    {
        EnsureUI();

        // ✅ 원하는 포맷: [플레이어이름] 보낸 채팅
        string formatted = $"[{nick}] {msg}";

        Debug.Log($"[Chat] RPC_ReceiveChat => {formatted} (chatUI null? {chatUI == null})");
        chatUI?.AddMessage(formatted);
    }

    public void SendSystem(string msg)
    {
        msg = (msg ?? "").Trim();
        if (string.IsNullOrEmpty(msg)) return;

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            EnsureUI();
            chatUI?.AddMessage($"[SYSTEM] {msg}");
            return;
        }

        photonView.RPC(nameof(RPC_ReceiveSystem), RpcTarget.All, msg);
    }

    [PunRPC]
    private void RPC_ReceiveSystem(string msg)
    {
        EnsureUI();
        chatUI?.AddMessage($"[SYSTEM] {msg}");
    }

    private void EnsureUI()
    {
        if (chatUI == null)
            chatUI = FindAnyObjectByType<ChatUIController>();
    }
}


