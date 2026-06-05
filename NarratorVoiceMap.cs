#nullable enable
using System;
using System.Collections.Generic;

namespace ZaQiZaBa;

internal enum NarratorEvent
{
    // 开局
    RunStart,

    // 战斗
    CardPlayed,
    CritHit,
    PlayerHurt,
    BlockAllDamage,
    LowHp,

    // 击杀
    KillFirst,
    KillNormal,
    KillLarge,
    KillStrong,
    KillDot,

    // 胜利与死亡
    CombatVictory,
    CombatVictoryFirst,
    PlayerDeath,

    // Boss
    BossEncounter,
    BossHalfHp,
    BossKilled,

    // 精英
    EliteEncounter,

    // 篝火
    CampRest,

    // 房间进入
    RoomEnterTreasure,
    RoomEnterShop,

    // 战利品
    LootObtained,

    // Buff / Debuff
    BuffGained,
    DebuffGained,

    // 敌人增强
    EnemyBuffGained,
}

internal sealed class VoiceEntry
{
    public string FileName { get; }
    public string I18nKey { get; }

    public VoiceEntry(string fileName, string i18nKey)
    {
        FileName = fileName;
        I18nKey = i18nKey;
    }
}

internal static class NarratorVoiceMap
{
    private const string BasePath = "res://voice/";

    public static readonly Dictionary<NarratorEvent, List<VoiceEntry>> Map = new();

    static NarratorVoiceMap()
    {
        // ========== 开局 ==========

        // 新建一局游戏
        Map[NarratorEvent.RunStart] = new List<VoiceEntry>
        {
            new("vo_narr_neut_queststart_01.wav",   "STORY_RUN_START_1"),
            new("vo_narr_neut_darkest01_load.wav",  "STORY_RUN_START_2"),
        };

        // ========== 战斗 ==========

        // 打出攻击牌
        Map[NarratorEvent.CardPlayed] = new List<VoiceEntry>
        {
            new("vo_narr_neut_warrens_kill.wav", "STORY_CARD_PLAY_1"),
            new("vo_narr_neut_weald_kill.wav",   "STORY_CARD_PLAY_2"),
        };

        // 单回合对单敌累积伤害 >= 30% 最大生命
        Map[NarratorEvent.CritHit] = new List<VoiceEntry>
        {
            new("vo_narr_good_crit_01.wav",     "STORY_CRIT_1"),
            new("vo_narr_good_crit_02.wav",     "STORY_CRIT_2"),
            new("vo_narr_good_crit_03.wav",     "STORY_CRIT_3"),
            new("vo_narr_good_crit_04.wav",     "STORY_CRIT_4"),
            new("vo_narr_good_crit_04_alt.wav", "STORY_CRIT_5"),
            new("vo_narr_good_crit_05.wav",     "STORY_CRIT_6"),
            new("vo_narr_good_crit_06.wav",     "STORY_CRIT_7"),
            new("vo_narr_good_crit_07.wav",     "STORY_CRIT_8"),
            new("vo_narr_good_crit_08.wav",     "STORY_CRIT_9"),
            new("vo_narr_good_crit_09.wav",     "STORY_CRIT_10"),
            new("vo_narr_good_crit_01_alt.wav", "STORY_CRIT_11"),
            new("vo_narr_good_crit_03_alt.wav", "STORY_CRIT_12"),
        };

        // 单次受伤 >= 最大生命 15%
        Map[NarratorEvent.PlayerHurt] = new List<VoiceEntry>
        {
            new("vo_narr_bad_crit_01.wav", "STORY_HURT_1"),
            new("vo_narr_bad_crit_02.wav", "STORY_HURT_2"),
            new("vo_narr_bad_crit_03.wav", "STORY_HURT_3"),
            new("vo_narr_bad_crit_04.wav", "STORY_HURT_4"),
            new("vo_narr_bad_crit_05.wav", "STORY_HURT_5"),
            new("vo_narr_bad_crit_06.wav", "STORY_HURT_6"),
            new("vo_narr_bad_crit_07.wav", "STORY_HURT_7"),
            new("vo_narr_bad_crit_08.wav", "STORY_HURT_8"),
            new("vo_narr_bad_crit_09.wav", "STORY_HURT_9"),
            new("vo_narr_bad_crit_10.wav", "STORY_HURT_10"),
        };

        // 完美格挡所有伤害
        Map[NarratorEvent.BlockAllDamage] = new List<VoiceEntry>
        {
            new("vo_narr_bad_obstacle_01.wav",    "STORY_BLOCK_1"),
            new("vo_narr_bad_obstacle_cove.wav",  "STORY_BLOCK_2"),
            new("vo_narr_bad_obstacle_crypts.wav","STORY_BLOCK_3"),
            new("vo_narr_bad_obstacle_warrens.wav","STORY_BLOCK_4"),
            new("vo_narr_bad_obstacle_weald.wav", "STORY_BLOCK_5"),
        };

        // 血量 <= 25%
        Map[NarratorEvent.LowHp] = new List<VoiceEntry>
        {
            new("vo_narr_bad_deathsdoor_01.wav", "STORY_LOWHP_1"),
            new("vo_narr_bad_deathsdoor_02.wav", "STORY_LOWHP_2"),
            new("vo_narr_bad_deathsdoor_03.wav", "STORY_LOWHP_3"),
            new("vo_narr_bad_deathsdoor_04.wav", "STORY_LOWHP_4"),
            new("vo_narr_bad_deathsdoor_05.wav", "STORY_LOWHP_5"),
        };

        // ========== 击杀 ==========

        // 每场战斗首次击杀
        Map[NarratorEvent.KillFirst] = new List<VoiceEntry>
        {
            new("vo_narr_good_killfirst_01.wav", "STORY_KILLFIRST_1"),
            new("vo_narr_good_killfirst_02.wav", "STORY_KILLFIRST_2"),
            new("vo_narr_good_killfirst_03.wav", "STORY_KILLFIRST_3"),
            new("vo_narr_good_killfirst_04.wav", "STORY_KILLFIRST_4"),
            new("vo_narr_good_killfirst_05.wav", "STORY_KILLFIRST_5"),
        };

        // 击杀普通敌人
        Map[NarratorEvent.KillNormal] = new List<VoiceEntry>
        {
            new("vo_narr_good_kill_weak_01.wav", "STORY_KILL_1"),
            new("vo_narr_good_kill_weak_02.wav", "STORY_KILL_2"),
            new("vo_narr_good_kill_weak_03.wav", "STORY_KILL_3"),
            new("vo_narr_good_kill_weak_04.wav", "STORY_KILL_4"),
            new("vo_narr_good_kill_weak_05.wav", "STORY_KILL_5"),
        };

        // 击杀大体型敌人
        Map[NarratorEvent.KillLarge] = new List<VoiceEntry>
        {
            new("vo_narr_good_kill_big_01.wav", "STORY_KILLBIG_1"),
            new("vo_narr_good_kill_big_02.wav", "STORY_KILLBIG_2"),
            new("vo_narr_good_kill_big_03.wav", "STORY_KILLBIG_3"),
            new("vo_narr_good_kill_big_04.wav", "STORY_KILLBIG_4"),
            new("vo_narr_good_kill_big_05.wav", "STORY_KILLBIG_5"),
        };

        // 击杀强敌
        Map[NarratorEvent.KillStrong] = new List<VoiceEntry>
        {
            new("vo_narr_good_kill_strong_01.wav",     "STORY_KILLSTRONG_1"),
            new("vo_narr_good_kill_strong_02.wav",     "STORY_KILLSTRONG_2"),
            new("vo_narr_good_kill_strong_03.wav",     "STORY_KILLSTRONG_3"),
            new("vo_narr_good_kill_strong_04_v1.wav",  "STORY_KILLSTRONG_4"),
            new("vo_narr_good_kill_strong_04_v2.wav",  "STORY_KILLSTRONG_5"),
            new("vo_narr_good_kill_strong_05.wav",     "STORY_KILLSTRONG_6"),
        };

        // 敌人死于持续伤害
        Map[NarratorEvent.KillDot] = new List<VoiceEntry>
        {
            new("vo_narr_good_kill_dot_01.wav", "STORY_KILLDOT_1"),
            new("vo_narr_good_kill_dot_02.wav", "STORY_KILLDOT_2"),
            new("vo_narr_good_kill_dot_03.wav", "STORY_KILLDOT_3"),
            new("vo_narr_good_kill_dot_04.wav", "STORY_KILLDOT_4"),
        };

        // ========== 胜利与死亡 ==========

        // 战斗胜利
        Map[NarratorEvent.CombatVictory] = new List<VoiceEntry>
        {
            new("vo_narr_good_victoryfirst_03.wav", "STORY_VICTORY_1"),
            new("vo_narr_good_victoryfirst_04.wav", "STORY_VICTORY_2"),
            new("vo_narr_good_victoryfirst_05.wav", "STORY_VICTORY_3"),
            new("vo_narr_good_victoryfirst_06.wav", "STORY_VICTORY_4"),
            new("vo_narr_good_victoryfirst_07.wav", "STORY_VICTORY_5"),
            new("vo_narr_good_victoryfirst_08.wav", "STORY_VICTORY_6"),
            new("vo_narr_good_victoryfirst_09.wav", "STORY_VICTORY_7"),
        };

        // 本局首次战斗胜利
        Map[NarratorEvent.CombatVictoryFirst] = new List<VoiceEntry>
        {
            new("vo_narr_good_victoryfirst_01.wav", "STORY_VICTORYFIRST_1"),
            new("vo_narr_good_victoryfirst_02.wav", "STORY_VICTORYFIRST_2"),
        };

        // 死亡
        Map[NarratorEvent.PlayerDeath] = new List<VoiceEntry>
        {
            new("vo_narr_bad_death_01.wav",     "STORY_DEATH_1"),
            new("vo_narr_bad_death_02.wav",     "STORY_DEATH_2"),
            new("vo_narr_bad_death_03.wav",     "STORY_DEATH_3"),
            new("vo_narr_bad_death_04.wav",     "STORY_DEATH_4"),
            new("vo_narr_bad_death_04_alt.wav", "STORY_DEATH_5"),
            new("vo_narr_bad_death_05.wav",     "STORY_DEATH_6"),
        };

        // ========== Boss ==========

        // 进入 Boss 房间
        Map[NarratorEvent.BossEncounter] = new List<VoiceEntry>
        {
            new("vo_narr_bad_boss_01.wav",             "STORY_BOSS_ENTER_1"),
            new("vo_narr_bad_boss_cove.wav",           "STORY_BOSS_ENTER_2"),
            new("vo_narr_bad_boss_cove_alt.wav",       "STORY_BOSS_ENTER_3"),
            new("vo_narr_bad_darkest_boss_01.wav",     "STORY_BOSS_ENTER_4"),
            new("vo_narr_bad_darkest_boss_02.wav",     "STORY_BOSS_ENTER_5"),
            new("vo_narr_bad_darkest_boss_03.wav",     "STORY_BOSS_ENTER_6"),
            new("vo_narr_bad_darkest_boss_04.wav",     "STORY_BOSS_ENTER_7"),
        };

        // Boss 血量首次低于 50%
        Map[NarratorEvent.BossHalfHp] = new List<VoiceEntry>
        {
            new("vo_narr_bad_halfstats_01.wav", "STORY_BOSS_HALF_1"),
        };

        // 击杀 Boss
        Map[NarratorEvent.BossKilled] = new List<VoiceEntry>
        {
            new("vo_narr_good_bosskill_01.wav",          "STORY_BOSSKILL_1"),
            new("vo_narr_good_bosskill_cove_01.wav",     "STORY_BOSSKILL_2"),
            new("vo_narr_good_bosskill_cove_02.wav",     "STORY_BOSSKILL_3"),
            new("vo_narr_good_bosskill_cove_alt_01.wav", "STORY_BOSSKILL_4"),
            new("vo_narr_good_bosskill_crypts_02.wav",   "STORY_BOSSKILL_5"),
            new("vo_narr_good_bosskill_crypts_alt_01.wav","STORY_BOSSKILL_6"),
            new("vo_narr_good_bosskill_crypts_alt_02.wav","STORY_BOSSKILL_7"),
            new("vo_narr_good_bosskill_warrens_01.wav",  "STORY_BOSSKILL_8"),
            new("vo_narr_good_bosskill_warrens_02.wav",  "STORY_BOSSKILL_9"),
            new("vo_narr_good_bosskill_weald_01.wav",    "STORY_BOSSKILL_10"),
            new("vo_narr_good_bosskill_weald_02.wav",    "STORY_BOSSKILL_11"),
            new("vo_narr_good_bosskill_weald_alt_01.wav","STORY_BOSSKILL_12"),
            new("vo_narr_good_bosskill_weald_alt_02.wav","STORY_BOSSKILL_13"),
        };

        // ========== 精英 ==========

        // 进入精英房间
        Map[NarratorEvent.EliteEncounter] = new List<VoiceEntry>
        {
            new("vo_narr_bad_boss_crypts_alt.wav",   "STORY_ELITE_ENTER_1"),
            new("vo_narr_bad_boss_warrens.wav",      "STORY_ELITE_ENTER_2"),
            new("vo_narr_bad_boss_warrens_alt.wav",  "STORY_ELITE_ENTER_3"),
            new("vo_narr_bad_boss_weald.wav",        "STORY_ELITE_ENTER_4"),
            new("vo_narr_bad_boss_weald_alt.wav",    "STORY_ELITE_ENTER_5"),
        };

        // ========== 篝火 ==========

        Map[NarratorEvent.CampRest] = new List<VoiceEntry>
        {
            new("vo_narr_good_camp_01.wav",           "STORY_CAMP_1"),
            new("vo_narr_good_camp_02.wav",           "STORY_CAMP_2"),
            new("vo_narr_good_camp_03.wav",           "STORY_CAMP_3"),
            new("vo_narr_good_camp_04.wav",           "STORY_CAMP_4"),
            new("vo_narr_good_camp_05.wav",           "STORY_CAMP_5"),
            new("vo_narr_good_afflictionpass_01.wav", "STORY_CAMP_6"),
        };

        // ========== 房间进入 ==========

        // 打开宝箱
        Map[NarratorEvent.RoomEnterTreasure] = new List<VoiceEntry>
        {
            new("vo_narr_good_lootfirst_01.wav", "STORY_TREASURE_1"),
            new("vo_narr_good_lootfirst_02.wav", "STORY_TREASURE_2"),
            new("vo_narr_good_lootfirst_03.wav", "STORY_TREASURE_3"),
            new("vo_narr_good_lootfirst_04.wav", "STORY_TREASURE_4"),
            new("vo_narr_good_lootfirst_05.wav", "STORY_TREASURE_5"),
            new("vo_narr_good_lootfirst_06.wav", "STORY_TREASURE_6"),
            new("vo_narr_good_lootfirst_07.wav", "STORY_TREASURE_7"),
            new("vo_narr_good_torchlight_02.wav", "STORY_TREASURE_8"),
        };

        // 进入商店
        Map[NarratorEvent.RoomEnterShop] = new List<VoiceEntry>
        {
            new("vo_narr_good_torchlight_01.wav", "STORY_SHOP_1"),
            new("vo_narr_good_torchlight_03.wav", "STORY_SHOP_2"),
            new("vo_narr_good_torchlight_04.wav", "STORY_SHOP_3"),
            new("vo_narr_good_torchlight_05.wav", "STORY_SHOP_4"),
        };

        // ========== 战利品 ==========

        // 获得遗物或稀有卡牌
        Map[NarratorEvent.LootObtained] = new List<VoiceEntry>
        {
            new("vo_narr_neut_cove_gather.wav",    "STORY_LOOT_1"),
            new("vo_narr_neut_crypts_gather.wav",  "STORY_LOOT_2"),
            new("vo_narr_neut_warrens_gather.wav", "STORY_LOOT_3"),
            new("vo_narr_neut_weald_gather.wav",   "STORY_LOOT_4"),
            new("vo_narr_good_gather_crypts_01.wav", "STORY_LOOT_5"),
            new("vo_narr_good_gather_weald_01.wav",  "STORY_LOOT_6"),
        };

        // ========== Buff ==========

        // 获得增益
        Map[NarratorEvent.BuffGained] = new List<VoiceEntry>
        {
            new("vo_narr_good_virtue_focused.wav",  "STORY_BUFF_1"),
            new("vo_narr_good_virtue_powerful.wav", "STORY_BUFF_2"),
            new("vo_narr_good_virtue_stalwart.wav", "STORY_BUFF_3"),
            new("vo_narr_good_virtue_vigorous.wav", "STORY_BUFF_4"),
        };

        // 受到 debuff
        Map[NarratorEvent.DebuffGained] = new List<VoiceEntry>
        {
            new("vo_narr_bad_afflicted_01.wav",       "STORY_DEBUFF_1"),
            new("vo_narr_bad_afflicted_02.wav",       "STORY_DEBUFF_2"),
            new("vo_narr_bad_afflicted_03.wav",       "STORY_DEBUFF_3"),
            new("vo_narr_bad_afflicted_04.wav",       "STORY_DEBUFF_4"),
            new("vo_narr_bad_afflicted_05.wav",       "STORY_DEBUFF_5"),
            new("vo_narr_bad_afflicted_06.wav",       "STORY_DEBUFF_6"),
            new("vo_narr_bad_afflicted_07.wav",       "STORY_DEBUFF_7"),
            new("vo_narr_bad_afflicted_08.wav",       "STORY_DEBUFF_8"),
            new("vo_narr_bad_afflicted_09.wav",       "STORY_DEBUFF_9"),
            new("vo_narr_bad_afflicted_abusive.wav",   "STORY_DEBUFF_10"),
            new("vo_narr_bad_afflicted_depressed.wav", "STORY_DEBUFF_11"),
            new("vo_narr_bad_afflicted_fearful.wav",   "STORY_DEBUFF_12"),
            new("vo_narr_bad_afflicted_irrational.wav","STORY_DEBUFF_13"),
            new("vo_narr_bad_afflicted_masochistic.wav","STORY_DEBUFF_14"),
            new("vo_narr_bad_afflicted_paranoid.wav",  "STORY_DEBUFF_15"),
            new("vo_narr_bad_afflicted_selfish.wav",   "STORY_DEBUFF_16"),
        };

        // ========== 敌人增强 ==========

        // 敌人获得增益
        Map[NarratorEvent.EnemyBuffGained] = new List<VoiceEntry>
        {
            new("vo_narr_good_activate_crypts_01.wav", "STORY_ENEMYBUFF_1"),
            new("vo_narr_good_activate_crypts_02.wav", "STORY_ENEMYBUFF_2"),
            new("vo_narr_good_activate_warrens_01.wav","STORY_ENEMYBUFF_3"),
            new("vo_narr_good_activate_warrens_02.wav","STORY_ENEMYBUFF_4"),
            new("vo_narr_good_activate_weald_01.wav",  "STORY_ENEMYBUFF_5"),
            new("vo_narr_good_activate_weald_02.wav",  "STORY_ENEMYBUFF_6"),
        };

    }

    public static string GetFullPath(string fileName) => BasePath + fileName;

    public static VoiceEntry PickRandom(NarratorEvent evt)
    {
        if (!Map.TryGetValue(evt, out var list) || list.Count == 0)
            return new VoiceEntry("blank.wav", "");
        int idx = _counter % list.Count;
        _counter++;
        return list[idx];
    }

    private static int _counter = 0;
}
