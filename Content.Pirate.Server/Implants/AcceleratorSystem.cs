using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Shared.Damage;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Pirate.Server.Implants;

/// <summary>
///     Server-side half of the Accelerator implant: burns through biological
///     energy faster and drains hit points once the host has been starving
///     for longer than the grace period.
/// </summary>
public sealed class AcceleratorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var speedup = _timing.CurTime;
        var query = EntityQueryEnumerator<AcceleratorComponent>();
        while (query.MoveNext(out var uid, out var accelerator))
        {
            var hunger = TryComp<HungerComponent>(uid, out var hungerComp) ? hungerComp : null;

            // Burn through biological energy 50% faster.
            if (hunger is not null)
            {
                var extraHungerDrain = hunger.BaseDecayRate * (accelerator.HungerRateMultiplier - 1f);
                _hunger.ModifyHunger(uid, -extraHungerDrain * frameTime, hunger);
            }

            if (TryComp<ThirstComponent>(uid, out var thirsty))
            {
                var extraThirstDrain = thirsty.BaseDecayRate * (accelerator.HungerRateMultiplier - 1f);
                _thirst.ModifyThirst(uid, thirsty, -extraThirstDrain * frameTime);
            }

            // Ignore the starvation grace while the host is well fed.
            if (hunger is null || hunger.CurrentThreshold != HungerThreshold.Starving)
            {
                accelerator.StarvingSince = null;
                accelerator.LastStarvationDamageTime = null;
                continue;
            }

            accelerator.StarvingSince ??= speedup;

            var grace = TimeSpan.FromSeconds(accelerator.StarvationGraceDuration);
            if (speedup - accelerator.StarvingSince.Value <= grace)
                continue;

            if (accelerator.LastStarvationDamageTime is null
                || speedup - accelerator.LastStarvationDamageTime.Value > TimeSpan.FromSeconds(accelerator.StarvationDamageInterval))
            {
                var firstHit = accelerator.LastStarvationDamageTime is null;
                _damageable.TryChangeDamage(uid, accelerator.StarvationDamage, origin: uid);
                accelerator.LastStarvationDamageTime = speedup;

                if (firstHit)
                    _popup.PopupEntity(Loc.GetString("accelerator-starvation-damage"), uid, PopupType.MediumCaution);
            }
        }
    }
}