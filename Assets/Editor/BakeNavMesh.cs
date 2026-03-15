using UnityEditor;
using UnityEngine.AI;
using UnityEditor.SceneManagement;

public class BakeNavMesh
{
    [MenuItem("Tools/Bake NavMesh")]
    public static void Execute()
    {
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
    }
}
