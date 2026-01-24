using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class VotingManager : MonoBehaviourPunCallbacks
{
    
    #region 입력/설정용
    [Header("연결")]
    public GameObject voteSlotPrefab;
    public Transform listContent; //GridLayout 있는 Content 오브젝트
    public Button skipButton; //투표 기권 버튼
    public TextMeshProUGUI totalVoteStatusText;
    #endregion

    //생성한 슬롯을 관리하는 사전 (Key: 플레이어 번호, Value: 슬롯 스크립트)
    private Dictionary<int, VoteSlot> slotList = new Dictionary<int, VoteSlot>();

    //자신이 투표했는지 여부 (중복 투표 방지)
    private bool hasVoted = false;

    #region 방장만 사용할 변수
    //[방장만] 투표 집계용 (Key: 지목당한 사람 ID, Value: 득표수)
    private Dictionary<int, int> voteResults = new Dictionary<int, int>();
    private int currentVoteCount = 0;
    #endregion

    //패널 켜질때(회의 시작) 시 자동으로 실행됨
    public override void OnEnable()
    {
        base.OnEnable();
        GeneratePlayerList();
        hasVoted = false;

        //초기화용
        voteResults.Clear();
        currentVoteCount = 0;

        if (skipButton != null) skipButton.interactable = true;
       
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    void GeneratePlayerList()
    {   
        //기존 슬롯 지우기
        foreach (Transform child in listContent)
        {
            Destroy(child.gameObject);
        }
        slotList.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {   
            //프리팹 생성
            GameObject gameObject = Instantiate(voteSlotPrefab, listContent);
            VoteSlot slot = gameObject.GetComponent<VoteSlot>();

            //죽은 플레이어인지 확인 (후에 CustomProperties 사용 예정. 현재는 false로 둠)
            bool isDead = false;
            // if (player.CustomProperties.ContainsKey("IsDead")) isDead = (bool)player.CustomProperties["IsDead"];

            //슬롯 데이터 세팅 (이름, 번호, 사망여부)
            slot.Setup(player.NickName, player.ActorNumber, isDead);
            slotList.Add(player.ActorNumber, slot);

            //슬롯 클릭이벤트 연결
            //버튼 클릭 시 그 슬롯의 'targetActorNumber'를 가지고 Vote함수 실행
            Button btn = gameObject.GetComponent<Button>();
            int targetID = player.ActorNumber;
            btn.onClick.AddListener(() => OnClickVote(targetID));
        }
    }

    public void OnClickVote(int targetID)
    {
        if (hasVoted) return; //이미 투표했으면 차단

        photonView.RPC("RPC_CastVote", RpcTarget.All, targetID);
    }

    public void OnClickSkip()
    {
        OnClickVote(-1);
    }

    [PunRPC]
    void RPC_CastVote (int targetID, PhotonMessageInfo info)
    {
        //info.Sender: 해당 RPC를 보낸 사람 (투표한 사람)
        int voterID = info.Sender.ActorNumber;

        //1. 투표한 사람에게 슬롯에 '체크표시'
        if (slotList.ContainsKey(voterID))
        {
            slotList[voterID].ShowVoteComplete();
        }

        //2. 내가 투표한거라면 더 이상 버튼 누르지 못하게 비활성화
        if (PhotonNetwork.LocalPlayer.ActorNumber == voterID)
        {
            hasVoted = true;
            skipButton.interactable = false;

            //모든 슬롯 비활성화 (선택X)
            foreach (var slot in slotList.Values)
            {
                slot.GetComponent<Button>().interactable = false;
            }

            Debug.Log("투표 완료! 결과 대기중...");
        }

        #region 방장만 가지는 로직
        //3. 투표 데이터 집계
        if (PhotonNetwork.IsMasterClient)
        {
            if (voteResults.ContainsKey(targetID))
            {
                voteResults[targetID]++;
            }
            else
            {
                voteResults.Add(targetID, 1);
            }

            currentVoteCount++;
            Debug.Log("$방장 집계중: {targetID}번 플레이어가 1표 받음. (현재 총 {currentVoteCount}표)");

            int totalLivingPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            //추후에 CustomProperties 플레이어에 세팅 후 주석 풀기 (죽은 사람 빼고 세는 부분) + 위에 totalPlayers = 0 처리!
            // foreach (Player p in PhotonNetwork.PlayerList)
            // {
            //     object isDeadValue;
            //     if (p.CustomProperties.TryGetValue("IsDead", out isDeadValue) && (bool)isDeadValue) continue;
            //     totalLivingPlayers++;
            // }

            if (currentVoteCount >= totalLivingPlayers)
            {
                Debug.Log("전원 투표 완료");
                FinishVote();
            }
        }
        #endregion
    }

    #region 방장만 가지는 메소드 로직
    void FinishVote()
    {
        //최다득표자 계산 + 결과 통보
        //CaculateResult();

        if (GameStateManager.instance != null) GameStateManager.instance.EndVoting();
        
    }
    #endregion
}