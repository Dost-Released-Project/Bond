using System.Collections.Generic;
using Bond.PartyManagement;
using Reactions;
using UnityEngine.UIElements;

namespace Bond.UI.RoleReactionEditor
{
    public class ReactionEditorPresenter
    {
        private readonly VisualElement _roleSlotContainer;
        private readonly VisualElement _traitSlotContainer;
        private readonly VisualTreeAsset _slotTemplate;
        private readonly SkillSelectPanelPresenter _skillPanel;

        private BaseCharacter _current;
        private readonly List<SlotViewModel> _roleSlotVMs  = new();
        private readonly List<SlotViewModel> _traitSlotVMs = new();
        private SlotViewModel _openSlot;

        private class SlotViewModel
        {
            public VisualElement Root;
            public DropdownField TriggerDropdown;
            public Button        SkillButton;
            public VisualElement EchoIcon;
            public VisualElement WarnIcon;
            public VisualElement SkillPanel;
            public VisualElement SkillGrid;
            public Reaction      Reaction;
            public bool          IsTraitSlot;
            public List<Trigger> AvailableTriggers = new();
        }

        public ReactionEditorPresenter(VisualElement root, VisualTreeAsset slotTemplate,
                                       SkillSelectPanelPresenter skillPanel)
        {
            _roleSlotContainer  = root.Q<VisualElement>("roleSlotContainer");
            _traitSlotContainer = root.Q<VisualElement>("traitSlotContainer");
            _slotTemplate       = slotTemplate;
            _skillPanel         = skillPanel;
            _skillPanel.OnSkillSelected += OnSkillPicked;
        }

        // 캐릭터 전환 시 호출
        public void BindCharacter(BaseCharacter character, List<Trigger> roleTriggers)
        {
            CloseSkillPanel();
            _current = character;
            BuildRoleSlots(roleTriggers);
            BuildTraitSlots();
        }

        // 역할 변경 시 호출 — 트리거 목록 갱신, 유효하지 않은 트리거 초기화
        public void RefreshRoleTriggers(List<Trigger> triggers)
        {
            foreach (var vm in _roleSlotVMs)
            {
                vm.AvailableTriggers = triggers;

                var choices = new List<string> { "— 없음 —" };
                choices.AddRange(triggers.ConvertAll(t => t.Description));
                vm.TriggerDropdown.choices = choices;

                if (vm.Reaction.Trigger is Trigger cur && !triggers.Contains(cur))
                {
                    vm.Reaction.Trigger = null;
                    vm.TriggerDropdown.SetValueWithoutNotify("— 없음 —");
                }

                RefreshIcons(vm);
            }
        }

        // ────────────────── 슬롯 생성 ──────────────────

        private void BuildRoleSlots(List<Trigger> roleTriggers)
        {
            _roleSlotContainer.Clear();
            _roleSlotVMs.Clear();

            for (int i = 0; i < _current.roleReactions.Length; i++)
            {
                var vm = CreateSlot(_current.roleReactions[i], isTraitSlot: false,
                                    roleTriggers);
                _roleSlotContainer.Add(vm.Root);
                _roleSlotVMs.Add(vm);
            }
        }

        private void BuildTraitSlots()
        {
            _traitSlotContainer.Clear();
            _traitSlotVMs.Clear();

            for (int i = 0; i < _current.traits.Length; i++)
            {
                var trait = _current.traits[i];
                if (trait == null || trait.fixedTrigger == null) continue;

                // 트리거는 Trait에서 고정. 스킬(Behaviour)은 traitReactions[i]에 저장
                var reaction = _current.traitReactions[i];
                reaction.Trigger = trait.fixedTrigger;

                var vm = CreateSlot(reaction, isTraitSlot: true,
                                    availableTriggers: null);

                vm.TriggerDropdown.SetValueWithoutNotify(trait.fixedTrigger.Description);
                vm.TriggerDropdown.SetEnabled(false);

                _traitSlotContainer.Add(vm.Root);
                _traitSlotVMs.Add(vm);
            }
        }

        private SlotViewModel CreateSlot(Reaction reaction, bool isTraitSlot,
                                         List<Trigger> availableTriggers)
        {
            var tree = _slotTemplate.CloneTree();
            var vm = new SlotViewModel
            {
                Root            = tree,
                TriggerDropdown = tree.Q<DropdownField>("triggerDropdown"),
                SkillButton     = tree.Q<Button>("skillButton"),
                EchoIcon        = tree.Q<VisualElement>("echoIcon"),
                WarnIcon        = tree.Q<VisualElement>("warnIcon"),
                SkillPanel      = tree.Q<VisualElement>("skillPanel"),
                SkillGrid       = tree.Q<VisualElement>("skillGrid"),
                Reaction        = reaction,
                IsTraitSlot     = isTraitSlot
            };

            // 인디케이터 색상
            tree.Q<VisualElement>("slotIndicator")
                .AddToClassList(isTraitSlot
                    ? "rre-slot__indicator--trait"
                    : "rre-slot__indicator--role");

            // 트리거 드롭다운 (역할 슬롯)
            if (!isTraitSlot && availableTriggers != null)
            {
                vm.AvailableTriggers = availableTriggers;
                var choices = new List<string> { "— 없음 —" };
                choices.AddRange(availableTriggers.ConvertAll(trig => trig.Description));
                vm.TriggerDropdown.choices = choices;
                vm.TriggerDropdown.SetValueWithoutNotify(
                    reaction.Trigger is Trigger cur ? cur.Description : "— 없음 —");

                vm.TriggerDropdown.RegisterValueChangedCallback(evt =>
                {
                    vm.Reaction.Trigger = vm.AvailableTriggers
                        .Find(trig => trig.Description == evt.newValue);
                    RefreshIcons(vm);
                });
            }

            // 스킬 버튼
            vm.SkillButton.text = reaction.Behaviour?.Data?.DisplayName ?? "— 선택 안 됨 —";
            vm.SkillButton.clicked += () => ToggleSkillPanel(vm);

            RefreshIcons(vm);
            return vm;
        }

        // ────────────────── 스킬 패널 ──────────────────

        private void ToggleSkillPanel(SlotViewModel vm)
        {
            if (_openSlot == vm) { CloseSkillPanel(); return; }

            CloseSkillPanel();
            vm.SkillPanel.style.display = DisplayStyle.Flex;
            _openSlot = vm;
            _skillPanel.Show(vm.SkillGrid, _current.skills, vm.Reaction.Behaviour);
        }

        private void CloseSkillPanel()
        {
            if (_openSlot == null) return;
            _openSlot.SkillPanel.style.display = DisplayStyle.None;
            _openSlot = null;
        }

        private void OnSkillPicked(SkillBase skill)
        {
            if (_openSlot == null) return;
            var vm = _openSlot;

            vm.Reaction.Behaviour = skill;
            vm.SkillButton.text   = skill.Data?.DisplayName ?? "—";

            RefreshIcons(vm);
            CloseSkillPanel();
        }

        // ────────────────── 아이콘 상태 갱신 ──────────────────

        private static void RefreshIcons(SlotViewModel vm)
        {
            bool hasAll = vm.Reaction.Trigger != null && vm.Reaction.Behaviour != null;

            // 에코 코어 클래스 초기화
            vm.EchoIcon.RemoveFromClassList("rre-slot__echo-icon--active");
            vm.EchoIcon.RemoveFromClassList("rre-slot__echo-icon--inactive");
            vm.EchoIcon.RemoveFromClassList("rre-slot__echo-icon--warn");

            if (vm.IsTraitSlot)
            {
                // 성향 슬롯: 에코 코어는 항상 경고 표시 (돌발 행동 가능성 암시)
                vm.EchoIcon.AddToClassList("rre-slot__echo-icon--warn");
                vm.WarnIcon.style.display = DisplayStyle.None;
                return;
            }

            // 역할 슬롯: 트리거-스킬 적합성 검사
            bool incompatible = false;
            if (hasAll && vm.Reaction.Trigger is Trigger trigger && vm.Reaction.Behaviour.Data != null)
                incompatible = !trigger.IsCompatibleWith(vm.Reaction.Behaviour.Data.Type);

            if (hasAll)
                vm.EchoIcon.AddToClassList("rre-slot__echo-icon--active");
            else
                vm.EchoIcon.AddToClassList("rre-slot__echo-icon--inactive");

            vm.WarnIcon.style.display = incompatible ? DisplayStyle.Flex : DisplayStyle.None;

            if (incompatible) vm.Root.AddToClassList("rre-slot--warn");
            else              vm.Root.RemoveFromClassList("rre-slot--warn");
        }
    }
}