using UnityEngine;

public class KillBtnUI : MonoBehaviour
{
    private static KillBtnUI instance = null;

    void Awake()
    {
        // KillController.cs에서 사용하기 위해 싱글톤으로 생성
        if(instance==null) instance = this;
        else Destroy(this.gameObject);
    }
}
