using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Content.Pirate.Server.Implants;

[AdminCommand(AdminFlags.Fun)]
public sealed class SetSanityCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public string Command => "setsanity";
    public string Description => "Sets sanity value for a player with CyberpsychosisComponent.";
    public string Help => "Usage: setsanity <value> [netEntity]. Defaults to your own mob.";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !int.TryParse(args[0], out var value))
        {
            shell.WriteError("Invalid value.");
            return;
        }

        EntityUid? target;
        if (args.Length >= 2)
        {
            if (!NetEntity.TryParse(args[1], out var net) || !_entities.TryGetEntity(net, out target))
            {
                shell.WriteError("Invalid entity.");
                return;
            }
        }
        else
        {
            target = shell.Player?.AttachedEntity;
        }

        if (target is not { } mob)
        {
            shell.WriteError("No valid target specified.");
            return;
        }

        if (!_entities.TryGetComponent<CyberpsychosisComponent>(mob, out var comp))
        {
            shell.WriteError($"{_entities.ToPrettyString(mob)} has no CyberpsychosisComponent.");
            return;
        }

        var cyber = _systems.GetEntitySystem<CyberpsychosisSystem>();

        comp.SanityValue = Math.Clamp(value, 0, 100);
        cyber.RefreshAlert(mob, comp);

        shell.WriteLine($"Sanity of {_entities.ToPrettyString(mob)} = {comp.SanityValue} ({comp.CurrentState}).");
    }
}
