using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class KillBtnController : MonoBehaviourPunCallbacks
{
    [SerializeField] private Button killButton; // 킬 버튼
    
    void Start()
    {
        Debug.Log("KillBtnController.cs 시작");
        
        // 일단 시작하자마자 킬 버튼 비활성화
        killButton.gameObject.SetActive(false);
        killButton.onClick.AddListener(OnClickKillButton);
        
        // 직업 확인 후 살인자만 KILL button UI 활성화
        CheckJobAndActivateUI();
    }

    public void CheckJobAndActivateUI()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("Job"))
        {
            string job = (string)PhotonNetwork.LocalPlayer.CustomProperties["Job"];
            
            if(job == "Killer") // 직업이 킬러인 사람만 킬 버튼 활성화
            {   
                Debug.Log("킬러의 킬 버튼 활성화!");
                killButton.gameObject.SetActive(true);
                killButton.interactable = true;
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 내가 + 직업이 배정(변경)되었다면
        if(targetPlayer.IsLocal && changedProps.ContainsKey("Job"))
        {
            CheckJobAndActivateUI();
        }
        
    }

    public void OnClickKillButton()
    {
        // 킬 버튼 이미지를 kill(active) -> kill(inactive)으로 변경
        // 킬 기능: 사거리 내 가장 가까운 플레이어 죽이기

        Debug.Log("킬 버튼 눌림!!");
    }
}
