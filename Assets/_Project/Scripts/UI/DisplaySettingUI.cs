using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DisplaySettingUI : MonoBehaviour
{
    [Header ("화면 설정")]
    public TMP_Dropdown resolutionDropDown;
    public Toggle fullScreenToggle;
    public Toggle vsyncToggle;

    [Header ("게임 플레이")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityText;

    private List<Resolution> resolutions = new List<Resolution>();

    void Start()
    {
        InitSettings();
    }
    //초기화 메소드
    public void InitSettings()
    {   
        //1. 해상도 설정
        resolutions.Clear();
        resolutionDropDown.ClearOptions();

        int currResolutionIndex = 0;
        List<string> options = new List<string>();

        //설정에서 존재하는 해상도 가져오기
        for (int i=0; i < Screen.resolutions.Length; i++)
        {
            Resolution res = Screen.resolutions[i];

            //16:9 비율만 가져오기 위해서
            float aspect = (float)res.width/res.height;
            if (aspect < 1.7f || aspect >1.8f) continue; //16:9 비율 아니면 버림
            if (res.width < 1280) continue; //너무 낮은 해상도 삭제

            string option = res.width + "x" + res.height;

            //중복 해상도 삭제
            if (!options.Contains(option))
            {
                options.Add(option);
                resolutions.Add(res);
            }
        }

        //해상도 드롭다운 선택지 추가
        resolutionDropDown.AddOptions(options);
        //현재 설정된 해상도에 맞는 인덱스 찾기
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
            {
                currResolutionIndex = i;
                break;
            }
        }
        resolutionDropDown.value = PlayerPrefs.GetInt("ResIndex", currResolutionIndex);

        //PlayerPrefs에 저장된 값이 있으면 쓰고, 없으면 현재 해상도 인덱스 사용
        int savedIndex = PlayerPrefs.GetInt("ResIndex", currResolutionIndex);

        //저장된 인덱스가 현재 만들어진 리스트 범위 밖이면 방어적 처림
        resolutionDropDown.value = Mathf.Clamp(savedIndex, 0, options.Count-1);
        resolutionDropDown.RefreshShownValue();

        //2.화면모드&수직동기화
        fullScreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1:0) == 1;
        vsyncToggle.isOn = PlayerPrefs.GetInt("VSync", QualitySettings.vSyncCount) == 1;

        //3.마우스 감도
        float savedSens = PlayerPrefs.GetFloat("MouseSens", 1.0f);
        sensitivitySlider.value = savedSens;
        sensitivityText.text = savedSens.ToString("F1");
    }

    //설정값 반영
    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResIndex", index);
    }

    public void SetFullScreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("Fullscreen", isFull ? 1:0);
    }

    public void SetVSynce(bool isVSync)
    {
        QualitySettings.vSyncCount = isVSync ? 1:0;
        PlayerPrefs.SetInt("VSync", isVSync ? 1:0);
    }

    public void OnSensitivityChanged(float val)
    {
        PlayerPrefs.SetFloat("MouseSens", val);
        sensitivityText.text = val.ToString("F1");
    }
}
