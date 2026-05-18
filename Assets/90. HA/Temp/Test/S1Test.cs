using BattleSystem;
using Bond.Embark;
using Bond.Expedition;
using PipeLine;
using Reactions;
using UnityEngine;
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

        public void Start()
        {
            payload.Clear();
            FillRoster();
            FillEnemy();
        }

        private void Update()
        {
            if (Keyboard.current.numpad0Key.wasPressedThisFrame)
            {
                var chara = _stageCoach.GetRandomCharacter();
                chara.RoleReactions[0] = new Reaction()
                {
                    Agent = chara,
                    Source = ReactionSource.Role,
                    Behaviour = chara.Skills[0],
                    Trigger = new Trigger()
                    {
                        Subject = chara,
                        Condition = new EvadeCondition()
                    }
                };
                chara.RoleReactions[1] = new Reaction()
                {
                    Agent = chara,
                    Source = ReactionSource.Role,
                    Behaviour = chara.Skills[0],
                    Trigger = new Trigger()
                    {
                        Subject = chara,
                        Condition = new CritCondition()
                    }
                };
            }
        }

        public void FillRoster()
        {
            for (int i = 0; i < 10; i++)
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