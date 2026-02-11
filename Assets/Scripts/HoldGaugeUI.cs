using UnityEngine;
using UnityEngine.UI;

public class HoldGaugeUI : MonoBehaviour
{
    [SerializeField] private Image fill;

    public void SetProgress(float t01)
    {
        fill.fillAmount = Mathf.Clamp01(t01);
    }

    public void ResetGauge()
    {
        fill.fillAmount = 0f;
    }
}
