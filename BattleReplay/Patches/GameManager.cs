using HarmonyLib;
using SquadTactics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System.Reflection.Emit;

namespace BattleReplay
{
    [HarmonyPatch(typeof(GameManager))]
    static class Patch_GameManager
    {
        public static bool calling = false;
        [HarmonyPatch("Start", MethodType.Enumerator)]
        [HarmonyPrefix]
        static void Start_Prefix()
        {
            calling = true;
            if (Main.Recording != null)
                Patch_Randomizer.Recording = Main.Recording.SetupRandoms;
            if (Main.Replaying != null)
                Patch_Randomizer.Replaying = Main.Replaying.SetupRandoms;
        }

        [HarmonyPatch("Start", MethodType.Enumerator)]
        [HarmonyFinalizer]
        static void Start_Finalizer(Exception __exception, bool __result)
        {
            calling = false;
            Patch_Randomizer.Recording = null;
            Patch_Randomizer.Replaying = null;
            if (__exception != null)
            {
                UICursorManager.pCursorManager.SetVisibility(true);
                GameUtilities.DisplayOKMessage("PfKAUIGenericDB", "An error occured while loading the replay", Main.instance.gameObject, "ReturnToMainMenu");
            }
            else if (!__result && Main.Replaying != null)
                GameManager.pInstance.StartCoroutine(ExtendedGameManager.DoTurns(GameManager.pInstance));
        }

        [HarmonyPatch("Restart")]
        [HarmonyPrefix]
        static void Restart()
        {
            if (Main.Replaying != null)
                Main.RestartLastReplay();
            if (Main.Recording != null)
                Main.Recording = new GameRecording() { RealmIndex = Main.Recording.RealmIndex, LevelIndex = Main.Recording.LevelIndex };
        }

        [HarmonyPatch("SetUnitTeams")]
        [HarmonyPostfix]
        static void SetUnitTeams(GameManager __instance, AvatarData.InstanceInfo __result)
        {
            if (Main.Replaying != null)
            {
                AvatarData.Load(__result, __result.mInstance);
                __instance.StartCoroutine(GameManagerPatchMethods.AfterCharLoad(__instance, __instance._ActiveUnits.Find(x => x.pCharacterData.pIsAvatar()), __result));
            }
        }

        public static int spawnOrder;
        [HarmonyPatch("SpawnEnemy")]
        [HarmonyPrefix]
        static void SpawnEnemy(Character enemy) => enemy.pCharacterData.pSpawnOrder = spawnOrder++;

        [HarmonyPatch("SetInteractiveHUD")]
        [HarmonyPrefix]
        static void SetInteractiveHUD(ref bool enable)
        {
            if (Main.Replaying != null)
                enable = true;
        }

        [HarmonyPatch("StartTurn")]
        [HarmonyPrefix]
        static bool StartTurn(GameManager __instance, Character.Team team, ref IEnumerator __result, List<Character> ___mEnemyUnits, List<Character> ___mPlayerUnits, AudioClip ____PlayerTurnBeginSFX)
        {
            if (Main.Replaying != null)
            {
                ExtendedGameManager.extra.GetOrCreateValue(__instance).CurrentTurn = team;
                __instance.SetGameState(GameManager.GameState.PLAYER);
                if (team == Character.Team.PLAYER)
                {
                    __instance.SetTurnState(GameManager.TurnState.INITIALIZATION);
                    if (____PlayerTurnBeginSFX)
                        SnChannel.Play(____PlayerTurnBeginSFX, "STSFX_Pool", true);
                    for (int i = ___mPlayerUnits.Count - 1; i >= 0; i--)
                        ___mPlayerUnits[i].BeginTurn();
                    if (___mPlayerUnits.Count > 0)
                        __instance.UpdateCharacterSelection();
                    if (__instance._HUD != null)
                    {
                        __instance._HUD.pCharacterInfoUI.UpdatePlayersInfoDisplay(__instance._SelectedCharacter, true);
                        __instance._HUD.SetCharactersMenuState(KAUIState.INTERACTIVE);
                    }
                    CameraMovement.pInstance.UpdateCameraFocus(__instance._SelectedCharacter.transform.position, true, true);
                    __instance.SetTurnState(GameManager.TurnState.INPUT);
                }
                else
                {
                    __instance.SetTurnState(GameManager.TurnState.INITIALIZATION);
                    ___mEnemyUnits.Sort((Character i1, Character i2) => -1 * i1.pInitiative.CompareTo(i2.pInitiative));
                }
                __result = GameManagerPatchMethods.Replacement(team, __instance, ___mEnemyUnits);
                return false;
            }
            return true;
        }

        [HarmonyPatch("EndTurn")]
        [HarmonyPostfix]
        static void EndTurn(Character.Team team)
        {
            if (Main.Recording != null && team == Character.Team.ENEMY && GameManager.pInstance._GameState != GameManager.GameState.GAMEOVER)
                Main.Recording.RecordedActions.Add(new RecordedEvent() { Type = EventType.EndOfTurn, Team = Character.Team.ENEMY });
        }

        [HarmonyPatch("ProcessMouseDown")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> ProcessMouseDown(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var code = instructions.ToList();
            code.Insert(
                code.FindIndex(x => x.opcode == OpCodes.Stfld && x.operand is FieldInfo m && m.Name == "mStartedMove"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GameManagerPatchMethods), nameof(GameManagerPatchMethods.EditStartedMove))));
            return code;
        }
    }

    static class GameManagerPatchMethods
    {
        public static IEnumerator AfterCharLoad(GameManager instance, Character component, AvatarData.InstanceInfo info)
        {
            if (string.IsNullOrEmpty(component.pCharacterData._WeaponData._MeshPrefab))
                yield break;
            while (info.mLoading)
                yield return null;
            info.mLoading = true;
            var start = instance.GetDataLoaded();
            var callback = (RsResourceEventHandler)typeof(GameManager).GetMethod("OnWeaponMeshBundleLoaded", ~BindingFlags.Default).CreateDelegate(typeof(RsResourceEventHandler), instance);
            RsResourceManager.LoadAssetFromBundle(component.pCharacterData._WeaponData._MeshPrefab, callback, typeof(GameObject), false, component);
            while (instance.GetDataLoaded() == start)
                yield return null;
            info.mLoading = false;
            yield break;
        }
        public static bool WaitingForTurnStart = false;
        public static IEnumerator Replacement(Character.Team team, GameManager instance, List<Character> enemyUnits)
        {
            if (team == Character.Team.ENEMY)
            {
                yield return instance.StartCoroutine(instance.ConditionalEnemySpawns());
                for (int j = enemyUnits.Count - 1; j >= 0; j--)
                    if (enemyUnits[j].pCharacterData._Team != Character.Team.INANIMATE)
                        enemyUnits[j].BeginTurn();
                instance._Grid.ShowCurrentValidMoves(false, null, null);
                yield return new WaitForSeconds(instance._TurnInitializationTimer);
                if (instance._HUD)
                    instance._HUD.SetCharactersMenuState(KAUIState.INTERACTIVE);
                instance.SetTurnState(GameManager.TurnState.INPUT);
            }
            if (instance._HUD != null)
                instance._HUD.VisibleEnemyTurnText(team == Character.Team.ENEMY);
            WaitingForTurnStart = false;
            yield break;
        }
        public static bool EditStartedMove(bool original) => original && Main.Replaying == null;
    }
}