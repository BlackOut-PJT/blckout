using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    public void GoConnect()
    {
        SceneManager.LoadScene("Scene_Connect");
    }

    public void GoHowTo()
    {
        SceneManager.LoadScene("HowTo");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
