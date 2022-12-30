using System.Linq;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Utils;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Components;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using UnityEngine;

namespace AscendedUpgrades;

/// <summary>
/// Allow ascended upgrades to show up in all 3 paths
/// </summary>
[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.IsUpgradePathClosed))]
internal class TowerSelectionMenu_IsUpgradePathClosed
{
    [HarmonyPrefix]
    internal static bool Prefix(TowerSelectionMenu __instance, ref bool __result)
    {
        if (__instance.selectedTower.tower.HasAscendedUpgrades())
        {
            __result = false;
            return false;
        }

        return true;
    }
}

/// <summary>
/// Make Ascended Upgrades show up when available
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.LoadUpgrades))]
internal static class UpgradeObject_LoadUpgrades
{
    [HarmonyPostfix]
    private static void Postfix(UpgradeObject __instance)
    {
        var ascendedPips = __instance.transform.GetComponentInChildren<AscendedPips>();
        if (ascendedPips == null)
        {
            ascendedPips = AscendedPips.Create(__instance);
        }

        if (!__instance.tts.tower.HasAscendedUpgrades())
        {
            ascendedPips.SetAmount(0);
            return;
        }

        var gameModel = InGame.instance.bridge.Model;
        var upgradeId = AscendedUpgrade.IdByPath[__instance.path];
        __instance.upgradeButton.SetUpgradeModel(gameModel.GetUpgrade(upgradeId));
        var tower = __instance.towerSelectionMenu.selectedTower.tower;
        if (AscendedUpgradesMod.ShowUpgradePips)
        {
            ascendedPips.SetAmount(tower.GetAscendedStacks().First(pair => pair.Key.Path == __instance.path).Value);
        }
    }
}

/// <summary>
/// Fixes a minor visual glitch where the wrong info can appear for one frame
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.IncreaseTier))]
internal static class UpgradeObject_IncreaseTier
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance)
    {
        return !__instance.tts.tower.HasAscendedUpgrades();
    }
}

/// <summary>
/// Change the look of the upgrade background for ascended upgrades
/// </summary>
[HarmonyPatch(typeof(UpgradeButton), nameof(UpgradeButton.UpdateVisuals))]
internal static class UpgradeButton_UpdateVisuals
{
    [HarmonyPostfix]
    private static void Postfix(UpgradeButton __instance)
    {
        var upgradeId = __instance.upgrade?.name ?? "";
        if (AscendedUpgrade.IdByPath.ContainsValue(upgradeId))
        {
            ResourceLoader.LoadSpriteFromSpriteReferenceAsync(
                ModContent.GetSpriteReference<AscendedUpgradesMod>("AscendedArrowBtn"),
                __instance.background);
        }
    }
}

/// <summary>
/// Make sure that an Ascended Upgrade goes through with the noise / sound, and also applies its effects
/// </summary>
[HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.UpgradeTower_Impl))]
internal static class UnityToSimulation_UpgradeTower_Impl
{
    internal static UpgradeModel? current;

    [HarmonyPostfix]
    private static void Postfix(UnityToSimulation __instance, ObjectId id, int pathIndex, int inputId)
    {
        if (current == null) return;

        var towerManager = __instance.simulation.towerManager;
        var tower = towerManager.GetTowerById(id);
        var cost = towerManager.GetTowerUpgradeCost(tower, pathIndex, 5, -1);


        if (tower.HasAscendedUpgrades() &&
            current.name.StartsWith(nameof(AscendedUpgrade)) &&
            cost < __instance.Simulation.GetCash(inputId))
        {
            towerManager.UpgradeTower(inputId, tower, tower.rootModel.Cast<TowerModel>(), pathIndex, cost, 1f);
#if DEBUG
            ModHelper.Msg<AscendedUpgradesMod>($"Doing ascended upgrade {pathIndex} with cost {cost}");
#endif
            var ascendedUpgrade = AscendedUpgrade.ByPath[pathIndex];
            ascendedUpgrade.Apply(tower);
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpgradeTower), typeof(UpgradeModel), typeof(int),
    typeof(float))]
internal static class TowerSelectionMenu_UpgradeTower
{
    [HarmonyPrefix]
    private static void Prefix(UpgradeModel upgrade)
    {
        UnityToSimulation_UpgradeTower_Impl.current = upgrade;
    }
}

/// <summary>
/// Undo possible upgrade cost changes if SharedTowerScaling is enabled
/// </summary>
[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.DestroyTower))]
internal static class TowerManager_DestroyTower
{
    [HarmonyPrefix]
    private static void Prefix(Tower tower)
    {
        if (tower.parentTowerId.Id != -1) return;

        foreach (var (ascendedUpgrade, stacks) in tower.GetAscendedStacks())
        {
            for (var i = 0; i < stacks; i++)
            {
                ascendedUpgrade.UnApply();
            }
        }
    }
}

/// <summary>
/// Change the ascended upgrade cost based on how many have been purchased for the tower
/// </summary>
[HarmonyPatch(typeof(Simulation), nameof(Simulation.GetSimulationBehaviorDiscount))]
internal static class Simulation_GetSimulationBehaviorDiscount
{
    [HarmonyPostfix]
    private static void Postfix(Tower tower, int path, ref float __result)
    {
        if (tower.HasAscendedUpgrades() && !AscendedUpgradesMod.SharedTowerScaling)
        {
            var mult = AscendedUpgradesMod.UpgradeCostIncrease / (float) AscendedUpgradesMod.BaseUpgradeCost;
            var stacks = tower.GetAscendedStacks();
            var amount = AscendedUpgradesMod.SharedUpgradeScaling
                ? stacks.Sum(pair => pair.Value)
                : stacks.Where(pair => pair.Key.Path == path).Select(pair => pair.Value).SingleOrDefault();
            __result -= amount * mult;
        }
    }
}