using System.Linq;
using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Pirate.Shared.Implants.ManiacHands;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Weapons.Melee;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Pirate.Server.Implants;

public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedGhostSystem _ghosts = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly ProtoId<NpcFactionPrototype> HostileFaction = "SimpleHostile";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberpsychosisLoadComponent, ImplantImplantedEvent>(OnImplantAdded);
        SubscribeLocalEvent<CyberpsychosisLoadComponent, ImplantRemovedEvent>(OnImplantRemoved);
        SubscribeLocalEvent<CyberpsychosisLoadComponent, OrganAddedEvent>(OnOrganLoadAdded);
        SubscribeLocalEvent<CyberpsychosisLoadComponent, OrganRemovedEvent>(OnOrganLoadRemoved);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CyberpsychosisComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var component = EnsureComp<CyberpsychosisComponent>(args.Mob);
        RecalculateSanity(args.Mob, component);

        Log.Info(
            $"[CYBERPSYCHOSIS] Added to {ToPrettyString(args.Mob)}. " +
            $"Sanity = {component.SanityValue}, State = {component.CurrentState}");
    }

    private void OnStartup(Entity<CyberpsychosisComponent> ent, ref ComponentStartup args)
    {
        RecalculateSanity(ent, ent.Comp);
    }

    private void OnImplantAdded(Entity<CyberpsychosisLoadComponent> ent, ref ImplantImplantedEvent args)
    {
        var cyber = EnsureComp<CyberpsychosisComponent>(args.Implanted);
        RecalculateSanity(args.Implanted, cyber);

        _popup.PopupEntity(
            Loc.GetString("cyberpsychosis-implant-info", ("value", cyber.SanityValue)),
            args.Implanted, PopupType.Medium);
    }

    private void OnImplantRemoved(Entity<CyberpsychosisLoadComponent> ent, ref ImplantRemovedEvent args)
    {
        if (!TryComp<CyberpsychosisComponent>(args.Implanted, out var cyber))
            return;

        RecalculateSanity(args.Implanted, cyber);

        _popup.PopupEntity(
            Loc.GetString("cyberpsychosis-implant-info", ("value", cyber.SanityValue)),
            args.Implanted, PopupType.Medium);
    }

    private void OnOrganLoadAdded(Entity<CyberpsychosisLoadComponent> ent, ref OrganAddedEvent args)
    {
        if (args.Body is not { Valid: true } body)
            return;

        var cyber = EnsureComp<CyberpsychosisComponent>(body);
        RecalculateSanity(body, cyber);

        _popup.PopupEntity(
            Loc.GetString("cyberpsychosis-implant-info", ("value", cyber.SanityValue)),
            body, PopupType.Medium);
    }

    private void OnOrganLoadRemoved(Entity<CyberpsychosisLoadComponent> ent, ref OrganRemovedEvent args)
    {
        if (args.OldBody is not { Valid: true } body)
            return;

        if (!TryComp<CyberpsychosisComponent>(body, out var cyber))
            return;

        RecalculateSanity(body, cyber);

        _popup.PopupEntity(
            Loc.GetString("cyberpsychosis-implant-info", ("value", cyber.SanityValue)),
            body, PopupType.Medium);
    }

    private void OnShutdown(EntityUid uid, CyberpsychosisComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, "CyberpsychosisSanity");
    }

    private void OnMobStateChanged(Entity<CyberpsychosisComponent> ent, ref MobStateChangedEvent args)
    {
        // Only snap when the character hits 0 sanity and actually dies.
        if (args.NewMobState != MobState.Dead || ent.Comp.SanityValue > 0)
            return;

        TakeOverAsHostile(ent);
    }

    public void RecalculateSanity(EntityUid mob, CyberpsychosisComponent component)
    {
        var totalLoad = 0f;
        var count = 0;

        if (TryComp<ImplantedComponent>(mob, out var implanted))
        {
            foreach (var implant in implanted.ImplantContainer.ContainedEntities)
            {
                if (!TryComp<CyberpsychosisLoadComponent>(implant, out var load))
                    continue;

                totalLoad += load.SanityCost;
                count++;
            }
        }

        // Cybernetic organ augments (e.g. Maniac Hands) also put strain on the mind.
        if (TryComp<BodyComponent>(mob, out _)
            && _body.TryGetBodyOrganEntityComps<CyberpsychosisLoadComponent>((mob, null), out var organs))
        {
            foreach (var organ in organs)
            {
                totalLoad += organ.Comp1.SanityCost;
                count++;
            }
        }

        var newLoad = totalLoad;
        var oldLoad = component.ActiveImplantLoad;

        // Apply only the NET change in cybernetic load to the current sanity, so
        // installing/removing an implant never resets or heals already-lost sanity.
        var delta = (int) MathF.Round(newLoad - oldLoad);
        if (delta != 0)
            component.SanityValue = Math.Clamp(component.SanityValue - delta, 0, component.BaseSanity);

        component.ActiveImplantCount = count;
        component.ActiveImplantLoad = newLoad;
        RefreshAlert(mob, component);
    }

    public void RefreshAlert(EntityUid uid, CyberpsychosisComponent component)
    {
        var severity = SeverityFromSanity(component);

        component.CurrentState = (SanityState)(severity - 1);
        Dirty(uid, component);

        _alerts.ShowAlert(uid, "CyberpsychosisSanity", (short) severity);

        if (component.SanityValue <= 0 && !_mobState.IsDead(uid))
        {
            // Capture the player before the death handler detaches them onto their ghost.
            ICommonSession? session = null;
            if (TryComp<ActorComponent>(uid, out var actor))
                session = actor.PlayerSession;

            Log.Info($"[CYBERPSYCHOSIS] {ToPrettyString(uid)} sanity reached 0, dying.");
            _mobState.ChangeMobState(uid, MobState.Dead);

            // The player exits the character permanently and can't return to it:
            // their ghost is stripped of the ability to re-enter this body.
            if (session?.AttachedEntity is { Valid: true } ghost
                && TryComp<GhostComponent>(ghost, out var ghostComp))
            {
                _ghosts.SetCanReturnToBody(ghost, false, ghostComp);
            }
        }
    }

    /// <summary>
    /// The player already died and got ghosted. Revive the husk as an AI-controlled
    /// hostile that melee attacks everyone.
    /// </summary>
    public void TakeOverAsHostile(EntityUid mob)
    {
        if (HasComp<HTNComponent>(mob))
            return;

        Log.Info($"[CYBERPSYCHOSIS] {ToPrettyString(mob)} snapped. AI takes control.");

        // Fully heal and revive the body (the player's mind is already on a ghost).
        RaiseLocalEvent(mob, new RejuvenateEvent(false, false));
        _mobState.ChangeMobState(mob, MobState.Alive);

        // Check if the body has BOTH Maniac Hands installed (paired cybernetic hands).
        bool hasManiacHands = false;
        if (TryComp<BodyComponent>(mob, out _))
        {
            // Find all body parts with ManiacHandComponent.
            var hands = _body.GetBodyPartChildrenWithComponent<ManiacHandComponent>(mob).ToList();
            bool hasLeft = hands.Any(h => h.Component.Side == HandSide.Left);
            bool hasRight = hands.Any(h => h.Component.Side == HandSide.Right);
            hasManiacHands = hasLeft && hasRight;
        }

        if (!hasManiacHands)
        {
            // Give it a generic melee attack.
            var melee = EnsureComp<MeleeWeaponComponent>(mob);
            melee.Damage = new DamageSpecifier
            {
                DamageDict = new()
                {
                    { "Blunt", 12 },
                    { "Piercing", 4 },
                    { "Structural", 8 }
                }
            };
            melee.Range = 1.5f;
            Dirty(mob, melee);
        }
        // else: rely on ManiacHandsSystem to boost natural unarmed damage.

        var combat = EnsureComp<CombatModeComponent>(mob);
        _combat.SetCanDisarm(mob, false, combat);

        // Enemy of everyone.
        _faction.ClearFactions(mob, dirty: false);
        _faction.AddFaction(mob, HostileFaction);

        // Hostile melee brain.
        var htn = EnsureComp<HTNComponent>(mob);
        htn.RootTask = new HTNCompoundTask { Task = "SimpleHostileCompound" };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, mob);
        _npc.SleepNPC(mob, htn);

        _mind.MakeSentient(mob);

        _npc.WakeNPC(mob, htn);

        _popup.PopupEntity(Loc.GetString("cyberpsychosis-takeover", ("target", mob)), mob, PopupType.LargeCaution);
    }

    private static int SeverityFromSanity(CyberpsychosisComponent c)
    {
        return c.SanityValue switch
        {
            > 70 => 1,
            > 45 => 2,
            > 20 => 3,
            _ => 4
        };
    }
}
