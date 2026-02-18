using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Unity.VisualScripting;
using HashTable = ExitGames.Client.Photon.Hashtable;

public class KillBtnController : MonoBehaviourPunCallbacks
{
    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Button killButton; // 킬 버튼
    [SerializeField] private Image hideImage; // 비활성화 역할 스킬 이미지

    [Header("스킬 설정")]
    [SerializeField] private float coolTime = 40f; // 쿨타임 40초(임시)
    [SerializeField] private float killRange = 1.5f; // 사정거리는 1.5유닛

    private bool isCoolDown = false; // 스킬쿨 세는 중인지


    void Start()
    {
        // 일단 시작하자마자 킬 버튼 비활성화(직업 확인 전이므로)
        hideImage.gameObject.SetActive(false);
        killButton.gameObject.SetActive(false);

        // 킬 버튼은 클릭 이벤트 연결
        killButton.onClick.AddListener(OnClickKillButton);

        // 직업 확인 후 살인자만 KILL button UI 활성화
        CheckJobAndActivateUI();
    }

    void Update()
    {
        // Killer만 스페이스바 스킬 발동되도록
        if(!GameUtils.IsMyPlayerKiller) return;

        // 스페이스바 누르면 스킬 발동 함수 호출
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("스페이스바 눌림!!");
            OnClickKillButton();
        }
    }

    #region [직업 체크 로직]

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 내가 + 직업이 배정(변경)되었다면
        if (targetPlayer.IsLocal && changedProps.ContainsKey("Job"))
        {
            CheckJobAndActivateUI();
        }

    }

    public void CheckJobAndActivateUI()
    {
        if (GameUtils.IsMyPlayerKiller)
        {
            Debug.Log("킬러의 킬 버튼 활성화!");
            killButton.gameObject.SetActive(true);
            killButton.interactable = true;

            // 만약 게임 시작하자마자 스킬 사용 가능 시 hide image는 false
            // 게임 시작 후 쿨타임 똑같이 지나야 스킬 버튼 활성화된다면 true로 변경하기
            hideImage.gameObject.SetActive(false);
        }
    }

    #endregion

    #region [스킬 사용/로직]
    public void OnClickKillButton()
    {
        // 킬 기능: 사거리 내 가장 가까운 플레이어 죽이기
        Debug.Log("킬 버튼 눌림!!");

        if (isCoolDown) return; // 쿨타임 중이라면 무시

        if (Attack()) // 킬 성공 시 코루틴 실행
        {
            Debug.Log("스킬 사용함. 쿨타임 시작");
            StartCoroutine(StartCoolDown());
        }
        else // 킬 실패 시
        {
            // 아무 처리도 하지 않기.
            Debug.Log("스킬 사용 실패. 아무 일도 일어나지 않음..");
        }
    }

    public bool Attack()
    {
        SoundManager.instance.SFXPlay("KnifeSwifting");

        PlayerController targetScript = null; // PlayerController.cs를 가져오기 위해
        GameObject myPlayer = null;
        GameObject closestPlayer = null;
        PhotonView pv = null;
        float closestDistance = Mathf.Infinity;

        // 1) 게임 내 플레이어들(tag가 Player)을 탐색
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // 2) 내 플레이어 가져오기
        foreach (GameObject p in players)
        {
            // 플레이어의 커스텀 프로퍼티 정보 가져오기
            pv = p.GetComponent<PhotonView>();
            if (pv.IsMine)
            {
                myPlayer = p; // 내 플레이어 저장
                break;
            }
        }

        // 3) 살아있고 가장 가까운 생존자 탐색
        foreach (GameObject p in players)
        {
            // 플레이어의 커스텀 프로퍼티 정보 가져오기
            pv = p.GetComponent<PhotonView>();
            bool isDead = (bool)pv.Owner.CustomProperties["IsDead"];

            if (!pv.IsMine && !isDead) // 내 캐릭터가 아니고 상대 플레이어가 살아있다면
            {
                float curDistance = Vector2.Distance(myPlayer.transform.position, p.transform.position);
                if (curDistance < closestDistance)
                {
                    closestDistance = curDistance; // 최솟값 갱신
                    closestPlayer = p; // 가장 가까운 플레이어도 갱신
                }
            }
        }


        // 디버그용
        pv = closestPlayer.GetComponent<PhotonView>();
        Debug.Log($"가장 가까운 플레이어: {pv.Owner.NickName}");

        // 가장 가까운 플레이어의 PlayerController.cs 스크립트를 가져오기
        if (pv != null) targetScript = closestPlayer.GetComponent<PlayerController>();

        // 4) 킬 스킬 사용했을 때 사정거리(1.5유닛) 안 가장 가까운 플레이어 죽이기
        if (closestDistance < killRange)
        {
            Debug.Log($"킬 성공! 사망자: {targetScript.photonView.Owner.NickName}");

            // 5) 타켓 플레이어 사망 처리
            // IsDead = true로 변경
            Hashtable props = new Hashtable();
            props.Add("IsDead", true);
            targetScript.photonView.Owner.SetCustomProperties(props);

            // 가져온 스크립트의 Die() 함수 호출
            targetScript.Die();

            #region [범인태그]
            //밝은 상태에서 Attack했다면
            if (GameStateManager.instance != null && GameStateManager.instance.currentState == GameState.Playing_OnLight)
            {
                //살인마 오브젝트 PhotonView 컴포넌트 가져옴
                PhotonView killerPV = myPlayer.GetComponent<PhotonView>();

                if (killerPV != null)
                    //모든 클라에게 RPC 호출 -> 10초 동안 범인 표시
                    killerPV.RPC("RPC_ShowCriminalTag", RpcTarget.All, 10f);

            }
            #endregion

            return true; // 스킬 사용 성공
        }
        else return false; // 스킬 사용 실패
    }

    // 스킬 쿨타임 360도 돌아가는 거(UI)용 코루틴 함수
    IEnumerator StartCoolDown()
    {
        isCoolDown = true;
        killButton.interactable = false;

        // 덮는 이미지 켜서 360도 돌리기
        hideImage.gameObject.SetActive(true);
        hideImage.fillAmount = 1.0f; // 처음은 360도 꽉 채워서 시작

        float cur = coolTime; // 쿨타임 40초로 시작해서
        while (cur > 0)
        {
            cur -= Time.deltaTime; // 시간 계속 깍기
            hideImage.fillAmount = cur / coolTime; // 360도에서 점점 돌기

            yield return null; // 1프레임 대기
        }

        // 쿨타임 종료 후 다시 버튼 활성화
        isCoolDown = false;
        killButton.interactable = true;
        hideImage.gameObject.SetActive(false);

        Debug.Log("쿨타임 종료.");
    }
}

    #endregion