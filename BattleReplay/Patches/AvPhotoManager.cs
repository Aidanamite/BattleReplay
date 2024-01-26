using HarmonyLib;
using UnityEngine;

namespace BattleReplay
{
    [HarmonyPatch(typeof(AvPhotoManager), "TakePhotoUI", typeof(string), typeof(Texture2D), typeof(AvPhotoCallback), typeof(object))]
    static class Patch_TakeAvatarPhoto
    {
        static bool Prefix(string userID, Texture2D defaultImage, ref AvPhotoCallback cbk, object udata)
        {
            if (Main.Replaying != null)
            {
                cbk(Main.Replaying.PlayerIcon, udata);
                return false;
            }
            if (Main.Recording != null)
            {
                cbk = (x, y) =>
                {
                    if (Main.Recording != null)
                        Main.Recording.PlayerIcon = (Texture2D)x;
                } + cbk;
            }
            return true;
        }
    }
}