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

namespace Content.Pirate.Client.Implants.Cyberpsychosis;

/// <summary>
///     Plays glitchy/horror sounds based on the player's sanity level.
///     Lower sanity = more frequent and more intense sounds.
/// </summary>
public sealed class CyberpsychosisAudioSystem : EntitySystem
{
    // Sound collections for different intensity layers
    private const string Layer1Collection = "CyberpsychosisGlitchLayer1";
    private const string Layer2Collection = "CyberpsychosisGlitchLayer2";
    private const string HorrorCollection = "CyberpsychosisHorrorLayer";

    // Sanity thresholds for sound layers (matching visual thresholds)
    private const int RealityBreakThreshold = 60;
    private const int HallucinationThreshold = 45;
    private const int GlitchThreshold = 20;

    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // Track last played times per layer
    private TimeSpan _lastLayer1Play = TimeSpan.Zero;
    private TimeSpan _lastLayer2Play = TimeSpan.Zero;
    private TimeSpan _lastHorrorPlay = TimeSpan.Zero;

    // Minimum intervals between plays (at max sanity for that layer)
    private readonly TimeSpan _baseLayer1Interval = TimeSpan.FromSeconds(12);
    private readonly TimeSpan _baseLayer2Interval = TimeSpan.FromSeconds(15);
    private readonly TimeSpan _baseHorrorInterval = TimeSpan.FromSeconds(18);

    public override void Initialize()
    {
        base.Initialize();
        // Validate sound collections exist
        foreach (var id in new[] { Layer1Collection, Layer2Collection, HorrorCollection })
        {
            if (!_prototypes.HasIndex<SoundCollectionPrototype>(id))
                Logger.Error($"[CyberpsychosisAudio] Missing sound collection: {id}");
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

        // Layer 1: Ambient glitch sounds (starts at Reality Break, sanity <= 60)
        if (sanity <= RealityBreakThreshold)
        {
            var interval = CalculateInterval(_baseLayer1Interval, sanity, RealityBreakThreshold, 6f);
            if (now - _lastLayer1Play >= interval)
            {
                PlayRandomFromCollection(Layer1Collection, CalculateVolume(sanity, RealityBreakThreshold, 0.6f, 1.0f));
                _lastLayer1Play = now;
            }
        }

        // Layer 2: More intense glitch sounds (starts at Hallucination, sanity <= 45)
        if (sanity <= HallucinationThreshold)
        {
            var interval = CalculateInterval(_baseLayer2Interval, sanity, HallucinationThreshold, 5f);
            if (now - _lastLayer2Play >= interval)
            {
                PlayRandomFromCollection(Layer2Collection, CalculateVolume(sanity, HallucinationThreshold, 0.7f, 1.0f));
                _lastLayer2Play = now;
            }
        }

        // Horror layer: Scary/disturbing sounds (starts at Glitch threshold, sanity <= 20)
        if (sanity <= GlitchThreshold)
        {
            var interval = CalculateInterval(_baseHorrorInterval, sanity, GlitchThreshold, 4f);
            if (now - _lastHorrorPlay >= interval)
            {
                PlayRandomFromCollection(HorrorCollection, CalculateVolume(sanity, GlitchThreshold, 0.8f, 1.0f));
                _lastHorrorPlay = now;
            }
        }
    }

    /// <summary>
    ///     Calculates dynamic interval based on sanity - lower sanity = shorter interval.
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

    /// <summary>
    ///     Calculates volume based on sanity - lower sanity = louder.
    /// </summary>
    private float CalculateVolume(int sanity, int threshold, float minVolume, float maxVolume)
    {
        if (sanity >= threshold)
            return minVolume;

        var progress = 1f - (float)sanity / threshold;
        return minVolume + (progress * (maxVolume - minVolume));
    }

    private void PlayRandomFromCollection(string collectionId, float volume)
    {
        if (!_prototypes.TryIndex<SoundCollectionPrototype>(collectionId, out var collection))
        {
            Logger.Error($"[CyberpsychosisAudio] Missing sound collection: {collectionId}");
            return;
        }

        var sounds = collection.PickFiles;
        if (sounds.Count == 0)
        {
            Logger.Error($"[CyberpsychosisAudio] Sound collection empty: {collectionId}");
            return;
        }

        var soundPath = _random.Pick(sounds);
        var pathString = soundPath.ToString();

        var audioParams = AudioParams.Default.WithVolume(volume);
        var result = _audio.PlayGlobal(
            pathString,
            Filter.Local(),
            false,
            audioParams);

        Logger.Info($"[CyberpsychosisAudio] Played {collectionId}: {pathString} (vol={volume}, result={result.HasValue})");
    }
}