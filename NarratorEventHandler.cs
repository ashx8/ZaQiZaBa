#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib;

namespace ZaQiZaBa;

internal static class NarratorEventHandler
{
    private static int _killCountThisCombat;
    private static bool _runVictoryPlayed;
    private static bool _lowHpPlayed;
    private static bool _bossHalfHpPlayed;
    private static bool _currentIsBoss;
    private static bool _currentIsElite;
    private static bool _characterDebutPlayed;
    private static readonly HashSet<Creature> _deadEnemies = new();
    private static readonly Dictionary<Creature, int> _preHp = new();
    private static readonly Dictionary<Creature, int> _enemyDamageThisTurn = new();
    private static readonly Dictionary<Creature, int> _enemyMaxHpSnapshot = new();
    private static readonly HashSet<int> _powerProcessedThisTurn = new();
    private static int _playerHpBeforeEnemyTurn;
    private static int _playerBlockBeforeEnemyTurn;
    private static double _healCooldownEnd;
    private static double _powerCooldownEnd;
    private static double _blockFullCooldownEnd;
    private const double HealCooldown = 8.0;
    private const double PowerCooldown = 5.0;
    private const double BlockFullCooldown = 8.0;
    private static Random? _rng;

    public static void Initialize()
    {
        _rng = new Random();

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt => SafeRun("CombatStarting", () => OnCombatStarting(evt)));
        RitsuLibFramework.SubscribeLifecycle<CombatVictoryEvent>(evt => SafeRun("CombatVictory", () => OnCombatVictory(evt)));
        RitsuLibFramework.SubscribeLifecycle<CombatEndedEvent>(evt => SafeRun("CombatEnded", () => OnCombatEnded(evt)));
        RitsuLibFramework.SubscribeLifecycle<CardPlayingEvent>(evt => SafeRun("CardPlaying", () => OnCardPlaying(evt)));
        RitsuLibFramework.SubscribeLifecycle<CardPlayedEvent>(evt => SafeRun("CardPlayed", () => OnCardPlayed(evt)));
        RitsuLibFramework.SubscribeLifecycle<CreatureDiedEvent>(evt => SafeRun("CreatureDied", () => OnCreatureDied(evt)));
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt => SafeRun("SideTurnStarted", () => OnSideTurnStarted(evt)));
        RitsuLibFramework.SubscribeLifecycle<BeforeFlushEvent>(evt => SafeRun("BeforeFlush", () => OnBeforeFlush(evt)));
        RitsuLibFramework.SubscribeLifecycle<RoomEnteredEvent>(evt => SafeRun("RoomEntered", () => OnRoomEntered(evt)));
        RitsuLibFramework.SubscribeLifecycle<RewardTakenEvent>(evt => SafeRun("RewardTaken", () => OnRewardTaken(evt)));
        RitsuLibFramework.SubscribeLifecycle<CreatureDyingEvent>(evt => SafeRun("CreatureDying", () => OnCreatureDying(evt)));
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(evt => SafeRun("RunStarted", () => OnRunStarted(evt)));
        NarratorVoicePlayer.Initialize();
    }

    private static void SafeRun(string name, Action action)
    {
        try { action(); }
        catch (Exception ex) { Entry.Logger.Error($"NarratorEventHandler.{name}: {ex.GetType().Name}: {ex.Message}"); }
    }

    private static void OnRunStarted(RunStartedEvent evt)
    {
        _runVictoryPlayed = false;
        if (!_characterDebutPlayed)
        {
            _characterDebutPlayed = true;
            NarratorVoicePlayer.TryPlay(NarratorEvent.RunStart);
        }
    }

    public static void OnPlayerTookDamage(Creature player, int dmg)
    {
        try
        {
            if (player.MaxHp <= 0 || dmg <= 0) return;
            float pct = (float)dmg / player.MaxHp;

            if (pct >= 0.30f)
            {
                NarratorVoicePlayer.TryPlay(NarratorEvent.PlayerHurt, chancePct: 100);
            }
            else if (pct >= 0.15f)
            {
                NarratorVoicePlayer.TryPlay(NarratorEvent.PlayerHurt, chancePct: ZaQiZaBaConfig.NarratorPlayerHurtChance);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"NarratorEventHandler.OnPlayerTookDamage: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void OnPlayerHealed(Creature player, int amount)
    {
        try
        {
            if (player.MaxHp <= 0 || amount <= 0) return;
            double now = Time.GetUnixTimeFromSystem();
            if (now < _healCooldownEnd) return;

            float pct = (float)amount / player.MaxHp;
            if (pct >= 0.10f)
            {
                _healCooldownEnd = now + HealCooldown;
                NarratorVoicePlayer.TryPlay(NarratorEvent.CampRest, chancePct: ZaQiZaBaConfig.NarratorHealChance);
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"NarratorEventHandler.OnPlayerHealed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void OnPowerApplied(Creature creature, PowerModel power)
    {
        try
        {
            double now = Time.GetUnixTimeFromSystem();
            if (now < _powerCooldownEnd) return;

            int hash = HashCode.Combine(creature.GetHashCode(), power.GetType().GetHashCode());
            if (_powerProcessedThisTurn.Contains(hash)) return;
            _powerProcessedThisTurn.Add(hash);

            if (power.Type == PowerType.Buff)
            {
                if (creature.IsPlayer)
                {
                    _powerCooldownEnd = now + PowerCooldown;
                    NarratorVoicePlayer.TryPlay(NarratorEvent.BuffGained, chancePct: ZaQiZaBaConfig.NarratorBuffGainedChance);
                }
                else
                {
                    // 敌人获得增益
                    _powerCooldownEnd = now + PowerCooldown;
                    NarratorVoicePlayer.TryPlay(NarratorEvent.EnemyBuffGained, chancePct: ZaQiZaBaConfig.NarratorEnemyBuffChance);
                }
            }
            else if (power.Type == PowerType.Debuff)
            {
                if (creature.IsPlayer)
                {
                    _powerCooldownEnd = now + PowerCooldown;
                    NarratorVoicePlayer.TryPlay(NarratorEvent.DebuffGained, chancePct: ZaQiZaBaConfig.NarratorDebuffGainedChance);
                }
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"NarratorEventHandler.OnPowerApplied: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void OnCombatStarting(CombatStartingEvent evt)
    {
        ResetCombatState();
    }

    private static void OnCombatVictory(CombatVictoryEvent evt)
    {
        if (!_runVictoryPlayed)
        {
            _runVictoryPlayed = true;
            NarratorVoicePlayer.TryPlay(NarratorEvent.CombatVictoryFirst);
        }
        else
        {
            NarratorVoicePlayer.TryPlay(NarratorEvent.CombatVictory);
        }
    }

    private static void OnCombatEnded(CombatEndedEvent evt)
    {
        ResetCombatState();
    }

    private static void OnCardPlaying(CardPlayingEvent evt)
    {
        _preHp.Clear();
        if (evt.CombatState?.Enemies == null) return;
        foreach (var enemy in evt.CombatState.Enemies)
        {
            _preHp[enemy] = enemy.CurrentHp;
        }
    }

    private static void OnCardPlayed(CardPlayedEvent evt)
    {
        var card = evt.CardPlay?.Card;
        if (card == null) return;

        bool isAttack = card.Type == CardType.Attack;
        if (isAttack)
        {
            AccumulateEnemyDamage(evt);
            NarratorVoicePlayer.TryPlay(NarratorEvent.CardPlayed, chancePct: ZaQiZaBaConfig.NarratorCardPlayChance);
        }

        CheckKillFromCardPlay(evt);
    }

    private static void AccumulateEnemyDamage(CardPlayedEvent evt)
    {
        if (evt.CombatState?.Enemies == null) return;
        foreach (var enemy in evt.CombatState.Enemies)
        {
            if (enemy.IsPet) continue;
            if (!_preHp.TryGetValue(enemy, out int preHp)) continue;

            int dmg = preHp - enemy.CurrentHp;
            if (dmg <= 0) continue;

            if (_enemyDamageThisTurn.ContainsKey(enemy))
                _enemyDamageThisTurn[enemy] += dmg;
            else
                _enemyDamageThisTurn[enemy] = dmg;

            if (!_enemyMaxHpSnapshot.ContainsKey(enemy))
                _enemyMaxHpSnapshot[enemy] = enemy.MaxHp;
        }
    }

    private static void CheckKillFromCardPlay(CardPlayedEvent evt)
    {
        if (evt.CombatState?.Enemies == null) return;
        foreach (var enemy in evt.CombatState.Enemies)
        {
            if (enemy.IsPet) continue;
            if (_preHp.TryGetValue(enemy, out int preHp) && preHp > 0 && enemy.CurrentHp <= 0)
            {
                HandleEnemyKilled(enemy);
            }
        }
    }

    private static void OnCreatureDying(CreatureDyingEvent evt)
    {
        if (evt.Creature == null || evt.Creature.IsPlayer || evt.Creature.IsPet) return;
        HandleEnemyKilled(evt.Creature);
    }

    private static void OnCreatureDied(CreatureDiedEvent evt)
    {
        if (evt.Creature?.IsPlayer == true && !evt.WasRemovalPrevented)
        {
            NarratorVoicePlayer.TryPlay(NarratorEvent.PlayerDeath);
        }
    }

    private static void HandleEnemyKilled(Creature creature)
    {
        if (_deadEnemies.Contains(creature)) return;
        _deadEnemies.Add(creature);
        _killCountThisCombat++;

        if (_currentIsBoss)
        {
            NarratorVoicePlayer.TryPlay(NarratorEvent.BossKilled, chancePct: 100);
            return;
        }

        if (_killCountThisCombat == 1)
        {
            NarratorVoicePlayer.TryPlay(NarratorEvent.KillFirst, chancePct: ZaQiZaBaConfig.NarratorKillEnemyFirstChance);
            return;
        }

        int roll = _rng?.Next(100) ?? 0;
        if (roll < 15)
            NarratorVoicePlayer.TryPlay(NarratorEvent.KillLarge, chancePct: ZaQiZaBaConfig.NarratorKillEnemyBigChance);
        else if (roll < 35)
            NarratorVoicePlayer.TryPlay(NarratorEvent.KillStrong, chancePct: ZaQiZaBaConfig.NarratorKillEnemyStrongChance);
        else if (roll < 55)
            NarratorVoicePlayer.TryPlay(NarratorEvent.KillDot, chancePct: ZaQiZaBaConfig.NarratorKillEnemyDotChance);
        else
            NarratorVoicePlayer.TryPlay(NarratorEvent.KillNormal, chancePct: ZaQiZaBaConfig.NarratorKillEnemyChance);
    }

    private static void OnSideTurnStarted(SideTurnStartedEvent evt)
    {
        if (evt.Side == CombatSide.Player)
        {
            _enemyDamageThisTurn.Clear();
            _enemyMaxHpSnapshot.Clear();
            _powerProcessedThisTurn.Clear();
            CheckBlockFullDamage(evt.CombatState);
            CheckLowHp(evt.CombatState);
        }
        else
        {
            CheckCumulativeDamage();
            _enemyDamageThisTurn.Clear();
            _enemyMaxHpSnapshot.Clear();

            if (evt.CombatState != null)
            {
                foreach (var creature in evt.CombatState.PlayerCreatures)
                {
                    if (creature.IsPet) continue;
                    _playerHpBeforeEnemyTurn = creature.CurrentHp;
                    _playerBlockBeforeEnemyTurn = creature.Block;
                    break;
                }
                CheckBossHalfHp(evt.CombatState);
            }
        }
    }

    private static void CheckCumulativeDamage()
    {
        foreach (var kvp in _enemyDamageThisTurn)
        {
            var enemy = kvp.Key;
            int totalDmg = kvp.Value;
            int maxHp = _enemyMaxHpSnapshot.TryGetValue(enemy, out int mh) ? mh : enemy.MaxHp;
            if (maxHp <= 0) continue;

            float pct = (float)totalDmg / maxHp;
            if (pct >= 0.30f)
            {
                NarratorVoicePlayer.TryPlay(NarratorEvent.CritHit, chancePct: 100);
                return;
            }
        }
    }

    private static void CheckBlockFullDamage(CombatState state)
    {
        try
        {
            double now = Time.GetUnixTimeFromSystem();
            if (now < _blockFullCooldownEnd) return;

            if (state.PlayerCreatures == null) return;
            foreach (var player in state.PlayerCreatures)
            {
                if (player.IsPet || player.IsDead) continue;
                int blockLost = _playerBlockBeforeEnemyTurn - player.Block;
                int hpLost = _playerHpBeforeEnemyTurn - player.CurrentHp;

                if (blockLost > 0 && hpLost <= 0)
                {
                    _blockFullCooldownEnd = now + BlockFullCooldown;
                    NarratorVoicePlayer.TryPlay(NarratorEvent.BlockAllDamage, chancePct: 100);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"NarratorEventHandler.CheckBlockFullDamage: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void CheckBossHalfHp(CombatState state)
    {
        if (!_currentIsBoss || _bossHalfHpPlayed) return;
        if (state.Enemies == null) return;

        foreach (var enemy in state.Enemies)
        {
            if (enemy.IsPet || enemy.IsDead) continue;
            float pct = enemy.MaxHp > 0 ? (float)enemy.CurrentHp / enemy.MaxHp : 1f;
            if (pct <= 0.50f && pct > 0f)
            {
                _bossHalfHpPlayed = true;
                NarratorVoicePlayer.TryPlay(NarratorEvent.BossHalfHp, chancePct: 100);
                return;
            }
        }
    }

    private static void CheckLowHp(CombatState state)
    {
        if (_lowHpPlayed) return;
        if (state == null || state.PlayerCreatures == null) return;
        foreach (var player in state.PlayerCreatures)
        {
            if (player.IsPet || player.IsDead) continue;
            float pct = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;
            if (pct <= 0.25f)
            {
                _lowHpPlayed = true;
                NarratorVoicePlayer.TryPlay(NarratorEvent.LowHp);
                return;
            }
        }
    }

    private static void OnBeforeFlush(BeforeFlushEvent evt)
    {
        _lowHpPlayed = false;
        _bossHalfHpPlayed = false;
        _deadEnemies.Clear();
        _preHp.Clear();
        _enemyDamageThisTurn.Clear();
        _enemyMaxHpSnapshot.Clear();
        _powerProcessedThisTurn.Clear();
    }

    private static void OnRoomEntered(RoomEnteredEvent evt)
    {
        try
        {
            var room = evt.Room;
            if (room == null) return;

            if (room is CombatRoom combatRoom)
            {
                _currentIsBoss = combatRoom.RoomType == RoomType.Boss;
                _currentIsElite = combatRoom.RoomType == RoomType.Elite;

                if (_currentIsBoss)
                {
                    _bossHalfHpPlayed = false;
                    NarratorVoicePlayer.TryPlay(NarratorEvent.BossEncounter, chancePct: 100);
                }
                else if (_currentIsElite)
                {
                    NarratorVoicePlayer.TryPlay(NarratorEvent.EliteEncounter);
                }
            }
            else
            {
                string roomType = room.GetType().Name.ToUpperInvariant();

                if (roomType.Contains("TREASURE") || roomType.Contains("CHEST"))
                {
                    NarratorVoicePlayer.TryPlay(NarratorEvent.RoomEnterTreasure);
                }
                else if (roomType.Contains("SHOP") || roomType.Contains("STORE") || roomType.Contains("MERCHANT"))
                {
                    NarratorVoicePlayer.TryPlay(NarratorEvent.RoomEnterShop);
                }
                else if (roomType.Contains("REST") || roomType.Contains("CAMP"))
                {
                    NarratorVoicePlayer.TryPlay(NarratorEvent.CampRest);
                }
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Error($"NarratorEventHandler.OnRoomEntered: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void OnRewardTaken(RewardTakenEvent evt)
    {
        NarratorVoicePlayer.TryPlay(NarratorEvent.LootObtained);
    }

    private static void ResetCombatState()
    {
        _killCountThisCombat = 0;
        _lowHpPlayed = false;
        _bossHalfHpPlayed = false;
        _deadEnemies.Clear();
        _preHp.Clear();
        _enemyDamageThisTurn.Clear();
        _enemyMaxHpSnapshot.Clear();
        _powerProcessedThisTurn.Clear();
    }
}
