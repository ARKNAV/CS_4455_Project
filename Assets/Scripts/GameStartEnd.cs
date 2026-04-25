using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartEnd : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Alpha";

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
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        bool isTitleScreen = string.Equals(activeScene.name, "TitleScreen", System.StringComparison.OrdinalIgnoreCase);

        if (isTitleScreen)
        {
            SceneManager.LoadScene(gameplaySceneName);
            return;
        }

        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
