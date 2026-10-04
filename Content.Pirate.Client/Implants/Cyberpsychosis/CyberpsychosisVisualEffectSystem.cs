// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Shared._DV.CCVars;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

/// <summary>
///     Drives the cyberpsychosis visual effect overlays for the local player.
/// </summary>
/// <remarks>
///     All three overlays are handled here rather than in one system each because
///     Robust only permits a single subscription per (component, event) pair. Three
///     separate systems subscribing to <see cref="CyberpsychosisComponent" /> threw
///     "Duplicate Subscriptions" on the client, which aborted player state application
///     and left the client stuck on the loading screen.
/// </remarks>
public sealed class CyberpsychosisVisualEffectSystem : EntitySystem
{
    /// <summary>
    ///     Sanity at which the reality break overlay begins ramping up. Must stay in
    ///     sync with <c>CyberpsychosisRealityBreakOverlay</c>.
    /// </summary>
    private const float RealityBreakThreshold = 10f;

    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CyberpsychosisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberpsychosisComponent, AddedComponentEventArgs>(OnAdded);
        SubscribeLocalEvent<CyberpsychosisComponent, RemovedComponentEventArgs>(OnRemoved);
        SubscribeLocalEvent<CyberpsychosisComponent, CyberpsychosisStateChangedEvent>(OnStateChanged);
    }

    private void OnStartup(EntityUid uid, CyberpsychosisComponent comp, ComponentStartup args)
    {
        UpdateOverlays();
    }

    private void OnAdded(EntityUid uid, CyberpsychosisComponent comp, AddedComponentEventArgs args)
    {
        UpdateOverlays();
    }

    private void OnRemoved(EntityUid uid, CyberpsychosisComponent comp, RemovedComponentEventArgs args)
    {
        UpdateOverlays();
    }

    private void OnStateChanged(EntityUid uid, CyberpsychosisComponent comp, CyberpsychosisStateChangedEvent args)
    {
        UpdateOverlays();
    }

    private void UpdateOverlays()
    {
        var player = _playerManager.LocalEntity;

        // The overlays are global to the viewport, so they must only ever reflect the
        // local player. Any other mob gaining or losing the component is irrelevant.
        if (_cfg.GetCVar(DCCVars.NoVisionFilters)
            || player is not { Valid: true }
            || !TryComp<CyberpsychosisComponent>(player, out var comp))
        {
            RemoveAll();
            return;
        }

        if (comp.CurrentState >= SanityState.CloseCyberpsychosis)
        {
            if (!_overlayManager.HasOverlay<CyberpsychosisHallucinationOverlay>())
                _overlayManager.AddOverlay(new CyberpsychosisHallucinationOverlay());
        }
        else
        {
            _overlayManager.RemoveOverlay<CyberpsychosisHallucinationOverlay>();
        }

        if (comp.CurrentState >= SanityState.Cyberpsychosis)
        {
            if (!_overlayManager.HasOverlay<CyberpsychosisGlitchOverlay>())
                _overlayManager.AddOverlay(new CyberpsychosisGlitchOverlay());
        }
        else
        {
            _overlayManager.RemoveOverlay<CyberpsychosisGlitchOverlay>();
        }

        if (comp.SanityValue <= RealityBreakThreshold)
        {
            if (!_overlayManager.HasOverlay<CyberpsychosisRealityBreakOverlay>())
                _overlayManager.AddOverlay(new CyberpsychosisRealityBreakOverlay());
        }
        else
        {
            _overlayManager.RemoveOverlay<CyberpsychosisRealityBreakOverlay>();
        }
    }

    private void RemoveAll()
    {
        _overlayManager.RemoveOverlay<CyberpsychosisHallucinationOverlay>();
        _overlayManager.RemoveOverlay<CyberpsychosisGlitchOverlay>();
        _overlayManager.RemoveOverlay<CyberpsychosisRealityBreakOverlay>();
    }
}