using UnityEngine;
using Photon.Pun;
using Photon.Voice.Unity;
using Photon.Voice.PUN;

public class VoiceController : MonoBehaviour
{
    public static VoiceController instance;
    private Recorder localRecorder;
    private bool isMicToggleOn = true;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this.gameObject);
        
        localRecorder = FindObjectOfType<Recorder>();
    }

    void Update()
    {
        HandleMicInput();
    }

    private void HandleMicInput()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isMicToggleOn = !isMicToggleOn;
            ApplyVoiceState();
            Debug.Log($"마이크 토글 상태: {isMicToggleOn}");
        }
    }

    public void ApplyVoiceState()
    {
        if (localRecorder == null) return;

        //죽은 사람은 마이크 사용 금지
        if (GameUtils.IsMyPlayerDead) localRecorder.TransmitEnabled = false;
        else localRecorder.TransmitEnabled = isMicToggleOn;
    }

    //투표 중 마이크 상태
    public void SetMeetingMicMode(bool isMeeting)
    {
        Speaker[] speakers = FindObjectsOfType<Speaker>();

        foreach (Speaker s in speakers)
        {
            AudioSource audioSource = s.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                if (isMeeting) audioSource.spatialBlend = 0f; //2D소리 -> 거리 상관없이 다 들림
                else audioSource.spatialBlend = 1f; //3D 소리 -> 거리 상관 O
            }
        }
    }
}
