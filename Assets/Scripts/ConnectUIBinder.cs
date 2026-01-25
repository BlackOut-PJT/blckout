using UnityEngine;
using UnityEngine.UI;

public class ConnectUIBinder : MonoBehaviour
{
    [Header("NickName")]
    public InputField nickNameInput;

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
    public InputField roomNameInput;
    public Button btnConfirm;
    public Button btnCancel;

    [Header("Room List")]
    public Transform roomListParent; //Content. Scroll View / Viewport / Content 
    public GameObject roomItemPrefab; //RoomItem prefab
}
