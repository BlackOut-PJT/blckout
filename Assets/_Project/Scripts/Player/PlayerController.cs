using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("플레이어 설정")]
    public float moveSpeed = 5f;

    [Header("컴포넌트 연결")]
    public Animator anim;
    public TextMeshProUGUI playerNameText;
    public SpriteRenderer spriteRenderer; //캐릭터 색 변경에 사용 (임시)
    private AudioSource myAudioSource;

    [Header("시체 이미지 설정")]
    public Sprite deadSprite; // 시체 이미지
    public int deadSortingOrder = 0; // 시체는 발 밑에 깔려야 하므로 순서 낮춤

    // private Vector3 currentPos;

    private Rigidbody2D rb;
    private Vector2 moveInput; //입력값 저장용 변수 추가

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        myAudioSource = GetComponent<AudioSource>();

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        if (photonView.Owner != null)
        {
            playerNameText.text = photonView.Owner.NickName;
            playerNameText.color = Color.black;
        }

        #region 맵 테스트용 임시 코드
        if (photonView.IsMine)
        {
            playerNameText.color = Color.green;
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.target = this.transform;
            }

            // 혹시 모르니 Start()에서 IsDead false로 저장.
            Hashtable startProps = new Hashtable();
            startProps.Add("IsDead", false);
            PhotonNetwork.LocalPlayer.SetCustomProperties(startProps);
        }
        #endregion

        ApplyKillerNameRed(); // 시작할 때 직업이 있을 수 있으니 여기서도 체크
    }

    void Update()
    {
        // 1.내 캐릭터 아니면 조종X
        if (!photonView.IsMine) return;

        // 2.내 캐릭터 생존여부 파악
        // if(GameUtils.IsMyPlayerDead) return;
        // 죽으면 유령으로 움직여야 하므로 주석 처리

        // 3.게임 상태 체크 + 게임 시작 하였는지 체크
        if (GameStateManager.instance.isGameStart == false || GameStateManager.instance.currentState == GameState.Voting)
        {
            moveInput = Vector2.zero;
            UpdateAnimation(Vector3.zero);
            return;
        }

        // 4.채팅 입력 중이면 이동 차단
        if (ChatUIController.IsChatFocused)
        {
            moveInput = Vector2.zero;
            UpdateAnimation(Vector3.zero);
            return;
        }

        //마우스 민감도 추가
        float sens = PlayerPrefs.GetFloat("MouseSens", 1.0f);
        float mouseX = Input.GetAxis("Mouse X") * sens;
        float mouseY = Input.GetAxis("Mouse Y") * sens;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x, y).normalized;

        UpdateAnimation(moveInput);
    }

    //물리적인 이동 처리 (벽에 부딪혔을 때 떨리는 현상 방지)
    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        // 게임 시작 전 or 투표 상태이면 물리 이동 정지
        if (GameStateManager.instance.isGameStart == false || GameStateManager.instance.currentState == GameState.Voting)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 nextPos = rb.position + (moveInput * moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);
    }


    public override void OnEnable()
    {
        base.OnEnable();
        CheckLifeStatus(); //재접속 대비
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer.ActorNumber == photonView.Owner.ActorNumber)
        {
            if (changedProps.ContainsKey("IsDead"))
            {
                CheckLifeStatus();
            }

            if (changedProps.ContainsKey("Job"))
            {
                ApplyKillerNameRed();
            }
        }
    }

    void UpdateAnimation(Vector3 moveDir)
    {
        if (moveDir.magnitude > 0)
        {
            anim.SetBool("IsWalking", true);
            anim.SetFloat("InputX", moveDir.x);
            anim.SetFloat("InputY", moveDir.y);
        }
        else
        {
            anim.SetBool("IsWalking", false);
        }
    }

    void CheckLifeStatus()
    {
        bool isDead = false;

        if (photonView.Owner.CustomProperties.ContainsKey("IsDead")) isDead = (bool)photonView.Owner.CustomProperties["IsDead"];
        if (isDead) Die();
    }

    void ApplyKillerNameRed()
    {
        object jobValue;
        if (photonView.Owner.CustomProperties.TryGetValue("Job", out jobValue))
        {
            string job = (string)jobValue;

            if (job == "Killer" && photonView.IsMine)
            {
                playerNameText.color = Color.red; // 킬러면 빨간색
            }
            //일반인 닉네임 검정색 설정은 다른데에서 해서 + 초록색 설정 안 덮어쓰도록 이 부분은 삭제함
        }
    }

    public void Die(bool isAssassinated = false)
    {        
        if (photonView.IsMine)
        {
            Vector3 diePos = transform.position;

            Vector3 randomPos = transform.position;
            ItemData dropItem = InventoryModel.instance.DropItem();
            if (dropItem != null) photonView.RPC(nameof(RPC_DropItems), RpcTarget.MasterClient, dropItem.itemID, randomPos);

            // 바닥에 남겨질 시체 프리팹 추가
            photonView.RPC(nameof(RPC_SpawnDeadBody), RpcTarget.MasterClient, diePos, photonView.Owner.NickName);
        }

        Debug.Log($"{photonView.Owner.NickName} 사망!");

        photonView.RPC("RPC_BecomeGhost", RpcTarget.All, isAssassinated);
        SoundManager.instance.SFXPlay("DeadPopNoise");
    }

    [PunRPC]
    void RPC_DropItems(int itemID, Vector3 randomPos)
    {
        //FieldItem 생성 로직 -> object 사용해서 ID 전달
        object[] data = new object[] { itemID };

        //플레이어가 나가도 아이템이 남아야하기 때문에 RoomObject로 생성하기
        PhotonNetwork.InstantiateRoomObject("FieldItemPrefab", randomPos, Quaternion.identity, 0, data);
    }

    [PunRPC]
    public void RPC_BecomeGhost(bool isAssassinated)
    {
        // 애니메이터에게 사망 신호 보내기
        if (anim != null)
        {
            anim.enabled = true; // 혹시 꺼져있을까봐 확실히 키기
            anim.SetBool("IsDead", true); // 유령으로 변경
        }

        // Collider2D 유지 → 유령도 벽 충돌 적용
        // (Ghost-Ghost 충돌 불필요 시 Physics2D Layer Collision Matrix에서 끄기)

        // 유령 상태에서도 WASD 키를 받아서 돌아다녀야 하기 때문에 기존 움직임 봉쇄 코드 삭제

        // 내 캐릭터의 레이어를 "Ghost"로 변경 -> 산 사람들는 유령 못 봄
        gameObject.layer = LayerMask.NameToLayer("Ghost");

        // 이름표도 "Ghost" 레이어로 변경
        if (playerNameText != null)
        {
            // 텍스트 오브젝트 자체의 레이어 변경
            playerNameText.gameObject.layer = LayerMask.NameToLayer("Ghost");
            Canvas parentCanvas = playerNameText.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.gameObject.layer = LayerMask.NameToLayer("Ghost");
            }
        }

        
        if (spriteRenderer != null)
        {
            // 모든 유령 투명도 70퍼로 설정
            Color ghostColor = spriteRenderer.color;
            ghostColor.a = 0.7f;
            spriteRenderer.color = ghostColor;

            // Sorting Layer를 GhostTop으로 변경
            spriteRenderer.sortingLayerName = "GhostTop";
        }

        // 만약 본인이 죽은 거면
        if (photonView.IsMine)
        {
            // 살인자에게 죽었을 때만
            if (isAssassinated) 
            {
                // 피해자이므로 킬 모션 재생
                if (KillMotionController.instance != null) 
                {
                    KillMotionController.instance.ShowKillMotion();
                }
            }
            
            // 메인 카메라를 찾아서
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                // Culling Mask에 "Ghost" 레이어를 추가해서 보이게 만들기
                mainCam.cullingMask |= (1 << LayerMask.NameToLayer("Ghost"));
            }

            // 컨트롤러에게 유령 모드 발동 명령
            SightSystemController sightSystem = Object.FindFirstObjectByType<SightSystemController>();
            if (sightSystem != null)
            {
                sightSystem.EnableGhostVision();
            }
        }

        Debug.Log("유령 변신 완료!! (벽 충돌 유지)");
    }

    [PunRPC]
    public void RPC_SpawnDeadBody(Vector3 spawnPos, string deadPlayerName)
    {
        // 방장만 시체를 생성할 권한이 있음
        if (!PhotonNetwork.IsMasterClient) return;

        // 시체가 누구인지 알 수 있게 닉네임 데이터를 배열에 담아 보내기
        object[] data = new object[] { deadPlayerName };

        // 플레이어가 나가도 유지되도록 RoomObject로 생성
        PhotonNetwork.InstantiateRoomObject("DeadBodyPrefab", spawnPos, Quaternion.identity, 0, data);

        Debug.Log($"방장이 {deadPlayerName}의 시체를 바닥에 생성했습니다!");
    }

    //발자국 소리
    public void PlayFootStep()
    {
        if (SoundManager.instance == null) return;
        Dictionary <string, AudioClip> sfxDictionary = SoundManager.instance.sfxDictionary;

        if (sfxDictionary.TryGetValue("Walking", out AudioClip clip))
        {
            myAudioSource.Stop();
            myAudioSource.clip = clip;
            myAudioSource.Play();
        }
    }
}
