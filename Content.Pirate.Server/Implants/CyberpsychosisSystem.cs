using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Server.Implants;

public sealed class CyberpsychosisSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var mob = args.Mob;

        var component = EnsureComp<CyberpsychosisComponent>(mob);

        Log.Info(
            $"[CYBERPSYCHOSIS] Added to {ToPrettyString(mob)}. " +
            $"Sanity = {component.SanityValue}, " +
            $"State = {component.CurrentState}");
    }
}
