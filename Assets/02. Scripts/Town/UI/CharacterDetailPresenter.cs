using Reactions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI.Town
{
    public class CharacterDetailPresenter
    {
        private readonly CharacterSelector _selector;
        private readonly CharacterDetailController _controller;
        private readonly AccessoryBagView _accessoryBagView;
        private readonly EquipSlotsPresenter _equipSlots;

        private readonly VisualElement _panel;
        private readonly Label _titleLabel;
        private readonly Label _classLevelLabel;

        // Task 4: 역할 드롭다운
        private readonly Button        _roleBtnCurrent;
        private readonly VisualElement _rolePicker;
        private readonly Button        _roleOptTanker, _roleOptDealer, _roleOptSupporter;
        private bool _rolePickerOpen;

        // Task 1: 기본 능력치 박스
        private readonly Label _baseStatStr, _baseStatAgi, _baseStatInt;

        // 전투 스탯
        private readonly Label _statHp, _statDef, _statAtk, _statSpd;
        private readonly Label _statCrt, _statAcc, _statReactionCtrl;

        // Task 2: 게이지
        private readonly Label         _gaugeInsanityVal;
        private readonly VisualElement _gaugeHpFill, _gaugeInsanityFill;

        // Task 3: 성향 태그
        private readonly VisualElement _traitList;

        private readonly VisualElement _skillGrid;

        // Task 5: 리액션 슬롯 3분할
        private readonly VisualElement[] _reactionSlots         = new VisualElement[6];
        private readonly Label[]         _reactionTargetLabels  = new Label[6];
        private readonly Label[]         _reactionTriggerLabels = new Label[6];
        private readonly Label[]         _reactionSkillLabels   = new Label[6];
        private readonly Label[]         _reactionEchoLabels    = new Label[6];
        private readonly VisualElement[] _reactionTargetParts   = new VisualElement[6];
        private readonly VisualElement[] _reactionTriggerParts  = new VisualElement[6];
        private readonly VisualElement[] _reactionSkillParts    = new VisualElement[6];

        private readonly VisualElement _skillPicker;
        private int _skillPickerTargetSlot = -1;

        private readonly VisualElement _targetPicker;
        private int _targetPickerTargetSlot = -1;

        private BaseCharacter _character;
        private CharacterDetailViewMode _viewMode;
        private IInventory _currentInventory;

        public CharacterDetailPresenter(
            VisualElement root,
            CharacterSelector selector,
            CharacterDetailController controller,
            AccessoryBagView accessoryBagView,
            InventoryTransferService transferService)
        {
            _selector         = selector;
            _controller       = controller;
            _accessoryBagView = accessoryBagView;

            _panel      = root.Q("character-detail");
            _titleLabel = root.Q<Label>("character-detail__title");
            root.Q<Button>("character-detail__close-btn").clicked += OnCloseBtnClicked;

            _classLevelLabel = root.Q<Label>("char-detail__class-level");

            // Task 4: 역할 드롭다운
            _roleBtnCurrent   = root.Q<Button>("role-btn-current");
            _rolePicker       = root.Q("char-detail__role-picker");
            _roleOptTanker    = root.Q<Button>("role-opt-tanker");
            _roleOptDealer    = root.Q<Button>("role-opt-dealer");
            _roleOptSupporter = root.Q<Button>("role-opt-supporter");

            _roleBtnCurrent.clicked   += ToggleRolePicker;
            _roleOptTanker.clicked    += () => { _controller.SetRole(RoleType.Tanker);    CloseRolePicker(); };
            _roleOptDealer.clicked    += () => { _controller.SetRole(RoleType.Dealer);    CloseRolePicker(); };
            _roleOptSupporter.clicked += () => { _controller.SetRole(RoleType.Supporter); CloseRolePicker(); };

            // Task 1: 기본 능력치 박스
            _baseStatStr = root.Q<Label>("base-stat-str");
            _baseStatAgi = root.Q<Label>("base-stat-agi");
            _baseStatInt = root.Q<Label>("base-stat-int");

            // 전투 스탯
            _statHp  = root.Q<Label>("stat-hp");
            _statDef = root.Q<Label>("stat-def");
            _statAtk = root.Q<Label>("stat-atk");
            _statSpd = root.Q<Label>("stat-spd");

            // Task 2: 추가 스탯 + 게이지
            _statCrt           = root.Q<Label>("stat-crt");
            _statAcc           = root.Q<Label>("stat-acc");
            _statReactionCtrl  = root.Q<Label>("stat-reaction-ctrl");
            _gaugeInsanityVal  = root.Q<Label>("gauge-insanity-val");
            _gaugeHpFill       = root.Q("gauge-hp");
            _gaugeInsanityFill = root.Q("gauge-insanity");

            _traitList = root.Q("char-detail__trait-list");
            _skillGrid  = root.Q("char-detail__skill-grid");

            // Task 5: 리액션 슬롯 3분할
            for (int i = 0; i < 6; i++)
            {
                _reactionSlots[i]         = root.Q($"reaction-slot-{i}");
                _reactionTargetLabels[i]  = root.Q<Label>($"reaction-slot-{i}__target");
                _reactionTriggerLabels[i] = root.Q<Label>($"reaction-slot-{i}__trigger");
                _reactionSkillLabels[i]   = root.Q<Label>($"reaction-slot-{i}__skill");
                _reactionEchoLabels[i]    = root.Q<Label>($"reaction-slot-{i}__echo");
                _reactionTargetParts[i]   = root.Q($"reaction-slot-{i}__target-part");
                _reactionTriggerParts[i]  = root.Q($"reaction-slot-{i}__trigger-part");
                _reactionSkillParts[i]    = root.Q($"reaction-slot-{i}__skill-part");
            }

            // 성향 슬롯(2~5) target·trigger는 편집 불가
            for (int i = 2; i < 6; i++)
            {
                _reactionTargetParts[i]?.AddToClassList("char-detail__reaction-part--fixed");
                _reactionTriggerParts[i]?.AddToClassList("char-detail__reaction-part--fixed");
            }

            var equipRoot = root.Q("char-detail__equip-slots");
            _equipSlots = new EquipSlotsPresenter(equipRoot, controller, transferService);
            _equipSlots.OnInventoryOpenRequested += () => _accessoryBagView.ToggleWindow();
            _equipSlots.OnUnequipRequested       += OnUnequipRequested;

            _controller.OnRoleChanged      += _ => RefreshIdentity();
            _controller.OnReactionChanged  += RefreshReactionSlot;
            _controller.OnAccessoryChanged += () => _equipSlots.SetCharacter(_character);

            // 스킬 파트 클릭 → 스킬 피커
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                _reactionSkillParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_viewMode == CharacterDetailViewMode.ReadOnly) return;
                    if (idx >= 2 && _viewMode == CharacterDetailViewMode.EquipOnly) return;
                    OpenSkillPicker(idx);
                    evt.StopPropagation();
                });
            }

            // 역할 슬롯(0~1) target 파트 클릭 → 대상 피커
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                _reactionTargetParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_viewMode != CharacterDetailViewMode.FullEdit) return;
                    OpenTargetPicker(idx);
                    evt.StopPropagation();
                });
            }

            // 스킬 피커
            _skillPicker = new VisualElement();
            _skillPicker.AddToClassList("char-detail__skill-picker");
            _skillPicker.style.display  = DisplayStyle.None;
            _skillPicker.style.position = Position.Absolute;
            _panel.Add(_skillPicker);

            // 대상 피커
            _targetPicker = new VisualElement();
            _targetPicker.AddToClassList("char-detail__skill-picker");
            _targetPicker.style.display  = DisplayStyle.None;
            _targetPicker.style.position = Position.Absolute;
            _panel.Add(_targetPicker);

            // 바깥 클릭 시 역할 드롭다운 닫기
            _panel.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_rolePickerOpen && !_rolePicker.worldBound.Contains(evt.position))
                    CloseRolePicker();
            });

            _selector.OnSelectionChanged += character => { if (character == null) Hide(); };
        }

        public void Show(BaseCharacter character, CharacterDetailViewMode mode, IInventory inventory)
        {
            _character        = character;
            _viewMode         = mode;
            _currentInventory = inventory;

            _controller.SetCharacter(character);
            _titleLabel.text = character.Name;

            RefreshAll();
            ApplyViewMode();

            _panel.RemoveFromClassList("character-detail--hidden");
            _panel.AddToClassList("character-detail--visible");
        }

        public void Hide()
        {
            _panel.RemoveFromClassList("character-detail--visible");
            _panel.AddToClassList("character-detail--hidden");
            CloseSkillPicker();
            CloseTargetPicker();
            CloseRolePicker();
        }

        public void SetViewMode(CharacterDetailViewMode mode)
        {
            _viewMode = mode;
            ApplyViewMode();
        }

        private void ApplyViewMode()
        {
            bool fullEdit = _viewMode == CharacterDetailViewMode.FullEdit;
            bool canEquip = _viewMode != CharacterDetailViewMode.ReadOnly;

            _roleBtnCurrent.SetEnabled(fullEdit);
            if (!fullEdit) CloseRolePicker();

            _equipSlots.SetEditable(canEquip);

            for (int i = 0; i < 6; i++)
            {
                bool editable = i < 2 && fullEdit;
                if (editable)
                    _reactionSlots[i].RemoveFromClassList("char-detail__reaction-slot--readonly");
                else
                    _reactionSlots[i].AddToClassList("char-detail__reaction-slot--readonly");
            }
        }

        private void RefreshAll()
        {
            if (_character == null) return;
            RefreshIdentity();
            RefreshStats();
            RefreshTraits();
            RefreshSkillGrid();
            for (int i = 0; i < 6; i++) RefreshReactionSlot(i);
            _equipSlots.SetCharacter(_character);
        }

        private void RefreshIdentity()
        {
            if (_character == null) return;

            string profName = _character.Profession?.Name ?? "—";
            _classLevelLabel.text = $"{profName}  Lv.{_character.Level}";

            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--tanker");
            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--dealer");
            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--supporter");
            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--none");

            switch (_character.RoleType)
            {
                case RoleType.Tanker:
                    _roleBtnCurrent.text = "탱커 ▶";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--tanker");
                    break;
                case RoleType.Dealer:
                    _roleBtnCurrent.text = "딜러 ▶";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--dealer");
                    break;
                case RoleType.Supporter:
                    _roleBtnCurrent.text = "서포터 ▶";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--supporter");
                    break;
                default:
                    _roleBtnCurrent.text = "미설정 ▶";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--none");
                    break;
            }
        }

        private void RefreshStats()
        {
            if (_character == null) return;
            var s = _character.Stat;

            _baseStatStr.text = s.STR.ToString();
            _baseStatAgi.text = s.AGI.ToString();
            _baseStatInt.text = s.INT.ToString();

            _statHp.text  = $"{s.current_Hp}/{s.max_Hp}";
            _statDef.text = s.def.ToString();
            _statAtk.text = s.atk.ToString();
            _statSpd.text = s.speed.ToString();
            _statCrt.text = $"{s.crt:P0}";
            _statAcc.text = $"{s.acc:P0}";
            _statReactionCtrl.text = s.Reaction_Ctrl switch
            {
                < 0.3f => "낮음",
                < 0.7f => "보통",
                _      => "높음"
            };

            float hpRatio = s.max_Hp > 0 ? (float)s.current_Hp / s.max_Hp : 0f;
            _gaugeHpFill.style.width = new StyleLength(new Length(hpRatio * 100f, LengthUnit.Percent));
            _gaugeHpFill.RemoveFromClassList("char-detail__gauge-fill--hp-low");
            if (hpRatio < 0.3f)
                _gaugeHpFill.AddToClassList("char-detail__gauge-fill--hp-low");

            float insanityRatio = _character.Insanity / 100f;
            _gaugeInsanityFill.style.width = new StyleLength(new Length(insanityRatio * 100f, LengthUnit.Percent));
            _gaugeInsanityVal.text = $"{_character.Insanity} / 100";
        }

        private void RefreshTraits()
        {
            _traitList.Clear();
            if (_character?.Traits == null) return;

            foreach (var trait in _character.Traits)
            {
                var tag = new Label();
                tag.AddToClassList("char-detail__trait-tag");

                if (trait == null)
                {
                    tag.text = "미해금";
                    tag.AddToClassList("char-detail__trait-tag--locked");
                }
                else
                {
                    tag.text    = trait.Name;
                    tag.tooltip = trait.Description;
                    tag.AddToClassList(trait.Type switch
                    {
                        E_TraitType.Positive => "char-detail__trait-tag--positive",
                        E_TraitType.Negative => "char-detail__trait-tag--negative",
                        _                    => "char-detail__trait-tag--neutral",
                    });
                }
                _traitList.Add(tag);
            }
        }

        private void RefreshSkillGrid()
        {
            _skillGrid.Clear();
            if (_character?.Skills == null) return;

            foreach (var skill in _character.Skills)
            {
                var chip = new VisualElement();
                chip.AddToClassList("char-detail__skill-chip");

                if (skill == null)
                {
                    chip.AddToClassList("char-detail__skill-chip--empty");
                }
                else
                {
                    var nameLabel = new Label(skill.Data?.DisplayName ?? "?");
                    nameLabel.AddToClassList("char-detail__skill-name");
                    chip.Add(nameLabel);
                }

                _skillGrid.Add(chip);
            }
        }

        private void RefreshReactionSlot(int slotIndex)
        {
            if (_character == null || slotIndex < 0 || slotIndex >= 6) return;

            Reaction reaction;
            if (slotIndex < 2)
            {
                reaction = slotIndex < _character.RoleReactions.Length
                    ? _character.RoleReactions[slotIndex]
                    : null;
            }
            else
            {
                int traitIdx = slotIndex - 2;
                reaction = traitIdx < _character.TraitReactions.Length
                    ? _character.TraitReactions[traitIdx]
                    : null;
            }

            bool locked = reaction == null;
            _reactionSlots[slotIndex].EnableInClassList("char-detail__reaction-slot--locked", locked);
            SetReactionPartsVisible(slotIndex, !locked);

            if (locked) return;

            // target
            bool hasTarget = reaction.Trigger is Trigger t0 && !string.IsNullOrEmpty(t0.SubjectCharacterId);
            _reactionTargetLabels[slotIndex].text = hasTarget
                ? ((Trigger)reaction.Trigger).SubjectCharacterId
                : "미설정";
            _reactionTargetLabels[slotIndex].EnableInClassList(
                "char-detail__reaction-part-val--placeholder", !hasTarget);

            // trigger
            bool hasTrigger = reaction.Trigger != null;
            _reactionTriggerLabels[slotIndex].text = hasTrigger
                ? GetTriggerDisplayText(reaction.Trigger)
                : "미설정";
            _reactionTriggerLabels[slotIndex].EnableInClassList(
                "char-detail__reaction-part-val--placeholder", !hasTrigger);

            // skill
            bool hasSkill = reaction.SkillIndex >= 0
                && reaction.SkillIndex < _character.Skills.Length
                && _character.Skills[reaction.SkillIndex] != null;
            _reactionSkillLabels[slotIndex].text = hasSkill
                ? GetSkillDisplayText(reaction.SkillIndex)
                : "미설정";
            _reactionSkillLabels[slotIndex].EnableInClassList(
                "char-detail__reaction-part-val--placeholder", !hasSkill);

            // echo
            bool complete = hasTarget && hasTrigger && hasSkill;
            _reactionEchoLabels[slotIndex].text = complete ? "◈" : "○";
            _reactionEchoLabels[slotIndex].EnableInClassList("char-detail__reaction-echo--empty", !complete);
        }

        private void SetReactionPartsVisible(int i, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_reactionTargetParts[i]  != null) _reactionTargetParts[i].style.display  = display;
            if (_reactionTriggerParts[i] != null) _reactionTriggerParts[i].style.display = display;
            if (_reactionSkillParts[i]   != null) _reactionSkillParts[i].style.display   = display;
            if (_reactionEchoLabels[i]   != null) _reactionEchoLabels[i].style.display   = display;
        }

        private string GetTriggerDisplayText(ITrigger trigger)
        {
            if (trigger is Trigger t && !string.IsNullOrEmpty(t.SubjectCharacterId))
                return $"대상: {t.SubjectCharacterId}";
            return "트리거";
        }

        private string GetSkillDisplayText(int skillIndex)
        {
            if (_character == null || skillIndex < 0 || skillIndex >= _character.Skills.Length)
                return "미설정";
            return _character.Skills[skillIndex]?.Data?.DisplayName ?? "미설정";
        }

        private void ToggleRolePicker()
        {
            if (_viewMode != CharacterDetailViewMode.FullEdit) return;
            _rolePickerOpen = !_rolePickerOpen;
            if (_rolePickerOpen)
                _rolePicker.AddToClassList("char-detail__role-picker--open");
            else
                _rolePicker.RemoveFromClassList("char-detail__role-picker--open");
        }

        private void CloseRolePicker()
        {
            _rolePickerOpen = false;
            _rolePicker.RemoveFromClassList("char-detail__role-picker--open");
        }

        private void OpenSkillPicker(int slotIndex)
        {
            CloseTargetPicker();
            _skillPickerTargetSlot = slotIndex;
            _skillPicker.Clear();
            _skillPicker.style.display = DisplayStyle.Flex;

            var skills = _controller.GetAvailableSkills();
            foreach (var skill in skills)
            {
                var captured = skill;
                var btn = new Button(() =>
                {
                    _controller.SetReactionSkill(_skillPickerTargetSlot, captured);
                    CloseSkillPicker();
                });
                btn.AddToClassList("char-detail__skill-picker-item");
                btn.text = captured.Data?.DisplayName ?? "?";
                _skillPicker.Add(btn);
            }

            if (skills.Count == 0)
            {
                var empty = new Label("스킬 없음");
                empty.AddToClassList("char-detail__skill-picker-empty");
                _skillPicker.Add(empty);
            }

            _panel.RegisterCallback<PointerDownEvent>(OnPanelPointerDownForPicker);
        }

        private void CloseSkillPicker()
        {
            _skillPicker.style.display = DisplayStyle.None;
            _skillPicker.Clear();
            _skillPickerTargetSlot = -1;
            _panel.UnregisterCallback<PointerDownEvent>(OnPanelPointerDownForPicker);
        }

        private void OnPanelPointerDownForPicker(PointerDownEvent evt)
        {
            if (_skillPicker.style.display == DisplayStyle.None) return;
            if (!_skillPicker.worldBound.Contains(evt.position))
                CloseSkillPicker();
        }

        private void OpenTargetPicker(int slotIndex)
        {
            CloseSkillPicker();
            _targetPickerTargetSlot = slotIndex;
            _targetPicker.Clear();
            _targetPicker.style.display = DisplayStyle.Flex;

            var members = _controller.GetPartyMembers();
            foreach (var member in members)
            {
                var captured = member;
                var btn = new Button(() =>
                {
                    _controller.SetReactionTarget(_targetPickerTargetSlot, captured.Id);
                    CloseTargetPicker();
                });
                btn.AddToClassList("char-detail__skill-picker-item");
                btn.text = captured.Name;
                _targetPicker.Add(btn);
            }

            if (members.Count == 0)
            {
                var empty = new Label("파티원 없음");
                empty.AddToClassList("char-detail__skill-picker-empty");
                _targetPicker.Add(empty);
            }

            _panel.RegisterCallback<PointerDownEvent>(OnPanelPointerDownForTargetPicker);
        }

        private void CloseTargetPicker()
        {
            _targetPicker.style.display = DisplayStyle.None;
            _targetPicker.Clear();
            _targetPickerTargetSlot = -1;
            _panel.UnregisterCallback<PointerDownEvent>(OnPanelPointerDownForTargetPicker);
        }

        private void OnPanelPointerDownForTargetPicker(PointerDownEvent evt)
        {
            if (_targetPicker.style.display == DisplayStyle.None) return;
            if (!_targetPicker.worldBound.Contains(evt.position))
                CloseTargetPicker();
        }

        private void OnUnequipRequested(int accIndex)
        {
            if (_currentInventory == null) return;
            _controller.UnequipAccessory(accIndex, _currentInventory);
        }

        private void OnCloseBtnClicked() => _selector.Deselect();
    }
}
