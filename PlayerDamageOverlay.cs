#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib;
using STS2RitsuLib.Data;

namespace ZaQiZaBa;

public sealed class OverlayPositionData
{
    public Dictionary<string, PositionRecord> CustomPositions { get; set; } = new();
}

public sealed class PositionRecord
{
    public float X { get; set; }
    public float Y { get; set; }
}

internal sealed class PlayerDamageIntent : SingleAttackIntent
{
    public PlayerDamageIntent(int effectiveDamage, int remainingHp, int maxHp) : base(effectiveDamage) { }

    protected override LocString IntentLabelFormat => new LocString("intents", "FORMAT_DAMAGE_SINGLE");

    public override Texture2D GetTexture(IEnumerable<Creature> targets, Creature owner)
    {
        var path = "res://images/atlases/intent_atlas.sprites/attack/intent_attack_2.tres";
        try { return GD.Load<Texture2D>(path); }
        catch { return base.GetTexture(targets, owner); }
    }

    public override string GetAnimation(IEnumerable<Creature> targets, Creature owner) => "attack_2";
    public override bool HasIntentTip => false;
}

internal static class PlayerDamageOverlay
{
    private static readonly Dictionary<Creature, NIntent> _intents = new();
    internal static readonly Dictionary<NIntent, (int damage, int remainingHp, int maxHp)> _intentData = new();

    public static void InitializeTurnEvents()
    {
        RitsuLibFramework.SubscribeLifecycle<SideTurnStartedEvent>(evt =>
        {
            if (evt.Side == CombatSide.Player)
                Refresh();
            else if (evt.Side == CombatSide.Enemy)
                Hide();
        });
    }

    public static void Refresh()
    {
        try
        {
            if (!ZaQiZaBaConfig.ShowPlayerIncomingDamage) { Hide(); return; }
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) { Hide(); return; }
            var cm = CombatManager.Instance;
            if (cm == null) { Hide(); return; }
            var state = cm.DebugOnlyGetState();
            if (state == null) { Hide(); return; }

            var activeCreatures = new HashSet<Creature>();

            foreach (var player in state.PlayerCreatures.Where(p => !p.IsDead && !p.IsPet && p.IsPlayer))
            {
                Creature? pet = null;
                var petList = player.Pets;
                if (petList != null && petList.Count > 0)
                    pet = petList.FirstOrDefault(p => !p.IsDead);

                var (playerDamage, petDamage) = CalcEffectiveDamage(state, player, pet);
                int remainingHp = Math.Max(0, player.CurrentHp - playerDamage);
                int maxHp = player.MaxHp;

                if (playerDamage > 0)
                {
                    var creatureNode = combatRoom.GetCreatureNode(player);
                    if (creatureNode != null)
                        CreateOrUpdateIntent(player, creatureNode, playerDamage, remainingHp, maxHp);
                }
                else
                {
                    RemoveIntent(player);
                }

                if (pet != null)
                {
                    if (petDamage > 0)
                    {
                        var petNode = combatRoom.GetCreatureNode(pet);
                        if (petNode != null)
                        {
                            int petRemainingHp = Math.Max(0, pet.CurrentHp - petDamage);
                            CreateOrUpdateIntent(pet, petNode, petDamage, petRemainingHp, pet.MaxHp);
                        }
                    }
                    else
                    {
                        RemoveIntent(pet);
                    }
                    activeCreatures.Add(pet);
                }

                activeCreatures.Add(player);
            }

            foreach (var creature in _intents.Keys.Where(c => !activeCreatures.Contains(c)).ToList())
                RemoveIntent(creature);
        }
        catch (Exception ex) { Entry.Logger.Error($"Refresh: {ex.Message}"); }
    }

    private static void CreateOrUpdateIntent(Creature creature, NCreature creatureNode, int damage, int remainingHp, int maxHp)
    {
        NIntent node;
        if (_intents.TryGetValue(creature, out var existing) && IsValid(existing))
            node = existing;
        else
        {
            node = NIntent.Create(0f);
            if (node == null) return;
            ApplySavedPosition(creatureNode);
            creatureNode.IntentContainer.AddChild(node);
            creatureNode.IntentContainer.Visible = true;
            creatureNode.IntentContainer.Modulate = Colors.White;
            EnableMouseInteraction(node, creatureNode);
            _intents[creature] = node;
            _intentData[node] = (damage, remainingHp, maxHp);
            PlayFadeInAnimation(node);
        }

        var intent = new PlayerDamageIntent(damage, remainingHp, maxHp);
        node.UpdateIntent(intent, Enumerable.Empty<Creature>(), creature);
        node.Visible = true;
        node.MouseFilter = Control.MouseFilterEnum.Pass;
        _intentData[node] = (damage, remainingHp, maxHp);
        ApplyCustomStyle(node);
    }

    private static (int playerDamage, int petDamage) CalcEffectiveDamage(CombatState state, Creature player, Creature? pet)
    {
        if (state.Enemies == null) return (0, 0);
        int block = player.Block;
        int playerDmg = 0;
        int petDmg = 0;

        bool petAbsorbs = pet != null && !pet.IsDead && pet.MaxHp > 0
            && (pet.Monster == null || pet.Monster.IsHealthBarVisible);
        int petHp = petAbsorbs ? pet!.CurrentHp : 0;
        int petBlock = petAbsorbs ? pet!.Block : 0;

        foreach (var enemy in state.Enemies.Where(e => e.Monster != null))
        {
            var nextMove = enemy.Monster?.NextMove;
            if (nextMove == null) continue;
            foreach (var intent in nextMove.Intents)
            {
                if (intent is not AttackIntent attack) continue;
                int single = attack.GetSingleDamage(state.PlayerCreatures, enemy);
                int all = attack.GetTotalDamage(state.PlayerCreatures, enemy);
                if (single <= 0 || all <= 0) continue;
                int hits = all / single;
                for (int i = 0; i < hits; i++)
                {
                    int remaining = single;
                    int absorbed = Math.Min(block, remaining);
                    block -= absorbed;
                    remaining -= absorbed;

                    if (petHp > 0 && remaining > 0)
                    {
                        int blocked = Math.Min(petBlock, remaining);
                        petBlock -= blocked;
                        remaining -= blocked;
                        int dmg = Math.Min(petHp, remaining);
                        petHp -= dmg;
                        petDmg += dmg;
                        remaining -= dmg;
                    }

                    playerDmg += remaining;
                }
            }
        }
        return (playerDmg, petDmg);
    }

    private static void ApplyCustomStyle(NIntent node)
    {
        try { SetIconColor(node, new Color(1f, 0.6f, 0f)); }
        catch (Exception ex) { Entry.Logger.Error($"ApplyCustomStyle: {ex.Message}"); }
    }

    private static void SetIconColor(Node node, Color color)
    {
        if (node is TextureRect tr) { tr.Modulate = color; return; }
        foreach (var child in node.GetChildren())
            SetIconColor(child, color);
    }

    internal static Color GetColorByPercent(float percent)
    {
        if (percent > 0.5f) return new Color(0.3f, 1f, 0.3f);
        if (percent > 0.25f) return new Color(1f, 1f, 0.3f);
        return new Color(1f, 0.3f, 0.3f);
    }

    private static bool IsValid(NIntent node)
        => GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();

    private static void RemoveIntent(Creature creature)
    {
        if (_intents.TryGetValue(creature, out var node))
        {
            _intentData.Remove(node);
            _intents.Remove(creature);
            if (IsValid(node)) PlayFadeOutAnimation(node);
        }
    }

    public static void Hide()
    {
        foreach (var node in _intents.Values)
        {
            _intentData.Remove(node);
            if (IsValid(node)) PlayFadeOutAnimation(node);
        }
        _intents.Clear();
    }

    internal static void ApplySavedPosition(NCreature creatureNode)
    {
        try
        {
            if (!_positionsLoaded) LoadCustomPositions();
            string creatureId = GetCreatureId(creatureNode);
            if (_customPositions.TryGetValue(creatureId, out var absPos))
                creatureNode.IntentContainer.Position = absPos;
        }
        catch { }
    }

    internal static bool _isDragging = false;
    private static Vector2 _dragOffset = Vector2.Zero;
    private static NIntent? _draggedNode = null;
    private static Dictionary<string, Vector2> _customPositions = new();
    private static bool _positionsLoaded = false;

    private static void EnableMouseInteraction(NIntent node, NCreature creatureNode)
    {
        try
        {
            node.MouseFilter = Control.MouseFilterEnum.Stop;
            var panel = new Panel
            {
                Name = "HoverPanel",
                Visible = false,
                ZIndex = 10,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            var styleBox = new StyleBoxFlat
            {
                BorderColor = Colors.White,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                BorderWidthTop = 2,
                BorderWidthBottom = 2,
                BgColor = Colors.Transparent,
                ContentMarginLeft = 4,
                ContentMarginRight = 4,
                ContentMarginTop = 2,
                ContentMarginBottom = 2,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4
            };
            panel.AddThemeStyleboxOverride("panel", styleBox);
            node.AddChild(panel);
            panel.CustomMinimumSize = new Vector2(128, 56);
            panel.Size = new Vector2(128, 56);
            panel.Position = new Vector2(-32, -4);

            node.MouseEntered += () =>
            {
                if (!_isDragging && GodotObject.IsInstanceValid(panel))
                {
                    panel.Visible = true;
                    var sb = panel.GetThemeStylebox("panel") as StyleBoxFlat;
                    if (sb != null)
                    {
                        sb.BgColor = Colors.Transparent;
                        sb.BorderWidthLeft = 2;
                        sb.BorderWidthRight = 2;
                        sb.BorderWidthTop = 2;
                        sb.BorderWidthBottom = 2;
                        sb.BorderColor = Colors.White;
                    }
                }
            };

            node.MouseExited += () =>
            {
                if (!_isDragging && GodotObject.IsInstanceValid(panel))
                    panel.Visible = false;
            };

            node.GuiInput += @event =>
            {
                if (@event is InputEventMouseButton mouseButton)
                {
                    if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
                    {
                        _isDragging = true;
                        _draggedNode = node;
                        _dragOffset = node.GetGlobalMousePosition() - node.GlobalPosition;
                        panel.Visible = true;
                        styleBox.BorderWidthLeft = 3;
                        styleBox.BorderWidthRight = 3;
                        styleBox.BorderWidthTop = 3;
                        styleBox.BorderWidthBottom = 3;
                        styleBox.BorderColor = new Color(1f, 0.9f, 0.5f);
                    }
                    else if (!mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left && _isDragging && _draggedNode == node)
                    {
                        _isDragging = false;
                        _draggedNode = null;
                        if (GodotObject.IsInstanceValid(panel))
                        {
                            var sb = panel.GetThemeStylebox("panel") as StyleBoxFlat;
                            if (sb != null)
                            {
                                sb.BorderWidthLeft = 2;
                                sb.BorderWidthRight = 2;
                                sb.BorderWidthTop = 2;
                                sb.BorderWidthBottom = 2;
                                sb.BorderColor = Colors.White;
                            }
                        }
                        SaveCustomPosition(creatureNode, creatureNode.IntentContainer.Position);
                    }
                }

                if (_isDragging && _draggedNode == node && @event is InputEventMouseMotion)
                {
                    var newPos = node.GetGlobalMousePosition() - _dragOffset;
                    var currentPos = creatureNode.IntentContainer.Position;
                    creatureNode.IntentContainer.Position = currentPos + (newPos - node.GlobalPosition);
                }
            };
        }
        catch (Exception ex) { Entry.Logger.Error($"EnableMouseInteraction: {ex.Message}"); }
    }

    private static void SaveCustomPosition(NCreature creatureNode, Vector2 position)
    {
        try
        {
            string creatureId = GetCreatureId(creatureNode);
            _customPositions[creatureId] = position;
            var store = RitsuLibFramework.GetDataStore(Entry.ModId);
            store.Modify<OverlayPositionData>("overlay_positions", data =>
            {
                data.CustomPositions[creatureId] = new PositionRecord { X = position.X, Y = position.Y };
            });
            store.Save("overlay_positions");
        }
        catch (Exception ex) { Entry.Logger.Error($"SaveCustomPosition: {ex.Message}"); }
    }

    private static void LoadCustomPositions()
    {
        try
        {
            _positionsLoaded = true;
            var store = RitsuLibFramework.GetDataStore(Entry.ModId);
            var data = store.Get<OverlayPositionData>("overlay_positions");
            if (data?.CustomPositions == null || data.CustomPositions.Count == 0) return;
            _customPositions.Clear();
            foreach (var kvp in data.CustomPositions)
                _customPositions[kvp.Key] = new Vector2(kvp.Value.X, kvp.Value.Y);
        }
        catch (Exception ex) { Entry.Logger.Error($"LoadCustomPositions: {ex.Message}"); }
    }

    private static string GetCreatureId(NCreature creatureNode)
    {
        try
        {
            var entity = creatureNode.Entity;
            if (entity != null)
            {
                string baseName = entity.Name ?? creatureNode.Name;
                if (entity.PetOwner != null)
                    return $"pet_{entity.PetOwner.Creature?.Name}_{baseName}";
                return $"player_{baseName}";
            }
            return $"player_{creatureNode.Name}";
        }
        catch { return "player_unknown"; }
    }

    private static void PlayFadeInAnimation(NIntent node)
    {
        try
        {
            node.Modulate = new Color(1f, 1f, 1f, 0f);
            var tween = node.CreateTween();
            tween.TweenProperty(node, "modulate:a", 1f, 1.0);
        }
        catch (Exception ex) { Entry.Logger.Error($"PlayFadeInAnimation: {ex.Message}"); }
    }

    private static void PlayFadeOutAnimation(NIntent node)
    {
        try
        {
            var tween = node.CreateTween();
            tween.TweenProperty(node, "modulate:a", 0f, 0.4);
            tween.Connect(Tween.SignalName.Finished, Callable.From(() =>
            {
                if (IsValid(node)) node.QueueFree();
            }));
        }
        catch (Exception)
        {
            if (IsValid(node)) node.QueueFree();
        }
    }
}

// ── 意图可视化相关补丁 ──

[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
internal static class Patch_NIntent_UpdateVisuals
{
    static void Postfix(NIntent __instance)
    {
        try
        {
            if (!PlayerDamageOverlay._intentData.TryGetValue(__instance, out var data)) return;
            var (damage, remainingHp, maxHp) = data;
            var valueLabelField = typeof(NIntent).GetField("_valueLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (valueLabelField == null) return;
            var valueLabel = valueLabelField.GetValue(__instance) as MegaRichTextLabel;
            if (valueLabel == null) return;
            float percent = maxHp > 0 ? (float)remainingHp / maxHp : 0f;
            Color hpColor = PlayerDamageOverlay.GetColorByPercent(percent);
            valueLabel.BbcodeEnabled = true;
            valueLabel.Text = $"{damage}[color=#{hpColor.ToHtml(false)}]({remainingHp})[/color]";
            Entry.ReplaceIntentIcon(__instance);
            AdjustLayout(__instance);
        }
        catch (Exception ex) { Entry.Logger.Error($"Patch_NIntent_UpdateVisuals: {ex.Message}"); }
    }

[HarmonyPatch(typeof(NIntent), "_Process")]
internal static class Patch_NIntent_Process
{
    static void Postfix(NIntent __instance)
    {
        if (PlayerDamageOverlay._intentData.ContainsKey(__instance))
            Entry.ReplaceIntentIcon(__instance);
    }
}

[HarmonyPatch(typeof(NSpeechBubbleVfx), "AnimateSpeechBubble")]
internal static class Patch_NSpeechBubbleVfx_Animate
{
    static void Prefix(NSpeechBubbleVfx __instance)
    {
        try
        {
            var textField = typeof(NSpeechBubbleVfx).GetField("_text", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (textField == null) return;
            var rawText = textField.GetValue(__instance) as string;
            if (string.IsNullOrEmpty(rawText)) return;
            if (!rawText.Contains('!') && !rawText.Contains('！')) return;
            textField.SetValue(__instance, $"[jitter]{rawText}[/jitter]");
        }
        catch { }
    }
}

    private static void AdjustLayout(NIntent node)
    {
        try
        {
            var intentHolderField = typeof(NIntent).GetField("_intentHolder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var valueLabelField = typeof(NIntent).GetField("_valueLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spriteField = typeof(NIntent).GetField("_intentSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (intentHolderField == null || valueLabelField == null || spriteField == null) return;
            var intentHolder = intentHolderField.GetValue(node) as Control;
            var valueLabel = valueLabelField.GetValue(node) as Control;
            var intentSprite = spriteField.GetValue(node) as Sprite2D;
            if (intentHolder == null || valueLabel == null || intentSprite == null) return;

            float iconSize = 28f;
            float scale = iconSize / 64f;
            intentSprite.Scale = new Vector2(scale, scale);
            intentSprite.Centered = false;
            float labelHeight = valueLabel.Size.Y > 0 ? valueLabel.Size.Y : 20f;
            float offsetX = -8f;
            float offsetY = 16f;
            float textCenterY = -5 + labelHeight / 2f;
            float iconTopY = textCenterY - iconSize / 2f + 3;
            intentSprite.Position = new Vector2(-4 + offsetX, iconTopY + offsetY);
            valueLabel.Position = new Vector2(iconSize + 6 + offsetX, -5 + offsetY);
        }
        catch (Exception ex) { Entry.Logger.Error($"AdjustLayout: {ex.Message}"); }
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature.RefreshIntents))]
internal static class Patch_RefreshIntents
{
    static void Postfix(NCreature __instance)
    {
        PlayerDamageOverlay.Refresh();
        PlayerDamageOverlay.ApplySavedPosition(__instance);
    }
}

[HarmonyPatch(typeof(NCreature), "UpdateBounds", new[] { typeof(Node) })]
internal static class Patch_UpdateBounds
{
    static void Postfix(NCreature __instance)
    {
        if (!PlayerDamageOverlay._isDragging) PlayerDamageOverlay.ApplySavedPosition(__instance);
    }
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.SetReadyToEndTurn))]
internal static class Patch_EndTurn
{
    static void Postfix() => PlayerDamageOverlay.Refresh();
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.Reset))]
internal static class Patch_Reset
{
    static void Postfix() => PlayerDamageOverlay.Hide();
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.EndCombatInternal))]
internal static class Patch_EndCombat
{
    static void Prefix() => PlayerDamageOverlay.Hide();
}

[HarmonyPatch(typeof(CombatManager), nameof(CombatManager.LoseCombat))]
internal static class Patch_LoseCombat
{
    static void Prefix() => PlayerDamageOverlay.Hide();
}
