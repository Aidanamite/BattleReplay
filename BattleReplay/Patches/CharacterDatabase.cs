using HarmonyLib;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(CharacterDatabase), "GetCharacter",typeof(string),typeof(int),typeof(string))]
    static class Patch_GetCharacter
    {
        static bool Prefix(string charName, ref CharacterData __result)
        {
            if (GameManager.pInstance)
            {
                var extra = ExtendedGameManager.extra.GetOrCreateValue(GameManager.pInstance);
                if (extra.Withhold == 0)
                {
                    if (extra.WithheldRec != null)
                    {
                        Patch_Randomizer.Recording = extra.WithheldRec;
                        extra.WithheldRec = null;
                    }
                    if (extra.WithheldRep != null)
                    {
                        Patch_Randomizer.Replaying = extra.WithheldRep;
                        extra.WithheldRep = null;
                    }
                }
                else
                    extra.Withhold--;
            }
            if (Main.Replaying == null || charName != Main.CustomUnitName)
                return true;
            __result = new CharacterData(null);
            return false;
        }
    }
}