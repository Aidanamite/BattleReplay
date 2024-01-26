using HarmonyLib;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using SquadTactics;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace BattleReplay
{
    [HarmonyPatch(typeof(UiSquadTacticsHUD))]
    static class Patch_UiSquadTacticsHUD
    {
        static ConditionalWeakTable<UiSquadTacticsHUD, KAWidget> autoplay = new ConditionalWeakTable<UiSquadTacticsHUD, KAWidget>();
        [HarmonyPatch("Initialize")]
        [HarmonyPostfix]
        static void Initialize(UiSquadTacticsHUD __instance, KAWidget ___mBtnFastForward, KAWidget ___mBtnEndTurn)
        {
            if (Main.Replaying == null)
                return;
            var n = __instance.DuplicateWidget(___mBtnFastForward, UIAnchor.Side.BottomRight);
            ___mBtnFastForward.pParentWidget?.AddChild(n);
            var lbl = n.GetComponentsInChildren<UILabel>(true).First(x => !string.IsNullOrWhiteSpace(x.text) || x.textID != 0);
            lbl.textID = 0;
            lbl.text = "AUTOPLAY";
            lbl.ResetEnglishText();
            var p = ___mBtnEndTurn.transform.position;
            var p2 = ___mBtnFastForward.transform.position;
            n.transform.position = p2 + p2 - p;
            autoplay.Add(__instance, n);
            //n.pos
            lbl = ___mBtnEndTurn.GetComponentsInChildren<UILabel>(true).First(x => !string.IsNullOrWhiteSpace(x.text) || x.textID != 0);
            lbl.textID = 0;
            lbl.text = "NEXT MOVE";
            lbl.ResetEnglishText();
        }

        [HarmonyPatch("OnClick")]
        [HarmonyPostfix]
        static void OnClick(UiSquadTacticsHUD __instance, KAWidget inWidget, KAWidget ___mBtnEndTurn)
        {
            if (autoplay.TryGetValue(__instance,out var w) && w == inWidget)
            {
                var extra = ExtendedGameManager.extra.GetOrCreateValue(GameManager.pInstance);
                extra.AutoPlay = !extra.AutoPlay;
                Debug.Log("AutoPlay = " + extra.AutoPlay);
            }
        }

        [HarmonyPatch("VisiblePlayerTurnOnlyUIs")]
        [HarmonyPrefix]
        static void VisiblePlayerTurnOnlyUIs(ref bool visible)
        {
            if (Main.Replaying != null)
                visible = true;
        }

        [HarmonyPatch("OnClick")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> OnClick(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.operand is MethodInfo m && m.Name == "EndPlayerTurn");
            code[ind] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UiSquadTacticsHUDPatchMethods), nameof(UiSquadTacticsHUDPatchMethods.TryEndTurn)));
            return code;
        }
    }

    [HarmonyPatch(typeof(UiSquadTacticsHUD), "OnClick")]
    static class UiSquadTacticsHUDPatchMethods
    {
        public static void TryEndTurn(GameManager instance)
        {
            if (Main.Recording != null)
                Main.Recording.RecordedActions.Add(new RecordedEvent() { Type = EventType.EndOfTurn, Team = Character.Team.PLAYER });
            if (Main.Replaying != null)
            {
                ExtendedGameManager.extra.GetOrCreateValue(GameManager.pInstance).DoSingleTurn = true;
                return;
            }
            instance.EndPlayerTurn();
        }
    }
}