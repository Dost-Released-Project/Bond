using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleSystem;
using Bond.Embark;
using Bond.Expedition;
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
        [Inject] public PartyController _partyManager;
        [Inject] public StageCoach _stageCoach;
        [Inject] public ExpeditionPayload payload;
        [Inject] public Roster roster;
        public DataBaseSO professionDb;
        
        public List<TriggerPreset> TriggerPresets;
        public List<CharacterPreset> CharacterPresets;

        public void Start()
        {
            professionDb = Addressables.LoadAssetAsync<DataBaseSO>("ClassDataBase").WaitForCompletion();
            
            payload.Clear();
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

        private void Update()
        {
        }

        public void FillRoster()
        {
            foreach (var preset in CharacterPresets)
            {
                roster.Hire(Instantiate(preset).BaseCharacter);
            }
        }

        public void Test()
        {
            roster.Characters[0].Name = "Hex";
        }
    }
}

#endif