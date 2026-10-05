// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.Wonderland;

/// <summary>
///     Wonderland v.12.5a — a subdermal cyberdeck carrying the AI "Wonderland".
/// </summary>
/// <remarks>
///     Wonderland borrows the owner's nervous system as a relay, so it can drive
///     devices at close range while the owner keeps the credit (or the blame). The
///     catch is autonomy: every command spends the owner's grip on the AI, and the
///     grip only returns while Wonderland is left idle. Lose it completely and the AI
///     takes the body, which is terminal — it drops the owner's sanity to zero and
///     hands them to the cyberpsychosis takeover.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WonderlandComponent : Component
{
    /// <summary>
    ///     How much control the owner currently holds over Wonderland. At zero the AI
    ///     seizes the body.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Control = 100f;

    [DataField]
    public float MaxControl = 100f;

    /// <summary>
    ///     Control regained per second while Wonderland is not being commanded.
    /// </summary>
    [DataField]
    public float ControlRegen = 4f;

    /// <summary>
    ///     Sanity spent per command.
    /// </summary>
    [DataField]
    public float SanityCostPerUse = 1.5f;

    [DataField]
    public float DoorControlCost = 8f;

    [DataField]
    public float OverloadControlCost = 18f;

    /// <summary>
    ///     How close the owner must be to a target. Wonderland reaches through the
    ///     owner's own body, not through the room.
    /// </summary>
    [DataField]
    public float MaxRange = 24f;

    [DataField]
    public float ActionCooldown = 2f;

    /// <summary>
    ///     Seconds an overloaded device arcs before it lets go, so a wearer being
    ///     warned has a moment to throw it.
    /// </summary>
    [DataField]
    public float OverloadFuse = 3f;

    /// <summary>
    ///     Remaining action cooldown. Server-side only.
    /// </summary>
    public float Cooldown;

    /// <summary>
    ///     Last control severity pushed to the client's alert, so regeneration only
    ///     touches the network when the alert actually changes. Server-side only.
    /// </summary>
    public int LastSeverity;

    /// <summary>
    ///     Seconds left before each pending overloaded device goes off. Server-side only.
    /// </summary>
    public readonly Dictionary<EntityUid, float> PendingOverloads = new();
}