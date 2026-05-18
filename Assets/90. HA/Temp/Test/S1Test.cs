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
            FillRoster();
        }

        private void CreateCharacterPresets()
        {
            var db = professionDb.Query<ClassSO>(so => true);

            Debug.Assert(db.Count() >= 4);

            for (int i = 0; i < 4; i++)
            {
                var chara = _stageCoach.GetCharacter(db.ElementAt(i));
                roster.Characters[i] = chara;

                chara.RoleReactions[0] = new Reaction();
                
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

            so.BaseCharacter = character;
            
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
                roster.Hire(preset.BaseCharacter);
            }
            // for (int i = 0; i < 20; i++)
            // {
            //     roster.Hire(new StageCoach().GetRandomCharacter());
            // }
        }

        public void FillEnemy()
        {
            BaseCharacter[] enemies = new BaseCharacter[4];
            for (int i = 0; i < 4; i++)
            {
                enemies[i] = _stageCoach.GetRandomCharacter();
            }
            payload.SetEnemy(enemies);
        }
    }
}

#endif