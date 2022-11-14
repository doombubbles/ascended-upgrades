using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Models.Towers.Upgrades;
using Assets.Scripts.Simulation.Towers;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Extensions;

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
        !tower.CanUpgradeToParagon(true) &&
        tower.towerModel.upgrades.Any(upgradePathModel => upgradePathModel.IsAscended());

    public static bool IsAscended(this UpgradePathModel model) => AscendedUpgrade.IdByPath.ContainsValue(model.upgrade);
    
    public static Dictionary<AscendedUpgrade, int> GetAscendedStacks(this Tower tower) =>
        ModContent.GetContent<AscendedUpgrade>()
            .ToDictionary(
                upgrade => upgrade,
                upgrade => tower.GetMutators().Where(mutator => mutator.mutator.id.Contains(upgrade.Id)).Count
            );
}