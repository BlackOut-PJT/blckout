using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoundSettingUI : MonoBehaviour
{
    [Header ("텍스트 연결")]
    [SerializeField] private TextMeshProUGUI masterVolText;
    [SerializeField] private TextMeshProUGUI bgmVolText;
    [SerializeField] private TextMeshProUGUI uiVolText;
    [SerializeField] private TextMeshProUGUI sfxVolText;
    [SerializeField] private TextMeshProUGUI playerVolText;

    [Header ("슬라이더 연결")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider uiSlider;
    [SerializeField] private Slider playerSlider;

    private void OnEnable()
    {
        //설정창 켜질 때마다 슬라이더 값 동기화 시키기
        InitUI();
    }

    private void InitUI()
    {
        float master = PlayerPrefs.GetFloat("MasterVol", 0.5f);
        float bgm = PlayerPrefs.GetFloat("BGMVol", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFXVol", 0.5f);
        float ui = PlayerPrefs.GetFloat("UIVol", 0.5f);
        float player = PlayerPrefs.GetFloat("PlayerVol", 0.5f);

        masterSlider.value = master;
        bgmSlider.value = bgm;
        sfxSlider.value = sfx;
        uiSlider.value = ui;
        playerSlider.value = player;
    }
    public void OnMasterSliderChanged(float val)
    {
        SoundManager.instance.SetMasterVolume(val);
        masterVolText.text = Mathf.RoundToInt(val*100).ToString();
    }

    public void OnUISliderChanged(float val)
    {
        SoundManager.instance.SetUIVolume(val);
        uiVolText.text = Mathf.RoundToInt(val*100).ToString();
    }

    public void OnSFXSliderChanged(float val)
    {
        SoundManager.instance.SetSFXVolume(val);
        sfxVolText.text = Mathf.RoundToInt(val*100).ToString();
    }

    public void OnBGMSliderChanged(float val)
    {
        SoundManager.instance.SetBGMVolume(val);
        bgmVolText.text = Mathf.RoundToInt(val*100).ToString();
    }

    public void OnPlayerSliderChanged(float val)
    {
        SoundManager.instance.SetPlayerVolume(val);
        playerVolText.text = Mathf.RoundToInt(val*100).ToString();
    }
}
