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
///     Severe reality distortion: geometry warping, color inversion, time dilation.
/// </summary>
public sealed class CyberpsychosisRealityBreakOverlay : Overlay
{
    private ShaderInstance? _shader;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    public CyberpsychosisRealityBreakOverlay()
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

        // Only at sanity <= 0
        if (comp.SanityValue > 0)
            return false;

        var intensity = MathHelper.Clamp(-comp.SanityValue / 50f, 0f, 1f);
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

        if (comp.SanityValue > 0)
            return;

        var intensity = MathHelper.Clamp(-comp.SanityValue / 50f, 0f, 1f);
        if (intensity <= 0f)
            return;

        _shader ??= IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>("CyberpsychosisRealityBreak").Instance().Duplicate();
        _shader.SetParameter("realityBreakPower", intensity);
        _shader.SetParameter("time", (float)IoCManager.Resolve<IGameTiming>().CurTime.TotalSeconds);

        var handle = args.ScreenHandle;
        handle.UseShader(_shader);
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        handle.DrawTextureRect(Texture.White, args.ViewportBounds);
        handle.UseShader(null);
    }
}
