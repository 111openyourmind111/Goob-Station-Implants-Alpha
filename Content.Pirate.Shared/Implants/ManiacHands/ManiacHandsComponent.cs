// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.ManiacHands;

/// <summary>
///     Which hand the ManiacHandComponent is on.
/// </summary>
public enum HandSide : byte
{
    Left,
    Right
}

/// <summary>
///     Added to the host while BOTH Maniac Hands are installed.
///     The kill counter lives here and persists while at least one hand remains.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ManiacHandsComponent : Component
{
    /// <summary>
    ///     The left maniac hand entity (if installed).
    /// </summary>
    [DataField]
    public EntityUid? LeftHand;

    /// <summary>
    ///     The right maniac hand entity (if installed).
    /// </summary>
    [DataField]
    public EntityUid? RightHand;

    /// <summary>
    ///     Damage dealt by an empty-handed strike with no accumulated kills.
    /// </summary>
    [DataField]
    public int BaseDamage = 27;

    /// <summary>
    ///     Extra damage per accumulated kill.
    /// </summary>
    [DataField]
    public int DamagePerKill = 3;

    /// <summary>
    ///     Sanity drained per empty-handed strike.
    /// </summary>
    [DataField]
    public float SanityDrainPerPunch = 0.5f;

    /// <summary>
    ///     Total kills accumulated across both hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Kills;

    /// <summary>
    ///     Fractional sanity drain accumulator, flushed into the integer
    ///     sanity value once it reaches a whole point.
    /// </summary>
    public float SanityDrainBuffer;

    /// <summary>
    ///     Tracks the pre-hit mob state of every entity hit this swing so
    ///     kills can be attributed to the Maniac Hands.
    /// </summary>
    public readonly Dictionary<EntityUid, bool> HitTargets = new();
}
