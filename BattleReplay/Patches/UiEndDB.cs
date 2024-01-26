using HarmonyLib;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(UiEndDB), "OkButtonPressed")]
    static class Patch_EndOkPressed
    {
        static bool Prefix(UiEndDB __instance, KAUIGenericDB ___mObjectivesEndDB)
        {
            if (Main.Replaying != null)
            {
                KAUI.RemoveExclusive(___mObjectivesEndDB);
                ___mObjectivesEndDB.SetVisibility(false);
                Main.Replaying = null;
                Traverse.Create(__instance).Method("ShowMainMenu").GetValue();
                return false;
            }
            if (Main.Recording != null)
            {
                if (Main.ReplaySaving == ReplaySaving.Ask)
                    Main.instance.Invoke("AskSaveReplay", 0.5f);
                else if (Main.ReplaySaving == ReplaySaving.AlwaysSave)
                    Main.instance.SaveReplay();
                else if (Main.ReplaySaving == ReplaySaving.NeverSave)
                    Main.instance.DiscardReplay();
            }
            return true;
        }
    }
}