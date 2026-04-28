using System.Collections.Generic;
using Bond.Embark;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Bond.UI.PartySelection
{
    public class PartySelectionController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private UIDocument uiDocument;

        [Header("다음 화면 (RoleReactionEditor)")]
        [SerializeField] private GameObject roleReactionEditorScreen;

        [Header("보유 대원 (테스트용 직접 할당)")]
        [SerializeField] private List<BaseCharacter> roster;

        private PartyPanelPresenter  _partyPanel;
        private RosterPanelPresenter _rosterPanel;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;

            _partyPanel  = new PartyPanelPresenter(root);
            _rosterPanel = new RosterPanelPresenter(root);

            _rosterPanel.Bind(roster);

            _partyPanel.OnMemberAssigned += OnMemberAssigned;
            _partyPanel.OnMemberRemoved  += OnMemberRemoved;
            _rosterPanel.OnCharacterSelected += OnCharacterSelected;

            _partyPanel.BindNextButton(GoToNextScreen);
        }

        private void OnCharacterSelected(BaseCharacter character)
        {
            if (_partyPanel.IsAssigned(character))
            {
                _partyPanel.TryRelease(character);
                return;
            }
            if (_partyPanel.TryAssign(character) == false) return;

            _rosterPanel.SetSelected(character, selected: true);
        }

        private void OnMemberAssigned(BaseCharacter character)
        {
            _rosterPanel.SetSelected(character, selected: true);
        }

        private void OnMemberRemoved(int slotIndex)
        {
            // 해제된 슬롯의 캐릭터를 로스터에서 다시 선택 가능하게 복원
            // (슬롯 상태는 이미 초기화됨 — 여기서 연결된 카드를 찾아 해제)
            foreach (var character in roster)
            {
                bool stillAssigned = _partyPanel.IsAssigned(character);
                _rosterPanel.SetSelected(character, stillAssigned);
            }
        }

        private void GoToNextScreen()
        {
            // 화면 전환: 이 GameObject 비활성화 → RoleReactionEditor 활성화
            if (roleReactionEditorScreen != null)
                roleReactionEditorScreen.SetActive(true);

            gameObject.SetActive(false);
        }
    }
}