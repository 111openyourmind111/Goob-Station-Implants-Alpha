using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Shared.Alert;
using Content.Shared.GameTicking;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Server.Implants;

public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberpsychosisLoadComponent, ImplantImplantedEvent>(OnImplantAdded);
        SubscribeLocalEvent<CyberpsychosisLoadComponent, ImplantRemovedEvent>(OnImplantRemoved);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentShutdown>(OnShutdown);
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
        if (!TryComp<CyberpsychosisComponent>(args.Implanted, out var cyber))
            return;

        RecalculateSanity(args.Implanted, cyber);
    }

    private void OnImplantRemoved(Entity<CyberpsychosisLoadComponent> ent, ref ImplantRemovedEvent args)
    {
        if (!TryComp<CyberpsychosisComponent>(args.Implanted, out var cyber))
            return;

        RecalculateSanity(args.Implanted, cyber);
    }

    private void OnShutdown(EntityUid uid, CyberpsychosisComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, "CyberpsychosisSanity");
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

        component.ActiveImplantCount = count;
        component.SanityValue = Math.Clamp(component.BaseSanity - (int) MathF.Round(totalLoad), 0, component.BaseSanity);
        RefreshAlert(mob, component);
    }

    public void RefreshAlert(EntityUid uid, CyberpsychosisComponent component)
    {
        var severity = SeverityFromSanity(component);

        component.CurrentState = (SanityState)(severity - 1);
        Dirty(uid, component);

        _alerts.ShowAlert(uid, "CyberpsychosisSanity", (short) severity);
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
