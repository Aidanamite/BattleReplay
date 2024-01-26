using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using SquadTactics;
using System.Runtime.Serialization;

namespace BattleReplay
{
    public static class ExtentionMethods
    {
        public static T MemberwiseClone<T>(this T obj)
        {
            if (obj == null)
                return obj;
            var t = obj.GetType();
            var nObj = (T)FormatterServices.GetUninitializedObject(t);
            var b = typeof(object);
            while (t != b)
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(nObj, f.GetValue(obj));
                t = t.BaseType;
            }
            return nObj;
        }

        public static T DeepMemberwiseClone<T>(this T obj)=> obj.DeepMemberwiseClone(new Dictionary<object,object>(),new HashSet<object>() );
        static T DeepMemberwiseClone<T>(this T obj, Dictionary<object,object> cloned, HashSet<object> created)
        {
            if (obj == null)
                return obj;
            if (cloned.TryGetValue(obj, out var clone))
                return (T)clone;
            if (created.Contains(obj))
                return obj;
            var t = obj.GetType();
            if (t.IsPrimitive || t == typeof(string))
                return obj;
            if (t.IsArray && obj is Array a)
            {
                var c = t.GetConstructors()[0];
                var o = new object[t.GetArrayRank()];
                for (int i = 0; i < o.Length; i++)
                    o[i] = a.GetLength(i);
                var na = (Array)c.Invoke(o);
                created.Add(na);
                cloned[a] = na;
                for (int i = 0; i < o.Length; i++)
                    if ((int)o[i] == 0)
                        return (T)(object)na;
                var ind = new int[o.Length];
                var flag = true;
                while(flag)
                {
                    na.SetValue(a.GetValue(ind).DeepMemberwiseClone(cloned, created), ind);
                    for (int i = 0; i < ind.Length; i++)
                    {
                        ind[i]++;
                        if (ind[i] == (int)o[i])
                        {
                            if (i == ind.Length - 1)
                                flag = false;
                            ind[i] = 0;
                        }
                        else
                            break;
                    }
                }
                return (T)(object)na;
            }
            var nObj = (T)FormatterServices.GetUninitializedObject(t);
            created.Add(nObj);
            cloned[obj] = nObj;
            var b = typeof(object);
            while (t != b)
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(nObj, f.GetValue(obj).DeepMemberwiseClone(cloned,created));
                t = t.BaseType;
            }
            return nObj;
        }

        public static Y GetOrCreate<X, Y>(this IDictionary<X, Y> d, X key) where Y : new()
        {
            if (d.TryGetValue(key, out var v))
                return v;
            return d[key] = new Y();
        }

        static MethodInfo _ConditionalEnemySpawns = typeof(GameManager).GetMethod("ConditionalEnemySpawns", ~BindingFlags.Default);
        public static IEnumerator ConditionalEnemySpawns(this GameManager manager) => (IEnumerator)_ConditionalEnemySpawns.Invoke(manager, new object[0]);
        static MethodInfo _EndTurn = typeof(GameManager).GetMethod("EndTurn", ~BindingFlags.Default);
        public static void EndTurn(this GameManager manager, Character.Team team) => _EndTurn.Invoke(manager, new object[] { team });
        static FieldInfo _mDataLoaded = typeof(GameManager).GetField("mDataLoaded", ~BindingFlags.Default);
        public static int GetDataLoaded(this GameManager manager) => (int)_mDataLoaded.GetValue(manager);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T SafeGet<T>(this T[] a, int index, T fallback = default) => a.Length > index ? a[index] : fallback;

        /*static Dictionary<int, FieldInfo> replacedFields = new Dictionary<int, FieldInfo>();
        static Dictionary<FieldInfo, int> replacedFields2 = new Dictionary<FieldInfo, int>();
        static int currReplace = 0;
        public static void FixPrivateFields(this List<CodeInstruction> code, MethodBase method)
        {
            var ldf = AccessTools.Method(typeof(ExtentionMethods), nameof(Ldfld));
            var stf = AccessTools.Method(typeof(ExtentionMethods), nameof(Stfld));
            var ldsf = AccessTools.Method(typeof(ExtentionMethods), nameof(Ldsfld));
            var stsf = AccessTools.Method(typeof(ExtentionMethods), nameof(Stsfld));
            for (int i = code.Count - 1; i >= 0; i--)
                if (code[i].operand is FieldInfo f && f.IsPrivate && f.DeclaringType == method.DeclaringType)
                {
                    var ld = code[i].opcode == OpCodes.Ldfld;
                    if (!replacedFields2.TryGetValue(f,out var id))
                    {
                        id = currReplace++;
                        replacedFields[id] = f;
                        replacedFields2[f] = id;
                    }
                    code.RemoveAt(i);
                    code.InsertRange(i, new[]
                    {
                        new CodeInstruction(OpCodes.Ldc_I4,id),
                        new CodeInstruction(OpCodes.Call,f.IsStatic ? (ld ? ldsf : stsf).MakeGenericMethod(f.FieldType) : (ld ? ldf : stf).MakeGenericMethod(f.DeclaringType,f.FieldType))
                    });
                }
        }
        public static void Stfld<X, Y>(X obj, Y val, int fid) => replacedFields[fid].SetValue(obj, val);
        public static Y Ldfld<X, Y>(X obj, int fid) => (Y)replacedFields[fid].GetValue(obj);
        public static void Stsfld<Y>(Y val, int fid) => replacedFields[fid].SetValue(null, val);
        public static Y Ldsfld<Y>(int fid) => (Y)replacedFields[fid].GetValue(null);*/
    }

    /// Disabled enumerator transpilers because there seems to be a bug atm that makes them not possible
    /*

    [HarmonyPatch(typeof(GameManager),"Start", MethodType.Enumerator)]
    static class Patch_StartSquadGame
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Stloc_S && (x.operand is LocalBuilder loc ? loc.LocalIndex : x.operand is int v ? v : -1) == 12);
            code.InsertRange(ind + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_S,10),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_StartSquadGame),nameof(OnLoadCharacterData)))
            });
            code.FixPrivateFields(method);
            return code;
        }
        static void OnLoadCharacterData(CharacterData data)
        {
            if (Main.Recording != null)
                Main.Recording.PlayerCharacters.Add(data.DeepMemberwiseClone());
        }
    }

    [HarmonyPatch(typeof(Character), "DoMovement", MethodType.Enumerator)]
    static class Patch_StartCharacterMovement
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Stfld && x.operand is FieldInfo f && f.Name == "_HasMoveAction");
            code.InsertRange(ind + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,AccessTools.Field(method.DeclaringType,"targetNode")),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_StartCharacterMovement),nameof(OnStartMovement)))
            });
            code.FixPrivateFields(method);
            return code;
        }
        static void OnStartMovement(Character chara, Node target)
        {
            if (Main.Recording != null)
                Main.Recording.RecordedActions.Add(new RecordedMovement() { Character = chara.pCharacterData.pSpawnOrder, TargetX = target._XPosition, TargetY = target._YPosition });
        }
    }

    [HarmonyPatch(typeof(Ability), "Activate", MethodType.Enumerator)]
    static class Patch_StartCharacterAbility
    {
        static List<int> ActiveRecording;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Stfld && x.operand is FieldInfo f && f.Name == "mCurrentCooldown");
            code.InsertRange(ind + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,method.DeclaringType.GetFields(~BindingFlags.Default).First(x => x.Name.Contains("<this>"))),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,AccessTools.Field(method.DeclaringType,"owner")),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld,AccessTools.Field(method.DeclaringType,"target")),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_StartCharacterAbility),nameof(OnStartAbility)))
            });
            code.FixPrivateFields(method);
            return code;
        }
        static void OnStartAbility(Ability ability, Character user, Character target)
        {
            if (Main.Recording != null)
                Main.Recording.RecordedActions.Add(new RecordedAbility() { Character = user.pCharacterData.pSpawnOrder, Target = target.pCharacterData.pSpawnOrder, Ability = user.pAbilities.IndexOf(ability), Randoms = (ActiveRecording = Patch_Randomizer.Recording = new List<int>()) });
        }
        static void Prefix() => Patch_Randomizer.Recording = ActiveRecording;
        static void Postfix(bool __result)
        {
            if (!__result)
                ActiveRecording = null;
        }
        static void Finalizer() => Patch_Randomizer.Recording = null;
    }

    [HarmonyPatch(typeof(GameManager), "DoEnemyTurn", MethodType.Enumerator)]
    static class Patch_DoEnemyTurn
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.operand is MethodInfo m && m.Name == "EndTurn");
            code.Insert(ind + 1, new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_DoEnemyTurn),nameof(OnEndTurn))));
            code.FixPrivateFields(method);
            return code;
        }
        static void OnEndTurn()
        {
            if (Main.Recording != null)
                Main.Recording.RecordedActions.Add(new RecordedEndOfTurn() { Team = Character.Team.ENEMY });
        }
    }
    */
}