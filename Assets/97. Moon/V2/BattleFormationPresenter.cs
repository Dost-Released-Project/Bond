using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace BattleSystem
{
    public class BattleFormationPresenter : MonoBehaviour
    {
        // 인스펙터 노출 제거: 이제 코드가 알아서 찾을 겁니다.
        private CharacterSlot[] characterSlots;
        private ICharacterSelector m_CharacterSelector;

        [Inject]
        public void Construct(ICharacterSelector characterSelector)
        {
            m_CharacterSelector = characterSelector;
            // 셀렉터 이벤트는 최초 1회만 구독하면 되므로 여기서 구독합니다.
            m_CharacterSelector.OnSelectionChanged += HandleSelectionChanged;
        }

        private void Start()
        {
            FindAndBindSlots();
        }

        private void OnDestroy()
        {
            // 컴포넌트 파괴 시 전체 이벤트 해제 (메모리 누수 방지)
            UnbindSlots();
            
            if (m_CharacterSelector != null)
            {
                m_CharacterSelector.OnSelectionChanged -= HandleSelectionChanged;
            }
        }
        
        private void Update()
        {
            // 테스트용: T 키를 누르면 씬에서 슬롯을 긁어옵니다.
            if (Keyboard.current.tKey.wasPressedThisFrame) FindAndBindSlots();
        }

        /// <summary>
        /// 💡 핵심 포인트: 전투 씬이 로드되거나 슬롯이 모두 생성된 직후 이 함수를 한 번 호출해주세요!
        /// 예: BattleFlowManager 같은 곳에서 슬롯 생성 직후 presenter.FindAndBindSlots(); 호출
        /// </summary>
        public void FindAndBindSlots()
        {
            // 0. 방어 코드: VContainer 주입이 정상적으로 되었는지 확인합니다.
            if (m_CharacterSelector == null)
            {
                Debug.LogError("🚨 [디버그] m_CharacterSelector가 null입니다! VContainer 주입(Inject)이 실패했습니다. LifetimeScope에 BattleFormationPresenter가 등록되어 있는지 확인해주세요.");
                return; // 에러를 띄우고 함수를 멈춰서 NRE를 방지합니다.
            }
            
            // 1. 혹시 이미 찾아둔 슬롯이 있다면 기존 이벤트를 먼저 안전하게 지워줍니다. (중복 구독 방지)
            UnbindSlots();

            // 2. 씬에 존재하는 모든 CharacterSlot 컴포넌트를 찾아 배열에 담습니다. (태그나 레이어 상관없이 스크립트 기준으로 찾음)
#if UNITY_2023_1_OR_NEWER
            characterSlots = FindObjectsByType<CharacterSlot>(FindObjectsSortMode.None);
#else
            characterSlots = FindObjectsOfType<CharacterSlot>();
#endif

            if (characterSlots.Length == 0)
            {
                Debug.LogWarning("경고: 씬에서 CharacterSlot을 하나도 찾지 못했습니다. 슬롯이 생성되기 전에 함수가 호출된 건 아닌지 확인해보세요.");
                return;
            }

            // 3. 찾아낸 슬롯들에 이벤트를 연결해줍니다.
            foreach (var slot in characterSlots)
            {
                if (slot != null) slot.OnSlotClicked += HandleSlotClicked;
            }

            // 4. 연결 직후, 현재 셀렉터에 선택되어 있는 캐릭터가 있다면 하이라이트 UI를 갱신합니다.
            HandleSelectionChanged(m_CharacterSelector.Selected);
            
            Debug.Log($"총 {characterSlots.Length}개의 슬롯을 성공적으로 찾아서 연결했습니다!");
        }

        // 기존 슬롯들의 이벤트를 해제하는 안전장치
        private void UnbindSlots()
        {
            if (characterSlots == null) return;

            foreach (var slot in characterSlots)
            {
                if (slot != null) slot.OnSlotClicked -= HandleSlotClicked;
            }
        }

        private void HandleSlotClicked(CharacterSlot clickedSlot)
        {
            if (clickedSlot.IsEmpty || clickedSlot.Occupant == null)
            {
                m_CharacterSelector.Deselect();
                return;
            }

            m_CharacterSelector.ToggleSelection(clickedSlot.Occupant);
        }

        private void HandleSelectionChanged(BaseCharacter selectedCharacter)
        {
            if (characterSlots == null) return;

            foreach (var slot in characterSlots)
            {
                if (slot == null) continue;
                bool isSelected = !slot.IsEmpty && slot.Occupant == selectedCharacter;
                slot.SetForceClick(isSelected);
            }
        }
    }
}