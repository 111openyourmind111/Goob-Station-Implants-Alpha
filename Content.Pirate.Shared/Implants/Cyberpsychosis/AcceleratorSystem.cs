using Content.Shared.Movement.Systems;

namespace Content.Pirate.Shared.Implants.Cyberpsychosis;

/// <summary>
///     Applies the movement speed edge of the Accelerator implant.
///     Runs on both client and server so the prediction matches.
/// </summary>
public sealed class AcceleratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AcceleratorComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
    }

    private static void OnRefreshMovementSpeed(Entity<AcceleratorComponent> ent,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedMultiplier);
    }
}