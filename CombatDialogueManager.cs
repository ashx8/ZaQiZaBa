#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using STS2RitsuLib;
using STS2RitsuLib.Utils;

namespace ZaQiZaBa;

internal static class CombatDialogueManager
{
    private static I18N? _i18n;
    private static int _pickCount;
    private static readonly Dictionary<Creature, int> _prePlayHp = new();
    private static readonly Dictionary<Creature, int> _prePlayBlock = new();

    private static bool _isEnemyTurn;

    private static bool _hitLightTriggered;
    private static bool _hit20Triggered;
    private static bool _hit40Triggered;
    private static bool _atk20Triggered;
    private static bool _atk40Triggered;
    private static bool _killTriggered;
    private static bool _blockedAtkTriggered;
    private static bool _blockedHitTriggered;
    private static bool _loaded;
    private static bool _pendingBlockedCheck;
    private static Creature? _pendingBlockedPlayer;
    private static bool _enemiesHaveAttacksThisTurn;

    private static bool _isBubbleShowing;

    public static void Initialize()
    {
        try
        {
            _i18n = RitsuLibFramework.CreateModLocalization(
                modId: "ZaQiZaBa",
                instanceName: "dialogue",
                pckFolders: ["res://localization"]);

            RitsuLibFramework.SubscribeLifecycle<CardPlayingEvent>(OnCardPlaying);
            RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(OnCardPlayed);
            RitsuLibFramework.SubscribeLifecycle<BeforeFlushEvent>(OnBeforeFlush);
            RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(OnSideTurnStarted);
            _loaded = true;
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"CombatDialogueManager.Initialize: {ex.Message}");
        }
    }

    public static void OnPlayerBlockDecreased(Creature player)
    {
        if (_isEnemyTurn && !_blockedHitTriggered && _enemiesHaveAttacksThisTurn)
        {
            _pendingBlockedCheck = true;
            _pendingBlockedPlayer = player;
        }
    }

    public static void OnPlayerTookDamage(Creature player, int damage)
    {
        _pendingBlockedCheck = false;

        if (!_loaded || !ZaQiZaBaConfig.ShowCombatDialogue) return;
        if (player.IsDead) return;

        float pct = player.MaxHp > 0 ? (float)damage / player.MaxHp * 100f : 0f;
        if (pct <= 0f) return;

        if (pct >= 40f && !_hit40Triggered)
        {
            _hit40Triggered = true;
            ShowBubble(player, PickLine("HIT_40"));
        }
        else if (pct >= 20f && !_hit20Triggered && !_hit40Triggered)
        {
            _hit20Triggered = true;
            if (Roll("HIT_20", ZaQiZaBaConfig.Hit20Chance))
                ShowBubble(player, PickLine("HIT_20"));
        }
        else if (!_hitLightTriggered && !_hit20Triggered && !_hit40Triggered)
        {
            _hitLightTriggered = true;
            if (Roll("HIT_LIGHT", ZaQiZaBaConfig.HitLightChance))
                ShowBubble(player, PickLine("HIT_LIGHT"));
        }
    }

    private static void OnCardPlaying(CardPlayingEvent evt)
    {
        if (!_loaded) return;
        _prePlayHp.Clear();
        _prePlayBlock.Clear();
        var enemies = evt.CombatState.Enemies;
        if (enemies != null)
        {
            foreach (var e in enemies)
            {
                _prePlayHp[e] = e.CurrentHp;
                _prePlayBlock[e] = e.Block;
            }
        }
    }

    private static void OnCardPlayed(CardPlayedEvent evt)
    {
        if (!_loaded || !ZaQiZaBaConfig.ShowCombatDialogue) return;
        var player = evt.CardPlay?.Card?.Owner?.Creature;
        if (player == null || player.IsPet) return;

        float cardMaxPct = 0f;
        foreach (var kv in _prePlayHp)
        {
            if (kv.Key.IsPet) continue;
            if (kv.Value <= 0) continue;
            int dmg = Math.Max(0, kv.Value - kv.Key.CurrentHp);
            if (dmg <= 0) continue;
            float maxHp = kv.Key.MaxHp > 0 ? kv.Key.MaxHp : 1f;
            float pct = (float)dmg / maxHp * 100f;
            cardMaxPct = Math.Max(cardMaxPct, pct);

            if (!_killTriggered && kv.Value > 0 && kv.Key.CurrentHp <= 0)
            {
                _killTriggered = true;
                if (Roll("KILL", ZaQiZaBaConfig.KillChance))
                    ShowBubble(player, PickLine("KILL"));
            }
        }

        if (cardMaxPct <= 0f)
        {
            if (!_blockedAtkTriggered)
            {
                foreach (var kv in _prePlayHp)
                {
                    if (kv.Key.IsPet) continue;
                    if (!_prePlayBlock.TryGetValue(kv.Key, out int preBlock)) continue;
                    if (preBlock <= 0) continue;
                    if (kv.Value == kv.Key.CurrentHp && preBlock > kv.Key.Block)
                    {
                        _blockedAtkTriggered = true;
                        if (Roll("BLOCKED_ATK", ZaQiZaBaConfig.BlockedAtkChance))
                            ShowBubble(player, PickLine("BLOCKED_ATK"));
                        break;
                    }
                }
            }
            return;
        }

        if (cardMaxPct >= 40f && !_atk40Triggered)
        {
            _atk40Triggered = true;
            ShowBubble(player, PickLine("ATK_40"));
        }
        else if (cardMaxPct >= 20f && !_atk20Triggered && !_atk40Triggered)
        {
            _atk20Triggered = true;
            if (Roll("ATK_20", ZaQiZaBaConfig.Atk20Chance))
                ShowBubble(player, PickLine("ATK_20"));
        }
    }

    private static void OnSideTurnStarted(SideTurnStartedEvent evt)
    {
        if (evt.Side == CombatSide.Enemy)
        {
            _isEnemyTurn = true;
            _pendingBlockedCheck = false;
            _pendingBlockedPlayer = null;

            _enemiesHaveAttacksThisTurn = false;
            try
            {
                var state = CombatManager.Instance?.DebugOnlyGetState();
                if (state?.Enemies != null)
                {
                    foreach (var enemy in state.Enemies)
                    {
                        var intents = enemy.Monster?.NextMove?.Intents;
                        if (intents == null) continue;
                        foreach (var intent in intents)
                        {
                            if (intent is AttackIntent)
                            {
                                _enemiesHaveAttacksThisTurn = true;
                                break;
                            }
                        }
                        if (_enemiesHaveAttacksThisTurn) break;
                    }
                }
            }
            catch (Exception ex) { Entry.Logger.Error($"OnSideTurnStarted(Enemy): {ex.Message}"); }
        }
        else if (evt.Side == CombatSide.Player)
        {
            _isEnemyTurn = false;
        }
    }

    public static void CheckPendingBlockedHit()
    {
        if (!_pendingBlockedCheck) return;
        _pendingBlockedCheck = false;

        if (_blockedHitTriggered || !_loaded || !ZaQiZaBaConfig.ShowCombatDialogue) return;
        if (_pendingBlockedPlayer == null) return;

        _blockedHitTriggered = true;
        if (Roll("BLOCKED_HIT", ZaQiZaBaConfig.BlockedHitChance))
            ShowBubble(_pendingBlockedPlayer, PickLine("BLOCKED_HIT"));
    }

    private static void OnBeforeFlush(BeforeFlushEvent evt)
    {
        _hitLightTriggered = false;
        _hit20Triggered = false;
        _hit40Triggered = false;
        _atk20Triggered = false;
        _atk40Triggered = false;
        _killTriggered = false;
        _blockedAtkTriggered = false;
        _blockedHitTriggered = false;
        _pendingBlockedCheck = false;
        _pendingBlockedPlayer = null;
    }

    private static string PickLine(string baseKey)
    {
        if (_i18n == null) return "";
        var lines = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            string text = _i18n.Get($"{baseKey}_{i}", "");
            if (string.IsNullOrEmpty(text)) break;
            lines.Add(text);
        }
        if (lines.Count == 0) return "";
        int hash = 17;
        foreach (char c in baseKey)
            hash = hash * 31 + c;
        int idx = (Math.Abs(hash) + _pickCount++) % lines.Count;
        return lines[idx];
    }

    private static bool Roll(string key, int chancePercent)
    {
        int hash = 17;
        foreach (char c in key)
            hash = hash * 31 + c;
        return Math.Abs(hash + _pickCount++) % 100 < chancePercent;
    }

    public static void ResetTriggerFlags()
    {
        _hitLightTriggered = false;
        _hit20Triggered = false;
        _hit40Triggered = false;
        _atk20Triggered = false;
        _atk40Triggered = false;
        _killTriggered = false;
        _blockedAtkTriggered = false;
        _blockedHitTriggered = false;
        _pendingBlockedCheck = false;
        _pendingBlockedPlayer = null;
        _isBubbleShowing = false;
    }

    private static void ShowBubble(Creature speaker, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (_isBubbleShowing) return;
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) return;
            var bubble = NSpeechBubbleVfx.Create(text, speaker, 3.5, VfxColor.Orange);
            if (bubble == null) return;
            room.AddChild(bubble);
            _isBubbleShowing = true;
            var timer = new Timer
            {
                OneShot = true,
                WaitTime = 3.5,
                Autostart = false
            };
            room.AddChild(timer);
            timer.Timeout += () =>
            {
                _isBubbleShowing = false;
                if (GodotObject.IsInstanceValid(timer)) timer.QueueFree();
            };
            timer.Start();

            int bangCount = text.Count(c => c == '!');
            if (bangCount > 0)
            {
                var strength = bangCount switch
                {
                    1 => ShakeStrength.Weak,
                    2 => ShakeStrength.Medium,
                    _ => ShakeStrength.Strong,
                };
                NGame.Instance?.ScreenShake(strength, ShakeDuration.Short);
            }
        }
        catch (Exception ex) { Entry.Logger.Error($"ShowBubble: {ex.Message}"); }
    }

}
