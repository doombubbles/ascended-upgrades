using System;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

namespace AscendedUpgrades;

public class AscendedForce : AscendedUpgrade<AscendedForceIcon>
{
    public override string Description =>
        "Infinitely Repeatable: Improved pierce, range, capacity, and range/pierce buffs.";

    public override int Path => 2;

    protected override BehaviorMutator CreateMutator(int stacks) =>
        new RangeSupport.MutatorTower(false, Id, 0, GetFactor(stacks), BuffIndicatorModel)
        {
            priority = stacks
        };
}

public class AscendedForceIcon : ModBuffIcon
{
    public override string Icon => nameof(AscendedForce);
    public override int MaxStackSize => 999;
    protected override int Order => 2;
}

[HarmonyPatch(typeof(RangeSupport.MutatorTower), nameof(RangeSupport.MutatorTower.Mutate))]
internal static class RangeSupport_MutatorTower_Mutate
{
    [HarmonyPrefix]
    private static bool Prefix(RangeSupport.MutatorTower __instance, Model model)
    {
        if (__instance.id == ModContent.GetInstance<AscendedForce>().Id)
        {
            var mult = 1 + __instance.multiplier;
            model.GetDescendants<ProjectileModel>().ForEach(projectileModel => projectileModel.pierce *= mult);
            model.GetDescendants<BankModel>().ForEach(bankModel => bankModel.capacity *= mult);
            model.GetDescendants<EatBloonModel>().ForEach(eatBloonModel => eatBloonModel.rbeCapacity *= mult);
            model.GetDescendants<RangeSupportModel>().ForEach(supportModel => supportModel.multiplier *= mult);
            model.GetDescendants<ActivatePierceSupportZoneModel>().ForEach(zoneModel =>
                zoneModel.pierceIncrease += (int) (zoneModel.pierceIncrease * __instance.multiplier));
            model.GetDescendants<PierceSupportModel>().ForEach(supportModel => supportModel.pierce *= mult);
            model.GetDescendants<ActivateRangeSupportZoneModel>().ForEach(zoneModel => zoneModel.multiplier *= mult);
            model.GetDescendants<CallToArmsModel>()
                .ForEach(zoneModel => zoneModel.multiplier *= (float) Math.Sqrt(mult));
            model.GetDescendants<PoplustSupportModel>()
                .ForEach(supportModel => supportModel.piercePercentIncrease *= mult);
            model.GetDescendants<OverclockModel>()
                .ForEach(overclockModel => overclockModel.villageRangeModifier *= mult);
            model.GetDescendants<OverclockPermanentModel>()
                .ForEach(permanentModel => permanentModel.villageRangeModifier *= mult);
        }

        return true;
    }
}