using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoteSlot : MonoBehaviour
{
    [Header("기본 연결")]
    public Image playerSlotBackground;
    public TextMeshProUGUI playerNameText;
    public Image playerImage;
    public GameObject borderline;

    [Header("상태 아이콘")]
    public GameObject voteIcon;
    public GameObject deadIcon;
    
    [Header("데이터")]
    public int targetPlayerNumber;
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        if (playerSlotBackground == null) playerSlotBackground = GetComponent<Image>();
    }

    public void Setup(string playername, int playerNum, bool isDead)
    {
        playerNameText.text = playername;
        targetPlayerNumber = playerNum;

        if (borderline != null) borderline.SetActive(false);
        if (voteIcon != null) voteIcon.SetActive(false);
        if (deadIcon != null) deadIcon.SetActive(false);

        if (isDead)
        {
            SetDeadVisual();
        }
        else
        {
            SetAliveVisual();
        }
    }

    public void ShowVoteComplete()
    {
        if (voteIcon != null && !deadIcon.activeSelf)
        {
            voteIcon.SetActive(true);
        }
    }

    void SetAliveVisual()
    {
        Color color;
        ColorUtility.TryParseHtmlString("#ECDAC0", out color);
        playerSlotBackground.color = color;

        myButton.interactable = true;
        playerNameText.color = Color.black;
    }

    void SetDeadVisual()
    {
        playerSlotBackground.color = new Color(0.3f, 0.3f, 0.3f);
        myButton.interactable = false;
        playerNameText.color = Color.red;

        if (deadIcon != null) deadIcon.SetActive(true);
    }

    public void SetBorderline(bool isOn)
    {
        if (borderline != null) borderline.SetActive(isOn);
    }
}
