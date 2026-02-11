using Photon.Pun;
using Photon.Realtime;

public static class GameUtils
{
    // 내 플레이어의 생존 여부 파악용 전역 함수
    public static bool IsMyPlayerDead
    {
        get
        {
            if(PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsDead", out object isDead))
            {
                return (bool)isDead;
            }
            return false;
        }
    }
}
