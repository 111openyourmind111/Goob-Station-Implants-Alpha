// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.RevivalHeart;

/// <summary>
///     A cybernetic heart that resurrects its host once, fully healed.
///     <see cref="Used"/> persists on the organ itself so re-implanting a spent
///     heart does not reset its single charge.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RevivalHeartComponent : Component
{
    /// <summary>
    ///     Whether the heart has already spent its single resurrection.
    /// </summary>
    [DataField]
    public bool Used;
}