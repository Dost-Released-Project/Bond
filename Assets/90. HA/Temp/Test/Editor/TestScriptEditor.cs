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
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            S1Test test = (S1Test)target;

            if (GUILayout.Button("Character To Json Test"))
            {
                test.CharacterToJson();
            }
            
            if (GUILayout.Button("Json To Character Test"))
            {
                test.JsonToCharacter();
            }

            if (GUILayout.Button("Fill Roster"))
            {
                test.FillRoster();
            }

            if (GUILayout.Button("Create Character Preset"))
            {
                test.CreateCharacterPresets();
            }
            
            if (GUILayout.Button("Fill Roster From Presets"))
            {
                test.FillRosterFromPreset();
            }
            
            if (GUILayout.Button("Test Presets"))
            {
                test.Test();
            }
        }
    }
}