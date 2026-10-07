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
///     TEST VERSION: Spams all cyberpsychosis sounds every 2 seconds regardless of sanity.
/// </summary>
public sealed class CyberpsychosisAudioSystem : EntitySystem
{
    private const string Layer1Collection = "CyberpsychosisGlitchLayer1";
    private const string Layer2Collection = "CyberpsychosisGlitchLayer2";
    private const string HorrorCollection = "CyberpsychosisHorrorLayer";

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _lastPlay = TimeSpan.Zero;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);

    public override void Initialize()
    {
        base.Initialize();
        Logger.Info("[CyberpsychosisAudio] TEST MODE INITIALIZED - will spam sounds every 2s");

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
                foreach (var f in proto.PickFiles)
                    Logger.Info($"[CyberpsychosisAudio]   - {f}");
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _playerManager.LocalEntity;
        if (player is not { Valid: true })
            return;

        var now = _timing.CurTime;
        if (now - _lastPlay < _interval)
            return;

        // Force play a sound from each collection every 2 seconds
        PlayCollection(Layer1Collection);
        PlayCollection(Layer2Collection);
        PlayCollection(HorrorCollection);

        _lastPlay = now;
    }

    private void PlayCollection(string collectionId)
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
            AudioParams.Default.WithVolume(1.0f));

        Logger.Info($"[CyberpsychosisAudio] PLAYED {collectionId}: {pathString} (result={result.HasValue})");
    }
}