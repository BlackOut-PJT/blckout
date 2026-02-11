using UnityEngine;
using TMPro;
using System.Collections;

public class NotifyUI : MonoBehaviour
{
    private TextMeshProUGUI text;
    private Coroutine hideCoroutine;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        gameObject.SetActive(false);
    }

    public void ShowMessage(string message, float duration = 2.0f)
    {
        text.text = message;
        gameObject.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(DisableDelay(duration));
    }

    IEnumerator DisableDelay (float time)
    {
        yield return new WaitForSeconds(time);
        gameObject.SetActive(false);
    }
}
