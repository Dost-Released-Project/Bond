using System;
using Bond.UI;
using Reactions;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Bond.UI
{
    public class CharacterDetailPresenter : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private CharacterDetailController _controller;
        private EquipSlotsPresenter _equipSlots;
        private ICharacterSelector _selector;

        // 씬별 연결 이벤트 — 구독 여부는 각 씬 코디네이터가 결정
        public event Action OnCloseRequested;
        public event Action OnInventoryOpenRequested;

        private VisualElement _panel;
        private Label _titleLabel;
        private Label _classLevelLabel;

        private Button        _roleBtnCurrent;
        private VisualElement _rolePicker;
        private Button        _roleOptTanker, _roleOptDealer, _roleOptSupporter;
        private bool _rolePickerOpen;

        private Label _baseStatStr, _baseStatAgi, _baseStatInt;
        private Label _statHp, _statDef, _statAtk, _statSpd;
        private Label _statCrt, _statAcc, _statInsanityCtrl, _statReactionCtrl;

        private Label         _gaugeInsanityVal;
        private Label         _gaugeInsanityWarnLabel;
        private VisualElement _gaugeHpFill, _gaugeInsanityFill;

        private Label _lockBannerRole, _lockBannerEquip, _lockBannerReaction;
        private VisualElement _traitList;
        private VisualElement _skillGrid;

        private readonly VisualElement[] _reactionSlots        = new VisualElement[6];
        private readonly Label[]         _reactionTargetLabels = new Label[6];
        private readonly Label[]         _reactionSkillLabels  = new Label[6];
        private readonly Label[]         _reactionEchoLabels   = new Label[6];
        private readonly VisualElement[] _reactionTargetParts  = new VisualElement[6];
        private readonly VisualElement[] _reactionSkillParts   = new VisualElement[6];

        // 역할·트레잇 슬롯(0-5) 공통 필드 — reaction 헤드라인 + editables 컨테이너
        private readonly VisualElement[] _reactionReactionParts = new VisualElement[6];
        private readonly Label[]         _reactionReactionVals  = new Label[6];
        private readonly VisualElement[] _reactionEditables     = new VisualElement[6];

        private readonly VisualElement[] _reactionPools = new VisualElement[6];
        private int    _openPoolSlot = -1;
        private string _openPoolPart = null;

        private BaseCharacter _character;
        private CharacterDetailEditMode _editMode;
        private IInventory _currentInventory;

        [Inject]
        public void Construct(CharacterDetailController controller, ICharacterSelector selector)
        {
            _controller      = controller;
            _selector        = selector;

            _controller.OnCharacterSet += RefreshCharacter;
            _selector.OnSelectionChanged += _controller.SetCharacter;
        }

        private void Start()
        {
            var root = _document.rootVisualElement;

            _panel      = root.Q("character-detail");
            _titleLabel = root.Q<Label>("character-detail__title");
            root.Q<Button>("character-detail__close-btn").clicked += () =>
            {
                Hide();
                OnCloseRequested?.Invoke();
            };

            _classLevelLabel = root.Q<Label>("char-detail__class-level");

            _roleBtnCurrent   = root.Q<Button>("role-btn-current");
            _rolePicker       = root.Q("char-detail__role-picker");
            _roleOptTanker    = root.Q<Button>("role-opt-tanker");
            _roleOptDealer    = root.Q<Button>("role-opt-dealer");
            _roleOptSupporter = root.Q<Button>("role-opt-supporter");

            _roleBtnCurrent.clicked   += ToggleRolePicker;
            _roleOptTanker.clicked    += () => { _controller.SetRole(RoleType.Tanker);    CloseRolePicker(); };
            _roleOptDealer.clicked    += () => { _controller.SetRole(RoleType.Dealer);    CloseRolePicker(); };
            _roleOptSupporter.clicked += () => { _controller.SetRole(RoleType.Supporter); CloseRolePicker(); };

            _baseStatStr = root.Q<Label>("base-stat-str");
            _baseStatAgi = root.Q<Label>("base-stat-agi");
            _baseStatInt = root.Q<Label>("base-stat-int");

            _statHp  = root.Q<Label>("stat-hp");
            _statDef = root.Q<Label>("stat-def");
            _statAtk = root.Q<Label>("stat-atk");
            _statSpd = root.Q<Label>("stat-spd");

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

            // 역할 슬롯(0-1) 쿼리
            for (int i = 0; i < 2; i++)
            {
                _reactionSlots[i]           = root.Q($"reaction-slot-{i}");
                _reactionReactionParts[i]   = root.Q($"reaction-slot-{i}__reaction-part");
                _reactionReactionVals[i]    = root.Q<Label>($"reaction-slot-{i}__reaction");
                _reactionEditables[i]       = root.Q($"reaction-slot-{i}__editables");
                _reactionTargetParts[i]     = root.Q($"reaction-slot-{i}__target-part");
                _reactionTargetLabels[i]    = root.Q<Label>($"reaction-slot-{i}__target");
                _reactionSkillParts[i]      = root.Q($"reaction-slot-{i}__skill-part");
                _reactionSkillLabels[i]     = root.Q<Label>($"reaction-slot-{i}__skill");
                _reactionEchoLabels[i]      = root.Q<Label>($"reaction-slot-{i}__echo");
            }

            // 트레잇 슬롯(2-5) 쿼리
            for (int i = 2; i < 6; i++)
            {
                int idx = i;
                _reactionSlots[idx]          = root.Q($"reaction-slot-{idx}");
                _reactionReactionParts[idx]  = root.Q($"reaction-slot-{idx}__reaction-part");
                _reactionReactionVals[idx]   = root.Q<Label>($"reaction-slot-{idx}__reaction");
                _reactionEditables[idx]      = root.Q($"reaction-slot-{idx}__editables");
                _reactionTargetParts[idx]    = root.Q($"reaction-slot-{idx}__target-part");
                _reactionTargetLabels[idx]   = root.Q<Label>($"reaction-slot-{idx}__target");
                _reactionSkillParts[idx]     = root.Q($"reaction-slot-{idx}__skill-part");
                _reactionSkillLabels[idx]    = root.Q<Label>($"reaction-slot-{idx}__skill");
                _reactionEchoLabels[idx]     = root.Q<Label>($"reaction-slot-{idx}__echo");

                // 트레잇은 리액션이 고정 — reaction-part 카탈로그 클릭 없음. 편집 빈칸만 클릭(FullEdit).
                _reactionTargetParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    TogglePool(idx, "observe");
                    evt.StopPropagation();
                });
                _reactionSkillParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    TogglePool(idx, "skill");
                    evt.StopPropagation();
                });
            }

            var equipRoot = root.Q("char-detail__equip-slots");
            _equipSlots = new EquipSlotsPresenter(equipRoot, _panel);
            _equipSlots.OnInventoryOpenRequested += () => OnInventoryOpenRequested?.Invoke();
            _equipSlots.OnUnequipRequested       += OnUnequipRequested;

            _controller.OnRoleChanged      += _ => RefreshIdentity();
            _controller.OnReactionChanged  += idx => { if (idx < 2) RefreshRoleSlot(idx); else RefreshTraitSlot(idx); };

            for (int i = 0; i < 6; i++)
                _reactionPools[i] = root.Q($"inline-pool-{i}");

            // 역할 슬롯(0-1) 클릭 핸들러
            for (int i = 0; i < 2; i++)
            {
                int idx = i;
                _reactionReactionParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    TogglePool(idx, "reaction");
                    evt.StopPropagation();
                });
                _reactionTargetParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    TogglePool(idx, "observe");
                    evt.StopPropagation();
                });
                _reactionSkillParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    TogglePool(idx, "skill");
                    evt.StopPropagation();
                });
            }

            _panel.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_rolePickerOpen && !_rolePicker.worldBound.Contains(evt.position))
                    CloseRolePicker();
            });
        }
        
        public void Show(BaseCharacter character, CharacterDetailEditMode mode, IInventory inventory)
        {
            _editMode         = mode;
            _currentInventory = inventory;

            // 상세 대상의 단일 출처는 컨트롤러다. selector 경유 없이 직접 열리는 호출처(탐사 준비 등)에서도
            // 컨트롤러 _character 가 동기화돼야 GetRoleReactionCatalog 등이 올바른 캐릭터로 동작한다.
            // SetCharacter → OnCharacterSet → RefreshCharacter 가 프레젠터 _character 동기화 + RefreshAll 까지 동기 수행.
            _controller.SetCharacter(character);

            ApplyViewMode();

            _panel.RemoveFromClassList("character-detail--hidden");
            _panel.AddToClassList("character-detail--visible");
        }

        private void RefreshCharacter(BaseCharacter character)
        {
            SetCharacterInternal(character);
            RefreshAll();
        }

        private void SetCharacterInternal(BaseCharacter character)
        {
            DetachCharacterEvents(_character);
            _character = character;
            AttachCharacterEvents(_character);
        }

        private void AttachCharacterEvents(BaseCharacter character)
        {
            if (character == null) return;
            character.OnHpChanged        += HandleHpChanged;
            character.OnInsanityChanged  += HandleInsanityChanged;
            character.OnStatRecalculated += HandleStatRecalculated;
            character.OnRoleChanged      += HandleRoleChanged;
        }

        private void DetachCharacterEvents(BaseCharacter character)
        {
            if (character == null) return;
            character.OnHpChanged        -= HandleHpChanged;
            character.OnInsanityChanged  -= HandleInsanityChanged;
            character.OnStatRecalculated -= HandleStatRecalculated;
            character.OnRoleChanged      -= HandleRoleChanged;
        }

        // RefreshStats가 HP/광기 게이지까지 함께 갱신하므로 별도 분기는 두지 않는다
        private void HandleHpChanged(BaseCharacter c)        => RefreshStats();
        private void HandleInsanityChanged(BaseCharacter c)  => RefreshStats();
        private void HandleStatRecalculated(BaseCharacter c) => RefreshStats();
        private void HandleRoleChanged(BaseCharacter c)      => RefreshIdentity();

        private void OnDestroy()
        {
            DetachCharacterEvents(_character);
            _equipSlots?.Dispose();
        }

        public void Hide()
        {
            _panel.RemoveFromClassList("character-detail--visible");
            _panel.AddToClassList("character-detail--hidden");
            CloseAllPools();
            CloseRolePicker();
        }

        public void SetViewMode(CharacterDetailEditMode mode)
        {
            _editMode = mode;
            ApplyViewMode();
        }

        private void ApplyViewMode()
        {
            bool fullEdit = _editMode == CharacterDetailEditMode.FullEdit;
            bool canEquip = _editMode != CharacterDetailEditMode.ReadOnly;

            _roleBtnCurrent.EnableInClassList("char-detail__role-current--disabled", !fullEdit);
            _roleBtnCurrent.pickingMode = fullEdit ? PickingMode.Position : PickingMode.Ignore;
            if (!fullEdit) CloseRolePicker();

            _equipSlots.SetEditable(canEquip);

            for (int i = 0; i < 6; i++)
            {
                bool editable = fullEdit;
                _reactionSlots[i].EnableInClassList("char-detail__reaction-slot--disabled", !editable);
                _reactionSlots[i].pickingMode = editable ? PickingMode.Position : PickingMode.Ignore;
            }

            SetLockBanner(_lockBannerRole, _editMode switch
            {
                CharacterDetailEditMode.EquipOnly => "탐사 중 변경 불가",
                CharacterDetailEditMode.ReadOnly  => "전투 중 변경 불가",
                _                                => "",
            });
            SetLockBanner(_lockBannerEquip, _editMode switch
            {
                CharacterDetailEditMode.ReadOnly => "전투 중 변경 불가",
                _                               => "",
            });
            SetLockBanner(_lockBannerReaction, _editMode switch
            {
                CharacterDetailEditMode.EquipOnly => "탐사 중 변경 불가",
                CharacterDetailEditMode.ReadOnly  => "전투 중 변경 불가",
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
            banner.AddToClassList(_editMode == CharacterDetailEditMode.EquipOnly
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
            for (int i = 0; i < 2; i++) RefreshRoleSlot(i);
            _character.SyncTraitReactions();
            for (int i = 2; i < 6; i++) RefreshTraitSlot(i);
            _equipSlots.SetCharacter(_character);
        }

        private void RefreshIdentity()
        {
            if (_character == null) return;

            _titleLabel.text = _character.Name;
            
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
            if (_character?.TraitIds == null) return;

            for (int i = 0; i < _character.TraitIds.Length; i++)
            {
                var traitSO = _character.GetTrait(i);
                var tag = new Label();
                tag.AddToClassList("char-detail__trait-tag");

                if (traitSO == null)
                {
                    tag.text = "미해금";
                    tag.AddToClassList("char-detail__trait-tag--locked");
                }
                else
                {
                    tag.text = traitSO.DisplayName;
                    tag.tooltip = traitSO.Description;
                    tag.AddToClassList(traitSO.Type switch
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

        private void RefreshRoleSlot(int i)
        {
            if (_character == null || i < 0 || i >= 2) return;

            var reaction = i < _character.RoleReactions.Length ? _character.RoleReactions[i] : null;
            var def = _controller.GetSlotDefinition(i);

            if (reaction == null)
            {
                _reactionReactionVals[i].text = "리액션 선택";
                _reactionReactionVals[i].AddToClassList("char-detail__slot-part-val--placeholder");
                if (_reactionEditables[i] != null)
                    _reactionEditables[i].style.display = DisplayStyle.None;
                _reactionSlots[i].RemoveFromClassList("char-detail__reaction-slot--locked");
                _reactionEchoLabels[i].text = "○";
                _reactionEchoLabels[i].AddToClassList("char-detail__slot-echo--empty");
                return;
            }

            _reactionReactionVals[i].text = def?.DisplayName ?? "(정의 없음)";
            _reactionReactionVals[i].RemoveFromClassList("char-detail__slot-part-val--placeholder");

            bool hasObserve = _controller.HasObserveEditable(i);
            bool hasSkill   = _controller.HasSkillEditable(i);

            if (_reactionEditables[i] != null)
                _reactionEditables[i].style.display = (hasObserve || hasSkill) ? DisplayStyle.Flex : DisplayStyle.None;

            if (_reactionTargetParts[i] != null)
                _reactionTargetParts[i].style.display = hasObserve ? DisplayStyle.Flex : DisplayStyle.None;
            if (_reactionSkillParts[i] != null)
                _reactionSkillParts[i].style.display = hasSkill ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasObserve)
            {
                bool hasSub = !string.IsNullOrEmpty(reaction.SubjectCharacterId);
                string targetName = hasSub && BaseCharacter.Dict.TryGetValue(reaction.SubjectCharacterId, out var subj)
                    ? subj.Name
                    : "미설정";
                _reactionTargetLabels[i].text = targetName;
                _reactionTargetLabels[i].EnableInClassList("char-detail__slot-part-val--placeholder", !hasSub);
            }

            if (hasSkill)
            {
                int skillIdx = (reaction.Effect as SkillCastReactionEffect)?.SkillIndex ?? -1;
                bool valid = skillIdx >= 0 && skillIdx < _character.Skills.Length && _character.Skills[skillIdx] != null;
                _reactionSkillLabels[i].text = valid
                    ? (_character.Skills[skillIdx].Data?.DisplayName ?? "미설정")
                    : "미설정";
                _reactionSkillLabels[i].EnableInClassList("char-detail__slot-part-val--placeholder", !valid);
            }

            bool complete = _controller.IsSlotComplete(i);
            _reactionEchoLabels[i].text = complete ? "◈" : "○";
            _reactionEchoLabels[i].EnableInClassList("char-detail__slot-echo--empty", !complete);
        }

        private void RefreshTraitSlot(int i)
        {
            if (_character == null || i < 2 || i >= 6) return;

            int traitIdx = i - 2;
            var reaction = traitIdx < _character.TraitReactions.Length ? _character.TraitReactions[traitIdx] : null;
            var def = _controller.GetSlotDefinition(i);

            if (reaction == null)
            {
                // 성향이 없거나 성향에 리액션이 없음
                _reactionReactionVals[i].text = "—";
                _reactionReactionVals[i].AddToClassList("char-detail__slot-part-val--placeholder");
                if (_reactionEditables[i] != null) _reactionEditables[i].style.display = DisplayStyle.None;
                _reactionSlots[i].AddToClassList("char-detail__reaction-slot--locked");
                _reactionEchoLabels[i].text = "○";
                _reactionEchoLabels[i].AddToClassList("char-detail__slot-echo--empty");
                return;
            }

            _reactionSlots[i].RemoveFromClassList("char-detail__reaction-slot--locked");
            _reactionReactionVals[i].text = def?.DisplayName ?? "(정의 없음)";
            _reactionReactionVals[i].RemoveFromClassList("char-detail__slot-part-val--placeholder");

            bool hasObserve = _controller.HasObserveEditable(i);
            bool hasSkill   = _controller.HasSkillEditable(i);

            if (_reactionEditables[i] != null)
                _reactionEditables[i].style.display = (hasObserve || hasSkill) ? DisplayStyle.Flex : DisplayStyle.None;
            if (_reactionTargetParts[i] != null)
                _reactionTargetParts[i].style.display = hasObserve ? DisplayStyle.Flex : DisplayStyle.None;
            if (_reactionSkillParts[i] != null)
                _reactionSkillParts[i].style.display = hasSkill ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasObserve)
            {
                bool hasSub = !string.IsNullOrEmpty(reaction.SubjectCharacterId);
                string targetName = hasSub && BaseCharacter.Dict.TryGetValue(reaction.SubjectCharacterId, out var subj)
                    ? subj.Name : "미설정";
                _reactionTargetLabels[i].text = targetName;
                _reactionTargetLabels[i].EnableInClassList("char-detail__slot-part-val--placeholder", !hasSub);
            }
            if (hasSkill)
            {
                int skillIdx = (reaction.Effect as SkillCastReactionEffect)?.SkillIndex ?? -1;
                bool valid = skillIdx >= 0 && skillIdx < _character.Skills.Length && _character.Skills[skillIdx] != null;
                _reactionSkillLabels[i].text = valid ? (_character.Skills[skillIdx].Data?.DisplayName ?? "미설정") : "미설정";
                _reactionSkillLabels[i].EnableInClassList("char-detail__slot-part-val--placeholder", !valid);
            }

            bool complete = _controller.IsSlotComplete(i);
            _reactionEchoLabels[i].text = complete ? "◈" : "○";
            _reactionEchoLabels[i].EnableInClassList("char-detail__slot-echo--empty", !complete);
        }

        private void ToggleRolePicker()
        {
            if (_editMode != CharacterDetailEditMode.FullEdit) return;
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

            var header = new VisualElement();
            header.AddToClassList("char-detail__pool-header");

            var title = new Label(part switch
            {
                "reaction" => "리액션",
                "observe"  => "관찰 대상",
                "skill"    => "행동",
                _ => ""
            });
            title.AddToClassList("char-detail__pool-title");
            header.Add(title);

            var closeBtn = new Button(CloseAllPools);
            closeBtn.AddToClassList("char-detail__pool-close-btn");
            closeBtn.text = "닫기 ×";
            header.Add(closeBtn);
            pool.Add(header);

            var chips = new VisualElement();
            chips.AddToClassList("char-detail__pool-chips");
            switch (part)
            {
                case "reaction": BuildReactionCatalogChips(slotIndex, chips); break;
                case "observe":  BuildObserveChips(slotIndex, chips);         break;
                case "skill":    BuildRoleSkillChips(slotIndex, chips);       break;
                default: break;
            }
            pool.Add(chips);

            pool.AddToClassList(part switch
            {
                "reaction" => "char-detail__inline-pool--target",
                "observe"  => "char-detail__inline-pool--target",
                "skill"    => "char-detail__inline-pool--skill",
                _ => ""
            });
        }

        private void CloseAllPools()
        {
            for (int i = 0; i < 6; i++)
            {
                var pool = _reactionPools[i];
                if (pool == null) continue;
                pool.RemoveFromClassList("char-detail__inline-pool--target");
                pool.RemoveFromClassList("char-detail__inline-pool--skill");
                pool.Clear();
            }
            _openPoolSlot = -1;
            _openPoolPart = null;
        }

        private void BuildReactionCatalogChips(int slotIndex, VisualElement container)
        {
            var catalog = _controller.GetRoleReactionCatalog(slotIndex);
            if (catalog.Count == 0)
            {
                AddPoolEmpty(container, "선택 가능한 리액션 없음");
                return;
            }

            var currentDef = _controller.GetSlotDefinition(slotIndex);
            var reaction = GetReactionAt(slotIndex);

            if (reaction != null)
            {
                var clearChip = new Button(() => { _controller.ClearRoleReaction(slotIndex); CloseAllPools(); });
                clearChip.AddToClassList("char-detail__pool-chip");
                clearChip.AddToClassList("char-detail__pool-chip--target");
                var clearLabel = new Label("해제");
                clearLabel.AddToClassList("char-detail__pool-chip-name");
                clearChip.Add(clearLabel);
                container.Add(clearChip);
            }

            foreach (var def in catalog)
            {
                var capturedDef = def;
                var chip = new Button(() => { _controller.SelectRoleReaction(slotIndex, capturedDef); CloseAllPools(); });
                chip.AddToClassList("char-detail__pool-chip");
                chip.AddToClassList("char-detail__pool-chip--target");
                if (currentDef != null && currentDef.Id == capturedDef.Id)
                    chip.AddToClassList("char-detail__pool-chip--selected");
                var nameLabel = new Label(capturedDef.DisplayName);
                nameLabel.AddToClassList("char-detail__pool-chip-name");
                chip.Add(nameLabel);
                container.Add(chip);
            }
        }

        private void BuildObserveChips(int slotIndex, VisualElement container)
        {
            var candidates = _controller.GetObserveTargetCandidates(slotIndex);
            if (candidates.Count == 0)
            {
                AddPoolEmpty(container, "파티 아군 없음");
                return;
            }

            var reaction = GetReactionAt(slotIndex);
            string currentId = reaction?.SubjectCharacterId;

            foreach (var c in candidates)
            {
                var captured = c;
                var chip = new Button(() => { _controller.SetObserveTarget(slotIndex, captured.Id); CloseAllPools(); });
                chip.AddToClassList("char-detail__pool-chip");
                chip.AddToClassList("char-detail__pool-chip--target");
                if (captured.Id == currentId)
                    chip.AddToClassList("char-detail__pool-chip--selected");
                var nameLabel = new Label(captured.Name);
                nameLabel.AddToClassList("char-detail__pool-chip-name");
                chip.Add(nameLabel);
                container.Add(chip);
            }
        }

        private void BuildRoleSkillChips(int slotIndex, VisualElement container)
        {
            var candidates = _controller.GetActionSkillCandidates(slotIndex);
            if (candidates.Count == 0)
            {
                AddPoolEmpty(container, "사용 가능한 스킬 없음");
                return;
            }

            var reaction = GetReactionAt(slotIndex);
            int currentIndex = (reaction?.Effect as SkillCastReactionEffect)?.SkillIndex ?? -1;

            foreach (var (index, skill) in candidates)
            {
                int capturedIndex = index;
                var chip = new Button(() => { _controller.SetActionSkill(slotIndex, capturedIndex); CloseAllPools(); });
                chip.AddToClassList("char-detail__pool-chip");
                chip.AddToClassList("char-detail__pool-chip--skill");
                if (capturedIndex == currentIndex)
                    chip.AddToClassList("char-detail__pool-chip--selected");
                var nameLabel = new Label(skill.Data?.DisplayName ?? "?");
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
    }
}
