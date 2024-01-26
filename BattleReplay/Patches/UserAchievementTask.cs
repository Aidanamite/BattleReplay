using HarmonyLib;

namespace BattleReplay
{
    [HarmonyPatch(typeof(UserAchievementTask), "Set",typeof(AchievementTask[]))]
    static class Patch_SetAchievements
    {
        static bool Prefix() => Main.Replaying == null;
    }
}