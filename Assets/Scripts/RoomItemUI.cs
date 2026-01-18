using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;

//방 하나 표시하는 UI 프리팹 전용 스크립트
public class RoomItemUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private Text roomName;//방 이름 표시 txt
    [SerializeField] private Text playerCount;//현재인원수 txt

    [Header("Button")]
    [SerializeField] private Button btnJoin;      //참여하기 버튼(Inspector에 연결)

    private string cachedRoomName;//버튼 클릭 때 넘길 방 이름 변수

    //방 정보 받아서 UI 세팅
    public void Set(RoomInfo info)
    {
        cachedRoomName = info.Name;
        //방 이름 표시
        if(roomName != null)
            roomName.text = info.Name;
        
        //현재 방 인원 수 표시
        if(playerCount != null)
            playerCount.text = $"{info.PlayerCount}명";

        //버튼 리스너를 코드로 고정 (중복 방지 포함)
        if (btnJoin != null)
        {
            btnJoin.onClick.RemoveAllListeners();
            btnJoin.onClick.AddListener(OnJoinButtonClicked);
        }
    }

    //참여하기 버튼 클릭 시 호출
    public void OnJoinButtonClicked()
    {
        if(string.IsNullOrEmpty(cachedRoomName)) return;
        NetworkManager.Instance.JoinRoom(cachedRoomName);
    }
}
