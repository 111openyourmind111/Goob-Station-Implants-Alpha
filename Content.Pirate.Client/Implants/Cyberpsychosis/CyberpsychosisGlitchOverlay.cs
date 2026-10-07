// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared._DV.CCVars;

using Content.Pirate.Shared.Implants.Cyberpsychosis;
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
///     Scanlines, tearing, pixel sorting, screen tearing.
/// </summary>
public sealed class CyberpsychosisGlitchOverlay : Overlay
{
    private ShaderInstance? _shader;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    public CyberpsychosisGlitchOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (IoCManager.Resolve<IConfigurationManager>().GetCVar(DCCVars.NoVisionFilters))
            return false;

        var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (player is not { Valid: true })
            return false;

        if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<CyberpsychosisComponent>(player, out var comp))
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

        var player = IoCManager.Resolve<IPlayerManager>().LocalEntity;
        if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<CyberpsychosisComponent>(player, out var comp))
            return;

        var intensity = CalculateIntensity(comp);
        if (intensity <= 0f)
            return;

        _shader ??= IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("CyberpsychosisGlitch").Instance().Duplicate();
        _shader.SetParameter("glitchPower", intensity);
        _shader.SetParameter("time", (float)IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds);
        _shader.SetParameter("sanityNormalized", intensity);

        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.DrawTextureRect(Texture.White, args.ViewportBounds);
        handle.UseShader(null);
    }

private float CalculateIntensity(CyberpsychosisComponent comp)
        {
            if (comp.CurrentState < SanityState.Cyberpsychosis)
                return 0f;

            var glitch = CyberpsychosisThresholds.Glitch;

            var baseIntensity = MathHelper.Clamp((glitch - comp.SanityValue) / (float) glitch, 0f, 1f);

            // Double the intensity for more psychedelic/crashable effects
            return MathHelper.Clamp(baseIntensity * 2f, 0f, 1f);
        }
}
