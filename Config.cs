#nullable enable
using System;
using MegaCrit.Sts2.Core.Modding;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace ZaQiZaBa;

internal static class ZaQiZaBaConfig
{
    private const string ConfigModId = "ZaQiZaBa";
    public static bool ShowPlayerIncomingDamage = true;
    public static bool SkipIntroLogo = true;
    public static bool ShowCombatDialogue = true;
    public static int HitLightChance = 30;
    public static int Hit20Chance = 60;
    public static int Atk20Chance = 60;
    public static int BlockedAtkChance = 20;
    public static int BlockedHitChance = 20;
    public static int KillChance = 60;
    public static bool EnableNarrator = true;
    public static bool ShowNarratorSubtitle = true;
    public static int NarratorVolume = 60;
    public static int NarratorCardPlayChance = 18;
    public static int NarratorPlayerHurtChance = 60;
    public static int NarratorKillEnemyFirstChance = 100;
    public static int NarratorKillEnemyBigChance = 40;
    public static int NarratorKillEnemyStrongChance = 40;
    public static int NarratorKillEnemyDotChance = 30;
    public static int NarratorKillEnemyChance = 25;
    public static int NarratorHealChance = 40;
    public static int NarratorBuffGainedChance = 30;
    public static int NarratorDebuffGainedChance = 40;
    public static int NarratorEnemyBuffChance = 30;

    public static void RegisterSettings()
    {
        RitsuLibFramework.RegisterModSettings(ConfigModId, page => page
            .WithModDisplayName(ModSettingsText.Literal("杂七杂八"))
            .AddSection("damageForecast", section => section
                .WithTitle(ModSettingsText.Literal("伤害预测"))
                .AddToggle("showPlayerIncomingDamage", ModSettingsText.Literal("显示玩家受伤预测"),
                    CallbackBool(() => ShowPlayerIncomingDamage, v =>
                    {
                        ShowPlayerIncomingDamage = v;
                        PlayerDamageOverlay.Refresh();
                    })))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("通用"))
                .AddToggle("skipIntroLogo", ModSettingsText.Literal("跳过开场动画"),
                    CallbackBool(() => SkipIntroLogo, v => SkipIntroLogo = v)))
            .AddSection("dialogue", section => section
                .WithTitle(ModSettingsText.Literal("对话气泡"))
                .AddToggle("showCombatDialogue", ModSettingsText.Literal("显示战斗对话气泡"),
                    CallbackBool(() => ShowCombatDialogue, v =>
                    {
                        ShowCombatDialogue = v;
                        CombatDialogueManager.ResetTriggerFlags();
                    }))
                .AddIntSlider("hitLightChance", ModSettingsText.Literal("受击轻击概率 (<20%)"),
                    CallbackInt(() => HitLightChance, v => HitLightChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("被敌人打掉少于20%生命时触发对话的概率，40%档必定触发"))
                .AddIntSlider("hit20Chance", ModSettingsText.Literal("受击中击概率 (≥20%)"),
                    CallbackInt(() => Hit20Chance, v => Hit20Chance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("被敌人打掉20%及以上生命时触发对话的概率，40%档必定触发"))
                .AddIntSlider("atk20Chance", ModSettingsText.Literal("攻击中击概率 (≥20%)"),
                    CallbackInt(() => Atk20Chance, v => Atk20Chance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("对敌人单张卡牌打掉20%及以上生命时触发对话的概率，40%档和击杀必定触发"))
                .AddIntSlider("blockedAtkChance", ModSettingsText.Literal("攻击被格挡概率"),
                    CallbackInt(() => BlockedAtkChance, v => BlockedAtkChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("攻击未造成伤害且敌方护甲减少时触发对话的概率"))
                .AddIntSlider("blockedHitChance", ModSettingsText.Literal("受击被格挡概率"),
                    CallbackInt(() => BlockedHitChance, v => BlockedHitChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("敌人攻击未造成伤害且自身护甲减少时触发对话的概率"))
                .AddIntSlider("killChance", ModSettingsText.Literal("击杀概率"),
                    CallbackInt(() => KillChance, v => KillChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("单张卡牌击杀敌人时触发对话的概率"))
            )
            .AddSection("narrator", section => section
                .WithTitle(ModSettingsText.Literal("老祖旁白"))
                .AddToggle("enableNarrator", ModSettingsText.Literal("启用老祖旁白语音"),
                    CallbackBool(() => EnableNarrator, v => EnableNarrator = v))
                .AddToggle("showNarratorSubtitle", ModSettingsText.Literal("显示旁白字幕"),
                    CallbackBool(() => ShowNarratorSubtitle, v => ShowNarratorSubtitle = v))
                .AddIntSlider("narratorVolume", ModSettingsText.Literal("旁白音量"),
                    CallbackInt(() => NarratorVolume, v =>
                    {
                        NarratorVolume = v;
                        NarratorVoicePlayer.UpdateVolume();
                    }),
                    0, 150, 5, v => $"{v}%",
                    ModSettingsText.Literal("基于游戏当前SFX音量的百分比，100%即与SFX等大"))
                .AddIntSlider("narratorCardPlayChance", ModSettingsText.Literal("攻击卡触发概率"),
                    CallbackInt(() => NarratorCardPlayChance, v => NarratorCardPlayChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("每张攻击卡打出时触发旁白的概率"))
                .AddIntSlider("narratorPlayerHurtChance", ModSettingsText.Literal("受创触发概率"),
                    CallbackInt(() => NarratorPlayerHurtChance, v => NarratorPlayerHurtChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("受到单次伤害≥15%最大生命时触发旁白的概率，≥30%必定触发"))
                .AddIntSlider("narratorKillEnemyChance", ModSettingsText.Literal("击杀触发概率"),
                    CallbackInt(() => NarratorKillEnemyChance, v => NarratorKillEnemyChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("击杀敌人触发旁白的概率"))
                .AddIntSlider("narratorHealChance", ModSettingsText.Literal("治疗触发概率"),
                    CallbackInt(() => NarratorHealChance, v => NarratorHealChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("单次治疗≥10%最大生命时触发旁白的概率"))
                .AddIntSlider("narratorBuffGainedChance", ModSettingsText.Literal("获得Buff触发概率"),
                    CallbackInt(() => NarratorBuffGainedChance, v => NarratorBuffGainedChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("自身获得增益效果时触发旁白的概率"))
                .AddIntSlider("narratorDebuffGainedChance", ModSettingsText.Literal("受到Debuff触发概率"),
                    CallbackInt(() => NarratorDebuffGainedChance, v => NarratorDebuffGainedChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("自身被施加减益效果时触发旁白的概率"))
                .AddIntSlider("narratorEnemyBuffChance", ModSettingsText.Literal("敌人Buff触发概率"),
                    CallbackInt(() => NarratorEnemyBuffChance, v => NarratorEnemyBuffChance = v),
                    0, 100, 5, v => $"{v}%",
                    ModSettingsText.Literal("敌人获得增益效果时触发旁白的概率"))
            ));
    }

    private static IModSettingsValueBinding<bool> CallbackBool(Func<bool> read, Action<bool> write)
        => ModSettingsBindings.Callback(ConfigModId, "config", read, write, () => { });

    private static IModSettingsValueBinding<int> CallbackInt(Func<int> read, Action<int> write)
        => ModSettingsBindings.Callback(ConfigModId, "config", read, write, () => { });
}
