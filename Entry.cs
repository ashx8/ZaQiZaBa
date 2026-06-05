#nullable enable
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using Sts2Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace ZaQiZaBa;

[ModInitializer(nameof(Init))]
public static partial class Entry
{
    public const string ModId = "ZaQiZaBa";
    public static readonly Sts2Logger Logger = new(ModId, LogType.Generic);
    private static Texture2D? _customIconTexture;

    public static void Init()
    {
        PreloadCustomIcon();
        ZaQiZaBaConfig.RegisterSettings();

        using (RitsuLibFramework.BeginModDataRegistration(ModId))
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Register<OverlayPositionData>(
                key: "overlay_positions",
                fileName: "ZaQiZaBa_overlay_positions.json",
                scope: SaveScope.Profile,
                defaultFactory: () => new OverlayPositionData(),
                autoCreateIfMissing: true
            );
        }

        RitsuLibFramework.SubscribeLifecycle<EssentialInitializationCompletedEvent>(evt =>
        {
            if (ZaQiZaBaConfig.SkipIntroLogo && SaveManager.Instance?.SettingsSave != null)
                SaveManager.Instance.SettingsSave.SkipIntroLogo = true;
        });

        CombatDialogueManager.Initialize();
        PlayerDamageOverlay.InitializeTurnEvents();
        NarratorEventHandler.Initialize();
        new Harmony(ModId).PatchAll();
    }

    private static void PreloadCustomIcon()
    {
        _customIconTexture = GD.Load<Texture2D>("res://icons/damage_icon.png");
    }

    internal static void ReplaceIntentIcon(NIntent node)
    {
        try
        {
            var spriteField = typeof(NIntent).GetField("_intentSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (spriteField == null) return;
            var intentSprite = spriteField.GetValue(node) as Sprite2D;
            if (intentSprite == null) return;
            if (_customIconTexture != null)
                intentSprite.Texture = _customIconTexture;
        }
        catch (Exception ex)
        {
            Logger.Error($"ReplaceIntentIcon: {ex.Message}");
        }
    }
}
