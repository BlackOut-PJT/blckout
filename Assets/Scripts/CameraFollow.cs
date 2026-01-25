using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header ("수치 조정 및 연결")]
    public Transform target; // 카메라가 따라다닐 목표물(내 캐릭터)
    public float smoothSpeed = 0.125f; // 따라가는 속도 (부드럽게)
    public Vector3 offset; // 카메라와 캐릭터 사이의 거리 조절

    void LateUpdate() // 플레이어가 움직인 뒤에 카메라가 움직여야 덜덜 떨리지 않음
    {
        if (target == null) return; // 따라갈 대상이 없으면 아무것도 안 함

        // 1. 목표 위치 계산 (캐릭터 위치 + 약간의 거리)
        Vector3 desiredPosition = target.position + offset;
        
        // 2. 부드럽게 이동 (Lerp 사용)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // 3. 카메라 위치 확정 (Z축은 -10으로 고정해야 함! 안 그러면 맵 뒤로 숨음)
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, -10f);
    }
}