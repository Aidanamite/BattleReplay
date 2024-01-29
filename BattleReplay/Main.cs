using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using SquadTactics;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ConfigTweaks;

namespace BattleReplay
{
    [BepInPlugin("com.aidanamite.BattleReplay", "Battle Replay", "1.0.2")]
    [BepInDependency("com.aidanamite.ConfigTweaks")]
    public class Main : BaseUnityPlugin
    {
        public const string LogPrefix = "[Battle Replay]: ";
        public const string CustomUnitName = "\u0000CUSTOM\u0000UNIT\u0000";
        public static string ReplayFolder = Environment.CurrentDirectory + "\\Dragon Tactics Replays";
        public static GameRecording Replaying;
        public static GameRecording Recording;
        public static Main instance;
        [ConfigField]
        public static ReplaySaving ReplaySaving;
        static DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(GameRecording),new DataContractJsonSerializerSettings() { DataContractSurrogate = new IgnoreComponents() });
        static string LastLoadedReplay;
        class IgnoreComponents : IDataContractSurrogate
        {
            Type IDataContractSurrogate.GetDataContractType(Type type) => typeof(Object).IsAssignableFrom(type) ? typeof(void) : type == typeof(void) ? typeof(Object) : type;
            object IDataContractSurrogate.GetDeserializedObject(object obj, Type targetType) => typeof(Object).IsAssignableFrom(targetType) ? null : obj;
            object IDataContractSurrogate.GetObjectToSerialize(object obj, Type targetType) => obj is Object ? null : obj;
            object IDataContractSurrogate.GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType) => null;
            object IDataContractSurrogate.GetCustomDataToExport(Type clrType, Type dataContractType) => null;
            void IDataContractSurrogate.GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<Type> customDataTypes) { }
            Type IDataContractSurrogate.GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData) => null;
            System.CodeDom.CodeTypeDeclaration IDataContractSurrogate.ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit) => typeDeclaration;
        }
        public void Awake()
        {
            instance = this;
            new Harmony("com.aidanamite.BattleReplay").PatchAll();
            Logger.LogInfo("Loaded");
        }
        public void AskSaveReplay()
        {
            GameUtilities.DisplayGenericDB("PfKAUIGenericDB", "Would you like to save battle replay?", "Save Replay", gameObject, "SaveReplay", "DiscardReplay", null, "DiscardReplay", true);
        }
        public void SaveReplay()
        {
            if (Recording == null)
                return;
            if (!Directory.Exists(ReplayFolder))
                Directory.CreateDirectory(ReplayFolder);
            try
            {
                using (var stream = File.Create($"{ReplayFolder}\\Replay ({DateTime.Now:HH-mm dd-MM-yyyy}).DTReplay"))
                    jsonSerializer.WriteObject(stream, Recording);
            } catch (Exception e)
            {
                Debug.LogError(e);
                GameUtilities.DisplayOKMessage("PfKAUIGenericDB", "An error occured while saving the replay file", null, "");
            }
            Recording = null;
        }
        public void DiscardReplay()
        {
            Recording = null;
        }
        public static void LoadReplay(string file)
        {
            LastLoadedReplay = file;
            try
            {
                using (var stream = File.OpenRead(file))
                    Replaying = jsonSerializer.ReadObject(stream) as GameRecording;
            } catch (Exception e)
            {
                Replaying = null;
                GameUtilities.DisplayOKMessage("PfKAUIGenericDB", "An error occured while loading the replay file", null, "");
                Debug.LogError(e);
                return;
            }
            var selected = new List<UnitSelection>();
            foreach (var i in Replaying.PlayerCharacters)
                selected.Add(new UnitSelection() { _UnitName = CustomUnitName });
            SquadTactics.LevelManager.pInstance.SetSquad(selected);
            SquadTactics.LevelManager.pInstance.SetLevelPlaying(Replaying.RealmIndex, Replaying.LevelIndex);
            SquadTactics.LevelManager.pInstance.LoadLevel();
        }
        public static void RestartLastReplay() => LoadReplay(LastLoadedReplay);
        public void ReturnToMainMenu()
        {
            Replaying = null;
            Recording = null;
            GameManager.pInstance.LoadMainMenu();
        }
    }
}