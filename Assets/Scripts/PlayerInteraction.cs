using UnityEngine;
using Photon.Pun;


//플레이어가 앞의 상호작용 대상을 Raycast로 감지, E키로 상호작용
public class PlayerInteraction : MonoBehaviourPun
{

    public static PlayerInteraction instance;

    [Header("Raycast")]
    //레이캐스트 쏘는 최대 거리. 이 거리 안에 있는 물체만 상호작용
    public float interactDistance = 1.2f;
    //Raycast가 맞출 레이어 필터(Interactable 레이어만 감지)
    public LayerMask interactableMask;

    [Header("Input")]
    //상호작용 키를 E로 설정.
    public KeyCode interactKey = KeyCode.E;

    //내부 상태 변수
    //현재 Raycast로 감지된 IInteractable 대상
    private IInteractable currentTarget;

    // (추가) 현재 레이캐스트로 바라보고 있는 대상 (E키 누른 후)
    private IInteractable activeInteractable;

    //플레이어가 바라보는 방향 벡터
    private Vector2 lookDir = Vector2.right; //기본은 일단 오른쪽

    void Awake()
    {
        if (photonView.IsMine) instance = this;
    }
    //매 프레임마다 자동 호출됨
    private void Update()
    {
        if (!photonView.IsMine) return;
        
        //현재 입력 기반으로 바라보는 방향(lookDir) 갱신
        UpdateLookDirection();
        //lookdir 방향으로 Raycast 쏴서 상호작용
        DetectInteractable();
        //대상이 있을 때 E키 입력 받으면 Interact() 실행
        HandleInput();
        CheckActiveDistance();
    }

    //바라보는 방향 결정
    void UpdateLookDirection()
    {
        //현재 입력 방향을 그대로 바라보는 방향으로 사용
        //좌/우 입력 값 가져오기
        float x = Input.GetAxisRaw("Horizontal");
        //상/하 입력 값 가져오기
        float y = Input.GetAxisRaw("Vertical");

        //2d 방향벡터로 묶어서 input 변수 선언
        Vector2 input = new Vector2(x, y);

        //방향키를 하나라도 누르고 있으면
        if (input.sqrMagnitude > 0.01f)
            // 입력 방향을 길이 1로 정규화해서(크기는 상관없으니까) lookDir로 저장
            lookDir = input.normalized;
    }

    //레이캐스트로 상호작용 가능한 물체 찾음
    void DetectInteractable()
    {
        //플레이어 위치(transform.position)에서 lookDir 방향으로 interactDistance만큼 Raycast 쏜다.
        //interactableMask로 interactable레이어만 맞도록 필터링
        RaycastHit2D hit = Physics2D.Raycast((Vector2)transform.position, lookDir, interactDistance, interactableMask);

        //이번 프레임에 새로 감지된 타겟 변수
        IInteractable newTarget = null;

        //레이캐스트가 어떤 콜라이더에 맞았으면
        if(hit.collider != null) 
            //맞은 오브젝트의 ItemBox2D 스크립트 확인
            newTarget = hit.collider.GetComponent<IInteractable>();

        //타겟이 바뀌었으면(플레이어가 이동하면서 ui 옮겨가야 함)
        if(newTarget != currentTarget)
        {
            //이전 타겟 ui 끔
            if(currentTarget != null)
                currentTarget.ShowUI(false);
            //현재 타겟을 새 타겟으로 교체
            currentTarget = newTarget;
            //새 타겟 ui 켬
            if(currentTarget!=null && currentTarget != activeInteractable)
                currentTarget.ShowUI(true);
        }


        //디버깅: Scene뷰에서 레이캐스트 어디로 쏴지는지 표시
        //currentTarget 있으면 초록, 없으면 빨강 표시
        Debug.DrawRay(transform.position, lookDir * interactDistance, currentTarget != null ? Color.green : Color.red);
    }

    //(추가) 거리 체크 함수: 패널이 열려있으면 거리가 멀어졌을 때 닫기 위한 함수
    void CheckActiveDistance()
    {
        if (activeInteractable == null) return;

        //현재 위치와 열린 대상 사이의 거리 계산
        MonoBehaviour targetMono = activeInteractable as MonoBehaviour;

        if (targetMono != null)
        {
            float distance = Vector2.Distance(transform.position, targetMono.transform.position);
            if (distance > interactDistance + 0.2f) CloseActivePanel();
        }
        else CloseActivePanel();
    }

    //패널 닫기 공통 함수
    void CloseActivePanel()
    {
        if (activeInteractable != null)
        {
            activeInteractable.ShowPanel(false);
            activeInteractable.ShowUI(false);
            activeInteractable = null;
        }

        if (InventoryUIController.instance != null) InventoryUIController.instance.setInteractTarget(null);
    }
    
    //입력(E키) 처리
    void HandleInput()
    {
        //이미 열린 패널이 있는데 E키를 또 눌렀을 때는 닫기
        if (Input.GetKeyDown(interactKey) && activeInteractable != null)
        {
            CloseActivePanel();
            return;
        }

        if(currentTarget == null) return;
        //이번 프레임에 상호작용 /ㅋㅋㅋㅋㅌ키(E) 눌렀으면 true
        if (Input.GetKeyDown(interactKey))
        {
            //(추가) 기존에 열려있던게 있으면 닫고 시작
            if (activeInteractable != null && activeInteractable != currentTarget) CloseActivePanel();
            //E키 누르자마자 끔
            currentTarget.ShowUI(false);
            //타겟의 Interact() 호출, 누른 사람이 누구인지 photon에 전달
            currentTarget.Interact(PhotonNetwork.LocalPlayer);
            //(추가) 현재 활성화된 대상으로 등록
            activeInteractable = currentTarget;
        }
    }

    //TEST
    public void SetInteractTarget (IInteractable interactTarget)
    {
        if (activeInteractable != null && activeInteractable != interactTarget) CloseActivePanel();
        this.activeInteractable = interactTarget;

        if (InventoryUIController.instance != null) InventoryUIController.instance.setInteractTarget(interactTarget);
    }

}
