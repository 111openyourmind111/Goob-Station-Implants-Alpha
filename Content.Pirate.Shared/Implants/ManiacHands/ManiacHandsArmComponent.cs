// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.ManiacHands;

/// <summary>
///     Lives on the Maniac Hands cybernetic arm and keeps the permanent
///     kill counter, which is persisted across surgical extraction and re-installation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ManiacHandsArmComponent : Component
{
    /// <summary>
    ///     How many kills the host has accumulated with these hands.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Kills;
}