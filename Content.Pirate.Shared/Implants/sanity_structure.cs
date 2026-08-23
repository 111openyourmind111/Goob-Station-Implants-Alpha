using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

public enum SanityState : byte {
    Normal = 0,
    LessNormal = 1,
    CloseCyberpsychosis = 2,
    Cyberpsychosis = 3,
}


[RegisterComponent]
public sealed partial class CyberpsychosisComponent : Component {

    [DataField("sanityValue")]
    public int SanityValue { get; set; } = 100;


    [DataField("sanityState")]
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


    [DataField("isCyberpsychotic")]
    public bool IsCyberpsychotic { get; set; } = false;


    [DataField("activeImplantCount")]
    public int ActiveImplantCount { get; set; } = 0;


    public float UncontrolledActionTimer { get; set; } = 0f;
    [DataField("uncontrolledActionCheckInterval")]


    public float UncontrolledActionCheckInterval { get; set; } = 1f;
    [DataField("uncontrolledActionChance")]


    public float UncontrolledActionChance { get; set; } = 5f;


}


[Serializable, NetSerializable]
public sealed class CyberpsychosisStateMessage : ComponentMessage {
    public int SanityValue { get; set; }
    public SanityState CurrentState { get; set; }
    public bool IsCyberpsychotic { get; set; }
}
