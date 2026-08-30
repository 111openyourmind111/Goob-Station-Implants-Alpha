// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Pirate.Shared.Implants.RevivalHeart;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;

namespace Content.Pirate.Server.Implants.RevivalHeart;

/// <summary>
///     Watches for death and, if the body contains an unused revival heart,
///     resurrects it once at full health.
/// </summary>
public sealed class RevivalHeartSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !TryComp<BodyComponent>(args.Target, out _))
            return;

        if (!_body.TryGetBodyOrganEntityComps<RevivalHeartComponent>((args.Target, null), out var organs))
            return;

        foreach (var organ in organs)
        {
            if (organ.Comp1.Used || organ.Comp2.OrganIntegrity <= 0)
                continue;

            Revive(args.Target, organ);
            return;
        }
    }

    private void Revive(EntityUid body, Entity<RevivalHeartComponent, OrganComponent> organ)
    {
        organ.Comp1.Used = true;
        Dirty(organ, organ.Comp1);

        Log.Info($"[REVIVAL HEART] {ToPrettyString(body)} was resurrected by {ToPrettyString(organ)}.");

        RaiseLocalEvent(body, new RejuvenateEvent());
        _mobState.ChangeMobState(body, MobState.Alive);

        // The mind visits the ghost while the body is dead, so return the player to the body.
        if (_mind.TryGetMind(body, out var mindId, out var mind)
            && mind.VisitingEntity is { Valid: true } visiting
            && HasComp<GhostComponent>(visiting))
        {
            _mind.UnVisit(mindId, mind);
        }

        _popup.PopupEntity(Loc.GetString("revival-heart-activation"), body, PopupType.LargeCaution);
    }
}