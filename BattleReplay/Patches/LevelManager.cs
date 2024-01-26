using HarmonyLib;

namespace BattleReplay
{
    [HarmonyPatch(typeof(SquadTactics.LevelManager), "SetLevelPlaying")]
    static class Patch_SetSquadLevel
    {
        static void Postfix(int inRealmIndex, int inLevelIndex)
        {
            if (Main.Replaying == null)
                Main.Recording = new GameRecording() { RealmIndex = inRealmIndex, LevelIndex = inLevelIndex };
        }
    }
}