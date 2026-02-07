using UnityEngine;
using Photon.Pun;

public class CriminalTagPunRelay : MonoBehaviour
{
    [SerializeField] private CriminalTagUI criminalTagUI;

    private void Awake()
    {
        if(criminalTagUI == null)
            criminalTagUI = GetComponent<criminalTagUI>();
    }

    [PunRPC]
    public void RPC_ShowCriminalTag(float seconds)
    {
        if(criminalTagUI == null) return;
        criminalTagUI.Show(seconds);
    }
}
