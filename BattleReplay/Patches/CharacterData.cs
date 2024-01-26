using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(CharacterData),MethodType.Constructor,typeof(CharacterData))]
    static class Patch_CreateData
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            code[code.FindIndex(x => x.opcode == OpCodes.Newobj && x.operand is ConstructorInfo c && c.DeclaringType == typeof(Stats))]
                = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_CreateData), nameof(CreateStats)));
            code[code.FindIndex(x => x.opcode == OpCodes.Newobj && x.operand is ConstructorInfo c && c.DeclaringType == typeof(WeaponData))]
                = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_CreateData), nameof(CreateWeapon)));
            return code;
        }
        static Stats CreateStats(Stats original) => original == null ? null : new Stats(original);
        static WeaponData CreateWeapon(WeaponData original) => original == null ? null : new WeaponData(original);
    }
}