using Photon.Pun;
using UnityEngine;

public class LobbyChatManager : MonoBehaviourPunCallbacks
{
    public ChatUIController chatUI;

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        SendSystem($"{newPlayer.NickName} joined");
    }


    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        SendSystem($"{otherPlayer.NickName} left");
    }

    public void SendChat(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;

        string nick = PhotonNetwork.NickName;
        photonView.RPC(nameof(RPC_ReceiveChat), RpcTarget.All, nick, msg);
    }

    [PunRPC]
    void RPC_ReceiveChat(string nick, string msg)
    {
        chatUI.AddMessage($"{nick}: {msg}");
    }

    public void SendSystem(string msg)
    {
        photonView.RPC(nameof(RPC_ReceiveSystem), RpcTarget.All, msg);
    }

    [PunRPC]
    void RPC_ReceiveSystem(string msg)
    {
        chatUI.AddMessage($"[SYSTEM] {msg}");
    }

}

