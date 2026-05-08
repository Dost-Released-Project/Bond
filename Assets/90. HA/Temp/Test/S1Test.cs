using System.Collections.Generic;
using Bond.Embark;
using Bond.Expedition;
using Bond.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using VContainer;

namespace _90._HA.Temp.Test
{
    public class S1Test : MonoBehaviour
    {
        [Inject] public ExpeditionInventory _expeditionInventory;
        [Inject] public EmbarkManager _embarkManager;
        [Inject] public PartyManager _partyManager;
        [Inject] public StageCoach _stageCoach;
        [Inject] public ExpeditionPayload payload;
        [Inject] public SaveLoadSystem saveLoadSystem;
        
        public List<BaseItem> items;
        public BaseCharacter _character;

        public void Start()
        {
            payload.Clear();
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                Debug.Log(payload);
            }
            
            if (Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                var slots = _expeditionInventory.GetAll();
                Debug.Log(slots.Count);
            }

            if (Keyboard.current.numpad7Key.wasPressedThisFrame)
            {
                payload.Clear();
                _partyManager.Clear();
            }

            if (Keyboard.current.numpad8Key.wasPressedThisFrame)
            {
                foreach (var item in items)
                {
                    _expeditionInventory.AddItemAuto(item, Random.Range(1, 5));
                }

                for (int i = 0; i < 4; i++)
                {
                    var chara = _stageCoach.GetRandomCharacter();
                    _partyManager.TryAddMember(chara);
                }
            
                _embarkManager.SavePayload();
            }

            if (Keyboard.current.numpad9Key.wasPressedThisFrame)
            {
                SceneManager.LoadSceneAsync("S2");
            }
        }

        public void Register()
        {
            saveLoadSystem.Register(_partyManager);
            saveLoadSystem.DebugList();
        }
    }
}