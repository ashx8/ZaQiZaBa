#nullable enable
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace ZaQiZaBa;

[HarmonyPatch(typeof(Creature), "Block", MethodType.Setter)]
internal static class Patch_BlockChanged
{
    static void Prefix(Creature __instance, out int __state)
    {
        __state = __instance.Block;
    }

    static void Postfix(Creature __instance, int __state)
    {
        if (__instance.IsPlayer && !__instance.IsDead && !__instance.IsPet)
        {
            int lost = __state - __instance.Block;
            if (lost > 0)
                CombatDialogueManager.OnPlayerBlockDecreased(__instance);
            PlayerDamageOverlay.Refresh();
        }
    }
}

[HarmonyPatch(typeof(Creature), "CurrentHp", MethodType.Setter)]
internal static class Patch_CurrentHpChanged
{
    static void Prefix(Creature __instance, out int __state)
    {
        __state = __instance.CurrentHp;
    }

    static void Postfix(Creature __instance, int __state)
    {
        if (__instance.IsPlayer && !__instance.IsPet)
        {
            int delta = __state - __instance.CurrentHp;
            if (delta > 0)
            {
                CombatDialogueManager.OnPlayerTookDamage(__instance, delta);
                NarratorEventHandler.OnPlayerTookDamage(__instance, delta);
            }
            else if (delta < 0)
            {
                NarratorEventHandler.OnPlayerHealed(__instance, -delta);
            }
        }
    }
}

[HarmonyPatch(typeof(Creature), nameof(Creature.ApplyPowerInternal))]
internal static class Patch_PowerApplied
{
    static void Postfix(Creature __instance, PowerModel power)
    {
        if (__instance.IsPet) return;
        NarratorEventHandler.OnPowerApplied(__instance, power);
    }
}
