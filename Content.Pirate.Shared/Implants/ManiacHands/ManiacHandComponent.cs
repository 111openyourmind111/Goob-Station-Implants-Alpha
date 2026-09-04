// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.ManiacHands;

/// <summary>
///     Marks a cybernetic hand as part of the Maniac Hands pair.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ManiacHandComponent : Component
{
    /// <summary>
    ///     Which hand this is (Left or Right).
    /// </summary>
    [DataField, AutoNetworkedField]
    public HandSide Side;
}