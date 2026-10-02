// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._DV.CCVars;
using Content.Shared._DV.CCVars;
using Content.Shared._DV.CCVars;
using Content.Shared._DV.CCVars;

using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

/// <summary>
///     Manages the reality break overlay lifecycle based on cyberpsychosis component state.
/// </summary>
public sealed class CyberpsychosisRealityBreakOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CyberpsychosisComponent, AddedComponentEventArgs>(OnAdded);
        SubscribeLocalEvent<CyberpsychosisComponent, RemovedComponentEventArgs>(OnRemoved);
        SubscribeLocalEvent<CyberpsychosisComponent, CyberpsychosisStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, CyberpsychosisComponent comp, ComponentStartup args)
    {
        UpdateOverlay(uid, comp);
    }

    private void OnAdded(EntityUid uid, CyberpsychosisComponent comp, AddedComponentEventArgs args)
    {
        UpdateOverlay(uid, comp);
    }

    private void OnRemoved(EntityUid uid, CyberpsychosisComponent comp, RemovedComponentEventArgs args)
    {
        _overlayManager.RemoveOverlay<CyberpsychosisRealityBreakOverlay>();
    }

    private void OnStateChanged(EntityUid uid, CyberpsychosisComponent comp, CyberpsychosisStateChangedEvent args)
    {
        UpdateOverlay(uid, comp);
    }

    private void UpdateOverlay(EntityUid uid, CyberpsychosisComponent comp)
    {
        if (IoCManager.Resolve<IConfigurationManager>().GetCVar(DCCVars.NoVisionFilters))
        {
            _overlayManager.RemoveOverlay<CyberpsychosisRealityBreakOverlay>();
            return;
        }

        var shouldShow = comp.SanityValue <= 0;

        if (shouldShow)
        {
            if (!_overlayManager.HasOverlay<CyberpsychosisRealityBreakOverlay>())
            {
                var overlay = new CyberpsychosisRealityBreakOverlay();
                _overlayManager.AddOverlay(overlay);
            }
        }
        else
        {
            _overlayManager.RemoveOverlay<CyberpsychosisRealityBreakOverlay>();
        }
    }
}