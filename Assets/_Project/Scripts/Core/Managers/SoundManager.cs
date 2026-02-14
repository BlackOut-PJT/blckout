using UnityEngine;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header ("사운드 연결")]
    [SerializeField] private SoundDataSO soundDataSO;
    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();
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

    public void SFXPlay(string sfxName)
    {
        if (sfxDictionary.TryGetValue(sfxName, out AudioClip clip))
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            Debug.Log("Audio Clip이 없음");
        }
    }
}
