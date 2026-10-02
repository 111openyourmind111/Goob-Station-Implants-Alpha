// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

/// <summary>
///     Raised when the cyberpsychosis state changes (sanity thresholds crossed).
/// </summary>
[ByRefEvent]
public readonly record struct CyberpsychosisStateChangedEvent(SanityState OldState, SanityState NewState);