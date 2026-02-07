using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
//2d renderer 조명 사용
using Photon.Pun;//PhotonView.IsMine 사용

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


    [Header("조명 설정")]

    [Tooltip("평소 VisionLight 밝기 (보통 0)")]
    [SerializeField] private float visionIntensity_Normal = 0f;

    [Tooltip("암전 시 VisionLight 밝기")]
    [SerializeField] private float visionIntensity_OffLight = 1f;

    [Tooltip("평소 visionLight의 시야 반경")] 
    [SerializeField] private float visionRadius_Normal = 10f;

    [Tooltip("암전 시 visionlight 시야 반경")]
    [SerializeField] private float visionRadius_OffLight = 2.5f;

    [Header("닉네임 숨기기")]
    [Tooltip("플레이어 프리팹 내 닉네임 UI 루트(캔버스) 오브젝트 이름")]
    [SerializeField] private string nicknameRootName = "Canvas";

    [Header("GameState")]
    [Tooltip("암전이 적용되는 게임 상태")]
    [SerializeField] private GameState offLightState = GameState.Playing_OffLight;

    //내부 상태 (중복 호출 방지 위해)
    private bool isBlackout;//현재 암전 켜져있는지 저장

    private void Start()
    {
        if (grayscaleVolume != null) grayscaleVolume.weight = 0f;
    }
    
    private void OnEnable()
    {
        //씬에 GameStateManager 싱글톤 존재하는지 확인
        if(GameStateManager.instance != null)
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
        if(GameStateManager.instance != null)
        {
            //이벤트 구독 해제
            GameStateManager.instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    //visionLight가 연결 안 됐으면 계속 연결 시도 (성공하면 더 이상 안 함)
    private void Update()
    {
        if (visionLight == null)
            TryBindLocalVisionLightByName();
    }

    //"VisionLight" 이름을 가진 자식 Light2D를 로컬 플레이어에서 자동으로 찾아 연결
    private void TryBindLocalVisionLightByName()
    {
        //씬에 있는 PhotonView 훑어서 IsMine 플레이어 찾음
        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        foreach(var pv in views)
        {
            if(!pv.IsMine) continue;

            //자식 이름이 정확인 VisionLight인 Transform 찾기
            Transform t = pv.transform.Find("VisionLight");
            if(t== null) continue;

            Light2D found = t.GetComponent<Light2D>();
            if(found == null) continue;

            visionLight = found;

            //연결되자마자 평소 상태 기본값 세팅
            visionLight.intensity = visionIntensity_Normal;
            SetLightRadius(visionLight, visionRadius_Normal);

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

        foreach(var p in players)
        {
            var pv = p.GetComponent<PhotonView>();
            if(pv==null) continue;

            //플레이어 프리팹 내부에서 canvas 이름의 자식 오브젝트 찾음
            Transform nicknameCanvas = p.transform.Find(nicknameRootName);
            if(nicknameCanvas == null) continue;
            //이 플레이어가 내 로컬 플레이어인지 확인
            if (pv.IsMine)
            {
                //내 닉네임 ui는 항상 보이게 유지
                nicknameCanvas.gameObject.SetActive(true);
            }
            //타인 캐릭터면
            else
            {
                //암전이면 숨김, 해제면 다시 표시
                nicknameCanvas.gameObject.SetActive(!isBlackout);
                //isBlackout이 true면 !isBlackout = false -> Canvas 꺼짐(닉네임 숨김)

            }
        }
    }

    //GameStateManager에서 상태가 바뀌면 이벤트로 호출되는 함수
    private void HandleGameStateChanged(GameState state)
    {
        // 새 상태가 '암전 상태(Playing_OffLight)'면 암전 연출 켜기
        if(state == offLightState) EnableBlackout();
        //그 외 암전 연출 끔
        else DisableBlackout();
    }


    //암전 연출 ON: 전체 어둡게 + 시야 축소 + 흑백 켜기
    public void EnableBlackout()
    {
        //이미 암전일 때
        if (isBlackout) return;

        //암전 상태로 기록
        isBlackout = true; 

        //1) VisionLight 시야 축소
        //visionlight 참조가 연결됐나 확인
        if(visionLight != null)
        {
            //visionLight 켜기
            visionLight.intensity = visionIntensity_OffLight;
            //플레이어 주변 빛 반경 줄여서 내 주변만 보이도록 만듦
            SetLightRadius(visionLight, visionRadius_OffLight);   
        }

        //2) 흑백 필터 ON(Volume 켜기)
        //인스펙터 연결 확인
        if(grayscaleVolume != null)
            //볼륨 켜서 흑백 효과 적용
            //grayscaleVolume.enabled = true;
            grayscaleVolume.weight = 1f;

        //3) 닉네임 숨김
        HideOtherNicknames(true);
        // (추후 Photon에서 내 것만 남기고 다른 사람 닉네임 끄는 로직을 여기에 넣을 수 있음)

        // 디버그용 로그(상태 토글이 실제로 호출됐는지 확인 가능)
        Debug.Log("[SightSystemController] Blackout ENABLED"); 
    }

    public void DisableBlackout()
    {
        //암전 연출 OFF: 조명/시야/필터 원상복구
        // 이미 암전이 꺼져 있으면 또 실행하지 않음
        if (!isBlackout) return; 

        // 암전 해제 상태로 기록
        isBlackout = false; 

        if (visionLight != null) // visionLight가 연결되어 있으면
        {
            //visionlight 끄기
            visionLight.intensity = visionIntensity_Normal;
            //시야 반경을 평소 값으로 복구(넓게)
            SetLightRadius(visionLight, visionRadius_Normal);    
        }
            

        if (grayscaleVolume != null) // 흑백 볼륨이 연결되어 있으면
            // 흑백 효과 끄기(원래 색으로)
            //grayscaleVolume.enabled = false;
            grayscaleVolume.weight = 0f;
            
        HideOtherNicknames(false);
        // 다른 사람 닉네임도 다시 켜기

        Debug.Log("[SightSystemController] Blackout DISABLED"); 
        // 디버그용 로그
    }

    //VisionLight 반경 값 바꾸는 함수
    private void SetLightRadius(Light2D light2D, float radius)
    {
        // URP 2D Light2D는 타입이 뭐든(버전에 따라) 이 값이 반경 역할을 함
        light2D.pointLightOuterRadius = radius;
    }



}
