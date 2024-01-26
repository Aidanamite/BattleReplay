using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using SquadTactics;

namespace BattleReplay
{
    static class UiCharactersMenuPatchMethods
    {
        public static void LoadImage(string type, int slotIdx, GameObject msgObject, RaisedPetData pet, KAWidget profile)
        {
            if (Main.Replaying != null)
                profile.SetTexture(Main.Replaying.PetIcons[pet.RaisedPetID]);
            else
                ImageData.Load(type, slotIdx, msgObject);
        }
        public static string ReplaceName(string original)
        {
            if (Main.Replaying != null)
                return Main.Replaying.PlayerAppearance.DisplayName;
            return original;
        }
    }

    [HarmonyPatch(typeof(UiCharactersMenu))]
    static class Patch_UiCharactersMenu
    {
        [HarmonyPatch("OnImageLoaded")]
        [HarmonyPrefix]
        static void OnImageLoaded(UiCharactersMenu __instance, ImageDataInstance img)
        {
            if (Main.Recording != null)
            {
                foreach (KAWidget kawidget in __instance.GetItems())
                {
                    var pet = RaisedPetData.GetByID((kawidget.GetUserData() as CharacterWidgetData)._Character.pCharacterData.pRaisedPetID);
                    if (pet != null && pet.ImagePosition == img.mSlotIndex)
                        Main.Recording.PetIcons[pet.RaisedPetID] = img.mIconTexture == null ? new IconData() : (IconData)img.mIconTexture;
                }
            }
        }

        [HarmonyPatch("SetupCharacterMenu")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> SetupCharacterMenu(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.operand is MethodInfo m && m.Name == "Load" && m.DeclaringType == typeof(ImageData));
            code.RemoveAt(ind);
            code.InsertRange(ind, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_S,5),
                new CodeInstruction(OpCodes.Ldloc_S,6),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof( UiCharactersMenuPatchMethods),nameof(UiCharactersMenuPatchMethods.LoadImage)))
            });
            code.Insert(
                code.FindIndex(x => x.opcode == OpCodes.Ldfld && x.operand is FieldInfo f && f.Name == "DisplayName" && f.DeclaringType == typeof(AvatarData)) + 1,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UiCharactersMenuPatchMethods), nameof(UiCharactersMenuPatchMethods.ReplaceName))));
            return code;
        }
    }
}