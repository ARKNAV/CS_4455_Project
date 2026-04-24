using UnityEngine;
using UnityEditor;

public class FixCanvasSortOrder
{
    public static string Execute()
    {
        // ObjectiveManager/Canvas should render below MenuCanvas
        // MenuCanvas (lose/win/pause screens) must always be on top
        var objCanvas = GameObject.Find("ObjectiveManager/Canvas")?.GetComponent<Canvas>();
        var menuCanvas = GameObject.Find("MenuCanvas")?.GetComponent<Canvas>();
        var hudCanvas = GameObject.Find("HUD Canvas")?.GetComponent<Canvas>();

        string result = "";

        if (objCanvas != null)
        {
            objCanvas.overrideSorting = true;
            objCanvas.sortingOrder = 10;
            EditorUtility.SetDirty(objCanvas);
            result += "ObjectiveManager/Canvas sortingOrder=10\n";
        }

        if (hudCanvas != null)
        {
            hudCanvas.overrideSorting = true;
            hudCanvas.sortingOrder = 20;
            EditorUtility.SetDirty(hudCanvas);
            result += "HUD Canvas sortingOrder=20\n";
        }

        if (menuCanvas != null)
        {
            menuCanvas.overrideSorting = true;
            menuCanvas.sortingOrder = 100;
            EditorUtility.SetDirty(menuCanvas);
            result += "MenuCanvas sortingOrder=100\n";
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        return result;
    }
}
