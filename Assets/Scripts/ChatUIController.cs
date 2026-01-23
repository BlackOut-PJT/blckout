using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;



public class ChatUIController : MonoBehaviour
{   
    [Header("Network")]
    public LobbyChatManager lobbyChat;

    [Header("Input")]
    public TMP_InputField inputField;
    public Button sendButton;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("Prefabs")]
    public GameObject chatMessagePrefab; // ChatMessageText 프리팹

    void Awake()
    {
        sendButton.onClick.AddListener(OnSend);

        // 엔터로 보내기(선택)
        inputField.onSubmit.AddListener(_ => OnSend());
    }

    void OnSend()
    {
        string msg = inputField.text;
        if (string.IsNullOrWhiteSpace(msg)) return;

        if (lobbyChat != null)
            lobbyChat.SendChat(msg);
        else
            AddMessage($"(LOCAL) {msg}");

        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void AddMessage(string msg)
    {
        if (chatMessagePrefab == null || content == null) return;

            GameObject go = Instantiate(chatMessagePrefab, content);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = msg;

            StartCoroutine(ScrollToBottomNextFrame());
        
    }

    IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }

}
