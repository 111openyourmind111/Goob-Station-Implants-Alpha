// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Pirate.Shared.Implants.Wonderland;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Damage;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Electrocution;
using Content.Shared.Tag;
using Content.Shared.Stunnable;
using Content.Server.Power.Components;
using Content.Server.Construction.Components;
using Content.Shared.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Server.Electrocution;

namespace Content.Pirate.Server.Implants.Wonderland;

/// <summary>
///     Server logic for the Wonderland v.12.5a cyberdeck implant.
/// </summary>
public sealed class WonderlandSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doors = default!;
    [Dependency] private readonly SharedExplosionSystem _explosions = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly CyberpsychosisSystem _cyberpsychosis = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Each ability is a verb offered on the thing being targeted, gated on the
        // acting user carrying the implant and standing close enough to reach it.
        SubscribeLocalEvent<DoorComponent, GetVerbsEvent<InteractionVerb>>(OnDoorVerbs);
        SubscribeLocalEvent<DamageableComponent, GetVerbsEvent<InteractionVerb>>(OnDeviceVerbs);
        SubscribeLocalEvent<MobStateComponent, GetVerbsEvent<InteractionVerb>>(OnMobVerbs);
        SubscribeLocalEvent<WonderlandComponent, MapInitEvent>(OnWonderlandMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var comp in EntityQuery<WonderlandComponent>())
        {
            var uid = comp.Owner;

            if (comp.Cooldown > 0f)
                comp.Cooldown = MathF.Max(0f, comp.Cooldown - frameTime);

            // Wonderland settles when it is left alone. Only push the change when the
            // alert severity actually shifts, rather than every single frame.
            if (comp.Control < comp.MaxControl)
            {
                comp.Control = MathF.Min(comp.MaxControl, comp.Control + comp.ControlRegen * frameTime);

                var severity = ControlSeverity(comp);
                if (severity != comp.LastSeverity)
                {
                    comp.LastSeverity = severity;
                    Dirty(uid, comp);
                    _alerts.ShowAlert(uid, "WonderlandControl", (short) severity);
                }
            }

            TickOverloads(uid, comp, frameTime);
        }
    }

    private void TickOverloads(EntityUid owner, WonderlandComponent comp, float frameTime)
    {
        if (comp.PendingOverloads.Count == 0)
            return;

        List<EntityUid>? detonate = null;

        foreach (var (target, fuse) in comp.PendingOverloads)
        {
            var remaining = fuse - frameTime;

            if (remaining > 0f)
            {
                comp.PendingOverloads[target] = remaining;
                continue;
            }

            (detonate ??= []).Add(target);
        }

        if (detonate is null)
            return;

        foreach (var target in detonate)
        {
            comp.PendingOverloads.Remove(target);

            if (!Exists(target) || Deleted(target))
                continue;

            _explosions.QueueExplosion(target, "Electrical", 4f, 6f, 3f);
            Say(owner, "wonderland-overload-detonate");
        }
    }

    private void OnDoorVerbs(Entity<DoorComponent> target, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!TryGetCaster(ref args, out var owner, out var comp))
            return;

        var verb = CreateVerb(ref args, "wonderland-verb-door");
        verb.Act = () => OpenDoor(owner, comp, target);
    }

    private void OnDeviceVerbs(Entity<DamageableComponent> target, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!TryGetCaster(ref args, out var owner, out var comp))
            return;

        // Mobs are not devices. Damaging people is the owner's own problem.
        if (HasComp<BodyComponent>(target))
            return;

        // Only overload electronics/devices that actually have power circuitry.
        if (!HasComp<ApcComponent>(target) &&
            !HasComp<ComputerComponent>(target))
        {
            return;
        }

        var verb = CreateVerb(ref args, "wonderland-verb-overload");
        verb.Act = () => Overload(owner, comp, target);
    }

    private InteractionVerb CreateVerb(ref GetVerbsEvent<InteractionVerb> args, string key)
    {
        var verb = new InteractionVerb();
        verb.Text = verb.Message = Loc.GetString(key);
        args.Verbs.Add(verb);
        return verb;
    }

    /// <summary>
    ///     Shared gating for every Wonderland verb: the user must carry the implant,
    ///     be in range, and not be mid-cooldown.
    /// </summary>
    private bool TryGetCaster(
        ref GetVerbsEvent<InteractionVerb> args,
        out EntityUid owner,
        out WonderlandComponent comp)
    {
        owner = default;
        comp = null!;

        if (!args.CanInteract || !args.CanAccess || args.User is not { Valid: true } user)
            return false;

        if (!TryComp<WonderlandComponent>(user, out var found))
            return false;

        comp = found;

        if (!InRange(user, args.Target, comp))
            return false;

        owner = user;
        return true;
    }

    private bool InRange(EntityUid owner, EntityUid target, WonderlandComponent comp)
    {
        // Unobstructed: Wonderland reaches through the owner's body, so a wall between
        // them is a wall between Wonderland and the target.
        var ownerT = Transform(owner);
        var targetT = Transform(target);
        if (ownerT.MapID != targetT.MapID)
            return false;
        var dist = (ownerT.WorldPosition - targetT.WorldPosition).Length();
        return dist <= comp.MaxRange;
    }

    private void OpenDoor(EntityUid owner, WonderlandComponent comp, EntityUid door)
    {
        if (!Spend(owner, comp, comp.DoorControlCost))
            return;

        // Wonderland carries no credentials of its own. It spends the owner's, so it
        // can only ever open what the owner could already open.
        // AI bypasses credentials completely.
        _doors.TryOpen(door, user: owner);
        Say(owner, "wonderland-door-open");
    }

    private void Overload(EntityUid owner, WonderlandComponent comp, EntityUid device)
    {
        if (!Spend(owner, comp, comp.OverloadControlCost))
            return;

        if (comp.PendingOverloads.ContainsKey(device))
            return;

        comp.PendingOverloads[device] = comp.OverloadFuse;
        Dirty(owner, comp);

        _popup.PopupEntity(
            Loc.GetString("wonderland-overload-warning", ("seconds", comp.OverloadFuse)),
            device,
            PopupType.MediumCaution);

        Say(owner, "wonderland-overload-start");
    }

    /// <summary>
    ///     Pays the sanity and control price of a command. Returns false — having
    ///     spent nothing — if the command is not currently available.
    /// </summary>
    private bool Spend(EntityUid owner, WonderlandComponent comp, float controlCost)
    {
        if (comp.Cooldown > 0f)
            return false;

        if (comp.Control < controlCost)
        {
            Say(owner, "wonderland-unruly");
            return false;
        }

        if (!TryComp<CyberpsychosisComponent>(owner, out var cyber))
            return false;

        // Do not spend the owner's last sanity on a command: that is how Wonderland
        // kills them by accident rather than by takeover.
        if (cyber.SanityValue - comp.SanityCostPerUse <= 0f)
        {
            Say(owner, "wonderland-too-far-gone");
            return false;
        }

        comp.Control -= controlCost;
        comp.Cooldown = comp.ActionCooldown;
        Dirty(owner, comp);

        cyber.SanityValue = Math.Clamp(cyber.SanityValue - (int) comp.SanityCostPerUse, 0, cyber.BaseSanity);
        _cyberpsychosis.RefreshAlert(owner, cyber);

        comp.LastSeverity = ControlSeverity(comp);
        _alerts.ShowAlert(owner, "WonderlandControl", (short) comp.LastSeverity);

        if (comp.Control <= 0f)
            LoseControl(owner, comp, cyber);

        return true;
    }

    /// <summary>
    ///     Wonderland takes the body. The owner is not coming back: this drops sanity
    ///     to zero and hands them to the cyberpsychosis takeover, same as any other
    ///     descent into it.
    /// </summary>
    private void LoseControl(EntityUid owner, WonderlandComponent comp, CyberpsychosisComponent cyber)
    {
        comp.Control = 0f;
        comp.PendingOverloads.Clear();
        Dirty(owner, comp);

        cyber.SanityValue = 0;
        _cyberpsychosis.RefreshAlert(owner, cyber);

        _popup.PopupEntity(Loc.GetString("wonderland-lost-control"), owner, PopupType.LargeCaution);
        Say(owner, "wonderland-lost-control");
    }


    private void OnMobVerbs(Entity<MobStateComponent> target, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!TryGetCaster(ref args, out var owner, out var comp))
            return;

        // Shock: 1 second, small cost
        var shock = CreateVerb(ref args, "wonderland-verb-shock");
        shock.Act = () => ShockTarget(owner, comp, target);

        // Kill: +10 sanity on top of normal costs; 10 seconds
        var kill = CreateVerb(ref args, "wonderland-verb-kill");
        kill.Act = () => KillTarget(owner, comp, target);
    }

    private void Say(EntityUid owner, string key, params (string, object)[] args)
    {
        var text = Loc.GetString(key, args);
        _popup.PopupEntity(text, owner, PopupType.Medium);

        if (TryComp<ActorComponent>(owner, out var actor))
            _chat.DispatchServerMessage(actor.PlayerSession, text);
    }

    private static int ControlSeverity(WonderlandComponent comp)
    {
        var fraction = comp.MaxControl <= 0f ? 0f : comp.Control / comp.MaxControl;

        return fraction switch
        {
            > 0.75f => 1,
            > 0.5f => 2,
            > 0.25f => 3,
            _ => 4
        };
    }

    private void OnWonderlandMapInit(Entity<WonderlandComponent> ent, ref MapInitEvent args)
    {
        _tagSystem.AddTag(ent, "BypassInteractionRangeChecks");
    }

    private void ShockTarget(EntityUid owner, WonderlandComponent comp, EntityUid target)
    {
        if (!Spend(owner, comp, 5f))
            return;

        var stun = Get<SharedStunSystem>();
        stun.TryKnockdown(target, TimeSpan.FromSeconds(1f), true);
        Say(owner, "wonderland-shock-done");
    }

    private void KillTarget(EntityUid owner, WonderlandComponent comp, EntityUid target)
    {
        if (!TryComp<CyberpsychosisComponent>(owner, out var cyber))
            return;

        if (cyber.SanityValue - 10f - comp.SanityCostPerUse <= 0f)
        {
            Say(owner, "wonderland-too-far-gone");
            return;
        }

        if (!Spend(owner, comp, 30f))
            return;

        cyber.SanityValue = Math.Clamp(cyber.SanityValue - (int)10f, 0, cyber.BaseSanity);
        _cyberpsychosis.RefreshAlert(owner, cyber);
        if (cyber.SanityValue <= 0)
        {
            LoseControl(owner, comp, cyber);
            return;
        }

        Say(owner, "wonderland-kill-start");

        // Kill after 10 seconds (lethal override)
        Timer.Spawn(TimeSpan.FromSeconds(10f), () =>
        {
            if (Deleted(target) || !Exists(target))
                return;

            var mobState = Get<Content.Shared.Mobs.Systems.MobStateSystem>();
            mobState.ChangeMobState(target, Content.Shared.Mobs.MobState.Dead, origin: owner);
        });
    }

}
