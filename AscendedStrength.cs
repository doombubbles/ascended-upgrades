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
using Il2CppAssets.Scripts.Models.SimulationBehaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;

namespace AscendedUpgrades;

public class AscendedStrength : AscendedUpgrade<AscendedStrengthIcon>
{
    public override string Description =>
        "Infinitely Repeatable: Improved damage, boss damage, cash generated, and damage buffs.";

    public override int Path => 0;

    protected override BehaviorMutator CreateMutator(int stacks) =>
        new DamageSupport.MutatorTower(GetFactor(stacks), false, Id, BuffIndicatorModel)
        {
            priority = stacks
        };
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
                if (!proj.HasBehavior<DamageModel>(out var damageModel) || !(damageModel.damage > 0)) return;

                damageModel.damage *= mult;

                var name = $"DamageModifierForTagModel_{__instance.id}";
                proj.AddBehavior(new DamageModifierForTagModel(name, BloonTag.Boss, mult, 0, false, true));
                proj.hasDamageModifiers = true;
            });

            model.GetDescendants<CashModel>().ForEach(cashModel => cashModel.bonusMultiplier += __instance.increase);
            model.GetDescendants<BonusCashPerRoundModel>().ForEach(roundModel => roundModel.baseCash *= mult);
            model.GetDescendants<EatBloonModel>().ForEach(eatBloonModel => eatBloonModel.rbeCashMultiplier *= mult);
            model.GetDescendants<DamageSupportModel>().ForEach(supportModel => supportModel.increase *= mult);
            model.GetDescendants<ActivateTowerDamageSupportZoneModel>()
                .ForEach(zoneModel => zoneModel.damageIncrease *= mult);

            __result = true;
            return false;
        }

        return true;
    }
}