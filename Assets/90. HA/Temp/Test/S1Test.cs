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
        DataBaseSO professionDb;
        public List<BaseCharacter> characters;

        public Reaction reaction = new Reaction() { Trigger = new Trigger()};

        public void Start()
        {
            professionDb = Addressables.LoadAssetAsync<DataBaseSO>("ClassDataBase").WaitForCompletion();
            
            payload.Clear();
            FillRoster();
            FillEnemy();

            var db = professionDb.Query<ClassSO>(so => true);

            Debug.Log(db.Count());
            foreach (var so in db)
            {
                var chara = roster.Characters.Find(c => so.DisplayName == c.Profession.Name);
                SaveCharacterAsPreset(chara);
            }
            
            characters = roster.Characters;
        }

        private void SaveCharacterAsPreset(BaseCharacter character)
        {
            string root = "Assets/90. HA/Temp/Test/So";
            string path = Path.Combine(root, $"CharacterPreset_{character.Name}.asset");
            
            var so = AssetDatabase.LoadAssetAtPath<CharacterPreset>(path);
            bool isNew = so == null;
            if (isNew) so = ScriptableObject.CreateInstance<CharacterPreset>();

            so.BaseCharacter = character;
            
            if (isNew) AssetDatabase.CreateAsset(so, path);
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssetIfDirty(so); // Unity 6+ 에서 즉시 저장을 보장
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                var chara = _stageCoach.GetRandomCharacter();
                chara.RoleReactions[0] = new Reaction()
                {
                    Source = ReactionSource.Role,
                    Behaviour = chara.Skills[0],
                    Trigger = new Trigger(E_ObserveFilter.Self, E_CompareFilter.Target, chara, new HpBelowCondition(0.3f))
                };
            }
        }

        public void FillRoster()
        {
            for (int i = 0; i < 20; i++)
            {
                roster.Hire(new StageCoach().GetRandomCharacter());
            }
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