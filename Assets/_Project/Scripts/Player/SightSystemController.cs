using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//2d renderer 조명 사용
using Photon.Pun;//PhotonView.IsMine 사용
using System.Collections;//폭죽 코루틴 사용

//GameStateManager의 상태 변경에 반응해서
//암전(시야 축소 + global light 어둡게 + 흑백 + 닉네임 숨김) 토글 스크립트
public class SightSystemController : MonoBehaviour
{
    [Header("설정")]

    [Tooltip("플레이어 프리팹 안의 VisionLight")]
    //플레이어 주변 시야를 밝히는 VisionLight(플레이어 자식 Light2D)
    [SerializeField] private Light2D visionLight;

    [Tooltip("흑백 필터용 Volume")]
    [SerializeField] private Volume grayscaleVolume;

    [Header("Global Light 조절")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private float globalIntensity_Normal = 1f;
    [SerializeField] private float globalIntensity_Blackout = 0f;
    [SerializeField] private float globalIntensity_Firework = 0.9f;

    [Header("visionlight 설정")]

    [Tooltip("평소 VisionLight 밝기 (보통 0)")]
    [SerializeField] private float visionIntensity_Normal = 0f;
    [SerializeField] private float visionIntensity_OffLight = 1f;
    [SerializeField] private float visionRadius_Normal = 10f;
    [SerializeField] private float visionRadius_OffLight = 2.5f;

    [Header("닉네임 숨기기")]
    [Tooltip("플레이어 프리팹 내 닉네임 UI 루트(캔버스) 오브젝트 이름")]
    [SerializeField] private string nicknameTextName = "NicknameText";

    [Header("암전 게임 상태")]
    [SerializeField] private GameState offLightState = GameState.Playing_OffLight;

    //내부 상태 (중복 호출 방지 위해)
    private bool isBlackout;//현재 암전 켜져있는지 저장
    private bool isFireworkActive; //폭죽 작동 중인지 체크
    private Coroutine fireworkCoroutine;
    private bool isLocalPlayerGhost = false; // 유령인지 확인하는 변수

    private void Start()
    {
        if (grayscaleVolume != null) grayscaleVolume.weight = 0f;
    }

    private void OnEnable()
    {
        //씬에 GameStateManager 싱글톤 존재하는지 확인
        if (GameStateManager.instance != null)
        {
            //gamestatemanager의 상태 변경 이벤트를 구독 (바뀌면 HandleGameStateChanaged 호출)
            GameStateManager.instance.OnGameStateChanged += HandleGameStateChanged;

            //시작 시점에 로컬 플레이어 visionLight 자동 연결
            TryBindLocalVisionLightByName();

            // 시작 시 현재 상태 반영
            HandleGameStateChanged(GameStateManager.instance.currentState);
        }
        else
        {
            // GameStateManager가 없으면 구독 불가 → 경고 로그로 원인 추적 가능하게 함
            Debug.LogWarning("[SightSystemController] GameStateManager.instance is null. 씬에 GameStateManager가 있는지 확인!");
        }
    }


    //비활시 자동 호출
    private void OnDisable()
    {
        //싱글톤 확인하고
        if (GameStateManager.instance != null)
        {
            //이벤트 구독 해제
            GameStateManager.instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    //visionLight가 연결 안 됐으면 계속 연결 시도 (성공하면 더 이상 안 함)
    private void Update()
    {
        if (isLocalPlayerGhost)
        {
            if (globalLight != null) globalLight.intensity = globalIntensity_Normal; // 강제로 1 유지
            if (grayscaleVolume != null) grayscaleVolume.weight = 0f; // 흑백 필터 강제 해제
        }
        
        if (visionLight == null)
            TryBindLocalVisionLightByName();
    }

    //"VisionLight" 이름을 가진 자식 Light2D를 로컬 플레이어에서 자동으로 찾아 연결
    private void TryBindLocalVisionLightByName()
    {
        //씬에 있는 PhotonView 훑어서 IsMine 플레이어 찾음
        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        foreach (var pv in views)
        {
            if (!pv.IsMine) continue;

            //자식 이름이 정확인 VisionLight인 Transform 찾기
            Transform t = pv.transform.Find("VisionLight");
            if (t == null) continue;

            Light2D found = t.GetComponent<Light2D>();
            if (found == null) continue;

            visionLight = found;

            //로컬에서만 visionlight 켜기
            visionLight.enabled = true;

            ApplyCurrentVisualState();//바인딩 직후 현재 상태 반영

            Debug.Log("[SightSystemController] Bound VisionLight (visionlight) from local player.");
            return; // 한 번 찾으면 끝
        }
    }

    //닉네임 숨기기 함수
    //암전 여부 따라 닉네임 숨기거나 보여줌
    private void HideOtherNicknames(bool isBlackout)
    {
        //씬에 있는 tag가 player인 오브젝트 배열로 가져옴.(Player 프리팹 tag: player 설정해야함..)
        var players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var p in players)
        {
            var pv = p.GetComponent<PhotonView>();
            if (pv == null) continue;

            //플레이어 프리팹 내부에서 canvas 이름의 자식 오브젝트 찾음
            Transform nicknameText = p.transform.Find("Canvas/" + nicknameTextName);
            if (nicknameText == null) continue;
            //이 플레이어가 내 로컬 플레이어인지 확인
            if (pv.IsMine)
            {
                //내 닉네임 ui는 항상 보이게 유지
                nicknameText.gameObject.SetActive(true);
            }
            //타인 캐릭터면
            else
            {
                //암전이면 숨김, 해제면 다시 표시
                nicknameText.gameObject.SetActive(!isBlackout);
                //isBlackout이 true면 !isBlackout = false -> Canvas 꺼짐(닉네임 숨김)

            }
        }
    }

    //GameStateManager에서 상태가 바뀌면 이벤트로 호출되는 함수
    private void HandleGameStateChanged(GameState state)
    {
        // 유령이면 상태 변경 무시
        if (isLocalPlayerGhost) return;

        // 폭죽이 터져있는 동안에는 상태 변경이 와도 무시 (폭죽이 우선)
        if (isFireworkActive) return;

        // 새 상태가 '암전 상태(Playing_OffLight)'면 암전 연출 켜기
        if (state == offLightState) EnableBlackout();
        //그 외 암전 연출 끔
        else DisableBlackout();
    }

    private void ApplyCurrentVisualState()
    {
        //폭죽 최우선
        if (isFireworkActive)
        {
            ApplyFireworkVisual();
            return;
        }

        // 유령이면 무조건 Normal로 덮어쓰기
        if (isLocalPlayerGhost) 
        {
            ApplyNormalVisual();
            return;
        }

        if (isBlackout) ApplyBlackoutVisual();
        else ApplyNormalVisual();
    }

    private void ApplyNormalVisual()
    {
        if (globalLight != null) globalLight.intensity = globalIntensity_Normal;
        if (visionLight != null)
        {
            visionLight.intensity = visionIntensity_Normal;
            SetLightRadius(visionLight, visionRadius_Normal);
        }

        if (grayscaleVolume != null) grayscaleVolume.weight = 0f;
        HideOtherNicknames(false);
    }

    private void ApplyBlackoutVisual()
    {
        if (globalLight != null) globalLight.intensity = globalIntensity_Blackout;
        if (visionLight != null)
        {
            visionLight.enabled = true;
            visionLight.intensity = visionIntensity_OffLight;
            SetLightRadius(visionLight, visionRadius_OffLight);
        }

        if (grayscaleVolume != null) grayscaleVolume.weight = 1f;
        HideOtherNicknames(true);
    }

    private void ApplyFireworkVisual()
    {
        if (globalLight != null) globalLight.intensity = globalIntensity_Firework;

        // 폭죽 중에는 흑백 끔
        if (grayscaleVolume != null) grayscaleVolume.weight = 0f;

        // 폭죽 중 닉네임은 보이게
        HideOtherNicknames(false);
    }


    //암전 연출 ON: 전체 어둡게 + 시야 축소 + 흑백 켜기
    public void EnableBlackout()
    {
        // 유령이면 암전 무시
        if (isLocalPlayerGhost) return;

        if (isFireworkActive) return;
        //if(isBlackout) return;

        isBlackout = true;
        ApplyBlackoutVisual();

        // 디버그용 로그(상태 토글이 실제로 호출됐는지 확인 가능)
        Debug.Log("[SightSystemController] Blackout ENABLED");
    }

    public void DisableBlackout()
    {
        if (isFireworkActive) return;
        //if(!isBlackout) return;

        isBlackout = false;
        ApplyNormalVisual();

        Debug.Log("[SightSystemController] Blackout DISABLED");
        // 디버그용 로그
    }

    //폭죽
    public void TriggerFirework(float duration)
    {
        // 중복 폭죽 방지: 기존 코루틴 끊고 새로 시작
        if (fireworkCoroutine != null)
        {
            StopCoroutine(fireworkCoroutine);
            fireworkCoroutine = null;
        }

        fireworkCoroutine = StartCoroutine(Co_Firework(duration));
    }

    private IEnumerator Co_Firework(float duration)
    {
        isFireworkActive = true;

        ApplyFireworkVisual();
        Debug.Log("[SightSystemController] Firework ENABLED");

        yield return new WaitForSeconds(duration);

        isFireworkActive = false;
        fireworkCoroutine = null;

        //폭죽 끝나면 현재 gamestate로 복귀
        if (GameStateManager.instance != null)
        {
            //currentState가 OffLight면 EnableBlackout
            HandleGameStateChanged(GameStateManager.instance.currentState);
            ApplyCurrentVisualState();
        }

        Debug.Log("[SightSystemController] Firework DISABLED");
    }

    //VisionLight 반경 값 바꾸는 함수
    private void SetLightRadius(Light2D light2D, float radius)
    {
        // URP 2D Light2D는 타입이 뭐든(버전에 따라) 이 값이 반경 역할을 함
        light2D.pointLightOuterRadius = radius;
    }

    // 유령이 되었을 때 호출할 함수
    public void EnableGhostVision()
    {
        isLocalPlayerGhost = true;
        ApplyNormalVisual(); // 즉시 맵을 밝히고 흑백을 끕니다!
        Debug.Log("[SightSystemController] 유령 시야 발동! 이제 암전에 걸리지 않습니다.");
    }

}
