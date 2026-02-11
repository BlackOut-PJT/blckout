using UnityEngine;

public interface IHoldInteractable : IInteractable
{
    //1.5초 홀드
    float HoldDuration {get; }
    //홀드 게이지 ui
    void ShowHoldUI(bool show);
    //홀드 진행도 표시 0과 1 사이
    void SetHoldProgress(float t01);
}
