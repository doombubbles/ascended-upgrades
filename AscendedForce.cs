using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;

namespace AscendedUpgrades;

public class AscendedForce : AscendedUpgrade<AscendedForceIcon>
{
    public override string Description => "Infinitely Repeatable: Increased pierce and range";

    public override int Path => 2;

    protected override BehaviorMutator CreateMutator() =>
        new RangeSupport.MutatorTower(false, Id, 0, AscendedUpgradesMod.UpgradeFactor, BuffIndicatorModel);
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
            model.GetDescendants<ProjectileModel>().ForEach(projectileModel =>
            {
                if (projectileModel.pierce > 0)
                {
                    projectileModel.pierce *= mult;
                }
            });
            
        }

        return true;
    }
}