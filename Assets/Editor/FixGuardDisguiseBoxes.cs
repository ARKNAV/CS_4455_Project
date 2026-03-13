using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixGuardDisguiseBoxes
{
    public static void Execute()
    {
        string[] guardBoxNames = new string[]
        {
            "DisguiseBox_Guard01",
            "DisguiseBox_Guard02",
            "DisguiseBox_Guard03",
            "DisguiseBox_Guard04"
        };

        // Grab material from an existing reference box
        GameObject referenceBox = GameObject.Find("DisguiseBox_DockingBay");
        Material referenceMat = null;
        if (referenceBox != null)
        {
            MeshRenderer refRenderer = referenceBox.GetComponentInChildren<MeshRenderer>();
            if (refRenderer != null)
                referenceMat = refRenderer.sharedMaterial;
        }

        foreach (string boxName in guardBoxNames)
        {
            GameObject go = GameObject.Find(boxName);
            if (go == null) { Debug.LogWarning("Not found: " + boxName); continue; }

            // 1. Add cube mesh so the box is visible
            MeshFilter mf = go.GetComponent<MeshFilter>();
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mf == null) mf = go.AddComponent<MeshFilter>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();

            mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            if (referenceMat != null)
                mr.sharedMaterial = referenceMat;
            else
                mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

            // Scale to a small chest shape
            go.transform.localScale = new Vector3(0.6f, 0.5f, 0.4f);

            // 2. Tighten trigger so it doesn't bleed into the guard's collider
            BoxCollider[] colliders = go.GetComponents<BoxCollider>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                {
                    col.size   = new Vector3(1.5f, 1.5f, 1.5f);
                    col.center = new Vector3(0f, 0.5f, 0f);
                }
            }

            Debug.Log("Fixed: " + boxName);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("FixGuardDisguiseBoxes complete.");
    }
}
