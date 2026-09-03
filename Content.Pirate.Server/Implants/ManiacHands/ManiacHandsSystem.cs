// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Pirate.Shared.Implants.ManiacHands;
using Content.Server.Chat.Managers;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
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
///     Server-side logic for the Maniac Hands cybernetic arm: while it is
///     surgically installed, empty-handed strikes deal escalating damage per
///     kill while gnawing at the host's sanity.
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

        SubscribeLocalEvent<ManiacHandsComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ManiacHandsComponent, AfterMeleeHitEvent>(OnAfterMeleeHit);
        SubscribeLocalEvent<ManiacHandsArmComponent, OrganAddedEvent>(OnArmAdded);
        SubscribeLocalEvent<ManiacHandsArmComponent, OrganRemovedEvent>(OnArmRemoved);
    }

    private void OnMeleeHit(Entity<ManiacHandsComponent> ent, ref MeleeHitEvent args)
    {
        if (args.Handled || !args.IsHit)
            return;

        // Only natural strikes are boosted: when the melee "weapon" is the mob itself.
        if (args.Weapon != args.User)
            return;

        // Strictly require completely empty hands.
        if (_hands.EnumerateHeld(args.User).Any())
            return;

        // Remember whether each target was already dead before the swing lands so
        // the after-hit handler can attribute actual kills.
        ent.Comp.HitTargets.Clear();
        foreach (var target in args.HitEntities)
            ent.Comp.HitTargets[target] = _mobState.IsDead(target);

        if (ent.Comp.Arm is not { Valid: true } arm
            || !TryComp<ManiacHandsArmComponent>(arm, out var armComp))
        {
            return;
        }

        var total = ent.Comp.BaseDamage + armComp.Kills * ent.Comp.DamagePerKill;
        var bonus = FixedPoint2.New(total) - args.BaseDamage.GetTotal();
        if (bonus > FixedPoint2.Zero)
            args.BonusDamage.DamageDict["Blunt"] = args.BonusDamage.DamageDict.GetValueOrDefault("Blunt") + bonus;
    }

    private void OnAfterMeleeHit(Entity<ManiacHandsComponent> ent, ref AfterMeleeHitEvent args)
    {
        if (ent.Comp.Arm is not { Valid: true } arm
            || !TryComp<ManiacHandsArmComponent>(arm, out var armComp))
        {
            ent.Comp.HitTargets.Clear();
            return;
        }

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
                armComp.Kills += kills;
                Dirty(arm, armComp);

                var damage = ent.Comp.BaseDamage + armComp.Kills * ent.Comp.DamagePerKill;
                _popup.PopupEntity(
                    Loc.GetString("maniac-hands-kill", ("kills", armComp.Kills), ("damage", damage)),
                    ent, PopupType.Medium);

                UpdateAlert(ent, armComp.Kills);
            }
        }

        ent.Comp.HitTargets.Clear();
        DrainSanity(ent);
    }

    private void OnArmAdded(Entity<ManiacHandsArmComponent> arm, ref OrganAddedEvent args)
    {
        if (args.Body is not { Valid: true } body)
            return;

        var host = EnsureComp<ManiacHandsComponent>(body);
        host.Arm = arm.Owner;
        Dirty(body, host);
    }

    private void OnArmRemoved(Entity<ManiacHandsArmComponent> arm, ref OrganRemovedEvent args)
    {
        if (args.OldBody is not { Valid: true } body)
            return;

        _alerts.ClearAlert(body, "ManiacHandsKills");

        if (TryComp<ManiacHandsComponent>(body, out var host))
        {
            if (host.Arm == arm.Owner)
                host.Arm = null;

            // No Maniac Hands arms left in the body: drop the host component.
            if (host.Arm is not { Valid: true }
                && TryComp<BodyComponent>(body, out _)
                && !_body.TryGetBodyOrganEntityComps<ManiacHandsArmComponent>((body, null), out _))
            {
                EntityManager.RemoveComponent<ManiacHandsComponent>(body);
                return;
            }

            Dirty(body, host);
        }
    }

    private void UpdateAlert(Entity<ManiacHandsComponent> ent, int kills)
    {
        _alerts.ShowAlert(ent.Owner, "ManiacHandsKills", (short) SeverityFromKills(kills));

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

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _chat.DispatchServerMessage(actor.PlayerSession, msg);

        var popupType = kills >= 8 ? PopupType.LargeCaution : PopupType.MediumCaution;
        _popup.PopupEntity(msg, ent, ent, popupType);
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
        if (!TryComp<CyberpsychosisComponent>(ent, out var cyber))
            return;

        // If sanity is already 0, stop draining — avoids re-triggering death.
        if (cyber.SanityValue <= 0)
            return;

        ent.Comp.SanityDrainBuffer += ent.Comp.SanityDrainPerPunch;
        var whole = (int) ent.Comp.SanityDrainBuffer;
        if (whole <= 0)
            return;

        ent.Comp.SanityDrainBuffer -= whole;
        cyber.SanityValue = Math.Clamp(cyber.SanityValue - whole, 0, cyber.BaseSanity);
        _cyberpsychosis.RefreshAlert(ent, cyber);
    }
}