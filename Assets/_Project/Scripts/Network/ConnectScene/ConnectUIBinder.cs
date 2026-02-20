using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConnectUIBinder : MonoBehaviour
{
    [Header("NickName")]
    public TMP_InputField nickNameInput;

    [Header("Top Buttons")]
    public Button btnBack;
    public Button btnSettings;

    [Header("Start")]
    public Button btnStart;

    [Header("Create Room Buttons")]
    public Button btnCreateRoom;
    public Button btnPublic;
    public Button btnPrivate;

    [Header("Create Room Popup")]
    public GameObject createRoomPopup;
    public TMP_InputField roomNameInput;
    public TMP_InputField roomPasswordInput;
    public Button btnConfirm;
    public Button btnCancel;

    [Header("Join Password Popup")]
    public GameObject joinPasswordPopup;
    public TMP_InputField joinPasswordInput;
    public Button btnJoinPwConfirm;
    public Button btnJoinPwCancel;

    [Header("Room List")]
    public Transform roomListParent; //Content. Scroll View / Viewport / Content 
    public GameObject roomItemPrefab; //RoomItem prefab
}
