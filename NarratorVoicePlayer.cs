#nullable enable
using System;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Utils;

namespace ZaQiZaBa;

internal static class NarratorVoicePlayer
{
    private static AudioStreamPlayer? _player;
    private static I18N? _i18n;
    private static bool _isPlaying;
    private static double _cooldownEnd;
    private const double CooldownSeconds = 3.0;
    private const double MinSubtitleDuration = 2.5;
    private const double MaxSubtitleDuration = 8.0;
    private static Random? _rng;

    public static void Initialize()
    {
        _rng = new Random();
        _i18n = RitsuLibFramework.CreateModLocalization(
            modId: "ZaQiZaBa",
            instanceName: "narrator",
            pckFolders: ["res://localization"]);
    }

    public static bool TryPlay(NarratorEvent evt, double? overrideDuration = null, int chancePct = 100)
    {
        try
        {
            if (!ZaQiZaBaConfig.EnableNarrator) return false;

            if (chancePct < 100 && (_rng?.Next(100) ?? 0) >= chancePct)
                return false;

            EnsurePlayer();
            if (_player == null) return false;

            double now = Time.GetUnixTimeFromSystem();
            if (_isPlaying || now < _cooldownEnd) return false;

            var entry = NarratorVoiceMap.PickRandom(evt);
            if (string.IsNullOrEmpty(entry.FileName) || entry.FileName == "blank.wav") return false;

            string fullPath = NarratorVoiceMap.GetFullPath(entry.FileName);
            var stream = GD.Load<AudioStreamWav>(fullPath);
            if (stream == null) return false;

            EnsureInScene();

            UpdateVolume();
            _player.Stream = stream;
            _player.Play();
            _isPlaying = true;

            double duration = stream.GetLength();
            if (duration <= 0.5) duration = MinSubtitleDuration;
            else duration = Math.Min(duration + 1.0, MaxSubtitleDuration);
            if (overrideDuration.HasValue && overrideDuration.Value > duration)
                duration = overrideDuration.Value;

            string text = "";
            if (_i18n != null && !string.IsNullOrEmpty(entry.I18nKey))
                text = _i18n.Get(entry.I18nKey, "");
            if (string.IsNullOrEmpty(text))
                text = entry.I18nKey;

            NarratorSubtitleUI.Instance?.ShowSubtitle(text, duration);

            return true;
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"TryPlay[{evt}] FAILED: {ex.GetType().Name}: {ex.Message}");
            _isPlaying = false;
            return false;
        }
    }

    public static void Stop()
    {
        if (_player != null && _player.Playing)
        {
            try { _player.Stop(); } catch { }
            _isPlaying = false;
            var ui = NarratorSubtitleUI.Instance;
            ui?.HideImmediate();
        }
    }

    public static void UpdateVolume()
    {
        if (_player == null) return;
        if (GodotObject.IsInstanceValid(_player))
        {
            float sfxVol = 1f;
            try { sfxVol = SaveManager.Instance.SettingsSave.VolumeSfx; } catch { }
            float userVol = ZaQiZaBaConfig.NarratorVolume / 100f;
            _player.VolumeDb = Mathf.LinearToDb(sfxVol * userVol);
        }
    }

    private static void OnPlaybackFinished()
    {
        _isPlaying = false;
        double now = Time.GetUnixTimeFromSystem();
        _cooldownEnd = now + CooldownSeconds;
        NarratorSubtitleUI.Instance?.HideImmediate();
    }

    private static void EnsurePlayer()
    {
        if (_player != null && GodotObject.IsInstanceValid(_player)) return;

        if (_player != null)
        {
            try { _player.Finished -= OnPlaybackFinished; } catch { }
        }

        _player = new AudioStreamPlayer { Name = "NarratorVoicePlayer", Bus = "Master" };
        _player.Finished += OnPlaybackFinished;
    }

    private static void EnsureInScene()
    {
        if (_player == null) return;
        if (!GodotObject.IsInstanceValid(_player))
        {
            EnsurePlayer();
        }
        if (_player.GetParent() == null)
        {
            if (NGame.Instance != null)
                NGame.Instance.AddChild(_player);
        }
    }
}