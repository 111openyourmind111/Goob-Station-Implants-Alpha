// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

/// <summary>
///     Registers the cyberpsychosis visual effect overlays for the client.
/// </summary>
/// <remarks>
///     The overlays are added once and left registered permanently. Each overlay's
///     <c>BeforeDraw</c> reads the local player's current
///     <see cref="Content.Pirate.Shared.Implants.Cyberpsychosis.CyberpsychosisComponent" />
///     and returns false when it should not be visible, so no add/remove bookkeeping
///     is needed here.
///
///     Two earlier designs were wrong and are deliberately avoided:
///     <list type="bullet">
///     <item>
///         Splitting this across three systems subscribed to the same component, which
///         Robust rejects with "Duplicate Subscriptions" and which aborted client
///         player-state application, leaving the client stuck on the loading screen.
///     </item>
///     <item>
///         Adding/removing overlays in response to local events. Sanity changes are
///         raised server-side via <c>RaiseLocalEvent</c> and are not networked, so the
///         client never receives them; component state simply replicates silently and
///         no event fires. The overlays therefore never appeared.
///     </item>
///     </list>
/// </remarks>
public sealed class CyberpsychosisVisualEffectSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (!_overlayManager.HasOverlay<CyberpsychosisHallucinationOverlay>())
            _overlayManager.AddOverlay(new CyberpsychosisHallucinationOverlay());

        if (!_overlayManager.HasOverlay<CyberpsychosisGlitchOverlay>())
            _overlayManager.AddOverlay(new CyberpsychosisGlitchOverlay());

        if (!_overlayManager.HasOverlay<CyberpsychosisRealityBreakOverlay>())
            _overlayManager.AddOverlay(new CyberpsychosisRealityBreakOverlay());
    }
}