using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

public class ChatUIController : MonoBehaviour
{
    [Header("Network")]
    public LobbyChatManager lobbyChat;

    [Header("Panel Root")]
    public GameObject chatPanelRoot;

    [Header("Input")]
    public TMP_InputField inputField;
    public Button sendButton;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("Prefabs")]
    public GameObject chatMessagePrefab;

    private void Awake()
    {
        if (lobbyChat == null)
            lobbyChat = FindAnyObjectByType<LobbyChatManager>();

        if (lobbyChat != null)
            lobbyChat.BindUI(this);

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSend);
        else
            Debug.LogWarning("[ChatUI] sendButton is null");

        if (inputField != null)
            inputField.onSubmit.AddListener(_ => OnSend());
        else
            Debug.LogWarning("[ChatUI] inputField is null");
    } 


    public void ToggleChatPanel()
    {
        SoundManager.instance.SFXPlay("ButtonClick");
        if (chatPanelRoot == null)
        {
            Debug.LogWarning("[ChatUI] chatPanelRoot is null");
            return;
        }

        bool next = !chatPanelRoot.activeSelf;
        chatPanelRoot.SetActive(next);

        bool dead = IsLocalPlayerDead();
        if (inputField != null) inputField.interactable = !dead;
        if (sendButton != null) sendButton.interactable = !dead;

        if (next && inputField != null && !dead)
            inputField.ActivateInputField();
    }

    private bool IsLocalPlayerDead()
    {
        if (PhotonNetwork.LocalPlayer == null) return false;
        object val;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("IsDead", out val))
            return (bool)val;
        return false;
    }

    private void OnSend()
    {
        if (IsLocalPlayerDead()) return;

        if (lobbyChat == null)
        {
            Debug.LogError("[ChatUI] lobbyChat is null. Check scene reference!");
            return;
        }

        string msg = inputField.text;
        if (string.IsNullOrWhiteSpace(msg)) return;

        Debug.Log($"[ChatUI] OnSend '{msg}' lobbyChat null? {lobbyChat == null}");

        if (lobbyChat != null)
            lobbyChat.SendChat(msg);
        else
            AddMessage($"[LOCAL] {msg}");

        inputField.text = "";
        inputField.ActivateInputField();
    }

    public void AddMessage(string msg)
    {   
        Debug.Log($"[ChatUI] content={content?.name} id={content?.GetInstanceID()}  / scrollRect.content={scrollRect?.content?.name} id={scrollRect?.content?.GetInstanceID()}");

        if (chatMessagePrefab == null || content == null) return;

        var go = Instantiate(chatMessagePrefab, content);
        Debug.Log($"[ChatUI] Instantiated => {go.name}  parent={go.transform.parent.name} childCount={content.childCount}");

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp != null) tmp.text = msg;

        // 수정: 채팅 패널이 비활성화되어 있을 때 코루틴을 실행하면 에러가 남.
        // 따라서 현재 활성화된 상태일 때만 스크롤을 내리도록 체크
        if (this.gameObject.activeInHierarchy)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }
        
        /* Debug.Log($"[ChatUI] AddMessage called. content={(content==null?"NULL":"OK")} prefab={(chatMessagePrefab==null?"NULL":"OK")}, msg={msg}");

        if (chatMessagePrefab == null)
        {
            Debug.LogWarning("[ChatUI] chatMessagePrefab is null");
            return;
        }
        if (content == null)
        {
            Debug.LogWarning("[ChatUI] content is null");
            return;
        }

        var go = Instantiate(chatMessagePrefab, content);
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp == null) tmp = go.GetComponentInChildren<TMP_Text>();

        if (tmp != null) tmp.text = msg;
        else Debug.LogWarning("[ChatUI] TMP_Text not found on chatMessagePrefab");

        StartCoroutine(ScrollToBottomNextFrame()); */
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }
}

