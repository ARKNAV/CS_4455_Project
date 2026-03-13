using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartEnd : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     public void QuitGame()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public void StartGame()
    {
        //print("game start");
        
        SceneManager.LoadScene("Alpha");
        Time.timeScale = 1f;
    }
}
