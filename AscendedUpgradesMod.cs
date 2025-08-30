using System;
using System.Collections.Generic;
using System.Linq;
using MelonLoader;
using BTD_Mod_Helper;
using AscendedUpgrades;
using Il2CppAssets.Scripts.Models.Profile;
using Il2CppAssets.Scripts.Simulation.Towers;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

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
        icon = VanillaSprites.CoinIcon
    };

    public static readonly ModSettingInt IncreaseUpgradeCost = new(500)
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

    public static readonly ModSettingBool OpMultiplicativeScaling = new(false)
    {
        description =
            "Brings back the old behavior of multiplicative upgrade effect scaling that was very OP given that " +
            "the cost scaling was additive.",
        icon = VanillaSprites.BiohackIconAA
    };

    public static readonly ModSettingInt MaxUpgradePips = new(25)
    {
        displayName = "Max Upgrade Pips",
        description = "The max number of total blue upgrade that shoul appear as you purchase Ascended Upgrades",
        icon = ModContent.GetTextureGUID<AscendedUpgradesMod>("AscendedPip"),
        min = 0,
        max = 100,
        slider = true
    };

    public static readonly ModSettingBool ShowBuffIndicators = new(true)
    {
        displayName = "Show Buff Indicators",
        description = "Whether to show the number of Ascended Upgrades purchased as buffs on the tower",
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
                ascendedUpgrade.Apply(tower, stacks);
            }
        }
    }

    private static Dictionary<AscendedUpgrade, int>? clipboard;

    public override object Call(string operation, params object[] parameters)
    {
        switch (operation)
        {
            case "OnTowerCopied" when parameters.CheckTypes(out Tower towerCopied):
                clipboard = towerCopied.GetAscendedStacks();
                break;
            case "OnTowerPasted" when parameters.CheckTypes(out Tower towerPasted):
                if (clipboard != null)
                {
                    foreach (var (ascendedUpgrade, stacks) in clipboard)
                    {
                        ascendedUpgrade.Apply(towerPasted, stacks);
                    }
                }
                break;
            case "OnClipboardCleared":
                clipboard = null;
                break;
            case "ModifyClipboardCost" when parameters.CheckTypes(out Tower tower):
                var total = 0;
                var count = 0;
                foreach (var (_, stacks) in tower.GetAscendedStacks())
                {
                    if (SharedUpgradeScaling)
                    {
                        for (var i = 0; i < stacks; i++)
                        {
                            total += CostHelper.CostForDifficulty(BaseUpgradeCost, InGame.instance);
                            total += (count++ + i) * CostHelper.CostForDifficulty(IncreaseUpgradeCost, InGame.instance);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < stacks; i++)
                        {
                            total += CostHelper.CostForDifficulty(BaseUpgradeCost, InGame.instance);
                            total += i * CostHelper.CostForDifficulty(IncreaseUpgradeCost, InGame.instance);
                        }
                    }
                }

                return total;
        }

        return base.Call(operation, parameters);
    }
}