using UnityEngine;
using Photon.Pun;

public class CriminalTagPunRelay : MonoBehaviourPun
{
    [SerializeField] private CriminalTagUI criminalTagUI;

    private void Awake()
    {
        if(criminalTagUI == null)
            criminalTagUI = GetComponent<CriminalTagUI>();
    }

    [PunRPC]
    public void RPC_ShowCriminalTag(float seconds)
    {
        if(criminalTagUI == null) return;
        criminalTagUI.Show(seconds);
    }
}
