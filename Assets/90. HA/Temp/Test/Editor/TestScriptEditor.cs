using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
                var roster = new Roster();

                for (int i = 0; i < 4; i++)
                {
                    roster.Hire(new StageCoach().GetRandomCharacter());
                }
                SaveLoadSystem.Save(roster);
            }
            
            if (GUILayout.Button("Load"))
            {
                var roster = new Roster();
                SaveLoadSystem.Load(roster);

                StringBuilder sb = new StringBuilder();
                foreach (var cha in roster.Characters)
                {
                    sb.AppendLine(cha.ToString());
                }
                Debug.Log(sb.ToString());
            }
        }
    }
}