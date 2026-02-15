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
        SceneManager.LoadScene("Scene_Connect");
    }

    public void GoHowTo()
    {
        gameRulePanel.SetActive(true);
    }

    public void OffRulePanel()
    {
        gameRulePanel.SetActive(false);
    }
}
