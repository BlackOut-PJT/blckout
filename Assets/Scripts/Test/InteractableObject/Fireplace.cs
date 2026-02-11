using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Fireplace : MonoBehaviourPun, IInteractable
{
    [Header("UI 연결")]
    public TextMeshProUGUI interactText;

    void Start()
    {
        if (interactText != null) interactText.gameObject.SetActive(false);
    }
    public void ShowUI(bool show)
    {
        interactText.gameObject.SetActive(show);
    }

    public void Interact(Player interacter)
    {   
        Debug.Log("interact");
        GameStateManager.instance.MeetingButtonPressed(interacter);
    }

    //사용X
    public void ShowPanel(bool show) {}
}
