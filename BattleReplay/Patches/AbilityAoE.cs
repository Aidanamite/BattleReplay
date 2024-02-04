using HarmonyLib;
using SquadTactics;
using System;

namespace BattleReplay
{
    [HarmonyPatch(typeof(AbilityAoE))]
    static class Patch_AbilityAoE
    {
        [HarmonyPatch("Activate")]
        [HarmonyPrefix]
        static void Activate(Ability __instance, Character owner, Character target) => Patch_Ability.Activate(__instance, owner, target);

        [HarmonyPatch("Activate", MethodType.Enumerator)]
        [HarmonyPrefix]
        public static void ActivateEnumerator_Prefix() => Patch_Ability.ActivateEnumerator_Prefix();

        [HarmonyPatch("Activate", MethodType.Enumerator)]
        [HarmonyPostfix]
        public static void ActivateEnumerator_Postfix(bool __result) => Patch_Ability.ActivateEnumerator_Postfix(__result);

        [HarmonyPatch("Activate", MethodType.Enumerator)]
        [HarmonyFinalizer]
        public static void ActivateEnumerator_Finalizer(Exception __exception) => Patch_Ability.ActivateEnumerator_Finalizer(__exception);
    }
}
