using UnityEngine;
using UnityEditor;

public class DiagnoseTriggers
{
    [MenuItem("Tools/Diagnose Trigger Setup")]
    public static void Execute()
    {
        Debug.Log("=== TRIGGER DIAGNOSIS ===");

        // Find all DisguiseBox objects
        var disguiseBoxes = Object.FindObjectsByType<DisguiseBox>(FindObjectsSortMode.None);
        Debug.Log($"DisguiseBox count: {disguiseBoxes.Length}");

        foreach (var box in disguiseBoxes)
        {
            var rb = box.GetComponent<Rigidbody>();
            var colliders = box.GetComponents<Collider>();
            int layer = box.gameObject.layer;
            Debug.Log($"[Box] {box.name} | Layer: {layer} ({LayerMask.LayerToName(layer)}) | Rigidbody: {rb != null} | Colliders: {colliders.Length}");
            foreach (var col in colliders)
                Debug.Log($"  Collider: {col.GetType().Name} isTrigger={col.isTrigger} enabled={col.enabled}");
        }

        // Find the player
        var players = Object.FindObjectsByType<DisguiseSystem>(FindObjectsSortMode.None);
        Debug.Log($"DisguiseSystem (player) count: {players.Length}");

        foreach (var player in players)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb == null) rb = player.GetComponentInParent<Rigidbody>();
            var cols = player.GetComponentsInChildren<Collider>();
            int layer = player.gameObject.layer;
            Debug.Log($"[Player] {player.name} | Layer: {layer} ({LayerMask.LayerToName(layer)}) | Rigidbody: {rb != null}{(rb != null ? $" isKinematic={rb.isKinematic}" : "")}");
            foreach (var col in cols)
                Debug.Log($"  PlayerCollider: {col.name} {col.GetType().Name} isTrigger={col.isTrigger} enabled={col.enabled} layer={col.gameObject.layer}({LayerMask.LayerToName(col.gameObject.layer)})");
        }

        // Check layer collision matrix for relevant layers
        Debug.Log("=== LAYER COLLISION MATRIX (relevant pairs) ===");
        for (int i = 0; i < 32; i++)
        {
            string nameI = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(nameI)) continue;
            for (int j = i; j < 32; j++)
            {
                string nameJ = LayerMask.LayerToName(j);
                if (string.IsNullOrEmpty(nameJ)) continue;
                bool ignored = Physics.GetIgnoreLayerCollision(i, j);
                if (ignored)
                    Debug.Log($"  IGNORED: {nameI}({i}) <-> {nameJ}({j})");
            }
        }

        Debug.Log("=== END DIAGNOSIS ===");
    }
}
