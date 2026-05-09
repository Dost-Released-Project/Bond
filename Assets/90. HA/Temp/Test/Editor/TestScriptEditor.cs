using System;
using System.Collections.Generic;
using System.IO;
using Bond.Persistence;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace _90._HA.Temp.Test
{
    [CustomEditor(typeof(S1Test))]
    public class TestScriptEditor : Editor
    {
        public JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto
        };
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            S1Test test = (S1Test)target;

            if (GUILayout.Button("Character To Json Test"))
            {
                BaseCharacter chara = new StageCoach().GetRandomCharacter();
                string output = JsonConvert.SerializeObject(chara, Formatting.Indented, settings);
                File.WriteAllText("Assets/90. HA/Temp/characterJsonTest.json", output, System.Text.Encoding.UTF8);
                Debug.Log(output);
            }
            
            if (GUILayout.Button("Json To Character Test"))
            {
                string json = File.ReadAllText("Assets/90. HA/Temp/characterJsonTest.json", System.Text.Encoding.UTF8);
                BaseCharacter chara = JsonConvert.DeserializeObject<BaseCharacter>(json, settings);
                Debug.Log(chara);
            }

            if (GUILayout.Button("Save"))
            {
                var saveLoadSystem = new SaveLoadSystem();
                var roster = new Roster();

                saveLoadSystem.Register(roster);
                for (int i = 0; i < 4; i++)
                {
                    roster.characters.Add(new StageCoach().GetRandomCharacter());
                }
                saveLoadSystem.Save(roster);
            }
            
            if (GUILayout.Button("Load"))
            {
                var saveLoadSystem = new SaveLoadSystem();
                var roster = new Roster();
                saveLoadSystem.LoadAndRegister(roster);
            }
            if (GUILayout.Button("Register Test"))
            {
                test.Register();
            }
        }
    }

    public class Roster : ISaveable
    {
        public List<BaseCharacter> characters = new List<BaseCharacter>();
        
        public string Key => "roster";
        public object Data => characters;
        public void Restore(object data)
        {
            characters = (List<BaseCharacter>)data;
        }
    }
}