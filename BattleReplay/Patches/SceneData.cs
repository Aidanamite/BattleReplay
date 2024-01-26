using HarmonyLib;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(SquadTactics.SceneData), "GetInanimateSpawns")]
    static class Patch_GetInanimateSpawns
    {
        static void Postfix()
        {
            var extra = ExtendedGameManager.extra.GetOrCreateValue(GameManager.pInstance);
            if (Patch_Randomizer.Recording != null)
            {
                extra.WithheldRec = Patch_Randomizer.Recording;
                Patch_Randomizer.Recording = null;
                extra.Withhold = SquadTactics.LevelManager.pInstance.GetSquadList().Count;
            }
            if (Patch_Randomizer.Replaying != null)
            {
                extra.WithheldRep = Patch_Randomizer.Replaying;
                Patch_Randomizer.Replaying = null;
                extra.Withhold = SquadTactics.LevelManager.pInstance.GetSquadList().Count;
            }
        }
    }
}