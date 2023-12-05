using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppAssets.Scripts.Models.Towers.Upgrades;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.TowerSelectionMenu;
using Il2CppAssets.Scripts.Utils;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Api.Helpers;
using BTD_Mod_Helper.Extensions;

namespace AscendedUpgrades;

public abstract class AscendedUpgrade : NamedModContent
{
    protected abstract ModBuffIcon ModBuffIcon { get; }

    protected BuffIndicatorModel? BuffIndicatorModel => AscendedUpgradesMod.ShowBuffIndicators
        ? (InGame.instance.Exists()?.GetGameModel() ?? Game.instance.model)
        .buffIndicatorModels
        .First(model => model.name == $"BuffIndicatorModel_{ModBuffIcon.Id}-{ModBuffIcon.Icon}")
        : null;

    public static readonly Dictionary<int, string> IdByPath = new();
    public static readonly Dictionary<int, AscendedUpgrade> ByPath = new();
    public static readonly Dictionary<string, AscendedUpgrade> ById = new();

    public abstract int Path { get; }

    protected sealed override int Order => Path;
    protected virtual string Icon => Name;
    protected virtual SpriteReference IconReference => GetSpriteReferenceOrDefault(Icon);

    public override void Register()
    {
        var upgradeModel = new UpgradeModel(Id, AscendedUpgradesMod.BaseUpgradeCost, 0,
            IconReference, Path, 4, 0, "", "");

        Game.instance.model.AddUpgrade(upgradeModel);

        IdByPath[Path] = Id;
        ByPath[Path] = this;
        ById[Id] = this;
    }

    protected abstract BehaviorMutator CreateMutator(int stacks);

    public UpgradeModel GetUpgradeModel(GameModel? gameModel = null) =>
        (gameModel ?? InGame.instance.Exists()?.GetGameModel() ?? Game.instance.model).GetUpgrade(Id);

    public void Apply(Tower tower, int stacks, int delta)
    {
        tower.RemoveMutatorsById(Id);
        tower.AddMutatorIncludeSubTowers(CreateMutator(stacks));
        ChangeUpgradeCosts(stacks);
    }

    public void UnApply(int stacks)
    {
        ChangeUpgradeCosts(-stacks);
    }

    private void ChangeUpgradeCosts(int deltaStacks)
    {
        if (!AscendedUpgradesMod.SharedTowerScaling) return;

        var gameModel = InGame.instance.bridge.Model;
        var affected = AscendedUpgradesMod.SharedUpgradeScaling
            ? GetContent<AscendedUpgrade>()
            : new List<AscendedUpgrade> { this };

        foreach (var ascendedUpgrade in affected)
        {
            ascendedUpgrade.GetUpgradeModel(gameModel).cost +=
                CostHelper.CostForDifficulty(deltaStacks * AscendedUpgradesMod.IncreaseUpgradeCost, gameModel);
        }

        if (TowerSelectionMenu.instance.Exists(out var tsm) && tsm.upgradeButtons != null)
        {
            for (var i = 0; i < tsm.upgradeButtons.Count; i++)
            {
                var upgradeButton = tsm.upgradeButtons[i];
                if (upgradeButton != null)
                {
                    upgradeButton.UpdateCost();
                    upgradeButton.UpdateVisuals(i, false);
                }
            }
        }
    }

    public virtual int GetStacks(BehaviorMutator behaviorMutator) => behaviorMutator.priority;

    protected static float GetFactor(int stacks) => AscendedUpgradesMod.OpMultiplicativeScaling
        ? (float) Math.Pow(1 + AscendedUpgradesMod.UpgradeFactor, stacks) - 1f
        : AscendedUpgradesMod.UpgradeFactor * stacks;
}

public abstract class AscendedUpgrade<T> : AscendedUpgrade where T : ModBuffIcon
{
    protected override ModBuffIcon ModBuffIcon => GetInstance<T>();
}