using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

/// <summary>
///     Added to the host body while a Accelerator implant is installed.
///     Grants a universal speed edge at the cost of rapid biological energy drain.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AcceleratorComponent : Component
{
    /// <summary>
    ///     Multiplier applied to walk and sprint speed while the implant is active.
    /// </summary>
    [DataField("speedMultiplier")]
    [AutoNetworkedField]
    public float SpeedMultiplier { get; set; } = 10f;

    /// <summary>
    ///     Multiplier applied to hunger and thirst decay rates.
    /// </summary>
    [DataField("hungerRateMultiplier")]
    [AutoNetworkedField]
    public float HungerRateMultiplier { get; set; } = 15f;

    /// <summary>
    ///     Seconds of continuous starvation ("hungry" threshold) before the
    ///     implant starts draining hit points.
    /// </summary>
    [DataField("starvationGraceDuration")]
    [AutoNetworkedField]
    public float StarvationGraceDuration { get; set; } = 80f;

    /// <summary>
    ///     Seconds between each hit point burst dealt while starving past the grace period.
    /// </summary>
    [DataField("starvationDamageInterval")]
    [AutoNetworkedField]
    public float StarvationDamageInterval { get; set; } = 30f;

    /// <summary>
    ///     Damage dealt to the host while starving past the grace period.
    /// </summary>
    [DataField("starvationDamage")]
    public DamageSpecifier StarvationDamage { get; set; } = new()
    {
        DamageDict = new()
        {
            { "Brute", 7 }
        }
    };

    // Server-side runtime tracking of continuous starvation.
    [ViewVariables]
    public TimeSpan? StarvingSince { get; set; }

    [ViewVariables]
    public TimeSpan? LastStarvationDamageTime { get; set; }
}
