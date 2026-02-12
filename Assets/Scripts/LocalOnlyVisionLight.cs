using UnityEngine;
using Photon.Pun;
using UnityEngine.Rendering.Universal;

public class LocalOnlyVisionLight : MonoBehaviour
{
    private void Awake()
    {
        //부모에 붙은 PhotonView 찾기
        var pv = GetComponentInParent<PhotonView>();
        var light2D = GetComponent<Light2D>();
        //타인의 visionlight이면
        if(pv != null && !pv.IsMine)
        {
            if(light2D != null) light2D.enabled = false;
            gameObject.SetActive(false);
        }
    }
}
