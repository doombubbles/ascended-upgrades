using System;
using System.Linq;
using MelonLoader;
using BTD_Mod_Helper;
using AscendedUpgrades;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Towers;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;

[assembly: MelonInfo(typeof(AscendedUpgradesMod), ModHelperData.Name, ModHelperData.Version, ModHelperData.RepoOwner)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AscendedUpgrades;

public class AscendedUpgradesMod : BloonsTD6Mod
{
    public static readonly ModSettingInt BaseUpgradeCost = new(5000)
    {
        description = "The starting price of Ascended Upgrades (medium difficulty).",
        min = 1000,
        onSave = cost => ModContent.GetContent<AscendedUpgrade>()
            .ForEach(upgrade => upgrade.GetUpgradeModel().cost = (int) cost),
        icon = VanillaSprites.CoinIcon // VanillaSprites.MoneyBag
    };

    public static readonly ModSettingInt UpgradeCostIncrease = new(1000)
    {
        description =
            "How much the cost increases for further Ascended Upgrades each time you buy one (medium difficulty).",
        min = 0,
        icon = VanillaSprites.ThriveStonksIcon
    };

    public static readonly ModSettingDouble UpgradeFactor = new(.1)
    {
        description = "How much Ascended Upgrades buff by. Default of .1 is buffing each listed stat by 10%.",
        slider = true,
        min = .01,
        max = .25,
        icon = VanillaSprites.MonkeyBoostIcon
    };

    public static readonly ModSettingBool ShowUpgradePips = new(true)
    {
        displayName = "Show Upgrade Pips",
        description = "Whether the grid of blue upgrade pips appears as you purchase Ascended Upgrades",
        icon = ModContent.GetTextureGUID<AscendedUpgradesMod>("AscendedPip")
    };
    
    public static readonly ModSettingBool ShowBuffIndicators = new(true)
    {
        displayName = "Show Buff Indicators",
        description = "",
        icon = VanillaSprites.BuffIconComeOnEverybodyRate
    };


    public static readonly ModSettingBool SharedUpgradeScaling = new(false)
    {
        displayName = "Cost Scaling Across Upgrades",
        description = "Whether the 3 Ascended Upgrades scale in cost all together vs individually.",
        button = true,
        disabledText = "Individual",
        disabledButton = VanillaSprites.BlueBtnLong,
        enabledText = "Together",
        enabledButton = VanillaSprites.YellowBtnLong
    };

    public static readonly ModSettingBool SharedTowerScaling = new(false)
    {
        displayName = "Cost Scaling Across Towers",
        description =
            "Whether Ascended Upgrade cost scaling happens globally across all towers or is personal for each tower.",
        button = true,
        disabledText = "Personal",
        disabledButton = VanillaSprites.BlueBtnLong,
        enabledText = "Global",
        enabledButton = VanillaSprites.YellowBtnLong
    };

    public override void OnProfileLoaded(ProfileModel profile)
    {
        foreach (var ascendedUpgrade in ModContent.GetContent<AscendedUpgrade>())
        {
            profile.acquiredUpgrades.AddIfNotPresent(ascendedUpgrade.Id);
        }
    }

    public override void PreCleanProfile(ProfileModel profile)
    {
        var ids = ModContent.GetContent<AscendedUpgrade>().Select(upgrade => upgrade.Id).ToArray();
        profile.acquiredUpgrades.RemoveWhere(new Func<string, bool>(s => ids.Contains(s)));
    }

    public override void PostCleanProfile(ProfileModel profile) => OnProfileLoaded(profile);

    public override void OnTowerSaved(Tower tower, TowerSaveDataModel saveData)
    {
        foreach (var (ascendedUpgrade, stacks) in tower.GetAscendedStacks())
        {
            if (stacks > 0)
            {
                saveData.metaData[ascendedUpgrade.Id] = stacks.ToString();
            }
        }
    }

    public override void OnTowerLoaded(Tower tower, TowerSaveDataModel saveData)
    {
        foreach (var ascendedUpgrade in ModContent.GetContent<AscendedUpgrade>())
        {
            if (saveData.metaData.ContainsKey(ascendedUpgrade.Id) &&
                int.TryParse(saveData.metaData[ascendedUpgrade.Id], out var stacks))
            {
                for (var i = 0; i < stacks; i++)
                {
                    ascendedUpgrade.Apply(tower);
                }
            }
        }
    }

    public override void OnNewGameModel(GameModel gameModel)
    {
        foreach (var towerModel in gameModel.towers.Where(model => !model.IsHero()))
        {
            var upgradeModels = towerModel.upgrades.Select(model => gameModel.GetUpgrade(model.upgrade));
            if (towerModel.tier == 5 && upgradeModels.All(model => model.IsParagon)) // Either none, or all paragon
            {
                towerModel.upgrades = ModContent.GetContent<AscendedUpgrade>()
                    .Select(upgrade => new UpgradePathModel(upgrade.Id, towerModel.name))
                    .ToIl2CppReferenceArray();
            }
        }
    }
}