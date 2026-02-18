using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class FireworkRpcRelay : MonoBehaviourPun
{
    public static FireworkRpcRelay Instance {get; private set;}

    [Header ("폭죽 지속 시간")]
    [SerializeField] private float defaultDuration = 8f;

    private void Awake()
    {
        //싱글톤
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    //인벤에서 폭죽 사용 시 호출
    public void UseFirework(float duration = -1f)
    {
        if(!PhotonNetwork.InRoom) return;

        SoundManager.instance.SFXPlay("FireworkNoise");
        //duration 음수면 기본값
        if(duration <= 0f) duration = defaultDuration;

        //전 플레이어에게 RPC 전송
        photonView.RPC(nameof(RPC_Firework), RpcTarget.All, duration);
    }

    [PunRPC]
    private void RPC_Firework(float duration)
    {
        //씬에서 SightSystemController 찾기
        var sight = FindFirstObjectByType<SightSystemController>();

        if(sight != null)
        {
            sight.TriggerFirework(duration);
        }
        else
        {
            Debug.LogWarning("[FireworkRpcRelay] SightSystemController not found in scene.");
        }
    }
}
