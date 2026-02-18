/// <summary>
/// Controls how an enemy reacts when the player enters its detection range.
/// Set per-enemy in the Inspector on EnemyController.
/// </summary>
public enum EnemyDisposition
{
    /// <summary>Detects player → immediately transitions to Chase.</summary>
    Hostile,

    /// <summary>Detects player → enters Alert stub (future state).</summary>
    Neutral,

    /// <summary>Never reacts to the player. Critters, ambient wildlife.</summary>
    Passive
}
