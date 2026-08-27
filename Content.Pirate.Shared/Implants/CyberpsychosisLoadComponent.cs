using Robust.Shared.GameObjects;

namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

[RegisterComponent]
public sealed partial class CyberpsychosisLoadComponent : Component
{
    [DataField("sanityCost")]
    public float SanityCost { get; set; } = 5f;
}
