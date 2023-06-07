using System.Collections.Generic;
using System.Linq;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppSystem.Linq;

namespace AscendedUpgrades;

public static class Extensions
{
    /// <summary>
    /// For the moment, just don't show ascended upgrades if the tower is able to become a paragon
    /// </summary>
    /// <param name="tower"></param>
    /// <returns></returns>
    public static bool HasAscendedUpgrades(this Tower tower) =>
        tower.towerModel.tier == 5 &&
        tower.towerModel.upgrades.Any(upgradePathModel => upgradePathModel.IsAscended()) &&
        !tower.CanUpgradeToParagon(true, 3);

    public static bool IsAscended(this UpgradePathModel model) => AscendedUpgrade.IdByPath.ContainsValue(model.upgrade);

    public static Dictionary<AscendedUpgrade, int> GetAscendedStacks(this Tower tower) =>
        ModContent.GetContent<AscendedUpgrade>()
            .ToDictionary(
                upgrade => upgrade,
                upgrade => tower.GetMutators().Cast<Il2CppSystem.Collections.Generic.IEnumerable<TimedMutator>>()
                    .ToArray().Count(mutator => mutator.mutator.id.Contains(upgrade.Id))
            );
}