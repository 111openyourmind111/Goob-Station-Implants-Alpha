using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Shared.Alert;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Server.Implants;

public sealed class CyberpsychosisSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var mob = args.Mob;

        var component = EnsureComp<CyberpsychosisComponent>(mob);
        RefreshAlert(mob, component);

        Log.Info(
            $"[CYBERPSYCHOSIS] Added to {ToPrettyString(mob)}. " +
            $"Sanity = {component.SanityValue}, " +
            $"State = {component.CurrentState}");
    }

    private void OnShutdown(EntityUid uid, CyberpsychosisComponent component, ComponentShutdown args)
    {
        _alerts.ClearAlert(uid, "CyberpsychosisSanity");
    }

    public void RefreshAlert(EntityUid uid, CyberpsychosisComponent component)
    {
        var severity = SeverityFromSanity(component.SanityValue);

        component.CurrentState = (SanityState)(severity - 1);
        Dirty(uid, component);

        _alerts.ShowAlert(uid, "CyberpsychosisSanity", (short) severity);
    }

    private static int SeverityFromSanity(int sanity)
    {
        return sanity switch
        {
            > 70 => 1,
            > 45 => 2,
            > 20 => 3,
            _ => 4
        };
    }
}
