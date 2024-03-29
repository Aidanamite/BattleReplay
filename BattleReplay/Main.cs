﻿using BepInEx;
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
    [BepInPlugin("com.aidanamite.BattleReplay", "Battle Replay", "1.0.8")]
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
        static Dictionary<string,DataContractJsonSerializer> JsonSerializers = new Dictionary<string, DataContractJsonSerializer>()
        {
            {
                "v0",
                new DataContractJsonSerializer(typeof(GameRecording), new DataContractJsonSerializerSettings() { DataContractSurrogate = new IgnoreComponents() })
            },
            {
                "v1",
                new DataContractJsonSerializer(typeof(GameRecording), new DataContractJsonSerializerSettings() { DataContractSurrogate = new IgnoreComponents(), DateTimeFormat = new DateTimeFormat("o") })
            }
        };
        static string CurrentSerializer = "v1";
        static string LastLoadedReplay;
        class IgnoreComponents : IDataContractSurrogate
        {
            Type IDataContractSurrogate.GetDataContractType(Type type) => typeof(Object).IsAssignableFrom(type) ? typeof(void) : type == typeof(void) ? typeof(Object) : type;
            object IDataContractSurrogate.GetDeserializedObject(object obj, Type targetType) => typeof(Object).IsAssignableFrom(targetType) ? null : obj;
            object IDataContractSurrogate.GetObjectToSerialize(object obj, Type targetType) => obj is Object ? null : obj is DateTime d ? d.ToUniversalTime() : obj;
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
            var f = $"{ReplayFolder}\\Replay ({DateTime.Now:HH-mm dd-MM-yyyy}).DTReplay";
            try
            {
                using (var stream = File.Create(f))
                {
                    var b = Encoding.UTF8.GetBytes(CurrentSerializer + "\n");
                    stream.Write(b,0,b.Length);
                    JsonSerializers[CurrentSerializer].WriteObject(stream, Recording);
                }
            } catch (Exception e)
            {
                Debug.LogError(e);
                try
                {
                    if (File.Exists(f))
                        File.Delete(f);
                } catch { }
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
                {
                    var serializer = "v0";
                    int b = stream.ReadByte();
                    stream.Seek(0, SeekOrigin.Begin);
                    if (b == Encoding.UTF8.GetBytes("v")[0])
                    {
                        var end = Encoding.UTF8.GetBytes("\n")[0];
                        var reading = new List<byte>();
                        while ((b = stream.ReadByte()) != -1 && b != end)
                            reading.Add((byte)b);
                        serializer = Encoding.UTF8.GetString(reading.ToArray());
                    }
                    Replaying = JsonSerializers[serializer].ReadObject(stream) as GameRecording;
                }
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
        public void ReturnToMainMenu() => GameManager.pInstance.LoadMainMenu();
    }
}