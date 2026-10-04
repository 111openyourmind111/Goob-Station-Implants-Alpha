using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

public enum SanityState : byte
{
    Normal = 0,
    LessNormal = 1,
    CloseCyberpsychosis = 2,
    Cyberpsychosis = 3,
}

/// <summary>
///     Sanity boundaries that drive cyberpsychosis escalation.
/// </summary>
/// <remarks>
///     These live here, in the shared project, because both the server (alert severity
///     and <see cref="SanityState" />) and the client (visual effect overlays) need to
///     agree on them. They used to be hardcoded independently in three separate files,
///     which let the visuals and the alerts drift apart.
/// </remarks>
public static class CyberpsychosisThresholds
{
    /// <summary>
    ///     Sanity at which the player stops being <see cref="SanityState.Normal" />.
    ///     Also where the reality break overlay starts, ramping up to full at sanity 0.
    /// </summary>
    public const int RealityBreak = 60;

    /// <summary>
    ///     Sanity at which the hallucination overlay begins ramping in.
    /// </summary>
    public const int Hallucination = 45;

    /// <summary>
    ///     Sanity at which the glitch overlay begins, and where
    ///     <see cref="SanityState.Cyberpsychosis" /> begins.
    /// </summary>
    public const int Glitch = 20;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CyberpsychosisComponent : Component
{
    [DataField("sanityValue")]
    [AutoNetworkedField]
    public int SanityValue { get; set; } = 100;

    [DataField("sanityState")]
    [AutoNetworkedField]
    public SanityState CurrentState { get; set; } = SanityState.Normal;

    [DataField("decreaseSanity")]
    public float DecreaseSanity { get; set; } = 0.1f;

    [DataField("autoActionThreshold")]
    public int AutoActionThreshold { get; set; } = 20;

    [DataField("cyberpsychosisThreshold")]
    public int CyberpsychosisThreshold { get; set; } = 0;

    [DataField("recoverySanity")]
    public float RecoverySanity { get; set; } = 0.05f;

    [DataField("antidepressantBonus")]
    public float AntidepressantBonus { get; set; } = 15f;

    [DataField("drugRecoveryBonus")]
    public float DrugRecoveryBonus { get; set; } = 10f;

    public bool IsCyberpsychotic =>
        CurrentState == SanityState.Cyberpsychosis;

    [DataField("activeImplantCount")]
    public int ActiveImplantCount { get; set; }

    [DataField("activeImplantLoad")]
    public float ActiveImplantLoad { get; set; }

    public float UncontrolledActionTimer { get; set; }

    [DataField("uncontrolledActionCheckInterval")]
    public float UncontrolledActionCheckInterval { get; set; } = 1f;

    [DataField("uncontrolledActionChance")]
    public float UncontrolledActionChance { get; set; } = 5f;

    [DataField("baseSanity")]
    public int BaseSanity { get; set; } = 100;
}
