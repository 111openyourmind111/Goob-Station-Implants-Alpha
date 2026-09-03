// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.ManiacHands;

/// <summary>
///     Added to the host while the Maniac Hands cybernetic arm is installed.
///     The permanent kill counter lives on <see cref="ManiacHandsArmComponent"/>
///     so it survives surgical extraction and re-installation.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ManiacHandsComponent : Component
{
    /// <summary>
    ///     The cybernetic arm entity backing these hands.
    /// </summary>
    [DataField]
    public EntityUid? Arm;

    /// <summary>
    ///     Damage dealt by an empty-handed strike with no accumulated kills.
    /// </summary>
    [DataField]
    public int BaseDamage = 28;

    /// <summary>
    ///     Extra damage per accumulated kill.
    /// </summary>
    [DataField]
    public int DamagePerKill = 4;

    /// <summary>
    ///     Sanity drained per empty-handed strike.
    /// </summary>
    [DataField]
    public float SanityDrainPerPunch = 0.7f;

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
