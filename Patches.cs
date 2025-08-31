using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;

namespace AscendedUpgrades;

/// <summary>
/// Allow ascended upgrades to show up in all 3 paths
/// </summary>
[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.IsUpgradePathClosed))]
internal class TowerSelectionMenu_IsUpgradePathClosed
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    internal static bool Prefix(TowerSelectionMenu __instance, int path, ref bool __result)
    {
        if (path <= 2 && __instance.ShowAscendedUpgrades())
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
        Setup(__instance, true);
    }

    private static void Setup(UpgradeObject __instance, bool retry)
    {
        if (retry) TaskScheduler.ScheduleTask(() => Setup(__instance, false));

        var ascendedPips = __instance.transform.GetComponentInChildren<AscendedPips>();
        if (ascendedPips == null)
        {
            ascendedPips = AscendedPips.Create(__instance);
        }

        if (!__instance.towerSelectionMenu.ShowAscendedUpgrades() || __instance.path >= 3)
        {
            if (!retry)
            {
                ascendedPips.SetAmount(0);
            }
            return;
        }

        var gameModel = InGame.instance.bridge.Model;
        var upgradeId = AscendedUpgrade.IdByPath[__instance.path];
        __instance.upgradeButton.SetUpgradeModel(gameModel.GetUpgrade(upgradeId));
        var tower = __instance.towerSelectionMenu.selectedTower.tower;
        ascendedPips.SetAmount(Math.Min(
            tower.GetAscendedStacks().First(pair => pair.Key.Path == __instance.path).Value,
            AscendedUpgradesMod.MaxUpgradePips));

        __instance.UpdateVisuals(__instance.path, false);
    }
}

/// <summary>
/// Fixes a minor visual glitch where the wrong info can appear for one frame
/// </summary>
[HarmonyPatch(typeof(UpgradeObject), nameof(UpgradeObject.IncreaseTier))]
internal static class UpgradeObject_IncreaseTier
{
    [HarmonyPrefix]
    private static bool Prefix(UpgradeObject __instance) =>
        (__instance.towerSelectionMenu?.selectedTower?.tower?.GetAscendedStacks().Values.Sum() ?? 0) == 0;
}

/// <summary>
/// Change the look of the upgrade background for ascended upgrades
/// </summary>
[HarmonyPatch]
internal static class UpgradeButton_Visuals
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(UpgradeButton), nameof(UpgradeButton.UpdateVisuals));
        yield return AccessTools.Method(typeof(UpgradeButton), nameof(UpgradeButton.LoadBackground));
        yield return AccessTools.Method(typeof(UpgradeButton), nameof(UpgradeButton.CheckCash));
    }

    [HarmonyPostfix]
    private static void Postfix(UpgradeButton __instance)
    {
        var upgradeId = __instance.upgrade?.name ?? "";
        if (AscendedUpgrade.IdByPath.ContainsValue(upgradeId) &&
            __instance.upgradeStatus == UpgradeButton.UpgradeStatus.Purchasable)
        {
            __instance.background.SetSprite(ModContent.GetSprite<AscendedUpgradesMod>("AscendedArrowBtn"));
        }
    }
}

[HarmonyPatch(typeof(TowerSelectionMenu), nameof(TowerSelectionMenu.UpgradeTower), typeof(UpgradeModel), typeof(int),
    typeof(float), typeof(double))]
internal static class TowerSelectionMenu_UpgradeTower
{
    [HarmonyPrefix]
    private static void Prefix(UpgradeModel upgrade)
    {
        UnityToSimulation_UpgradeTower_Impl.current = upgrade;
        UnityToSimulation_UpgradeTower_Impl.cash = InGame.instance.GetCash();
    }
}

/// <summary>
/// Make sure that an Ascended Upgrade goes through with the noise / sound, and also applies its effects
/// </summary>
[HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.UpgradeTower_Impl))]
internal static class UnityToSimulation_UpgradeTower_Impl
{
    internal static UpgradeModel? current;
    internal static double cash;

    [HarmonyPostfix]
    private static bool Prefix(UnityToSimulation __instance, ObjectId id, int callbackId, int pathIndex, int inputId)
    {
        if (current == null || pathIndex >= 3 || !current.name.StartsWith(nameof(AscendedUpgrade))) return true;

        var action = __instance.UnregisterCallback(callbackId, inputId);

        var towerManager = __instance.simulation.towerManager;
        var tower = towerManager.GetTowerById(id);

        var cost = towerManager.GetTowerUpgradeCost(tower, pathIndex, 5);

        towerManager.UpgradeTower(inputId, tower, tower.rootModel.Cast<TowerModel>(), pathIndex, cost);
        InGame.instance.SetCash(cash - cost);

#if DEBUG
        ModHelper.Msg<AscendedUpgradesMod>($"Doing ascended upgrade {pathIndex} with cost {cost}");
#endif

        var ascendedUpgrade = AscendedUpgrade.ByPath[pathIndex];
        var stacks = tower.GetAscendedStacks()[ascendedUpgrade];
        ascendedUpgrade.Apply(tower, stacks + 1);

        if (action != null)
        {
            action.Invoke(true);
        }
        UpgradeButton.upgradeCashOffset = 0;
        current = null;

        return false;
    }
}

/// <summary>
/// Get upgrade cost for Ascended Upgrades
/// </summary>
[HarmonyPatch(typeof(TowerManager), nameof(TowerManager.GetTowerUpgradeCost))]
internal static class TowerManager_GetTowerUpgradeCost
{
    [HarmonyPrefix]
    internal static bool Prefix(Tower tower, int path, ref float __result)
    {
        if (TowerSelectionMenu.instance == null ||
            TowerSelectionMenu.instance.selectedTower?.tower != tower ||
            !TowerSelectionMenu.instance.ShowAscendedUpgrades()) return true;

        __result = CostHelper.CostForDifficulty(AscendedUpgradesMod.BaseUpgradeCost, InGame.instance);

        var stacks = tower.GetAscendedStacks();
        var amount = AscendedUpgradesMod.SharedUpgradeScaling
                         ? stacks.Sum(pair => pair.Value)
                         : stacks.Where(pair => pair.Key.Path == path).Select(pair => pair.Value).SingleOrDefault();

        __result += amount * CostHelper.CostForDifficulty(AscendedUpgradesMod.IncreaseUpgradeCost, InGame.instance);

        return false;
    }
}

[HarmonyPatch(typeof(BuffIcon), nameof(BuffIcon.Show))]
internal static class BuffIcon_Show
{
    [HarmonyPrefix]
    private static void Prefix(BuffQuery buff)
    {
        if (!buff.buffIndicator.name.Contains(nameof(AscendedUpgrades))) return;

        buff.availableBuffCount = buff.timedMutator.mutator.AscendedStackCount();
    }
}