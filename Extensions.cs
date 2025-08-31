using System.Collections.Generic;
using System.Linq;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using BTD_Mod_Helper.Api;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppSystem.Linq;

namespace AscendedUpgrades;

public static class Extensions
{
    public static bool ShowAscendedUpgrades(this TowerSelectionMenu tsm) =>
        tsm != null &&
        tsm.selectedTower != null &&
        (tsm.upgradeButtons.All(o =>
             o.upgradeButton.upgradeStatus == UpgradeButton.UpgradeStatus.None ||
             o.upgradeButton.upgrade?.IsAscended() == true) ||
         tsm.selectedTower.tower.GetAscendedStacks().Values.Sum() > 0);

    public static bool IsAscended(this UpgradeModel upgrade) => AscendedUpgrade.IdByPath.ContainsValue(upgrade.name);

    public static int AscendedStackCount(this BehaviorMutator behaviorMutator) =>
        AscendedUpgrade.ById.TryGetValue(behaviorMutator.id, out var upgrade)
            ? upgrade.GetStacks(behaviorMutator)
            : 0;

    public static Dictionary<AscendedUpgrade, int> GetAscendedStacks(this Tower tower) =>
        ModContent.GetContent<AscendedUpgrade>().ToDictionary(
            upgrade => upgrade,
            upgrade => tower.GetMutators().Cast<Il2CppSystem.Collections.Generic.IEnumerable<TimedMutator>>()
                .ToArray()
                .Where(mutator => mutator.mutator.id.Contains(upgrade.Id))
                .Select(mutator => mutator.mutator.AscendedStackCount())
                .SingleOrDefault(0)
        );
}