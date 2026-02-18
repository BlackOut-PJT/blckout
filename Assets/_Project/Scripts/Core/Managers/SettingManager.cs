using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;

    [Header ("패널 연결")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private GameObject displayPanel;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            settingPanel.SetActive(false);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void OpenSoundSettings()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        soundPanel.SetActive(true);
        displayPanel.SetActive(false);
    }

    public void OpenDisplaySettings()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        soundPanel.SetActive(false);
        displayPanel.SetActive(true);
    }

    public void OpenSettingPanel()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        settingPanel.SetActive(true);
    }

    public void CloseSettingPanel()
    {
        SoundManager.instance.UISoundPlay("ButtonClick");
        settingPanel.SetActive(false);
    }
    
}
