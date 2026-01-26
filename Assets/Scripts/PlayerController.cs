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

    // private Vector3 currentPos;

    private Rigidbody2D rb;
    private Vector2 moveInput; //입력값 저장용 변수 추가


    public SpriteRenderer spriteRenderer; //캐릭터 색 변경에 사용 (임시)

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
    }

    void Update()
    {   
        // 1.내 캐릭터 아니면 조종X
        if (!photonView.IsMine) return;

        // 2.게임 상태 체크
        if (GameStateManager.instance.currentState == GameState.Voting)
        {
            moveInput = Vector2.zero;
            UpdateAnimation(Vector3.zero);
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(x,y).normalized;

       UpdateAnimation(moveInput);
    }

    //물리적인 이동 처리 (벽에 부딪혔을 때 떨리는 현상 방지)
    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        if (GameStateManager.instance.currentState == GameState.Voting)
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
        if (targetPlayer.ActorNumber == photonView.Owner.ActorNumber && changedProps.ContainsKey("IsDead"))
        {
            CheckLifeStatus();
        }
    }

    void UpdateAnimation (Vector3 moveDir)
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

    void Die()
    {
        Debug.Log($"{photonView.Owner.NickName} 사망!");

        if (spriteRenderer != null)
        {
            playerNameText.color = Color.red;
            Color color = spriteRenderer.color;
            color.a = 0.5f; //반투명
            spriteRenderer.color = color;
        }

    }
}
