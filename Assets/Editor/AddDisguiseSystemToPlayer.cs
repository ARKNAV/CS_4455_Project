using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AddDisguiseSystemToPlayer
{
    [MenuItem("Tools/Add DisguiseSystem To Player")]
    public static void Execute()
    {
        // Find the player by CharacterInputController tag or component
        CharacterInputController inputCtrl = Object.FindFirstObjectByType<CharacterInputController>();
        if (inputCtrl == null)
        {
            Debug.LogError("[AddDisguiseSystem] Could not find CharacterInputController in scene!");
            return;
        }

        GameObject player = inputCtrl.gameObject;
        Debug.Log($"[AddDisguiseSystem] Found player: {player.name}");

        // Check if DisguiseSystem already present
        DisguiseSystem existing = player.GetComponent<DisguiseSystem>();
        if (existing != null)
        {
            Debug.Log($"[AddDisguiseSystem] Player already has DisguiseSystem. Nothing to do.");
            return;
        }

        // Add the component
        DisguiseSystem ds = player.AddComponent<DisguiseSystem>();
        Debug.Log($"[AddDisguiseSystem] Added DisguiseSystem to {player.name}.");

        // Mark scene dirty and save
        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        Debug.Log("[AddDisguiseSystem] Scene saved.");
    }
}
