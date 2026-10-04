// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

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
    /// <summary>
    ///     Shader prototype ids, kept in sync with
    ///     <c>Resources/Prototypes/_Pirate/Shaders/shaders.yml</c>. These are checked
    ///     at startup so a renamed or missing prototype is reported immediately
    ///     instead of silently failing inside <c>Overlay.Draw</c>.
    /// </summary>
    private static readonly string[] ShaderIds =
    [
        "CyberpsychosisHallucination",
        "CyberpsychosisGlitch",
        "CyberpsychosisRealityBreak"
    ];

    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ILogManager _log = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (!_overlayManager.HasOverlay<CyberpsychosisHallucinationOverlay>())
            _overlayManager.AddOverlay(new CyberpsychosisHallucinationOverlay());

        if (!_overlayManager.HasOverlay<CyberpsychosisGlitchOverlay>())
            _overlayManager.AddOverlay(new CyberpsychosisGlitchOverlay());

        if (!_overlayManager.HasOverlay<CyberpsychosisRealityBreakOverlay>())
            _overlayManager.AddOverlay(new CyberpsychosisRealityBreakOverlay());

        ValidateShaders();
    }

    private void ValidateShaders()
    {
        var sawmill = _log.GetSawmill("cyberpsychosis");

        foreach (var id in ShaderIds)
        {
            if (!_prototypeManager.Resolve<ShaderPrototype>(id, out _))
                sawmill.Error($"Shader prototype '{id}' is missing; cyberpsychosis overlays will not render.");
            else
                sawmill.Debug($"Shader prototype '{id}' OK.");
        }

        sawmill.Info(
            $"Registered cyberpsychosis overlays (hallucination={_overlayManager.HasOverlay<CyberpsychosisHallucinationOverlay>()}, " +
            $"glitch={_overlayManager.HasOverlay<CyberpsychosisGlitchOverlay>()}, " +
            $"realityBreak={_overlayManager.HasOverlay<CyberpsychosisRealityBreakOverlay>()}).");
    }
}