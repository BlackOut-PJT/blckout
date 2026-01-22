using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviourPun
{
    [Header("플레이어 설정")]
    public float moveSpeed = 5f;

    [Header("컴포넌트 연결")]
    public Animator anim;
    public TextMeshProUGUI playerNameText;

    private Vector3 currentPos;

    void Start()
    {
        if (photonView.Owner != null)
        {
            playerNameText.text = photonView.Owner.NickName;
            playerNameText.color = Color.black;
        }
    }

    void Update()
    {
        if  (photonView.IsMine)
        {
            ProcessInput();
        }
    }

    void ProcessInput()
    {
        float y = Input.GetAxisRaw("Vertical");
        float x = Input.GetAxisRaw("Horizontal");

        Vector3 moveDir = new Vector3(x, y, 0).normalized;

        transform.position += moveDir * moveSpeed * Time.deltaTime;
        UpdateAnimation(moveDir);

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
}
