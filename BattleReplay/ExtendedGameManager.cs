using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using SquadTactics;
using System;

namespace BattleReplay
{
    public class ExtendedGameManager
    {
        public static ConditionalWeakTable<GameManager, ExtendedGameManager> extra = new ConditionalWeakTable<GameManager, ExtendedGameManager>();
        public bool AutoPlay = false;
        public bool DoSingleTurn;
        public Randoms WithheldRec;
        public Randoms WithheldRep;
        public int Withhold = 0;
        public Character.Team CurrentTurn;
        public static IEnumerator DoTurns(GameManager instance)
        {
            Debug.Log(Main.LogPrefix + "DoTurns");
            var extra = ExtendedGameManager.extra.GetOrCreateValue(instance);
            while (true)
            {
                if (GameManager.pInstance._GameState == GameManager.GameState.GAMEOVER)
                    break;
                if (!extra.AutoPlay && !extra.DoSingleTurn)
                {
                    yield return null;
                    continue;
                }
                extra.DoSingleTurn = false;
                if (!DoNextAction(instance, out var coroutine))
                    break;
                yield return coroutine;
                yield return new WaitForSeconds(0.1f);
                if (GameManagerPatchMethods.WaitingForTurnStart)
                    yield return instance.StartCoroutine(RecordedEvent.WaitForEndOfTurn());
            }
            Debug.Log(Main.LogPrefix + "DoTurns exit");
            yield break;
        }
        static bool DoNextAction(GameManager instance, out Coroutine coroutine)
        {
            try
            {
                coroutine = null;
                Debug.Log($"{Main.LogPrefix}DoNextAction start: ({Main.Replaying.ActionPos}/{Main.Replaying.RecordedActions.Count})");
                if (Main.Replaying.ActionPos >= Main.Replaying.RecordedActions.Count)
                    return false;
                var i = Main.Replaying.RecordedActions[Main.Replaying.ActionPos++];
                if (i == null)
                    return true;
                var e = i.Execute(instance);
                if (e != null)
                    coroutine = instance.StartCoroutine(e);
                return true;
            }
            catch (Exception e)
            {
                GameUtilities.DisplayOKMessage("PfKAUIGenericDB", "An error occured while playing the replay", Main.instance.gameObject, "ReturnToMainMenu");
                throw e;
            }
        }
    }
}