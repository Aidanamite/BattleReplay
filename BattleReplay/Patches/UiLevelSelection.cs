using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(UiLevelSelection))]
    static class Patch_UiLevelSelection
    {
        static ConditionalWeakTable<UiLevelSelection, KAWidget> loadreplay = new ConditionalWeakTable<UiLevelSelection, KAWidget>();
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void Start(UiLevelSelection __instance, KAWidget ___mBtnPlay)
        {
            var n = __instance.DuplicateWidget(___mBtnPlay,___mBtnPlay.pAnchor.side);
            ___mBtnPlay.pParentWidget?.AddChild(n);
            n.SetText("Replays");
            n.SetVisibility(true);
            n.SetInteractive(true);
            var p = ___mBtnPlay.transform.localPosition;
            p.x = -p.x;
            n.transform.localPosition = p;
            loadreplay.Add(__instance, n);
        }
        [HarmonyPatch("OnClick")]
        [HarmonyPostfix]
        static void OnClick(UiLevelSelection __instance, KAWidget item)
        {
            if (loadreplay.TryGetValue(__instance, out var w) && w == item)
            {
                var old = Cursor.visible;
                Cursor.visible = true;
                if (NativeMethods.OpenFileDialog("Select replay file", Main.ReplayFolder, out var file, "Replay Files\0*.DTReplay\0All Files\0*.*\0"))
                    Main.LoadReplay(file);
                Cursor.visible = old;
            }
        }
    }
}