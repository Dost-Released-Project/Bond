using System.Collections.Generic;
using Bond.Embark;
using Reactions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI.RoleReactionEditor
{
    public class RoleReactionEditorController : MonoBehaviour
    {
        [Header("UI 템플릿")]
        [SerializeField] private VisualTreeAsset slotTemplate;
        [SerializeField] private VisualTreeAsset charIconTemplate;

        [Header("파티 (테스트용 직접 할당)")]
        [SerializeField] private List<BaseCharacter> partyMembers;

        private PartyBarPresenter    _partyBar;
        private CharacterInfoPresenter _charInfo;
        private ReactionEditorPresenter _reactionEditor;
        private SkillSelectPanelPresenter _skillPanel;

        private int _selectedIndex = 0;

        // TODO: 실제 데이터는 TriggerDatabase ScriptableObject 등에서 로드해야 함
        private readonly Dictionary<RoleType, List<Trigger>> _triggersByRole = new()
        {
            [RoleType.Tanker] = new List<Trigger>
            {
                new Trigger { Id = 1, TriggerKey = "TRG_DEF_TG_IN",       Description = "피격 예고",    Category = 1 },
                new Trigger { Id = 2, TriggerKey = "TRG_SIT_ALLY_CRISIS",  Description = "아군 위기",    Category = 2 }
            },
            [RoleType.Dealer] = new List<Trigger>
            {
                new Trigger { Id = 3, TriggerKey = "TRG_OFF_KILL",  Description = "적 처치",  Category = 0 },
                new Trigger { Id = 4, TriggerKey = "TRG_OFF_CRIT",  Description = "치명타",   Category = 0 }
            },
            [RoleType.Supporter] = new List<Trigger>
            {
                new Trigger { Id = 5, TriggerKey = "TRG_SIT_ALLY_TURN_END", Description = "아군 턴 종료", Category = 2 },
                new Trigger { Id = 6, TriggerKey = "TRG_DEF_STATUS",        Description = "상태이상 발생", Category = 1 }
            },
            [RoleType.None] = new List<Trigger>()
        };

        private void Start()
        {
            if (partyMembers == null || partyMembers.Count == 0)
            {
                Debug.LogWarning("[RoleReactionEditor] 파티 멤버가 없습니다. Inspector에서 할당하세요.");
                return;
            }

            var root = GetComponent<UIDocument>().rootVisualElement;

            _skillPanel     = new SkillSelectPanelPresenter();
            _partyBar       = new PartyBarPresenter(root, charIconTemplate);
            _charInfo       = new CharacterInfoPresenter(root);
            _reactionEditor = new ReactionEditorPresenter(root, slotTemplate, _skillPanel);

            _partyBar.OnCharacterSelected += OnCharacterSelected;
            _charInfo.OnRoleChanged       += OnRoleChanged;

            _partyBar.Bind(partyMembers);
            SelectCharacter(0);
        }

        private void SelectCharacter(int index)
        {
            // AUTO-SAVE: 캐릭터 전환 시 별도 저장 처리 없이 roleReactions가 이미 갱신된 상태
            // TODO: 미저장 확인 다이얼로그가 필요하다면 여기에 추가

            _selectedIndex = index;
            var character = partyMembers[index];

            _partyBar.SetSelected(index);
            _charInfo.Bind(character);
            _reactionEditor.BindCharacter(character, GetRoleTriggers(character.roleType));
        }

        private void OnCharacterSelected(int index) => SelectCharacter(index);

        private void OnRoleChanged(RoleType role)
        {
            var character = partyMembers[_selectedIndex];
            _partyBar.RefreshSummary(partyMembers);
            _reactionEditor.RefreshRoleTriggers(GetRoleTriggers(role));
        }

        private List<Trigger> GetRoleTriggers(RoleType role)
        {
            return _triggersByRole.TryGetValue(role, out var list) ? list : new List<Trigger>();
        }
    }
}