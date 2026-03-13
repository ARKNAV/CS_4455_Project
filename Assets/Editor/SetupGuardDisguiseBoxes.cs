using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupGuardDisguiseBoxes
{
    public static void Execute()
    {
        // Step 1: Remove physical sprite meshes from Bodyguard 01
        RemoveSpritesFromGuard("SkelMesh_Bodyguard_01", new string[]
        {
            "BodyGuard_Hands", "BodyGuard_Head", "BodyGuard_Pants", "BodyGuard_Shoes", "BodyGuard_Torso"
        });

        // Step 2: Remove physical sprite meshes from Bodyguard 02
        RemoveSpritesFromGuard("SkelMesh_Bodyguard_02", new string[]
        {
            "BodyGuard02_Hands", "BodyGuard02_Head", "BodyGuard02_Shoes", "BodyGuard02_Torso", "BodyGuard_2_Pants"
        });

        // Step 3: Create DisguiseBoxes at each guard's torso position
        CreateDisguiseBox("DisguiseBox_Guard01", new Vector3(190f, 0.19f, 40f),
            "Bodyguard Mesh 01 Torso", SecurityClearance.Guard01,
            "Assets/Scenes/Scripts/DisguiseOutfits/Bodyguard01Outfit.asset");

        CreateDisguiseBox("DisguiseBox_Guard02", new Vector3(175f, 0.19f, 50f),
            "Bodyguard Mesh 02 Torso", SecurityClearance.Guard02,
            "Assets/Scenes/Scripts/DisguiseOutfits/Bodyguard02Outfit.asset");

        CreateDisguiseBox("DisguiseBox_Guard03", new Vector3(185f, 0.19f, 25f),
            "Bodyguard Mesh 03 Torso", SecurityClearance.Guard03,
            "Assets/Scenes/Scripts/DisguiseOutfits/Bodyguard03Outfit.asset");

        CreateDisguiseBox("DisguiseBox_Guard04", new Vector3(185f, 0.19f, -55f),
            "Bodyguard Mesh 04 Torso", SecurityClearance.Guard04,
            "Assets/Scenes/Scripts/DisguiseOutfits/Bodyguard04Outfit.asset");

        // Save the scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("SetupGuardDisguiseBoxes: Done. Sprites removed and 4 DisguiseBoxes created.");
    }

    private static void RemoveSpritesFromGuard(string guardName, string[] meshChildNames)
    {
        GameObject guard = GameObject.Find(guardName);
        if (guard == null)
        {
            Debug.LogError("Guard not found: " + guardName);
            return;
        }

        foreach (string childName in meshChildNames)
        {
            Transform child = guard.transform.Find(childName);
            if (child != null)
            {
                GameObject.DestroyImmediate(child.gameObject);
                Debug.Log($"Removed '{childName}' from '{guardName}'");
            }
            else
            {
                Debug.LogWarning($"Child '{childName}' not found on '{guardName}' — skipping.");
            }
        }
    }

    private static void CreateDisguiseBox(string goName, Vector3 position, string disguiseName,
        SecurityClearance clearance, string outfitAssetPath)
    {
        // Create GameObject
        GameObject go = new GameObject(goName);
        go.transform.position = position;

        // Solid BoxCollider (physical presence)
        BoxCollider solidCol = go.AddComponent<BoxCollider>();
        solidCol.size = new Vector3(1f, 1f, 1f);
        solidCol.center = Vector3.zero;
        solidCol.isTrigger = false;

        // DisguiseBox component
        DisguiseBox box = go.AddComponent<DisguiseBox>();
        SerializedObject so = new SerializedObject(box);

        so.FindProperty("disguiseName").stringValue = disguiseName;
        so.FindProperty("grantedClearance").enumValueIndex = (int)clearance;
        so.FindProperty("glowColor").colorValue = new Color(0.2f, 0.8f, 1f, 0.5f);

        DisguiseOutfit outfit = AssetDatabase.LoadAssetAtPath<DisguiseOutfit>(outfitAssetPath);
        if (outfit != null)
            so.FindProperty("disguiseOutfit").objectReferenceValue = outfit;
        else
            Debug.LogWarning($"Outfit not found at: {outfitAssetPath}");

        so.ApplyModifiedPropertiesWithoutUndo();

        // Trigger BoxCollider (interaction zone)
        BoxCollider triggerCol = go.AddComponent<BoxCollider>();
        triggerCol.size = new Vector3(3f, 3f, 3f);
        triggerCol.center = new Vector3(0f, 1f, 0f);
        triggerCol.isTrigger = true;

        Debug.Log($"Created '{goName}' at {position} with clearance {clearance}");
    }
}
