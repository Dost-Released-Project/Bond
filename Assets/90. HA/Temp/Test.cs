using System.Collections.Generic;
using System.IO;
using Bond.Expedition;
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
        [Inject] ExpeditionPayload payload;

        List<BaseCharacter> characters = new List<BaseCharacter>();
        
        private void Awake()
        {
            
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                Debug.Log(payload);
            }
        }

        private BaseCharacter CreateCharacter()
        {
            var c = stageCoach.GetRandomCharacter();
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