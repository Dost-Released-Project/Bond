using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Ha
{
    public class Test : MonoBehaviour
    {
        [Inject] StageCoach stageCoach;
        
        public List<BaseCharacter> characters = new List<BaseCharacter>();
        
        private void Start()
        {
            foreach (var chara in characters)
            {
                chara.StatComponent.StatCalculate();
            }
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                var d = stageCoach.GetRandomCharacter();
                Show(d);
            }
        }

        private void Show(BaseCharacterData data)
        {
            string str = "";
            str += data.Name;

            foreach (var trait in data.Traits)
            {
                str += $"\nTrait: {trait}";
            }
            foreach (var skill in data.Skills)
            {
                str += $"\nSkill: {skill}";
            }
            
            Debug.Log(str);
        }
    }
}