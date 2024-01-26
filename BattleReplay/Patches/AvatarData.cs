using HarmonyLib;

namespace BattleReplay
{
    [HarmonyPatch(typeof(AvatarData), "CreateFilteredData")]
    static class Patch_GetAvatarData
    {
        static void Postfix(ref AvatarData __result)
        {
            if (Main.Recording != null)
            {
                Main.Recording.PlayerAppearance = __result.DeepMemberwiseClone();
                Main.Recording.PlayerAppearance.DisplayName = AvatarData.pInstance.DisplayName;
            }
            if (Main.Replaying != null)
                __result = Main.Replaying.PlayerAppearance;
        }
    }
}