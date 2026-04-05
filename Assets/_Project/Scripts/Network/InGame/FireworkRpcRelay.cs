using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class FireworkRpcRelay : MonoBehaviourPun
{
    public static FireworkRpcRelay Instance { get; private set; }

    [Header("폭죽 지속 시간")]
    private float defaultDuration = 20f;

    public bool isFireworkActive { get; private set; } = false;

    private void Awake()
    {
        //싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    //인벤에서 폭죽 사용 시 호출
    public void UseFirework(float duration = -1f)
    {
        if (!PhotonNetwork.InRoom) return;

        //duration 음수면 기본값
        if (duration <= 0f) duration = defaultDuration;

        //전 플레이어에게 RPC 전송
        photonView.RPC(nameof(RPC_Firework), RpcTarget.All, duration);
    }

    [PunRPC]
    private void RPC_Firework(float duration)
    {
        SoundManager.instance.SFXPlay("FireworkNoise"); //효과음 재생

        isFireworkActive = true;
        CancelInvoke(nameof(ResetFireworkState)); // (혹시 연속으로 터뜨렸을 때 꼬임 방지)
        Invoke(nameof(ResetFireworkState), duration);

        //씬에서 SightSystemController 찾기
        var sight = FindFirstObjectByType<SightSystemController>();

        if (sight != null)
        {
            sight.TriggerFirework(duration);
        }
        else
        {
            Debug.LogWarning("[FireworkRpcRelay] SightSystemController not found in scene.");
        }
    }

    private void ResetFireworkState()
    {
        isFireworkActive = false;
        Debug.Log("폭죽 효과 종료. 다시 어두워짐 판정 시작!");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}