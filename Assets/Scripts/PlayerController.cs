using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using TMPro;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("플레이어 설정")]
    public float moveSpeed = 5f;

    [Header("컴포넌트 연결")]
    public Animator anim;
    public TextMeshProUGUI playerNameText;
    public SpriteRenderer spriteRenderer; //캐릭터 색 변경에 사용 (임시)

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

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        if (photonView.Owner != null)
        {
            playerNameText.text = photonView.Owner.NickName;
            playerNameText.color = Color.black;
        }

        #region 맵 테스트용 임시 코드
        if (photonView.IsMine)
        {
            CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
            if (cam != null)
            {
                cam.target = this.transform;
            }
        }
        #endregion

        ApplyKillerNameRed(); // 시작할 때 직업이 있을 수 있으니 여기서도 체크
    }

    void Update()
    {
        // 1.내 캐릭터 아니면 조종X
        if (!photonView.IsMine) return;

        // 2.게임 상태 체크 + 게임 시작 하였는지 체크
        if (GameStateManager.instance.isGameStart == false || GameStateManager.instance.currentState == GameState.Voting)
        {
            moveInput = Vector2.zero;
            UpdateAnimation(Vector3.zero);
            return;
        }

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
            else
            {
                playerNameText.color = Color.black; // 생존자면 검정색
            }
        }
    }

    public void Die()
    {
        Debug.Log($"{photonView.Owner.NickName} 사망!");
        photonView.RPC("RPC_ChangeToDeadBody", RpcTarget.All);

        /*
        if (spriteRenderer != null)
        {
            playerNameText.color = Color.red;
            Color color = spriteRenderer.color;
            color.a = 0.5f; //반투명
            spriteRenderer.color = color;
        }
        */
    }

    // 모든 플레이어들에게 죽은 플레이어가 시체로 보이게
    [PunRPC]
    public void RPC_ChangeToDeadBody()
    {
        // 애니메이터 끄기
        if (anim != null) anim.enabled = false;
        
        // 스프라이트 교체
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && deadSprite != null)
        {
            sr.sprite = deadSprite;

            // 시체는 바닥에 깔려야 하므로 그리기 순서를 낮춤
            sr.sortingOrder = deadSortingOrder;

            Debug.Log("스프라이트 교체!");
        }

        // 더 이상 충돌하지 않게 Collider 끄기
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 움직임 멈추기
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // PlayerController 스크립트 기능 정지
        this.enabled = false;

        Debug.Log("충돌, 움직임, 조작 모두 끔!");
    }
}
