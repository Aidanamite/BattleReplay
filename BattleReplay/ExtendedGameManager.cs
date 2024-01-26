using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using SquadTactics;

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
        public static IEnumerator DoTurns(GameManager instance)
        {
            Debug.Log("DoTurns");
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
            }
            Debug.Log("DoTurns exit");
            yield break;
        }
        static bool DoNextAction(GameManager instance, out Coroutine coroutine)
        {
            coroutine = null;
            Debug.Log($"DoNextAction start: ({Main.Replaying.ActionPos}/{Main.Replaying.RecordedActions.Count})");
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
    }
}