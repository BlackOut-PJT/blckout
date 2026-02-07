using UnityEngine;
using System.Collections;//코루틴 위해 필요
using TMPro;

public class CriminalTagUI : MonoBehaviour
{
    [Header("범인 텍스트 연결")]
    [SerializeField] private TMP_Text criminalText;
    [Header("범인 텍스트 노출 시간")]
    [SerializeField] private float duration = 10f;

    private Coroutine routine;

    private void Awake()
    {
        //첨 시작 범인 텍스트 비활
        if(criminalText != null)
            criminalText.gameObject.SetActive(false);
    }

    private void Show(float customDuration = -1f)
    {
        if(criminalText == null) return;

        if(routine != null) StopCoroutine(routine);

        routine = StartCoroutine(
            CoShow(customDuration > 0 ? customDuration : duration)
        );
    }

    private IEnumerator CoShow(float time)
    {
        //텍스트 활성화
        criminalText.gameObject.SetActive(true);
        //10초 동안 노출
        yield return new WaitForSeconds(time);
        //비활
        criminalText.gameObject.SetActive(false);
        routine = null;
    }
}
