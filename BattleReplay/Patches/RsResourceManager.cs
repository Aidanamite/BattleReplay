using HarmonyLib;
using System;
using System.Collections.Generic;
using SquadTactics;

namespace BattleReplay
{
    [HarmonyPatch(typeof(RsResourceManager), "LoadAssetFromBundle", typeof(string), typeof(RsResourceEventHandler), typeof(Type), typeof(bool), typeof(object))]
    static class Patch_RequestAssetLoad
    {
        static void Prefix(ref string inBundleURL, object inUserData)
        {
            if (Patch_GameManager.calling && inUserData is List<object> l && l.Count == 2 && l[0] is CharacterData data)
            {
                if (data._Team == Character.Team.PLAYER)
                {
                    Patch_GameManager.spawnOrder = data.pSpawnOrder + 1;
                    if (Main.Recording != null)
                        Main.Recording.PlayerCharacters.Add(data.DeepMemberwiseClone());
                    if (Main.Replaying != null)
                    {
                        l[0] = data = Main.Replaying.PlayerCharacters.Find(x => x.pSpawnOrder == data.pSpawnOrder);
                        inBundleURL = "RS_DATA/" + data._PrefabName + ".unity3d/" + data._PrefabName;
                    }
                }
                else
                    data.pSpawnOrder = Patch_GameManager.spawnOrder++;
            }
        }
    }
}