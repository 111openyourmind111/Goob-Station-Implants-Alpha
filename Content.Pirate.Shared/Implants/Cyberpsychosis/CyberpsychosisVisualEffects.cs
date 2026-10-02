// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

/// <summary>
///     Visual hallucinations: chromatic aberration, noise, geometric distortions.
///     Activates at CloseCyberpsychosis (sanity <= 45).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberpsychosisHallucinationComponent : Component
{
}

/// <summary>
///     Visual glitches: scanlines, tearing, pixel sorting, screen tearing.
///     Activates at Cyberpsychosis (sanity <= 20).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberpsychosisGlitchComponent : Component
{
}

/// <summary>
///     Severe reality distortion: geometry warping, color inversion, time dilation.
///     Activates at sanity <= 0 (full cyberpsychosis).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CyberpsychosisRealityBreakComponent : Component
{
}