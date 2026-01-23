using UnityEngine;
using UnityEngine.SceneManagement;

public class InitSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //게임 시작하자마자 TitleScene으로 이동
        SceneManager.LoadScene("Scene_Title");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
