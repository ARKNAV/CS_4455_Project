using UnityEngine;

/// <summary>
/// Holds a set of materials that define a disguise outfit.
/// Materials map to torso clothing slots on the player character.
/// This keeps the player model intact and only swaps upper-cloth visuals.
/// </summary>
[CreateAssetMenu(fileName = "NewDisguiseOutfit", menuName = "Disguise/Outfit")]
public class DisguiseOutfit : ScriptableObject
{
    [Header("Torso Materials")]
    [Tooltip("Material for the open-rolled shirt (upper_cloth outer layer)")]
    public Material shirtMaterial;

    [Tooltip("Material for the t-shirt (upper_cloth inner layer)")]
    public Material tshirtMaterial;
}
