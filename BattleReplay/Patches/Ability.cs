using HarmonyLib;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(Ability))]
    static class Patch_StartCharacterAbility
    {
        public static Randoms NextReplay;
        [HarmonyPatch("Activate")]
        [HarmonyPrefix]
        static void Activate(Ability __instance, Character owner, Character target)
        {
            if (__instance.pCurrentCooldown <= 0)
            {
                if (Main.Recording != null)
                    Main.Recording.RecordedActions.Add(new RecordedEvent() { Type = EventType.Ability, Character = owner.pCharacterData.pSpawnOrder, Target = target.pCharacterData.pSpawnOrder, Ability = owner.pAbilities.IndexOf(__instance), Randoms = ActiveRecording = new Randoms() });
                else if (NextReplay != null)
                {
                    ActiveReplay = NextReplay;
                    NextReplay = null;
                }
            }
        }

        public static Randoms ActiveRecording;
        public static Randoms ActiveReplay;
        [HarmonyPatch("Activate", MethodType.Enumerator)]
        [HarmonyPrefix]
        static void ActivateEnumerator_Prefix()
        {
            Patch_Randomizer.Recording = ActiveRecording;
            Patch_Randomizer.Replaying = ActiveReplay;
        }
        [HarmonyPatch("Activate", MethodType.Enumerator)]
        [HarmonyPostfix]
        static void ActivateEnumerator_Postfix(bool __result)
        {
            if (!__result)
            {
                ActiveRecording = null;
                ActiveReplay = null;
            }
        }
        [HarmonyPatch("Activate", MethodType.Enumerator)]
        [HarmonyFinalizer]
        static void ActivateEnumerator_Finalizer()
        {
            Patch_Randomizer.Recording = null;
            Patch_Randomizer.Replaying = null;
        }
    }
}