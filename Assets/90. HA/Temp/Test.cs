using System.Collections.Generic;
using System.IO;
using Bond.UI.PartySelection;
using Bond.UI.RoleReactionEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Ha
{
    public class Test : MonoBehaviour
    {
        [Inject] StageCoach stageCoach;
        [Inject] PartySelectionController partySelectionController;
        [Inject] RoleReactionEditorController roleReactionEditorController;

        List<BaseCharacter> characters = new List<BaseCharacter>();
        
        private void Awake()
        {
            for (int i = 0; i < 3; i++)
            {
                characters.Add(CreateCharacter());
            }

            partySelectionController.roster = characters;
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                CreateCharacter();
            }
        }

        private BaseCharacter CreateCharacter()
        {
            var c = new BaseCharacter(stageCoach.GetRandomCharacter());
            Show(c.Data);
            return c;
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