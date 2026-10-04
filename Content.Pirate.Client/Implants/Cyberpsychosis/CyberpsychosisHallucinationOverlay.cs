// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Content.Shared._DV.CCVars;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

/// <summary>
///     Chromatic aberration, noise, geometric distortions.
/// </summary>
public sealed class CyberpsychosisHallucinationOverlay : Overlay
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ShaderInstance? _shader;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    public CyberpsychosisHallucinationOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_cfg.GetCVar(DCCVars.NoVisionFilters))
            return false;

        var player = _playerManager.LocalEntity;
        if (player is not { Valid: true })
            return false;

        if (!_entityManager.TryGetComponent<CyberpsychosisComponent>(player, out var comp))
            return false;

        var intensity = CalculateIntensity(comp);
        if (intensity <= 0f)
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture is null)
            return;

        var player = _playerManager.LocalEntity;
        if (!_entityManager.TryGetComponent<CyberpsychosisComponent>(player, out var comp))
            return;

        var intensity = CalculateIntensity(comp);
        if (intensity <= 0f)
            return;

        _shader ??= _prototypeManager.Index<ShaderPrototype>("CyberpsychosisHallucination").Instance().Duplicate();

        _shader.SetParameter("hallucinationPower", intensity);
        _shader.SetParameter("time", (float)_timing.CurTime.TotalSeconds);
        _shader.SetParameter("sanityNormalized", comp.SanityValue / 100f);

        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.DrawTextureRect(Texture.White, args.ViewportBounds);
        handle.UseShader(null);
    }

    private float CalculateIntensity(CyberpsychosisComponent comp)
    {
        return comp.CurrentState switch
        {
            SanityState.Normal => 0f,
            SanityState.LessNormal => MathHelper.Clamp((45f - comp.SanityValue) / 25f, 0f, 0.3f),
            SanityState.CloseCyberpsychosis => MathHelper.Clamp(0.3f + (20f - comp.SanityValue) / 20f * 0.4f, 0.3f, 0.7f),
            SanityState.Cyberpsychosis => MathHelper.Clamp(0.7f + (20f - comp.SanityValue) / 20f * 0.3f, 0.7f, 1f),
            _ => 0f
        };
    }
}
