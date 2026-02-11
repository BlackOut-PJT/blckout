using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class TestJobToggle_JY : MonoBehaviourPunCallbacks
{
    private bool jobSet = false;

    void Awake()
    {
        // TestNWManager 끄기
        var nw = FindObjectOfType<Test_NW_Manager>();
        if (nw != null)
            nw.gameObject.SetActive(false);

        // 오프라인 모드 + 방 생성
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.JoinOrCreateRoom("TestRoom_JY", new RoomOptions(), null);

        // GameStateManager가 이미 Awake 끝났으면 여기서 세팅
        ApplyTestSettings();
    }

    void Start()
    {
        // Awake 순서 보장 안 되니까 Start에서도 한번 더
        ApplyTestSettings();
    }

    void ApplyTestSettings()
    {
        if (GameStateManager.instance == null) return;
        GameStateManager.instance.skipWinCondition = true;
        GameStateManager.instance.blackoutDelay = 99999f;
    }

    void Update()
    {
        // 플레이어 스폰 후 Job 설정 (1회)
        if (!jobSet && InventoryModel.instance != null)
        {
            jobSet = true;

            GameStateManager.instance.isGameStart = true;
            ApplyTestSettings();

            Hashtable props = new Hashtable();
            props["Job"] = "Survivor";
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log("[TestJobToggle_JY] Test environment ready (Survivor)");
        }

        // K키: Killer <-> Survivor 전환
        if (jobSet && Input.GetKeyDown(KeyCode.K))
        {
            object jobObj;
            string currentJob = "Survivor";
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Job", out jobObj))
                currentJob = (string)jobObj;

            string newJob = (currentJob == "Killer") ? "Survivor" : "Killer";
            Hashtable props = new Hashtable();
            props["Job"] = newJob;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Debug.Log($"[TestJobToggle_JY] Job changed: {currentJob} -> {newJob}");
        }
    }
}
