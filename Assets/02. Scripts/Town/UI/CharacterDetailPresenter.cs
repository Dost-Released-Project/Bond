using System;
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
        private readonly Label _statCrt, _statAcc, _statInsanityCtrl, _statReactionCtrl;

        // Task 2: 게이지
        private readonly Label         _gaugeInsanityVal;
        private readonly Label         _gaugeInsanityWarnLabel;
        private readonly VisualElement _gaugeHpFill, _gaugeInsanityFill;

        // 잠금 배너
        private readonly Label _lockBannerRole, _lockBannerEquip, _lockBannerReaction;

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

        // 인라인 풀
        private readonly VisualElement[] _reactionPools = new VisualElement[6];
        private int    _openPoolSlot = -1;
        private string _openPoolPart = null;

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
            _statCrt                = root.Q<Label>("stat-crt");
            _statAcc                = root.Q<Label>("stat-acc");
            _statInsanityCtrl       = root.Q<Label>("stat-insanity-ctrl");
            _statReactionCtrl       = root.Q<Label>("stat-reaction-ctrl");
            _gaugeInsanityVal       = root.Q<Label>("gauge-insanity-val");
            _gaugeInsanityWarnLabel = root.Q<Label>("gauge-insanity-warn");
            _gaugeHpFill            = root.Q("gauge-hp");
            _gaugeInsanityFill      = root.Q("gauge-insanity");

            _lockBannerRole     = root.Q<Label>("lock-banner-role");
            _lockBannerEquip    = root.Q<Label>("lock-banner-equip");
            _lockBannerReaction = root.Q<Label>("lock-banner-reaction");

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
                _reactionTargetParts[i]?.AddToClassList("char-detail__slot-part--fixed");
                _reactionTriggerParts[i]?.AddToClassList("char-detail__slot-part--fixed");
            }

            var equipRoot = root.Q("char-detail__equip-slots");
            _equipSlots = new EquipSlotsPresenter(equipRoot, controller, transferService);
            _equipSlots.OnInventoryOpenRequested += () => _accessoryBagView.ToggleWindow();
            _equipSlots.OnUnequipRequested       += OnUnequipRequested;

            _controller.OnRoleChanged      += _ => RefreshIdentity();
            _controller.OnReactionChanged  += RefreshReactionSlot;
            _controller.OnAccessoryChanged += () => _equipSlots.SetCharacter(_character);

            // 리액션 인라인 풀 요소 쿼리
            for (int i = 0; i < 6; i++)
                _reactionPools[i] = root.Q($"inline-pool-{i}");

            // 스킬 파트 클릭 → 인라인 풀
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                _reactionSkillParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_viewMode == CharacterDetailViewMode.ReadOnly) return;
                    if (idx >= 2 && _viewMode == CharacterDetailViewMode.EquipOnly) return;
                    TogglePool(idx, "skill");
                    evt.StopPropagation();
                });
            }

            // 역할 슬롯(0~1) target·trigger 파트 클릭 → 인라인 풀
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                _reactionTargetParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_viewMode != CharacterDetailViewMode.FullEdit) return;
                    TogglePool(idx, "target");
                    evt.StopPropagation();
                });
                _reactionTriggerParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_viewMode != CharacterDetailViewMode.FullEdit) return;
                    TogglePool(idx, "trigger");
                    evt.StopPropagation();
                });
            }

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
            CloseAllPools();
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

            _roleBtnCurrent.EnableInClassList("char-detail__role-current--disabled", !fullEdit);
            _roleBtnCurrent.pickingMode = fullEdit ? PickingMode.Position : PickingMode.Ignore;
            if (!fullEdit) CloseRolePicker();

            _equipSlots.SetEditable(canEquip);

            for (int i = 0; i < 6; i++)
            {
                bool editable = i < 2 && fullEdit;
                _reactionSlots[i].EnableInClassList("char-detail__reaction-slot--disabled", !editable);
                _reactionSlots[i].pickingMode = editable ? PickingMode.Position : PickingMode.Ignore;
            }

            SetLockBanner(_lockBannerRole, _viewMode switch
            {
                CharacterDetailViewMode.EquipOnly => "탐사 중 변경 불가",
                CharacterDetailViewMode.ReadOnly  => "전투 중 변경 불가",
                _                                => "",
            });
            SetLockBanner(_lockBannerEquip, _viewMode switch
            {
                CharacterDetailViewMode.ReadOnly => "전투 중 변경 불가",
                _                               => "",
            });
            SetLockBanner(_lockBannerReaction, _viewMode switch
            {
                CharacterDetailViewMode.EquipOnly => "탐사 중 변경 불가",
                CharacterDetailViewMode.ReadOnly  => "전투 중 변경 불가",
                _                                => "",
            });
        }

        private void SetLockBanner(Label banner, string msg)
        {
            banner.RemoveFromClassList("char-detail__lock-banner--visible");
            banner.RemoveFromClassList("char-detail__lock-banner--equip-only");
            banner.RemoveFromClassList("char-detail__lock-banner--read-only");
            banner.text = msg;
            if (string.IsNullOrEmpty(msg)) return;
            banner.AddToClassList("char-detail__lock-banner--visible");
            banner.AddToClassList(_viewMode == CharacterDetailViewMode.EquipOnly
                ? "char-detail__lock-banner--equip-only"
                : "char-detail__lock-banner--read-only");
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
            _statInsanityCtrl.text = s.Insanity_Ctrl switch
            {
                < 0.3f => "낮음",
                < 0.7f => "보통",
                _      => "높음"
            };
            _statReactionCtrl.text = s.Reaction_Ctrl switch
            {
                < 0.3f => "낮음",
                < 0.7f => "보통",
                _      => "높음"
            };

            float hpRatio = s.max_Hp > 0 ? (float)s.current_Hp / s.max_Hp : 0f;
            _gaugeHpFill.style.width = new StyleLength(new Length(hpRatio * 100f, LengthUnit.Percent));
            _gaugeHpFill.EnableInClassList("char-detail__gauge-fill--hp-low", hpRatio < 0.3f);

            float insanityRatio = _character.Insanity / 100f;
            _gaugeInsanityFill.style.width = new StyleLength(new Length(insanityRatio * 100f, LengthUnit.Percent));
            _gaugeInsanityFill.RemoveFromClassList("char-detail__gauge-fill--insanity-safe");
            _gaugeInsanityFill.RemoveFromClassList("char-detail__gauge-fill--insanity-warn");
            _gaugeInsanityFill.RemoveFromClassList("char-detail__gauge-fill--insanity-crit");
            _gaugeInsanityFill.AddToClassList(insanityRatio switch
            {
                < 0.5f => "char-detail__gauge-fill--insanity-safe",
                < 0.8f => "char-detail__gauge-fill--insanity-warn",
                _      => "char-detail__gauge-fill--insanity-crit",
            });
            _gaugeInsanityVal.text = $"{_character.Insanity} / 100";

            bool insanityWarn = insanityRatio >= 0.5f;
            _gaugeInsanityWarnLabel.EnableInClassList("char-detail__gauge-warn--visible", insanityWarn);
            _gaugeInsanityWarnLabel.text = insanityWarn ? "⚠" : "";
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

            // target (SubjectCharacterId)
            bool hasTarget = reaction.Trigger is Trigger t0 && !string.IsNullOrEmpty(t0.SubjectCharacterId);
            string targetName = hasTarget && BaseCharacter.Dict.TryGetValue(((Trigger)reaction.Trigger).SubjectCharacterId, out var subj)
                ? subj.Name
                : hasTarget ? ((Trigger)reaction.Trigger).SubjectCharacterId : "미설정";
            _reactionTargetLabels[slotIndex].text = targetName;
            _reactionTargetLabels[slotIndex].EnableInClassList(
                "char-detail__slot-part-val--placeholder", !hasTarget);

            // trigger (조건)
            bool hasTrigger = reaction.Trigger is Trigger tt && tt.Conditions.Count > 0;
            _reactionTriggerLabels[slotIndex].text = hasTrigger
                ? GetTriggerDisplayText(reaction.Trigger)
                : "미설정";
            _reactionTriggerLabels[slotIndex].EnableInClassList(
                "char-detail__slot-part-val--placeholder", !hasTrigger);

            // skill
            bool hasSkill = reaction.SkillIndex >= 0
                && reaction.SkillIndex < _character.Skills.Length
                && _character.Skills[reaction.SkillIndex] != null;
            _reactionSkillLabels[slotIndex].text = hasSkill
                ? GetSkillDisplayText(reaction.SkillIndex)
                : "미설정";
            _reactionSkillLabels[slotIndex].EnableInClassList(
                "char-detail__slot-part-val--placeholder", !hasSkill);

            // echo
            bool complete = hasTarget && hasTrigger && hasSkill;
            _reactionEchoLabels[slotIndex].text = complete ? "◈" : "○";
            _reactionEchoLabels[slotIndex].EnableInClassList("char-detail__slot-echo--empty", !complete);
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
            if (trigger == null) return "미설정";
            if (trigger is Trigger t)
            {
                if (t.Conditions.Count > 0)
                    return t.Conditions[0].Description ?? "트리거";
                return "트리거";
            }
            return trigger.GetType().Name;
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

        private void TogglePool(int slotIndex, string part)
        {
            if (_openPoolSlot == slotIndex && _openPoolPart == part)
            {
                CloseAllPools();
                return;
            }
            CloseAllPools();
            _openPoolSlot = slotIndex;
            _openPoolPart = part;

            var pool = _reactionPools[slotIndex];
            if (pool == null) return;

            // 헤더
            var header = new VisualElement();
            header.AddToClassList("char-detail__pool-header");

            var title = new Label(part switch
            {
                "target"  => "관찰 대상",
                "trigger" => "조건",
                _         => "스킬",
            });
            title.AddToClassList("char-detail__pool-title");
            header.Add(title);

            var closeBtn = new Button(CloseAllPools);
            closeBtn.AddToClassList("char-detail__pool-close-btn");
            closeBtn.text = "닫기 ×";
            header.Add(closeBtn);
            pool.Add(header);

            // 칩 컨테이너
            var chips = new VisualElement();
            chips.AddToClassList("char-detail__pool-chips");
            switch (part)
            {
                case "target":  BuildTargetChips(slotIndex, chips);  break;
                case "trigger": BuildTriggerChips(slotIndex, chips); break;
                default:        BuildSkillChips(slotIndex, chips);   break;
            }
            pool.Add(chips);

            bool isRole = slotIndex < 2;
            pool.AddToClassList(part switch
            {
                "target"  => "char-detail__inline-pool--target",
                "trigger" => "char-detail__inline-pool--trigger",
                _         => isRole ? "char-detail__inline-pool--skill"
                                    : "char-detail__inline-pool--trait-skill",
            });
        }

        private void CloseAllPools()
        {
            for (int i = 0; i < 6; i++)
            {
                var pool = _reactionPools[i];
                if (pool == null) continue;
                pool.RemoveFromClassList("char-detail__inline-pool--target");
                pool.RemoveFromClassList("char-detail__inline-pool--trigger");
                pool.RemoveFromClassList("char-detail__inline-pool--skill");
                pool.RemoveFromClassList("char-detail__inline-pool--trait-skill");
                pool.Clear();
            }
            _openPoolSlot = -1;
            _openPoolPart = null;
        }

        private void BuildTargetChips(int slotIndex, VisualElement container)
        {
            var reaction = GetReactionAt(slotIndex);
            string currentId = reaction?.Trigger is Trigger t ? t.SubjectCharacterId : null;

            var members = _controller.GetPartyMembers();
            if (members.Count == 0)
            {
                AddPoolEmpty(container, "파티원 없음");
                return;
            }
            foreach (var member in members)
            {
                var captured = member;
                var chip = new Button(() => { _controller.SetReactionTarget(slotIndex, captured.Id); CloseAllPools(); });
                chip.AddToClassList("char-detail__pool-chip");
                chip.AddToClassList("char-detail__pool-chip--target");
                if (captured.Id == currentId)
                    chip.AddToClassList("char-detail__pool-chip--selected");
                var label = new Label(captured.Name);
                label.AddToClassList("char-detail__pool-chip-name");
                chip.Add(label);
                container.Add(chip);
            }
        }

        private void BuildTriggerChips(int slotIndex, VisualElement container)
        {
            var triggers = _controller.GetRoleTriggers(_character.RoleType);
            if (triggers.Count == 0)
            {
                AddPoolEmpty(container, "설정 가능한 조건 없음");
                return;
            }
            foreach (var trigger in triggers)
            {
                var captured = trigger;
                string label = GetTriggerDisplayText(captured);
                var chip = new Button(() => { _controller.SetReactionTrigger(slotIndex, captured); CloseAllPools(); });
                chip.AddToClassList("char-detail__pool-chip");
                chip.AddToClassList("char-detail__pool-chip--trigger");
                var nameLabel = new Label(label);
                nameLabel.AddToClassList("char-detail__pool-chip-name");
                chip.Add(nameLabel);
                container.Add(chip);
            }
        }

        private void BuildSkillChips(int slotIndex, VisualElement container)
        {
            var reaction = GetReactionAt(slotIndex);
            int currentSkillIndex = reaction?.SkillIndex ?? -1;

            var skills = _controller.GetAvailableSkills();
            if (skills.Count == 0)
            {
                AddPoolEmpty(container, "스킬 없음");
                return;
            }
            foreach (var skill in skills)
            {
                var captured = skill;
                int skillIdx = Array.IndexOf(_character.Skills, captured);
                var chip = new Button(() => { _controller.SetReactionSkill(slotIndex, captured); CloseAllPools(); });
                chip.AddToClassList("char-detail__pool-chip");
                chip.AddToClassList("char-detail__pool-chip--skill");
                if (skillIdx == currentSkillIndex)
                    chip.AddToClassList("char-detail__pool-chip--selected");
                var nameLabel = new Label(captured.Data?.DisplayName ?? "?");
                nameLabel.AddToClassList("char-detail__pool-chip-name");
                chip.Add(nameLabel);
                container.Add(chip);
            }
        }

        private static void AddPoolEmpty(VisualElement container, string msg)
        {
            var label = new Label(msg);
            label.AddToClassList("char-detail__pool-empty");
            container.Add(label);
        }

        private Reaction GetReactionAt(int slotIndex)
        {
            if (_character == null) return null;
            if (slotIndex < 2)
                return slotIndex < _character.RoleReactions.Length ? _character.RoleReactions[slotIndex] : null;
            int ti = slotIndex - 2;
            return ti < _character.TraitReactions.Length ? _character.TraitReactions[ti] : null;
        }

        private void OnUnequipRequested(int accIndex)
        {
            if (_currentInventory == null) return;
            _controller.UnequipAccessory(accIndex, _currentInventory);
        }

        private void OnCloseBtnClicked() => _selector.Deselect();
    }
}
