using HarmonyLib;

namespace BattleReplay
{
    [HarmonyPatch(typeof(RaisedPetData), "GetByID")]
    static class Patch_GetPetData
    {
        static void Postfix(ref RaisedPetData __result, int raisedPetID)
        {
            if (Main.Recording != null)
                Main.Recording.PetAppearances[raisedPetID] = __result.DeepMemberwiseClone();
            if (Main.Replaying != null)
                __result = Main.Replaying.PetAppearances[raisedPetID];
        }
    }
}