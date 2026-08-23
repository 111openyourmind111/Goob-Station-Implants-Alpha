using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

public sealed class CyberpsychosisClientSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        Logger.Info("[CLIENT CYBERPSYCHOSIS] SYSTEM INITIALIZED");

        SubscribeLocalEvent<CyberpsychosisComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(
        EntityUid uid,
        CyberpsychosisComponent component,
        ComponentStartup args)
    {
        Logger.Info(
            $"[CLIENT CYBERPSYCHOSIS] STARTUP: Sanity = {component.SanityValue}, State = {component.CurrentState}");
    }
}
