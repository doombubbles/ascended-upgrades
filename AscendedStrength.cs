using System;
using System.Linq;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Api.Enums;
using BTD_Mod_Helper.Extensions;
using HarmonyLib;

namespace AscendedUpgrades;

public class AscendedStrength : AscendedUpgrade<AscendedStrengthIcon>
{
    public override string Description => "Infinitely Repeatable: Increased damage and boss damage";
    public override int Path => 0;

    protected override BehaviorMutator CreateMutator() =>
        new DamageSupport.MutatorTower(AscendedUpgradesMod.UpgradeFactor, false, Id, BuffIndicatorModel);
}

public class AscendedStrengthIcon : ModBuffIcon
{
    public override string Icon => nameof(AscendedStrength);
    public override int MaxStackSize => 999;
    protected override int Order => 0;
}

[HarmonyPatch(typeof(DamageSupport.MutatorTower), nameof(DamageSupport.MutatorTower.Mutate))]
internal static class DamageSupport_MutatorTower_Mutate
{
    [HarmonyPrefix]
    private static bool Prefix(DamageSupport.MutatorTower __instance, Model model, ref bool __result)
    {
        if (__instance.id == ModContent.GetInstance<AscendedStrength>().Id)
        {
            var mult = 1 + __instance.increase;
            model.GetDescendants<ProjectileModel>().ForEach(proj =>
            {
                if (proj.HasBehavior<DamageModel>(out var damageModel) && damageModel.damage > 0)
                {
                    damageModel.damage *= mult;

                    foreach (var tag in new[] { BloonTag.Boss, BloonTag.Elite })
                    {
                        var name = $"DamageModifierForTagModel_{__instance.id}_{tag}";
                        if (proj.behaviors.FirstOrDefault(b => b.name == name)
                            .Is(out DamageModifierForTagModel modifier))
                        {
                            modifier.damageMultiplier *= mult;
                        }
                        else
                        {
                            proj.AddBehavior(new DamageModifierForTagModel(name, tag, mult, 0, false, true));
                        }
                    }
                }
            });

            __result = true;
            return false;
        }

        return true;
    }
}