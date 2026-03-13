/// <summary>
/// Security clearance tiers used by the disguise system and zone access control.
/// Each disguise grants a specific clearance level. Zones require a minimum clearance.
/// Higher numeric value = higher clearance.
/// </summary>
public enum SecurityClearance
{
    /// <summary>No disguise / civilian clothes. Access to public areas only.</summary>
    Civilian = 0,

    /// <summary>Guard 01 uniform. Access to maintenance and engineering areas.</summary>
    Guard01 = 1,

    /// <summary>Guard 02 outfit. Access to security checkpoints and transit zones.</summary>
    Guard02 = 2,

    /// <summary>Guard 03 uniform. Access to research labs and operations.</summary>
    Guard03 = 3,

    /// <summary>Guard 04 rank. Access to command core and all restricted areas.</summary>
    Guard04 = 4
}
