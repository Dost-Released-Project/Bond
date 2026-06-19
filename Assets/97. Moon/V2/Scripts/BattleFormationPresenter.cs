using System;
using System.Collections.Generic;
using System.Linq;
using BattleSystem.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace BattleSystem
{
    public class BattleFormationPresenter : MonoBehaviour
    {
        [SerializeField]
        private CharacterSlot[] characterSlots = new CharacterSlot[8]; // 플레이어 4 + 적군 4
        private ICharacterSelector m_CharacterSelector;
        private IFormationManager m_FormationManager;

        private BaseCharacter m_currentActor;
        private BaseCharacter m_TargetingCaster; // 현재 타겟팅을 주도하는 캐릭터
        private SkillBase m_currentSkill;
        private List<CharacterSlot> m_validSlots = new List<CharacterSlot>();
        private bool m_isTargetingMode;

        [Inject]
        public void Construct(ICharacterSelector characterSelector, IFormationManager formationManager)
        {
            m_CharacterSelector = characterSelector;
            m_FormationManager = formationManager;
            
            m_CharacterSelector.OnSelectionChanged += HandleSelectionChanged;
        }

        private void Start()
        {
            FindAndBindSlots();
        }

        private void OnDestroy()
        {
            UnbindSlots();
            ResetAllHighlights();
            
            if (m_CharacterSelector != null)
                m_CharacterSelector.OnSelectionChanged -= HandleSelectionChanged;
            
            if (m_currentActor != null)
                m_currentActor.onTargetSelectionStarted -= HandleTargetSelectionStarted;
        }

        private void OnDisable()
        {
            ResetAllHighlights();
        }
        
        public void FindAndBindSlots()
        {
            if (m_CharacterSelector == null) return;
            
            UnbindSlots();
            ResetAllHighlights();

            // 씬의 모든 슬롯을 가져옴 (PlayerSlot, EnemySlot 등 태그 무시하고 전체 수집)
            characterSlots = FindObjectsByType<CharacterSlot>(FindObjectsSortMode.None).ToArray();

            foreach (var slot in characterSlots)
            {
                if (slot != null) slot.OnSlotClicked += HandleSlotClicked;
            }

            HandleSelectionChanged(m_CharacterSelector.Selected);
        }

        private void UnbindSlots()
        {
            if (characterSlots == null) return;

            foreach (var slot in characterSlots)
            {
                if (slot != null) slot.OnSlotClicked -= HandleSlotClicked;
            }
        }

        /// <summary>
        /// 모든 슬롯의 강조 효과(Hover, Click)를 강제로 해제합니다.
        /// </summary>
        public void ResetAllHighlights()
        {
            if (characterSlots == null) return;
            foreach (var slot in characterSlots)
            {
                if (slot == null) continue;
                slot.ResetAllStates(); // 잔상 방지: 모든 플래그 완벽히 밀어버림
            }
        }

        private void HandleSlotClicked(CharacterSlot clickedSlot)
        {
            if (m_isTargetingMode && m_TargetingCaster != null)
            {
                // [방어막] 타겟팅 모드: 유효한 슬롯인 경우에만 확정. 
                // 무효 슬롯 클릭 시 Deselect()를 타지 않도록 하여 데드락 방지.
                if (m_validSlots.Contains(clickedSlot))
                {
                    var caster = m_TargetingCaster;
                    ExitTargetingMode();
                    caster.ConfirmTargetSelection(clickedSlot);
                }
                return;
            }

            // 일반 모드: 캐릭터 선택 전환
            if (clickedSlot.IsEmpty || clickedSlot.Occupant == null)
            {
                m_CharacterSelector.Deselect();
                return;
            }

            m_CharacterSelector.ToggleSelection(clickedSlot.Occupant);
        }

        private void HandleSelectionChanged(BaseCharacter selectedCharacter)
        {
            if (characterSlots == null || m_isTargetingMode) return;

            // 기존 행동 캐릭터 이벤트 해제
            if (m_currentActor != null)
                m_currentActor.onTargetSelectionStarted -= HandleTargetSelectionStarted;

            m_currentActor = selectedCharacter;

            // 새 행동 캐릭터 이벤트 구독
            if (m_currentActor != null)
                m_currentActor.onTargetSelectionStarted += HandleTargetSelectionStarted;

            foreach (var slot in characterSlots)
            {
                if (slot == null) continue;
                
                // [핵심] 턴(선택)이 넘어갈 때마다 이전 상태 찌꺼기를 전부 지워버림
                slot.ResetAllStates();
                
                // [기획 요구사항] 현재 턴 캐릭터(Actor) = Hover 색상 (IsSelected 상태)
                bool isSelected = !slot.IsEmpty && slot.Occupant == selectedCharacter;
                if (isSelected) slot.SetSelected(true); 
            }
        }

        private void HandleTargetSelectionStarted(BaseCharacter actor, SkillBase skill)
        {
            if (skill == null)
            {
                ExitTargetingMode();
                return;
            }

            m_TargetingCaster = actor;
            m_currentSkill = skill;
            m_isTargetingMode = true;

            // 유효한 타겟 슬롯 계산
            m_validSlots = actor.GetSelectableSlots(skill.Data);

            // 유효 슬롯 하이라이트 (시각적 피드백)
            foreach (var slot in characterSlots)
            {
                if (slot == null) continue;

                slot.SetTargetable(false); // 기존 타겟 상태 초기화
                
                // [기획 요구사항] 스킬 대상(Target) = Click 색상 (IsTargetable 상태)
                bool isValid = m_validSlots.Contains(slot);
                if (isValid) slot.SetTargetable(true);
            }
        }

        private void ExitTargetingMode()
        {
            m_isTargetingMode = false;
            m_TargetingCaster = null;
            // HandleSelectionChanged에서 모든 상태를 초기화하고 Actor를 다시 세팅함
            HandleSelectionChanged(m_currentActor);
        }
    }
}