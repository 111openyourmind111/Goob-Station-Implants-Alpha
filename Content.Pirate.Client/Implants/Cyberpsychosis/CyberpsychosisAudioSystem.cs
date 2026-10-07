// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Pirate.Shared.Implants.Cyberpsychosis;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

/// <summary>
///     Cyberpsychosis ambient sounds based on sanity level.
///     Lower sanity = more frequent and more intense sounds.
/// </summary>
public sealed class CyberpsychosisAudioSystem : EntitySystem
{
    private const string Layer1Collection = "CyberpsychosisGlitchLayer1";
    private const string Layer2Collection = "CyberpsychosisGlitchLayer2";
    private const string HorrorCollection = "CyberpsychosisHorrorLayer";

    // Sanity thresholds and intervals
    private const int RealityBreakThreshold = 60;
    private const int HallucinationThreshold = 40;
    private const int GlitchThreshold = 20;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Track last played times per layer
    private TimeSpan _lastLayer1Play = TimeSpan.Zero;
    private TimeSpan _lastLayer2Play = TimeSpan.Zero;
    private TimeSpan _lastHorrorPlay = TimeSpan.Zero;

    // Base intervals (at threshold)
    private readonly TimeSpan _baseLayer1Interval = TimeSpan.FromSeconds(20); // at sanity 60
    private readonly TimeSpan _baseLayer2Interval = TimeSpan.FromSeconds(15); // at sanity 40
    private readonly TimeSpan _baseHorrorInterval = TimeSpan.FromSeconds(12);  // at sanity 20

    public override void Initialize()
    {
        base.Initialize();
        Logger.Info("[CyberpsychosisAudio] Initialized - sanity-based ambient sounds");

        foreach (var id in new[] { Layer1Collection, Layer2Collection, HorrorCollection })
        {
            if (!_prototypes.HasIndex<SoundCollectionPrototype>(id))
            {
                Logger.Error($"[CyberpsychosisAudio] MISSING sound collection: {id}");
            }
            else
            {
                var proto = _prototypes.Index<SoundCollectionPrototype>(id);
                Logger.Info($"[CyberpsychosisAudio] Loaded collection {id}: {proto.PickFiles.Count} sounds");
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _playerManager.LocalEntity;
        if (player is not { Valid: true })
            return;

        if (!_entities.TryGetComponent<CyberpsychosisComponent>(player, out var comp))
            return;

        var sanity = comp.SanityValue;
        var now = _timing.CurTime;

        // Layer 1: Glitch sounds (sanity ≤ 60, every 20s at threshold, faster as sanity drops)
        if (sanity <= RealityBreakThreshold)
        {
            var interval = CalculateInterval(_baseLayer1Interval, sanity, RealityBreakThreshold, 3f);
            if (now - _lastLayer1Play >= interval)
            {
                PlayRandomFromCollection(Layer1Collection, 1.0f);
                _lastLayer1Play = now;
            }
        }

        // Layer 2: More intense glitch sounds (sanity ≤ 40, every 15s at threshold)
        if (sanity <= HallucinationThreshold)
        {
            var interval = CalculateInterval(_baseLayer2Interval, sanity, HallucinationThreshold, 3f);
            if (now - _lastLayer2Play >= interval)
            {
                PlayRandomFromCollection(Layer2Collection, 1.0f);
                _lastLayer2Play = now;
            }
        }

        // Horror layer: Scary sounds (sanity ≤ 20, every 12s at threshold)
        if (sanity <= GlitchThreshold)
        {
            var interval = CalculateInterval(_baseHorrorInterval, sanity, GlitchThreshold, 3f);
            if (now - _lastHorrorPlay >= interval)
            {
                PlayRandomFromCollection(HorrorCollection, 1.0f);
                _lastHorrorPlay = now;
            }
        }
    }

    /// <summary>
    ///     Calculates dynamic interval based on sanity - lower sanity = shorter interval.
    ///     At threshold = baseInterval, at 0 = baseInterval / scaleFactor
    /// </summary>
    private TimeSpan CalculateInterval(TimeSpan baseInterval, int sanity, int threshold, float scaleFactor)
    {
        if (sanity >= threshold)
            return baseInterval;

        var progress = 1f - (float)sanity / threshold;
        var multiplier = 1f - (progress * (1f - 1f / scaleFactor));
        var seconds = baseInterval.TotalSeconds * multiplier;
        return TimeSpan.FromSeconds(Math.Max(seconds, baseInterval.TotalSeconds / scaleFactor));
    }

    private void PlayRandomFromCollection(string collectionId, float volume)
    {
        if (!_prototypes.TryIndex<SoundCollectionPrototype>(collectionId, out var collection))
        {
            Logger.Error($"[CyberpsychosisAudio] MISSING sound collection: {collectionId}");
            return;
        }

        var sounds = collection.PickFiles;
        if (sounds.Count == 0)
        {
            Logger.Error($"[CyberpsychosisAudio] EMPTY sound collection: {collectionId}");
            return;
        }

        var soundPath = _random.Pick(sounds);
        var pathString = soundPath.ToString();

        var result = _audio.PlayGlobal(
            pathString,
            Filter.Local(),
            false,
            AudioParams.Default.WithVolume(volume));

        Logger.Info($"[CyberpsychosisAudio] PLAYED {collectionId}: {pathString} (result={result.HasValue})");
    }
}