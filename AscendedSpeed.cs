using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;

namespace AscendedUpgrades;

public class AscendedSpeed : AscendedUpgrade<AscendedSpeedIcon>
{
    public override string Description => "Infinitely Repeatable: Increased attack speed and projectile speed";

    public override int Path => 1;

    protected override BehaviorMutator CreateMutator() => new RateSupportModel.RateSupportMutator(false, Id,
        1 / (1 + AscendedUpgradesMod.UpgradeFactor), 999, BuffIndicatorModel);
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
            /*
            var mult = 1 / __instance.multiplier;
            model.GetDescendants<TravelStraitModel>().ForEach(straitModel => straitModel.Speed *= mult);
            */
        }

        return true;
    }
}