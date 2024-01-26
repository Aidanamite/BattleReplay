using HarmonyLib;

namespace BattleReplay
{
    [HarmonyPatch]
    static class Patch_Randomizer
    {
        public static Randoms Recording;
        public static Randoms Replaying;
        [HarmonyPatch(typeof(UnityEngine.Random), "Range", typeof(int), typeof(int))]
        static void Postfix(int minInclusive, int maxExclusive, ref int __result)
        {
            if (Replaying != null && Replaying.Ints.Values.TryGetValue((minInclusive, maxExclusive),out var v))
                __result = v.Results[v.Pos++];
            Recording?.Ints.Values.GetOrCreate((minInclusive, maxExclusive)).Results.Add(__result);
        }
        [HarmonyPatch(typeof(UnityEngine.Random), "Range", typeof(float), typeof(float))]
        static bool Prefix(float minInclusive, float maxInclusive, ref float __result)
        {
            __result = UnityEngine.Random.value * (maxInclusive - minInclusive) + minInclusive;
            if (Replaying != null && Replaying.Floats.Values.TryGetValue((minInclusive, maxInclusive), out var v))
                __result = v.Results[v.Pos++];
            Recording?.Floats.Values.GetOrCreate((minInclusive, maxInclusive)).Results.Add(__result);
            return false;
        }
    }
}