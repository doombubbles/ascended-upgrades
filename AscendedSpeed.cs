using System;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Weapons.Behaviors;
using Il2CppAssets.Scripts.Simulation.Objects;

namespace AscendedUpgrades;

public class AscendedSpeed : AscendedUpgrade<AscendedSpeedIcon>
{
    public override string Description =>
        "Infinitely Repeatable: Improved attack speed, production speed, and attack speed buffs.";

    public override int Path => 1;

    protected override BehaviorMutator CreateMutator(int stacks) => new RateSupportModel.RateSupportMutator(false, Id,
        1 / (1 + GetFactor(stacks)), stacks, BuffIndicatorModel);
}

public class AscendedSpeedIcon : ModBuffIcon
{
    public override string Icon => nameof(AscendedSpeed);
    public override int MaxStackSize => 999;
    protected override int Order => 1;
}

[HarmonyPatch(typeof(RateSupportModel.RateSupportMutator), nameof(RateSupportModel.RateSupportMutator.Mutate))]
internal static class RateMutator_Mutate
{
    [HarmonyPrefix]
    private static bool Prefix(RateSupportModel.RateSupportMutator __instance, Model model)
    {
        if (__instance.id == ModContent.GetInstance<AscendedSpeed>().Id)
        {
            model.GetDescendants<EmissionsPerRoundFilterModel>()
                .ForEach(filter => filter.count = (int) Math.Ceiling(filter.count / __instance.multiplier));
            model.GetDescendants<RateSupportModel>()
                .ForEach(supportModel => supportModel.multiplier *= __instance.multiplier);
            model.GetDescendants<ActivateRateSupportZoneModel>()
                .ForEach(zoneModel => zoneModel.rateModifier *= __instance.multiplier);
            model.GetDescendants<CallToArmsModel>()
                .ForEach(zoneModel => zoneModel.multiplier *= (float) Math.Sqrt(1 / __instance.multiplier));
            model.GetDescendants<PoplustSupportModel>()
                .ForEach(supportModel => supportModel.ratePercentIncrease /= __instance.multiplier);
            model.GetDescendants<OverclockModel>()
                .ForEach(overclockModel => overclockModel.rateModifier *= __instance.multiplier);
            model.GetDescendants<OverclockPermanentModel>()
                .ForEach(permanentModel => permanentModel.rateModifier *= __instance.multiplier);
        }

        return true;
    }
}