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

    public float UncontrolledActionTimer { get; set; }

    [DataField("uncontrolledActionCheckInterval")]
    public float UncontrolledActionCheckInterval { get; set; } = 1f;

    [DataField("uncontrolledActionChance")]
    public float UncontrolledActionChance { get; set; } = 5f;
}
