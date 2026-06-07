using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleSystem;
using Bond.Embark;
using Bond.Expedition;
using Bond.Persistence;
using Newtonsoft.Json;
using PipeLine;
using Reactions;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using VContainer;

#if UNITY_EDITOR

namespace _90._HA.Temp.Test
{
    public class S1Test : MonoBehaviour
    {
        [Inject] public ExpeditionInventory _expeditionInventory;
        [Inject] public EmbarkController _embarkManager;
        [Inject] public IPartyController _partyManager;
        [Inject] public StageCoach _stageCoach;
        [Inject] public ExpeditionPayload payload;
        [Inject] public Roster roster;
        public DataBaseSO professionDb;
        
        public List<CharacterPreset> CharacterPresets;

        public async void Start()
        {
            await DBSORegistry.PreloadByLabelAsync("DBSO");
            professionDb = DBSORegistry.GetDb<DataBaseSO>("ClassDataBase");
            
            payload.Clear();
            //FillRosterFromPreset();
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
                Depart();
            if (Keyboard.current.numpad9Key.wasPressedThisFrame)
                SaveLoadSystem.Load(roster);
        }

        public void CharacterToJson()
        {
            BaseCharacter chara = new StageCoach().GetRandomCharacter();
            string output = JsonConvert.SerializeObject(chara, Formatting.Indented, SaveLoadSystem.Settings);
            File.WriteAllText("Assets/90. HA/Temp/characterJsonTest.json", output, System.Text.Encoding.UTF8);
            Debug.Log(output);
        }
        public void JsonToCharacter()
        {
            string json = File.ReadAllText("Assets/90. HA/Temp/characterJsonTest.json", System.Text.Encoding.UTF8);
            BaseCharacter chara = JsonConvert.DeserializeObject<BaseCharacter>(json, SaveLoadSystem.Settings);
            Debug.Log(chara);
            roster.Hire(chara);
        }
        
        public void CreateCharacterPresets()
        {
            var db = professionDb.Query<ClassSO>(so => true);

            Debug.Assert(db.Count() >= 4);

            for (int i = 0; i < 4; i++)
            {
                var chara = _stageCoach.GetCharacter(db.ElementAt(i));
                
                SaveCharacterAsPreset(chara);
            }
        }

        private void SaveCharacterAsPreset(BaseCharacter character)
        {
            string root = "Assets/90. HA/Temp/Test/So";
            string path = Path.Combine(root, $"CharacterPreset_{character.Profession.Id}.asset");
            
            var so = AssetDatabase.LoadAssetAtPath<CharacterPreset>(path);
            bool isNew = so == null;
            if (isNew) so = ScriptableObject.CreateInstance<CharacterPreset>();

            so.BaseCharacter = (character);
            
            if (isNew) AssetDatabase.CreateAsset(so, path);
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssetIfDirty(so);
        }
        
        public void Depart()
        {
            foreach (var rosterCharacter in roster.Characters)
            {
                _partyManager.TryAddMember(rosterCharacter);
            }

            var region = new ExpeditionRegion("forest_01", "속삭이는 숲", "난이도: 평이 · 권장 4인", DungeonType.Forest);
            _embarkManager.SelectRegion(region);
            _embarkManager.ConfirmEmbark();
        }

        public void FillRoster()
        {
            while (roster.IsFull == false)
            {
                roster.Hire(_stageCoach.GetRandomCharacter());
            }
        }

        public void FillRosterFromPreset()
        {
            foreach (var preset in CharacterPresets)
            {
                var c = Instantiate(preset).BaseCharacter;
                c.CalcStat();
                BaseCharacter.Dict[c.Id] = c;
                c.SetHpFull();
                roster.Hire(c);
                c.isPlayable = true;
            }
        }

        public void Test()
        {
            roster.Characters[0].Name = "Hex";
        }
    }
}

#endif