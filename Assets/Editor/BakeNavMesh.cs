using UnityEditor;
using UnityEditor.AI;

public class BakeNavMesh
{
    public static void Execute()
    {
        NavMeshBuilder.BuildNavMesh();
        UnityEngine.Debug.Log("NavMesh bake complete.");
    }
}
