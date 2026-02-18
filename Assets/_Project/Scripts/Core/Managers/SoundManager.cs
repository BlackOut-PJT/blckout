using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioMixer masterMixer;

    [Header ("사운드 연결")]
    [SerializeField] private SoundDataSO soundDataSO;

    [Header ("Audio Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    public Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
    private AudioSource audioSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            audioSource = GetComponent<AudioSource>();
            DontDestroyOnLoad(instance);
        }
        else
        {
            Destroy(gameObject);
        }

        if (soundDataSO != null)
        {
            foreach (var entry in soundDataSO.soundEntries)
            {
                if (!sfxDictionary.ContainsKey(entry.name))
                {
                    sfxDictionary.Add(entry.name, entry.clip);
                }
            }
        }
    }

    void Start()
    {   
        //게임 시작 시 소리 초기 세팅
        LoadSettings();
    }

    //소리 재생 관련 메소드들

    public void SFXPlay(string sfxName)
    {
        if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.Log("Audio Clip이 없음");
        }
    }

    public void UISoundPlay(string soundName)
    {
        if (sfxDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            uiSource.PlayOneShot(clip);
        }
        else
        {
            Debug.Log("Audio Clip(UI) 없음");
        }
    }

    public void BGMPlay()
    {   
        Debug.Log("BGM Play 메소드 SoundManager에서 실행");
        if (sfxDictionary.TryGetValue("BGM", out AudioClip clip))
        {
            bgmSource.clip = clip;
            bgmSource.Play();
        }
    }

    #region 소리 볼륨 설정 관련 메소드

    public void SetVolume(string paramName, float sliderVal)
    {
        //믹서 볼륨은 데시벨 단위 쓰기 때문에 로그 계산
        float vol = Mathf.Log10(Mathf.Max(0.0001f, sliderVal)) * 20;

        //paramName = 믹서에서 정한 'Exposed Parameter'의 이름이어야함
        masterMixer.SetFloat(paramName, vol);

        //데이터 저장
        PlayerPrefs.SetFloat(paramName, sliderVal);
    }

    public void LoadSettings()
    {
        string[] paramsToLoad = {"MasterVol", "BGMVol", "SFXVol", "PlayerVol", "UIVol"};
        foreach (string p in paramsToLoad)
        {
            float savedValue = PlayerPrefs.GetFloat(p, 1f);
            SetVolume(p, savedValue);
        }
    }

    public void SetMasterVolume(float val) => SetVolume("MasterVol", val);
    public void SetBGMVolume(float val) => SetVolume("BGMVol", val);
    public void SetUIVolume(float val) => SetVolume("UIVol", val);
    public void SetSFXVolume(float val) => SetVolume("SFXVol", val);
    public void SetPlayerVolume(float val) => SetVolume("PlayerVol", val);
    #endregion
}
