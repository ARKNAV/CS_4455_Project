using UnityEngine;
using UnityEditor;
using System.Text;

public class TrimTakedownClips
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        string[] paths = {
            "Assets/Animations/Strangling.fbx",
            "Assets/Animations/Being Strangled.fbx"
        };

        // Report actual runtime clip lengths after the reimport
        foreach (var path in paths)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                var clips = importer.clipAnimations;
                if (clips.Length > 0)
                    sb.AppendLine($"{path}: firstFrame={clips[0].firstFrame} lastFrame={clips[0].lastFrame}");
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip c && !c.name.StartsWith("__preview__"))
                    sb.AppendLine($"  Runtime clip '{c.name}': length={c.length:F4}s  fps={c.frameRate}");
            }
        }

        return sb.ToString();
    }
}
