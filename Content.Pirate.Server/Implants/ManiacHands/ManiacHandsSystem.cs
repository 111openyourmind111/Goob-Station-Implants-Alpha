// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Pirate.Shared.Implants.ManiacHands;
using Content.Server.Chat.Managers;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._Pirate.Weapons.Melee.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Pirate.Server.Implants.ManiacHands;

/// <summary>
///     Server-side logic for the Maniac Hands paired cybernetic hands.
///     When both hands are installed, empty-handed strikes deal escalating
///     damage per kill while gnawing at the host's sanity.
/// </summary>
public sealed class ManiacHandsSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly CyberpsychosisSystem _cyberpsychosis = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManiacHandComponent, BodyPartAddedEvent>(OnHandAdded);
        SubscribeLocalEvent<ManiacHandComponent, BodyPartRemovedEvent>(OnHandRemoved);
        SubscribeLocalEvent<ManiacHandsComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ManiacHandsComponent, AfterMeleeHitEvent>(OnAfterMeleeHit);
    }

    private void OnHandAdded(Entity<ManiacHandComponent> hand, ref BodyPartAddedEvent args)
    {
        var body = args.Part.Comp.Body;
        if (body is not { Valid: true })
            return;

        // Link this hand to the host's ManiacHandsComponent.
        var host = EnsureComp<ManiacHandsComponent>(body.Value);
        if (hand.Comp.Side == HandSide.Left)
            host.LeftHand = hand.Owner;
        else
            host.RightHand = hand.Owner;

        Dirty(body.Value, host);

        // If both hands are now present, activate the mechanic.
        if (host.LeftHand is { Valid: true } && host.RightHand is { Valid: true })
        {
            _alerts.ShowAlert(body.Value, "ManiacHandsKills", (short)SeverityFromKills(host.Kills));
            _popup.PopupEntity(Loc.GetString("maniac-hands-activated"), body.Value, PopupType.LargeCaution);
        }
    }

    private void OnHandRemoved(Entity<ManiacHandComponent> hand, ref BodyPartRemovedEvent args)
    {
        var body = args.Part.Comp.Body;
        if (body is not { Valid: true })
            return;

        if (!TryComp<ManiacHandsComponent>(body.Value, out var host))
            return;

        // Unlink the hand.
        if (hand.Comp.Side == HandSide.Left && host.LeftHand == hand.Owner)
            host.LeftHand = null;
        else if (hand.Comp.Side == HandSide.Right && host.RightHand == hand.Owner)
            host.RightHand = null;

        // If we no longer have both hands, deactivate.
        if (host.LeftHand is not { Valid: true } || host.RightHand is not { Valid: true })
        {
            _alerts.ClearAlert(body.Value, "ManiacHandsKills");

            // If no hands left at all, drop the host component entirely.
            if (host.LeftHand is not { Valid: true } && host.RightHand is not { Valid: true })
            {
                EntityManager.RemoveComponent<ManiacHandsComponent>(body.Value);
                return;
            }
        }

        Dirty(body.Value, host);
    }

    private void OnMeleeHit(Entity<ManiacHandsComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || !args.IsHit)
            return;

        // Only boost natural, empty-handed punches.
        if (args.Weapon != args.User)
            return;

        if (_hands.EnumerateHeld(args.User).Any())
            return;

        // Record pre-hit states for kill attribution.
        ent.Comp.HitTargets.Clear();
        foreach (var target in args.HitEntities)
            ent.Comp.HitTargets[target] = _mobState.IsDead(target);

        var total = ent.Comp.BaseDamage + ent.Comp.Kills * ent.Comp.DamagePerKill;
        var bonus = FixedPoint2.New(total) - args.BaseDamage.GetTotal();
        if (bonus > FixedPoint2.Zero)
            args.BonusDamage.DamageDict["Blunt"] = args.BonusDamage.DamageDict.GetValueOrDefault("Blunt") + bonus;
    }

    private void OnAfterMeleeHit(Entity<ManiacHandsComponent> ent, ref AfterMeleeHitEvent args)
    {
        if (args.IsHit && args.Weapon == args.User)
        {
            var kills = 0;
            foreach (var target in args.HitEntities)
            {
                if (ent.Comp.HitTargets.TryGetValue(target, out var wasDead) && !wasDead && _mobState.IsDead(target))
                    kills++;
            }

            if (kills > 0)
            {
                ent.Comp.Kills += kills;
                Dirty(ent.Owner, ent.Comp);

                var damage = ent.Comp.BaseDamage + ent.Comp.Kills * ent.Comp.DamagePerKill;
                _popup.PopupEntity(
                    Loc.GetString("maniac-hands-kill", ("kills", ent.Comp.Kills), ("damage", damage)),
                    ent, PopupType.Medium);

                UpdateAlert(ent, ent.Comp.Kills);
            }
        }

        ent.Comp.HitTargets.Clear();
        DrainSanity(ent);
    }

    private void UpdateAlert(Entity<ManiacHandsComponent> ent, int kills)
    {
        _alerts.ShowAlert(ent.Owner, "ManiacHandsKills", (short)SeverityFromKills(kills));

        // One-off dramatic killstreak announcements.
        string? msg = kills switch
        {
            3 => Loc.GetString("maniac-hands-streak-triple-kill"),
            5 => Loc.GetString("maniac-hands-streak-bloodshed"),
            8 => Loc.GetString("maniac-hands-streak-rageee"),
            _ => null
        };
        if (msg is null)
            return;

        if (!TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        _chat.DispatchServerMessage(actor.PlayerSession, msg);

        var popupType = kills >= 8 ? PopupType.LargeCaution : PopupType.MediumCaution;
        _popup.PopupEntity(msg, ent.Owner, ent.Owner, popupType);
    }

    private static int SeverityFromKills(int kills)
    {
        return kills switch
        {
            >= 8 => 4,
            >= 5 => 3,
            >= 3 => 2,
            _ => 1
        };
    }

    private void DrainSanity(Entity<ManiacHandsComponent> ent)
    {
        if (!TryComp<CyberpsychosisComponent>(ent.Owner, out var cyber))
            return;

        if (cyber.SanityValue <= 0)
            return;

        ent.Comp.SanityDrainBuffer += ent.Comp.SanityDrainPerPunch;
        var whole = (int)ent.Comp.SanityDrainBuffer;
        if (whole <= 0)
            return;

        ent.Comp.SanityDrainBuffer -= whole;
        cyber.SanityValue = Math.Clamp(cyber.SanityValue - whole, 0, cyber.BaseSanity);
        _cyberpsychosis.RefreshAlert(ent.Owner, cyber);
    }
}