using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KillMotionController : MonoBehaviour
{
    public static KillMotionController instance;

    [Header("연결할 UI")]
    public GameObject killMotionPanel; // 반투명 배경 패널 전체
    public Animator killAnimator;      // KillMotion_Image에 달린 애니메이터

    private void Awake()
    {
        instance = this;
        // 시작할 때는 무조건 꺼두기
        if (killMotionPanel != null) killMotionPanel.SetActive(false);
    }

    public void ShowKillMotion()
    {
        killMotionPanel.SetActive(true);
        StartCoroutine(PlayAndHideRoutine());
    }

    private IEnumerator PlayAndHideRoutine()
    {
        killAnimator.SetTrigger("PlayKill");

        float emergencyTimer = 0f;
        // 애니메이션이 넘어갈 때까지 기다리되, 최대 1초까지만 기다림 -> 무한 루프 방지
        while (!killAnimator.GetCurrentAnimatorStateInfo(0).IsName("Kill_Action"))
        {
            emergencyTimer += Time.unscaledDeltaTime; // 멈춘 시간과 무관하게 실제 시간 계산
            if (emergencyTimer > 1.0f) break; // 1초가 넘어가면 강제 진행
            yield return null;
        }

        // 애니메이션 길이 가져오기
        float animLength = 1.5f;
        if (killAnimator.GetCurrentAnimatorStateInfo(0).IsName("Kill_Action"))
        {
            animLength = killAnimator.GetCurrentAnimatorStateInfo(0).length;
        }

        // 시간이 멈춰 있어도 현실 시간으로 정확히 대기
        yield return new WaitForSecondsRealtime(animLength);

        // 안전하게 UI 끄기
        killMotionPanel.SetActive(false);
    }
}