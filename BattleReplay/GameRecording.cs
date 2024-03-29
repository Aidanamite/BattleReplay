﻿using System;
using System.Collections;
using System.Collections.Generic;
using SquadTactics;
using System.Runtime.Serialization;
using UnityEngine;

namespace BattleReplay
{
    [Serializable]
    public class GameRecording
    {
        public int RealmIndex;
        public int LevelIndex;
        public Randoms SetupRandoms = new Randoms();
        public List<CharacterData> PlayerCharacters = new List<CharacterData>();
        public AvatarData PlayerAppearance;
        public Dictionary<int, RaisedPetData> PetAppearances = new Dictionary<int, RaisedPetData>();
        public IconData PlayerIcon;
        public Dictionary<int, IconData> PetIcons = new Dictionary<int, IconData>();
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public int ActionPos;
        public List<RecordedEvent> RecordedActions = new List<RecordedEvent>();
    }
    [Serializable]
    public class IconData
    {
        string pixels;
        int width;
        public static implicit operator Texture2D(IconData data)
        {
            var p2 = Convert.FromBase64String(data.pixels.Replace('-', '/'));
            var p = new Color[p2.Length / 4];
            var t = new Texture2D(data.width, p.Length / data.width, TextureFormat.RGBA32, false);
            for (int i = 0; i < p.Length; i++)
                p[i] = new Color32(p2.SafeGet(i * 4), p2.SafeGet(i * 4 + 1), p2.SafeGet(i * 4 + 2), p2.SafeGet(i * 4 + 3));
            t.SetPixels(p);
            t.Apply();
            return t;
        }
        public static implicit operator IconData(Texture2D texture)
        {
            var d = new IconData() { width = texture.width };
            var p = texture.GetPixels(0);
            var p2 = new byte[p.Length * 4];
            for (int i = 0; i < p.Length; i++)
            {
                var c = (Color32)p[i];
                p2[i * 4] = c.r;
                p2[i * 4 + 1] = c.g;
                p2[i * 4 + 2] = c.b;
                p2[i * 4 + 3] = c.a;
            }
            d.pixels = Convert.ToBase64String(p2).Replace('/', '-');
            return d;
        }
    }
    [Serializable]
    public class RecordedEvent
    {
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public Randoms Randoms;
        public int Character;
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public int TargetX;
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public int TargetY;
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public int Target;
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public int Ability;
        [OptionalField]
        [DataMember(EmitDefaultValue = false)]
        public Character.Team Team;
        public EventType Type;
        public IEnumerator Execute(GameManager instance)
        {
            Debug.Log(Main.LogPrefix + "EventExecute: " + Type);
            if (Type == EventType.EndOfTurn)
            {
                Debug.Log(Main.LogPrefix + "EndOfTurn for team " + Team);
                if (ExtendedGameManager.extra.GetOrCreateValue(instance).CurrentTurn == Team)
                {
                    if (Team == SquadTactics.Character.Team.PLAYER)
                        instance.EndPlayerTurn();
                    else
                        instance.EndTurn(Team);
                }
                return WaitForEndOfTurn();
            }
            else if (Type == EventType.Movement)
            {
                var c = GetCharacter(instance, Character);
                if (c == null)
                    Debug.LogError($"{Main.LogPrefix}Character {Character} not found. Event Type: Movement");
                else
                    Debug.Log($"{Main.LogPrefix}Move character {Character} ({c.pCharacterData._DisplayNameText.GetLocalizedString()}) to tile {TargetX}, {TargetY}");
                return Watch(c, instance, c.DoMovement(instance._Grid.GetNodeByPosition(TargetX, TargetY)));
            }
            else if (Type == EventType.Ability)
            {
                var c = GetCharacter(instance, Character);
                if (c == null)
                    Debug.LogError($"{Main.LogPrefix}Character {Character} not found. Event Type: Ability");
                var t = GetCharacter(instance, Target);
                if (t == null)
                    Debug.LogError($"{Main.LogPrefix}Target {Character} not found. Event Type: Ability");
                if (t && c)
                    Debug.Log($"{Main.LogPrefix}Character {Character} ({c.pCharacterData._DisplayNameText.GetLocalizedString()}) use ability {Ability} ({c.pAbilities[Ability]._Name.GetLocalizedString()}) on character {Target} ({t.pCharacterData._DisplayNameText.GetLocalizedString()})");
                c.SetAbility(c.pAbilities[Ability]);
                Patch_Ability.NextReplay = Randoms;
                return Watch(c, instance, c.UseAbility(t));
            }
            return DoNothing();
        }
        static IEnumerator Watch(Character character, MonoBehaviour coroutineRunner, IEnumerator during)
        {
            CameraMovement.pInstance.UpdateCameraFocus(character.transform.position, false, false);
            while (CameraMovement.pInstance.pIsCameraMoving)
                yield return null;
            CameraMovement.pInstance.SetFollowTarget(character.transform);
            yield return coroutineRunner.StartCoroutine(during);
            CameraMovement.pInstance.SetFollowTarget(null);
            yield break;
        }
        public static IEnumerator WaitForEndOfTurn()
        {
            Debug.Log(Main.LogPrefix + "Waiting for next turn start");
            while (GameManagerPatchMethods.WaitingForTurnStart)
                yield return null;
            Debug.Log(Main.LogPrefix + "Next turn started");
            yield break;
        }
        static IEnumerator DoNothing()
        {
            yield break;
        }
        static Character GetCharacter(GameManager instance, int character)
        {
            for (var i = SquadTactics.Character.Team.PLAYER; i <= SquadTactics.Character.Team.INANIMATE; i++)
                foreach (var c in instance.GetTeamCharacters(i))
                    if (c && c.pCharacterData.pSpawnOrder == character)
                        return c;
            return null;
        }
    }
    [Serializable]
    public class Randoms
    {
        public RandomCollection<int> Ints = new RandomCollection<int>();
        public RandomCollection<float> Floats = new RandomCollection<float>();
    }
    [Serializable]
    public class RandomCollection<T>
    {
        public Dictionary<(T, T), Value> Values = new Dictionary<(T, T), Value>();

        [Serializable]
        public class Value
        {
            [OptionalField]
            [DataMember(EmitDefaultValue = false)]
            public int Pos;
            public List<T> Results = new List<T>();
        }
    }
}