using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] public GameObject gameRulePanel;

    void Start()
    {
        if (gameRulePanel != null) gameRulePanel.SetActive(false);
    }
    public void GoConnect()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        SceneManager.LoadScene("Scene_Connect");
    }

    public void GoHowTo()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        gameRulePanel.SetActive(true);
    }

    public void OffRulePanel()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        gameRulePanel.SetActive(false);
    }
}
