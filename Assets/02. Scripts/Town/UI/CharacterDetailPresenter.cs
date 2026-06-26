using System;
using System.Collections.Generic;
using BattleSystem.UI;
using Bond.UI;
using Cysharp.Threading.Tasks;
using Reactions;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        private InventoryTransferService _transferService;

        // 씬별 연결 이벤트 — 구독 여부는 각 씬 코디네이터가 결정
        public event Action OnCloseRequested;
        public event Action OnInventoryOpenRequested;

        private VisualElement _panel;
        private Label _titleLabel;
        private Label _classLevelLabel;

        private Image  _portrait;
        private string _portraitAddress;

        private Button        _roleBtnCurrent;
        private VisualElement _rolePicker;
        private Button        _roleOptTanker, _roleOptDealer, _roleOptSupporter;
        private bool _rolePickerOpen;

        private VisualElement _controlSeg;
        private Button        _segManualBtn, _segAutoBtn;

        private Label _baseStatStr, _baseStatAgi, _baseStatInt;
        private Label _statHp, _statDef, _statAtk, _statSpd;
        private Label _statCrt, _statAcc, _statEva;

        private Label         _gaugeInsanityVal;
        private Label         _gaugeInsanityWarnLabel;
        private VisualElement _gaugeHpFill, _gaugeInsanityFill;

        private Label _lockBannerRole, _lockBannerEquip, _lockBannerReaction;
        private VisualElement _traitList;
        private VisualElement _skillGrid;

        // 슬롯(0-5) — 헤더 + 3분할(대상/조건/행동) + 에코
        private readonly VisualElement[] _reactionSlots        = new VisualElement[6];
        private readonly VisualElement[] _reactionHeaders      = new VisualElement[6];
        private readonly Label[]         _reactionHeaderLabels = new Label[6];
        private readonly VisualElement[] _reactionParts        = new VisualElement[6];
        private readonly Label[]         _reactionEchoLabels   = new Label[6];

        private readonly VisualElement[] _reactionTargetParts   = new VisualElement[6];
        private readonly Label[]         _reactionTargetVals    = new Label[6];
        private readonly Image[]         _reactionTargetIcons   = new Image[6];
        private readonly Label[]         _reactionConditionVals = new Label[6];
        private readonly VisualElement[] _reactionActionParts   = new VisualElement[6];
        private readonly Label[]         _reactionActionVals    = new Label[6];
        private readonly Image[]         _reactionActionIcons   = new Image[6];

        // 아이콘 뒤 문구(편집칸 {icon} 토큰 뒤) — 아이콘 오른쪽에 가로 배치
        private readonly Label[]         _reactionTargetAfter   = new Label[6];
        private readonly Label[]         _reactionActionAfter   = new Label[6];

        // 떠있는 드롭다운(카탈로그/대상/행동 공용)
        private SlotDropdown _dropdown;
        // 슬롯 아이콘 비동기 로드 경합 가드 — 마지막 요청 주소를 기억해 stale 응답을 폐기
        private readonly string[] _targetIconAddr = new string[6];
        private readonly string[] _actionIconAddr = new string[6];

        private BaseCharacter _character;
        private CharacterDetailEditMode _editMode;
        private IInventory _currentInventory;

        // 해고(꾹 눌러 확정) 버튼 — 마을 관리(allowFire)에서만 동작. 누르는 동안 테두리가 시계방향으로 채워진다.
        private Roster        _roster;
        private bool          _allowFire;
        private VisualElement _fireBtn;
        private VisualElement _fireRing;
        private IVisualElementScheduledItem _fireTicker;
        private float _fireHoldStart;
        private float _fireProgress;
        private bool  _fireHolding;
        private int   _firePointerId = -1;
        private const float FIRE_HOLD_SECONDS = 3f;
    
        [Inject]
        public void Construct(CharacterDetailController controller, ICharacterSelector selector, InventoryTransferService transfer, Roster roster)
        {
            _controller      = controller;
            _selector        = selector;
            _transferService = transfer;
            _roster          = roster;

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

            SetupFireButton(root);

            _classLevelLabel = root.Q<Label>("char-detail__class-level");
            _portrait        = root.Q<Image>("char-detail__portrait");
            _portrait.scaleMode = ScaleMode.ScaleAndCrop;   // 고정 박스에 맞춰 채우고 넘치면 크롭

            _roleBtnCurrent   = root.Q<Button>("role-btn-current");
            _rolePicker       = root.Q("char-detail__role-picker");
            _roleOptTanker    = root.Q<Button>("role-opt-tanker");
            _roleOptDealer    = root.Q<Button>("role-opt-dealer");
            _roleOptSupporter = root.Q<Button>("role-opt-supporter");

            _roleBtnCurrent.clicked   += ToggleRolePicker;
            _roleOptTanker.clicked    += () => { _controller.SetRole(RoleType.Tanker);    CloseRolePicker(); };
            _roleOptDealer.clicked    += () => { _controller.SetRole(RoleType.Dealer);    CloseRolePicker(); };
            _roleOptSupporter.clicked += () => { _controller.SetRole(RoleType.Supporter); CloseRolePicker(); };

            _controlSeg   = root.Q("control-mode");
            _segManualBtn = root.Q<Button>("control-seg-manual");
            _segAutoBtn   = root.Q<Button>("control-seg-auto");
            _segManualBtn.clicked += () => SetControlMode(true);
            _segAutoBtn.clicked   += () => SetControlMode(false);

            _baseStatStr = root.Q<Label>("base-stat-str");
            _baseStatAgi = root.Q<Label>("base-stat-agi");
            _baseStatInt = root.Q<Label>("base-stat-int");

            _statHp  = root.Q<Label>("stat-hp");
            _statDef = root.Q<Label>("stat-def");
            _statAtk = root.Q<Label>("stat-atk");
            _statSpd = root.Q<Label>("stat-spd");

            _statCrt                = root.Q<Label>("stat-crt");
            _statAcc                = root.Q<Label>("stat-acc");
            _statEva                = root.Q<Label>("stat-eva");
            _gaugeInsanityVal       = root.Q<Label>("gauge-insanity-val");
            _gaugeInsanityWarnLabel = root.Q<Label>("gauge-insanity-warn");
            _gaugeHpFill            = root.Q("gauge-hp");
            _gaugeInsanityFill      = root.Q("gauge-insanity");

            _lockBannerRole     = root.Q<Label>("lock-banner-role");
            _lockBannerEquip    = root.Q<Label>("lock-banner-equip");
            _lockBannerReaction = root.Q<Label>("lock-banner-reaction");

            _traitList = root.Q("char-detail__trait-list");
            _skillGrid  = root.Q("char-detail__skill-grid");

            _dropdown = new SlotDropdown(root);

            // 슬롯(0-5) 쿼리 + 클릭 배선. 역할(0-1)은 헤더로 카탈로그를 열고, 성향(2-5)은 리액션 고정.
            // 편집칸(대상/행동) 클릭은 해당 편집슬롯이 있을 때만(FullEdit) 드롭다운을 연다.
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                _reactionSlots[idx]         = root.Q($"reaction-slot-{idx}");
                _reactionHeaders[idx]       = root.Q($"reaction-slot-{idx}__header");
                _reactionHeaderLabels[idx]  = root.Q<Label>($"reaction-slot-{idx}__header-label");
                _reactionParts[idx]         = root.Q($"reaction-slot-{idx}__parts");
                _reactionEchoLabels[idx]    = root.Q<Label>($"reaction-slot-{idx}__echo");

                _reactionTargetParts[idx]   = root.Q($"reaction-slot-{idx}__target-part");
                _reactionTargetVals[idx]    = root.Q<Label>($"reaction-slot-{idx}__target");
                _reactionTargetIcons[idx]   = root.Q<Image>($"reaction-slot-{idx}__target-icon");
                _reactionConditionVals[idx] = root.Q<Label>($"reaction-slot-{idx}__condition");
                _reactionActionParts[idx]   = root.Q($"reaction-slot-{idx}__action-part");
                _reactionActionVals[idx]    = root.Q<Label>($"reaction-slot-{idx}__action");
                _reactionActionIcons[idx]   = root.Q<Image>($"reaction-slot-{idx}__action-icon");

                _reactionTargetAfter[idx]   = root.Q<Label>($"reaction-slot-{idx}__target-after");
                _reactionActionAfter[idx]   = root.Q<Label>($"reaction-slot-{idx}__action-after");

                if (_reactionTargetIcons[idx] != null) _reactionTargetIcons[idx].scaleMode = ScaleMode.ScaleAndCrop;
                if (_reactionActionIcons[idx] != null) _reactionActionIcons[idx].scaleMode = ScaleMode.ScaleAndCrop;

                if (idx < 2)
                    _reactionHeaders[idx]?.RegisterCallback<ClickEvent>(evt =>
                    {
                        if (_editMode != CharacterDetailEditMode.FullEdit) return;
                        OpenCatalogDropdown(idx);
                        evt.StopPropagation();
                    });

                _reactionTargetParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    if (!_controller.HasObserveEditable(idx)) return;
                    OpenObserveDropdown(idx);
                    evt.StopPropagation();
                });
                _reactionActionParts[idx]?.RegisterCallback<ClickEvent>(evt =>
                {
                    if (_editMode != CharacterDetailEditMode.FullEdit) return;
                    if (!_controller.HasSkillEditable(idx)) return;
                    OpenActionDropdown(idx);
                    evt.StopPropagation();
                });
            }

            var equipRoot = root.Q("char-detail__equip-slots");
            _equipSlots = new EquipSlotsPresenter(equipRoot, _panel);
            _equipSlots.OnInventoryOpenRequested += () => OnInventoryOpenRequested?.Invoke();
            _equipSlots.OnUnequipRequested       += OnUnequipRequested;
            
            // 중복 등록 방지를 위해 배선 연결 전 안전하게 한 번 빼준 뒤 이벤트를 연결합니다. (드래그용 이벤트)
            _equipSlots.OnDragStartRequested -= HandleEquipSlotDragStart;
            _equipSlots.OnDragStartRequested += HandleEquipSlotDragStart;
            
            _equipSlots.OnDragDropRequested -= HandleEquipSlotDragDrop;
            _equipSlots.OnDragDropRequested += HandleEquipSlotDragDrop;

            _controller.OnRoleChanged      += HandleControllerRoleChanged;
            _controller.OnReactionChanged  += RefreshReactionSlot;

            _panel.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_rolePickerOpen && !_rolePicker.worldBound.Contains(evt.position))
                    CloseRolePicker();
            });
        }
        
        // allowFire: 해고 버튼 노출 여부. 마을 관리에서만 true — 탐사 준비(embark)는 파티 슬롯 정합성 때문에 false.
        public void Show(BaseCharacter character, CharacterDetailEditMode mode, IInventory inventory, bool allowFire = false)
        {
            _editMode         = mode;
            _currentInventory = inventory;
            _allowFire        = allowFire;

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
            character.OnSkillsChanged    += HandleSkillsChanged;
        }

        private void DetachCharacterEvents(BaseCharacter character)
        {
            if (character == null) return;
            character.OnHpChanged        -= HandleHpChanged;
            character.OnInsanityChanged  -= HandleInsanityChanged;
            character.OnStatRecalculated -= HandleStatRecalculated;
            character.OnRoleChanged      -= HandleRoleChanged;
            character.OnSkillsChanged    -= HandleSkillsChanged;
        }

        // RefreshStats가 HP/광기 게이지까지 함께 갱신하므로 별도 분기는 두지 않는다
        private void HandleHpChanged(BaseCharacter c)        => RefreshStats();
        private void HandleInsanityChanged(BaseCharacter c)  => RefreshStats();
        private void HandleStatRecalculated(BaseCharacter c) => RefreshStats();
        private void HandleRoleChanged(BaseCharacter c)      => RefreshIdentity();

        // 컨트롤러발 역할 변경(Action<RoleType>). 캐릭터발 HandleRoleChanged 와 시그니처가 달라 별도 메서드로 둔다
        // (익명 람다로 두면 OnDestroy 에서 -= 가 불가능).
        private void HandleControllerRoleChanged(RoleType role) => RefreshIdentity();

        // 스킬 편성 변경 → 그리드 갱신 + 리액션 슬롯 재표시(압축으로 SkillIndex 가 재매핑/해제됐을 수 있음)
        private void HandleSkillsChanged(BaseCharacter c)
        {
            // 열려 있던 드롭다운(행동 스킬 선택 등)은 후보가 바뀌었을 수 있으므로 닫는다.
            _dropdown?.Close();
            RefreshSkillGrid();
            for (int i = 0; i < 6; i++) RefreshReactionSlot(i);
        }

        private void OnDestroy()
        {
            // 주입된 컨트롤러/셀렉터 구독 해제. 지금은 셋이 같은 스코프라 동시 소멸하지만,
            // 컨벤션(구독 측이 OnDestroy 에서 해제) 준수 + 향후 셀렉터/컨트롤러가 상위 스코프로 올라가도 안전하도록.
            if (_controller != null)
            {
                _controller.OnCharacterSet    -= RefreshCharacter;
                _controller.OnRoleChanged     -= HandleControllerRoleChanged;
                _controller.OnReactionChanged -= RefreshReactionSlot;
                if (_selector != null)
                    _selector.OnSelectionChanged -= _controller.SetCharacter;
            }

            DetachCharacterEvents(_character);
            _equipSlots?.Dispose();
            _dropdown?.Dispose();
            CancelFireHold();
        }

        public void Hide()
        {
            _panel.RemoveFromClassList("character-detail--visible");
            _panel.AddToClassList("character-detail--hidden");
            CancelFireHold();
            _dropdown?.Close();
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

            // 해고는 마을 관리(allowFire + FullEdit)에서만 노출. 조건이 풀리면 진행 중인 홀드는 취소.
            if (_fireBtn != null)
            {
                bool canFire = fullEdit && _allowFire;
                _fireBtn.style.display = canFire ? DisplayStyle.Flex : DisplayStyle.None;
                if (!canFire) CancelFireHold();
            }

            _roleBtnCurrent.EnableInClassList("char-detail__role-current--disabled", !fullEdit);
            _roleBtnCurrent.pickingMode = fullEdit ? PickingMode.Position : PickingMode.Ignore;
            if (!fullEdit) CloseRolePicker();

            _controlSeg.EnableInClassList("char-detail__control-seg--disabled", !fullEdit);
            _segManualBtn.pickingMode = fullEdit ? PickingMode.Position : PickingMode.Ignore;
            _segAutoBtn.pickingMode   = fullEdit ? PickingMode.Position : PickingMode.Ignore;

            _equipSlots.SetEditable(canEquip);

            if (!fullEdit) _dropdown?.Close();
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
            RefreshControlMode();
            RefreshStats();
            RefreshTraits();
            RefreshSkillGrid();
            for (int i = 0; i < 2; i++) RefreshReactionSlot(i);
            _character.SyncTraitReactions();
            for (int i = 2; i < 6; i++) RefreshReactionSlot(i);
            _equipSlots.SetCharacter(_character);
        }

        private void RefreshIdentity()
        {
            if (_character == null) return;

            _titleLabel.text = _character.Name;
            
            string profName = _character.Profession?.Name ?? "—";
            _classLevelLabel.text = $"{profName}  Lv.{_character.Level}";

            RefreshPortrait();

            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--tanker");
            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--dealer");
            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--supporter");
            _roleBtnCurrent.RemoveFromClassList("char-detail__role-current--none");

            switch (_character.RoleType)
            {
                case RoleType.Tanker:
                    _roleBtnCurrent.text = "탱커";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--tanker");
                    break;
                case RoleType.Dealer:
                    _roleBtnCurrent.text = "딜러";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--dealer");
                    break;
                case RoleType.Supporter:
                    _roleBtnCurrent.text = "서포터";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--supporter");
                    break;
                default:
                    _roleBtnCurrent.text = "미설정";
                    _roleBtnCurrent.AddToClassList("char-detail__role-current--none");
                    break;
            }
        }

        /// <summary>대기 이미지를 초상화에 로드. 주소가 같으면 재로드하지 않는다(역할 변경 등으로 RefreshIdentity가 재호출돼도 중복 로드 방지).</summary>
        private void RefreshPortrait()
        {
            if (_portrait == null) return;

            string address = _character?.EffectiveIdleImageAddress;
            if (address == _portraitAddress) return;
            _portraitAddress = address;

            _portrait.sprite = null;   // 이전 초상화 제거
            if (!string.IsNullOrEmpty(address))
                LoadPortraitAsync(address).Forget();
        }

        private async UniTaskVoid LoadPortraitAsync(string address)
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
            // 로드가 끝나기 전에 캐릭터가 바뀌었으면 폐기(경합 방지)
            if (sprite == null || _portrait == null || _portraitAddress != address) return;
            _portrait.sprite = sprite;
        }

        private void RefreshStats()
        {
            if (_character == null) return;
            var s = _character.Stat;

            _baseStatStr.text = s.STR.ToString();
            _baseStatAgi.text = s.AGI.ToString();
            _baseStatInt.text = s.INT.ToString();

            _statHp.text  = $"{s.current_Hp}/{s.max_Hp}";
            _statDef.text = $"{s.def:P0}";
            _statAtk.text = s.atk.ToString();
            _statSpd.text = s.speed.ToString();
            _statCrt.text = $"{s.crt:P0}";
            _statAcc.text = $"{s.acc:P0}";
            _statEva.text = $"{s.eva:P0}";

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

        // 직업이 사용 가능한 전체 스킬을 칩으로 표시하고, 장착된 것은 강조한다.
        // FullEdit 에서만 클릭으로 편성/해제(최대 슬롯에서 미선택 칩은 비활성).
        private void RefreshSkillGrid()
        {
            _skillGrid.Clear();
            if (_character?.Profession == null) return;

            bool editable = _editMode == CharacterDetailEditMode.FullEdit;
            bool full     = _character.EquippedSkillCount >= _character.Skills.Length;

            foreach (var data in _controller.GetProfessionSkills())
                _skillGrid.Add(BuildSkillChip(data, editable, full));
        }

        private VisualElement BuildSkillChip(SkillData data, bool editable, bool full)
        {
            var chip = new VisualElement();
            chip.AddToClassList("char-detail__skill-chip");

            // 스킬 아이콘(주소는 SkillData.IconAddress, Addressables). 주소 없으면 빈 칸.
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("char-detail__skill-chip-icon");
            chip.Add(icon);
            if (!string.IsNullOrEmpty(data?.IconAddress))
                LoadImageSpriteAsync(icon, data.IconAddress).Forget();

            var nameLabel = new Label(data?.DisplayName ?? "?");
            nameLabel.AddToClassList("char-detail__skill-name");
            chip.Add(nameLabel);

            // 미장착 풀 스킬도 툴팁을 위해 SkillBase 로 임시 래핑(Build 는 .Data 만 읽음).
            var preview = new SampleSkill(data);
            TooltipPopup.AttachFollow(chip, () => SkillTooltipContent.Build(preview));

            bool equipped = _character.HasSkill(data);
            if (equipped) chip.AddToClassList("char-detail__skill-chip--selected");

            if (editable)
            {
                bool blocked = full && !equipped;   // 만석 상태에서 새 스킬 추가 불가
                if (blocked)
                    chip.AddToClassList("char-detail__skill-chip--disabled");
                else
                    chip.RegisterCallback<ClickEvent>(_ => _controller.ToggleSkill(data));
            }

            return chip;
        }

        // 역할(0-1)·성향(2-5) 슬롯 공통 갱신. i<2 = 역할(헤더로 카탈로그 선택), 그 외 = 성향(고정).
        private void RefreshReactionSlot(int i)
        {
            if (_character == null || i < 0 || i >= 6) return;
            bool isRole = i < 2;
            string kind = isRole ? "역할 리액션" : "성향 리액션";

            var reaction = GetReactionAt(i);

            // 리액션 미선택(역할) 또는 성향 미해금/무반응 → 3분할 숨김, 헤더만.
            // 빈 상태를 헤더 문구로 알린다: 역할=선택 유도, 성향=비어 있음.
            if (reaction == null)
            {
                if (_reactionHeaderLabels[i] != null)
                {
                    _reactionHeaderLabels[i].text = isRole ? $"{kind} - 리액션을 선택하세요" : $"{kind} - 비어 있음";
                    _reactionHeaderLabels[i].AddToClassList("char-detail__slot-header-label--empty");
                }
                if (_reactionParts[i] != null) _reactionParts[i].style.display = DisplayStyle.None;
                // 역할은 '선택 가능'(잠금 아님), 성향은 잠금 표시.
                _reactionSlots[i].EnableInClassList("char-detail__reaction-slot--locked", !isRole);
                SetEcho(i, complete: false, empty: true);
                return;
            }

            _reactionSlots[i].RemoveFromClassList("char-detail__reaction-slot--locked");

            // 헤더: "역할/성향 리액션 - [이름]"
            var def = _controller.GetSlotDefinition(i);
            string defName = def?.DisplayName;
            if (_reactionHeaderLabels[i] != null)
            {
                _reactionHeaderLabels[i].RemoveFromClassList("char-detail__slot-header-label--empty");
                _reactionHeaderLabels[i].text = string.IsNullOrEmpty(defName) ? kind : $"{kind} - {defName}";
            }

            if (_reactionParts[i] != null) _reactionParts[i].style.display = DisplayStyle.Flex;

            var (targetText, conditionText, actionText) = _controller.GetPartTexts(i);

            // 조건: 항상 고정(편집 불가), 아이콘 없음.
            SetPartVal(_reactionConditionVals[i], conditionText, PartState.Fixed);

            // 대상: 관찰 편집슬롯이 있으면 편집칸(할당 여부로 상태/아이콘), 없으면 고정.
            bool targetEditable = _controller.HasObserveEditable(i);
            UpdateEditablePart(
                _reactionTargetParts[i], _reactionTargetVals[i], _reactionTargetAfter[i], _reactionTargetIcons[i],
                targetText, targetEditable,
                filled: targetEditable && _controller.IsObserveFilled(i),
                iconAddr: _controller.GetObserveIconAddress(i),
                iconTip: _controller.GetObserveTargetName(i),
                slot: i, isTarget: true);

            // 행동: 행동 스킬 편집슬롯이 있으면 편집칸, 없으면 고정.
            bool actionEditable = _controller.HasSkillEditable(i);
            UpdateEditablePart(
                _reactionActionParts[i], _reactionActionVals[i], _reactionActionAfter[i], _reactionActionIcons[i],
                actionText, actionEditable,
                filled: actionEditable && _controller.IsActionFilled(i),
                iconAddr: _controller.GetActionIconAddress(i),
                iconTip: _controller.GetActionSkill(i)?.Data?.DisplayName,
                slot: i, isTarget: false);

            SetEcho(i, complete: _controller.IsSlotComplete(i), empty: false);
        }

        private enum PartState { Fixed, Assigned, Unassigned }

        // 칸 값 텍스트 + 3색 상태(고정/할당/미할당) 적용.
        private static void SetPartVal(Label val, string text, PartState state)
        {
            if (val == null) return;
            val.text = text;
            val.RemoveFromClassList("char-detail__slot-part-val--fixed");
            val.RemoveFromClassList("char-detail__slot-part-val--assigned");
            val.RemoveFromClassList("char-detail__slot-part-val--unassigned");
            val.AddToClassList(state switch
            {
                PartState.Assigned   => "char-detail__slot-part-val--assigned",
                PartState.Unassigned => "char-detail__slot-part-val--unassigned",
                _                    => "char-detail__slot-part-val--fixed",
            });
        }

        // 편집 가능한 칸(대상/행동)의 값·색·클릭성·아이콘 갱신.
        // editable=false 면 고정 칸(아이콘 숨김). editable=true 면 filled 에 따라 할당/미할당(빈 아이콘 슬롯).
        // text 에 {icon} 토큰이 있으면 앞 문구(val, 아이콘 위)와 뒤 문구(after, 아이콘 오른쪽)로 나눈다. 없으면 전부 val.
        private void UpdateEditablePart(VisualElement part, Label val, Label after, Image icon, string text,
            bool editable, bool filled, string iconAddr, string iconTip, int slot, bool isTarget)
        {
            PartState state = !editable ? PartState.Fixed : (filled ? PartState.Assigned : PartState.Unassigned);

            string before = text ?? string.Empty, afterText = string.Empty;
            int tk = before.IndexOf("{icon}", StringComparison.Ordinal);
            if (tk >= 0) { afterText = before.Substring(tk + 6).TrimStart(); before = before.Substring(0, tk).TrimEnd(); }

            // 상태 색(할당/미할당)은 아이콘 위 문구(val)에만. 옆 문구(after)는 항상 중립색(서술 문구).
            SetPartVal(val, before, state);
            if (after != null)
            {
                bool hasAfter = !string.IsNullOrEmpty(afterText);
                after.style.display = hasAfter ? DisplayStyle.Flex : DisplayStyle.None;
                if (hasAfter) SetPartVal(after, afterText, PartState.Fixed);
            }
            part?.EnableInClassList("char-detail__slot-part--editable", editable);

            if (icon == null) return;

            // 고정 칸이거나 아직 미할당(편집 전)이면 아이콘을 숨긴다. 할당돼야 비로소 아이콘 표시.
            // (미할당 경고는 값 문구의 빨강 색 + 다이아몬드 에코로 충분히 드러난다.)
            if (!editable || !filled)
            {
                icon.style.display = DisplayStyle.None;
                icon.RemoveFromClassList("char-detail__slot-part-icon--empty");
                icon.sprite = null;
                icon.tooltip = string.Empty;
                if (isTarget) _targetIconAddr[slot] = null; else _actionIconAddr[slot] = null;
                return;
            }

            icon.style.display = DisplayStyle.Flex;
            icon.tooltip = iconTip ?? string.Empty;
            if (!string.IsNullOrEmpty(iconAddr))
            {
                LoadSlotIcon(icon, iconAddr, slot, isTarget);
            }
            else
            {
                icon.sprite = null;
                if (isTarget) _targetIconAddr[slot] = null; else _actionIconAddr[slot] = null;
            }
        }

        private void SetEcho(int i, bool complete, bool empty)
        {
            var echo = _reactionEchoLabels[i];
            if (echo == null) return;
            echo.text = complete ? "◆" : "◇";
            echo.EnableInClassList("char-detail__slot-echo--complete", complete);
            echo.EnableInClassList("char-detail__slot-echo--empty", empty);
        }

        // 슬롯 아이콘 비동기 로드. 같은 주소가 이미 요청됐으면 재로드 생략(중복 LoadAssetAsync 로 핸들 ref 누적 방지).
        private void LoadSlotIcon(Image icon, string address, int slot, bool isTarget)
        {
            string prev = isTarget ? _targetIconAddr[slot] : _actionIconAddr[slot];
            if (prev == address) return;   // 이미 이 주소를 로드(또는 로드 중/실패) — 재요청 안 함

            if (isTarget) _targetIconAddr[slot] = address; else _actionIconAddr[slot] = address;
            LoadSlotIconAsync(icon, address, slot, isTarget).Forget();
        }

        private async UniTaskVoid LoadSlotIconAsync(Image icon, string address, int slot, bool isTarget)
        {
            var sprite = await TryLoadSpriteAsync(address);
            string current = isTarget ? _targetIconAddr[slot] : _actionIconAddr[slot];
            // 로드 끝나기 전에 캐릭터/할당이 바뀌었으면 폐기.
            if (sprite == null || icon == null || icon.panel == null || current != address) return;
            icon.sprite = sprite;
        }

        // 주소→스프라이트 캐시. 그리드/드롭다운이 갱신 때마다 재빌드되므로 같은 주소를 매번
        // 다시 로드(=Addressables 핸들 누적)하지 않도록 한 번 로드한 스프라이트를 재사용한다.
        private static readonly Dictionary<string, Sprite> _iconSpriteCache = new Dictionary<string, Sprite>();

        private static async UniTask<Sprite> TryLoadSpriteAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;

            if (_iconSpriteCache.TryGetValue(address, out var cached))
            {
                if (cached != null) return cached;   // Unity-null 체크: 파괴된 스프라이트면 캐시 무효 → 재로드
                _iconSpriteCache.Remove(address);
            }

            var locHandle = Addressables.LoadResourceLocationsAsync(address);
            var locations = await locHandle.ToUniTask();
            bool found = locations != null && locations.Count > 0;
            Addressables.Release(locHandle);

            if (!found)
            {
                Debug.LogWarning($"<color=orange>[CharacterDetail]</color> 아이콘 주소 없음 — '{address}'");
                return null;
            }

            var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
            if (sprite != null) _iconSpriteCache[address] = sprite;
            return sprite;
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

        private void SetControlMode(bool manual)
        {
            if (_editMode != CharacterDetailEditMode.FullEdit || _character == null) return;
            _controller.SetPlayable(manual);
            RefreshControlMode();
        }

        /// <summary>현재 캐릭터의 isPlayable 을 세그먼트 활성 칸에 반영. true=수동(플레이어 조작), false=자동(AI 위임).</summary>
        private void RefreshControlMode()
        {
            if (_character == null) return;
            bool manual = _character.isPlayable;
            _segManualBtn.EnableInClassList("char-detail__control-seg__opt--active", manual);
            _segAutoBtn.EnableInClassList("char-detail__control-seg__opt--active", !manual);
        }

        // ── 드롭다운(카탈로그 / 대상 / 행동) ────────────────────────────

        private void OpenCatalogDropdown(int slot)
        {
            // 카탈로그 항목엔 대상·조건·행동 미리보기 부제가 붙어 더 넓게 연다(--wide).
            var anchor = _reactionHeaders[slot];
            if (anchor != null) ToggleDropdown(anchor, c => BuildCatalogItems(slot, c), "char-detail__dropdown--wide");
        }

        private void OpenObserveDropdown(int slot)
        {
            var anchor = _reactionTargetParts[slot];
            if (anchor != null) ToggleDropdown(anchor, c => BuildObserveItems(slot, c));
        }

        private void OpenActionDropdown(int slot)
        {
            var anchor = _reactionActionParts[slot];
            if (anchor != null) ToggleDropdown(anchor, c => BuildActionItems(slot, c));
        }

        // 같은 앵커를 다시 누르면 토글로 닫는다.
        private void ToggleDropdown(VisualElement anchor, Action<VisualElement> build, string variantClass = null)
        {
            if (_dropdown == null) return;
            if (_dropdown.CurrentAnchor == anchor) { _dropdown.Close(); return; }
            _dropdown.Open(anchor, build, variantClass);
        }

        private void BuildCatalogItems(int slot, VisualElement container)
        {
            var catalog = _controller.GetRoleReactionCatalog(slot);
            var currentDef = _controller.GetSlotDefinition(slot);
            bool hasReaction = GetReactionAt(slot) != null;

            if (hasReaction)
            {
                var clear = MakeDropdownItem("해제", null, null, false);
                clear.AddToClassList("char-detail__dropdown-item--clear");
                clear.RegisterCallback<ClickEvent>(_ => { _controller.ClearRoleReaction(slot); _dropdown.Close(); });
                container.Add(clear);
            }

            if (catalog.Count == 0)
            {
                if (!hasReaction) AddDropdownEmpty(container, "선택 가능한 리액션 없음");
                return;
            }

            foreach (var def in catalog)
            {
                var captured = def;
                var (t, cnd, a) = def.ResolvePartTexts();
                var item = MakeDropdownItem(def.DisplayName, $"{t} · {cnd} · {a}", null,
                    currentDef != null && currentDef.Id == def.Id);
                item.RegisterCallback<ClickEvent>(_ => { _controller.SelectRoleReaction(slot, captured); _dropdown.Close(); });
                container.Add(item);
            }
        }

        private void BuildObserveItems(int slot, VisualElement container)
        {
            var candidates = _controller.GetObserveTargetCandidates(slot);
            if (candidates.Count == 0) { AddDropdownEmpty(container, "파티 아군 없음"); return; }

            string currentId = GetReactionAt(slot)?.SubjectCharacterId;
            foreach (var c in candidates)
            {
                var captured = c;
                var item = MakeDropdownItem(captured.Name, null, captured.EffectiveIdleImageAddress, captured.Id == currentId);
                item.RegisterCallback<ClickEvent>(_ => { _controller.SetObserveTarget(slot, captured.Id); _dropdown.Close(); });
                container.Add(item);
            }
        }

        private void BuildActionItems(int slot, VisualElement container)
        {
            var candidates = _controller.GetActionSkillCandidates(slot);
            if (candidates.Count == 0) { AddDropdownEmpty(container, "사용 가능한 스킬 없음"); return; }

            int currentIndex = (GetReactionAt(slot)?.BaseEffect as SkillCastReactionEffect)?.SkillIndex ?? -1;
            foreach (var (index, skill) in candidates)
            {
                int capturedIndex = index;
                var capturedSkill = skill;
                var item = MakeDropdownItem(skill.Data?.DisplayName ?? "?", null, skill.Data?.IconAddress, capturedIndex == currentIndex);
                // 행동 후보는 스킬 리치 툴팁 재사용
                TooltipPopup.AttachFollow(item, () => SkillTooltipContent.Build(capturedSkill));
                item.RegisterCallback<ClickEvent>(_ => { _controller.SetActionSkill(slot, capturedIndex); _dropdown.Close(); });
                container.Add(item);
            }
        }

        // 드롭다운 1행: (옵션)아이콘 + 이름(+부제). 아이콘 주소가 있으면 비동기 로드.
        private VisualElement MakeDropdownItem(string displayName, string sub, string iconAddress, bool selected)
        {
            var item = new VisualElement();
            item.AddToClassList("char-detail__dropdown-item");
            if (selected) item.AddToClassList("char-detail__dropdown-item--selected");

            if (!string.IsNullOrEmpty(iconAddress))
            {
                var icon = new Image { scaleMode = ScaleMode.ScaleAndCrop };
                icon.AddToClassList("char-detail__dropdown-item-icon");
                item.Add(icon);
                LoadImageSpriteAsync(icon, iconAddress).Forget();
            }

            var textCol = new VisualElement();
            textCol.AddToClassList("char-detail__dropdown-item-text");
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("char-detail__dropdown-item-name");
            textCol.Add(nameLabel);
            if (!string.IsNullOrEmpty(sub))
            {
                var subLabel = new Label(sub);
                subLabel.AddToClassList("char-detail__dropdown-item-sub");
                textCol.Add(subLabel);
            }
            item.Add(textCol);
            return item;
        }

        // 새로 만들어 재사용하지 않는 Image(드롭다운 행·스킬 그리드 칩)에 스프라이트를 비동기 로드.
        // 패널 부착 전에 로드가 끝나도 sprite 는 유지됐다가 부착 시 렌더되므로 panel 체크로 폐기하지 않는다.
        private static async UniTaskVoid LoadImageSpriteAsync(Image icon, string address)
        {
            var sprite = await TryLoadSpriteAsync(address);
            if (sprite == null || icon == null) return;
            icon.sprite = sprite;
        }

        private static void AddDropdownEmpty(VisualElement container, string msg)
        {
            var label = new Label(msg);
            label.AddToClassList("char-detail__dropdown-empty");
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
        
        // ── 클래스 최하단에 헬퍼 콜백으로 추가 ──

        // [해제 시작] 장신구 슬롯에서 드래그를 시작할 때 전역 트레커 플래그를 흔들어 깨웁니다.
        private void HandleEquipSlotDragStart(int accSlotIndex)
        {
            if (_transferService != null)
            {
                // 전역 트레커에 출처 인벤토리(현재 장신구 창이 띄워놓은 가방)와 장착 슬롯 번호를 등록합니다.
                _transferService.StartEquipmentDrag(accSlotIndex);
            }
        }

        // [장착 완료] 가방에서 장신구 슬롯 위로 아이템 드롭 정산이 요청되었을 때
        private void HandleEquipSlotDragDrop(int accSlotIndex)
        {
            // 현재 마우스로 무언가 드래그 중인 상태가 맞는지 검증
            if (_transferService != null && _transferService.IsDragging)
            {
                IInventory sourceInv = _transferService.CurrentSourceInventory;
                int sourceIdx = _transferService.CurrentDraggingIndex;

                if (sourceInv != null && sourceIdx != -1)
                {
                    // 우회 규칙에 따라 주입을 피하고 컨트롤러에게 다이렉트 장착/스왑 처리 명령 하달
                    _controller.EquipAccessoryFromDrag(sourceInv, sourceIdx, accSlotIndex);
            
                    // 정산이 완료되었으므로 트레커 초기화
                    _transferService.ResetDrag();
                }
            }
        }

        // ── 해고(꾹 눌러 확정) ──────────────────────────────────────────────

        private void SetupFireButton(VisualElement root)
        {
            _fireBtn  = root.Q("character-detail__fire-btn");
            _fireRing = root.Q("character-detail__fire-ring");
            if (_fireBtn == null || _fireRing == null) return;

            // 링은 매 프레임 진행도(_fireProgress)에 따라 시계방향 호를 다시 그린다.
            _fireRing.generateVisualContent += OnGenerateFireRing;

            // 링이 버튼 테두리-박스를 정확히 덮도록 인셋(-border-width)을 동기화. 레이아웃이 잡힌 뒤 한 번 + 변경 시.
            _fireBtn.RegisterCallback<GeometryChangedEvent>(_ => SyncFireRingInset());

            // 누르는 동안에만 Resume — 진행도 갱신용 프레임 틱.
            _fireTicker = _fireBtn.schedule.Execute(UpdateFireHold).Every(16);
            _fireTicker.Pause();

            _fireBtn.RegisterCallback<PointerDownEvent>(OnFirePointerDown);
            _fireBtn.RegisterCallback<PointerUpEvent>(OnFirePointerUp);
            _fireBtn.RegisterCallback<PointerCaptureOutEvent>(_ => CancelFireHold());
        }

        /// <summary>링 오버레이를 버튼 테두리-박스에 맞춘다(인셋 = -테두리두께). border-width 만 바꿔도 정렬이 자동 추종된다.</summary>
        private void SyncFireRingInset()
        {
            if (_fireBtn == null || _fireRing == null) return;
            float bw = _fireBtn.resolvedStyle.borderTopWidth;
            _fireRing.style.left   = -bw;
            _fireRing.style.top    = -bw;
            _fireRing.style.right  = -bw;
            _fireRing.style.bottom = -bw;
        }

        private void OnFirePointerDown(PointerDownEvent evt)
        {
            // 해고는 마을 관리(allowFire + FullEdit) + 좌클릭/주 포인터에서만 시작.
            if (!_allowFire || _editMode != CharacterDetailEditMode.FullEdit || _character == null) return;
            if (evt.button != 0) return;

            _fireHolding   = true;
            _firePointerId = evt.pointerId;
            _fireHoldStart = Time.realtimeSinceStartup;
            _fireProgress  = 0f;
            _fireBtn.CapturePointer(evt.pointerId);   // 손가락이 버튼 밖으로 나가도 PointerUp 을 받기 위해
            _fireBtn.AddToClassList("character-detail__fire-btn--holding");
            _fireTicker.Resume();
            evt.StopPropagation();
        }

        private void OnFirePointerUp(PointerUpEvent evt)
        {
            if (!_fireHolding) return;
            CancelFireHold();   // 3초 전에 떼면 취소
            evt.StopPropagation();
        }

        private void UpdateFireHold()
        {
            if (!_fireHolding) return;
            _fireProgress = Mathf.Clamp01((Time.realtimeSinceStartup - _fireHoldStart) / FIRE_HOLD_SECONDS);
            _fireRing.MarkDirtyRepaint();
            if (_fireProgress >= 1f) PerformFire();
        }

        private void CancelFireHold()
        {
            if (!_fireHolding && _fireProgress <= 0f) return;

            // 상태부터 초기화 — ReleasePointer 가 PointerCaptureOut→CancelFireHold 로 재진입해도 가드에서 즉시 빠진다.
            _fireHolding  = false;
            _fireProgress = 0f;
            _fireTicker?.Pause();

            int pid = _firePointerId;
            _firePointerId = -1;
            if (pid != -1 && _fireBtn != null && _fireBtn.HasPointerCapture(pid))
                _fireBtn.ReleasePointer(pid);

            _fireBtn?.RemoveFromClassList("character-detail__fire-btn--holding");
            _fireRing?.MarkDirtyRepaint();
        }

        /// <summary>홀드가 3초를 채우면 호출. 진행 상태를 정리하고 Detail 을 닫은 뒤 로스터에서 해고한다.</summary>
        private void PerformFire()
        {
            var victim = _character;
            CancelFireHold();            // 캡처 해제 + 링 리셋 (재진입/중복 해고 방지)
            if (victim == null) return;

            Hide();
            OnCloseRequested?.Invoke();
            _roster?.Fire(victim);
        }

        /// <summary>버튼의 각진 사각 테두리를 12시(위 변 중앙)에서 시계방향으로 진행도만큼 그린다.</summary>
        private void OnGenerateFireRing(MeshGenerationContext mgc)
        {
            if (_fireProgress <= 0f) return;

            var rect = _fireRing.contentRect;
            if (rect.width <= 0f || rect.height <= 0f) return;

            // 선 두께 = 버튼 테두리 두께. 링이 테두리-박스를 덮으므로(SyncFireRingInset) 띠가 테두리 위에 정확히 겹친다.
            float lineWidth = _fireBtn.resolvedStyle.borderTopWidth;
            if (lineWidth <= 0f) return;
            float inset = lineWidth * 0.5f;             // 선이 밖으로 삐져나가지 않게 절반만큼 안쪽
            float w = rect.width, h = rect.height;
            float left = inset, top = inset, right = w - inset, bottom = h - inset;
            if (right <= left || bottom <= top) return;

            var painter = mgc.painter2D;
            painter.lineWidth   = lineWidth;
            painter.lineCap     = LineCap.Butt;
            painter.lineJoin    = LineJoin.Miter;        // 각진 모서리
            painter.strokeColor = _fireRing.resolvedStyle.color;

            // 시계방향 꼭짓점: 위 변 중앙 → 우상 → 우하 → 좌하 → 좌상 → 위 변 중앙 복귀
            float midX = w * 0.5f;
            var pts = new[]
            {
                new Vector2(midX,  top),
                new Vector2(right, top),
                new Vector2(right, bottom),
                new Vector2(left,  bottom),
                new Vector2(left,  top),
                new Vector2(midX,  top),
            };

            float perimeter = 2f * (right - left) + 2f * (bottom - top);
            float target    = perimeter * _fireProgress;

            painter.BeginPath();
            painter.MoveTo(pts[0]);

            // 변 길이를 누적하며 target 까지 그린다. 마지막 변은 중간에서 멈춘다.
            float acc = 0f;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1];
                float segLen = Vector2.Distance(a, b);
                if (segLen <= 0f) continue;

                if (acc + segLen <= target)
                {
                    painter.LineTo(b);                       // 변 전체
                }
                else
                {
                    float t = (target - acc) / segLen;
                    painter.LineTo(Vector2.Lerp(a, b, t));   // 변 중간에서 멈춤
                    break;
                }
                acc += segLen;
            }

            painter.Stroke();
        }
    }
}
