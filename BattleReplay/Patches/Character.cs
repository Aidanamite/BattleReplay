using HarmonyLib;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(Character), "DoMovement")]
    static class Patch_StartCharacterMovement
    {
        static void Prefix(Character __instance, Node targetNode)
        {
            if (Main.Recording != null && __instance.CanMove())
                Main.Recording.RecordedActions.Add(new RecordedEvent() { Type = EventType.Movement, Character = __instance.pCharacterData.pSpawnOrder, TargetX = targetNode._XPosition, TargetY = targetNode._YPosition });
        }
    }
}