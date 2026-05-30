using Bond.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using VContainer;

public class CharacterCombatPanelPresenter : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private CharacterCombatPanelController _controller;
    private CharacterDetailPresenter _detailPresenter;
    private ICharacterSelector _selector;
    private EquipSlotsPresenter _equipSlots;

    private VisualElement _root;
    private VisualElement _charIcon;
    private Label _iconText;
    private Label _charName;
    private Label _charJob;
    private Label _charRole;

    private VisualElement _hpFill;
    private Label _hpValue;
    private VisualElement _insanityFill;
    private Label _insanityValue;

    private Label _statAtk;
    private Label _statDef;
    private Label _statSpAtk;
    private Label _statSpd;
    private Label _statCrt;
    private Label _statAcc;

    private readonly VisualElement[] _skillSlots = new VisualElement[4];
    private readonly Label[]         _skillNames = new Label[4];
    private readonly VisualElement[] _skillIcons = new VisualElement[4];

    private BaseCharacter _character;

    [Inject]
    public void Construct(
        CharacterDetailPresenter detailPresenter,
        ICharacterSelector selector)
    {
        _detailPresenter  = detailPresenter;
        _selector         = selector;
    }

    private void Start()
    {
        _controller = new CharacterCombatPanelController();

        _root = _document.rootVisualElement;

        _charIcon  = _root.Q("combat-panel__char-icon");
        _iconText  = _root.Q<Label>("combat-panel__icon-text");
        _charName  = _root.Q<Label>("combat-panel__char-name");
        _charJob   = _root.Q<Label>("combat-panel__char-job");
        _charRole  = _root.Q<Label>("combat-panel__char-role");

        _hpFill        = _root.Q("combat-panel__hp-fill");
        _hpValue       = _root.Q<Label>("combat-panel__hp-value");
        _insanityFill  = _root.Q("combat-panel__insanity-fill");
        _insanityValue = _root.Q<Label>("combat-panel__insanity-value");

        _statAtk   = _root.Q<Label>("stat-atk-value");
        _statDef   = _root.Q<Label>("stat-def-value");
        _statSpAtk = _root.Q<Label>("stat-spatk-value");
        _statSpd   = _root.Q<Label>("stat-spd-value");
        _statCrt   = _root.Q<Label>("stat-crt-value");
        _statAcc   = _root.Q<Label>("stat-acc-value");

        for (int i = 0; i < 4; i++)
        {
            _skillSlots[i] = _root.Q($"skill-slot-{i}");
            if (_skillSlots[i] != null)
            {
                _skillNames[i] = _skillSlots[i].Q<Label>(className: "combat-panel__skill-name");
                _skillIcons[i] = _skillSlots[i].Q(className: "combat-panel__skill-icon");
            }
        }

        var equipMount = _root.Q("combat-panel__equip-mount");
        if (equipMount != null)
        {
            _equipSlots = new EquipSlotsPresenter(equipMount, _root);
            _equipSlots.SetEditable(false);
        }

        _controller.OnCharacterUpdated += BindCharacter;
        _controller.OnTurnStateChanged += RefreshSkillSlots;
        _controller.OnSkillSelected    += RefreshSkillSelection;

        _selector.OnSelectionChanged += character =>
        {
            if (character != null) SetCharacter(character);
        };

        if (_selector.Selected != null)
            SetCharacter(_selector.Selected);

        RegisterRightClickOnIcon();
        RegisterSkillSlotClicks();
    }

    public void SetCharacter(BaseCharacter character) => _controller.SetCharacter(character);

    // ── 바인딩 ────────────────────────────────────────────────────────────

    private void BindCharacter(BaseCharacter character)
    {
        DetachCharacterEvents(_character);
        _character = character;
        AttachCharacterEvents(_character);

        RefreshIdentity();
        RefreshHpBar(character.Stat.current_Hp, character.Stat.max_Hp);
        RefreshInsanityBar(character.Insanity, 100);
        RefreshStats();
        BindSkillSlots(character);
        _equipSlots?.SetCharacter(character);
    }

    private void RefreshIdentity()
    {
        if (_character == null) return;

        if (_charName != null) _charName.text = _character.Name ?? "";
        if (_charJob  != null) _charJob.text  = _character.Profession?.Name ?? "";
        if (_charRole != null) _charRole.text = _character.RoleType.ToString();

        if (_iconText != null)
            _iconText.text = FirstChar(_character.Profession?.Name);
        if (!string.IsNullOrEmpty(_character.ImageAddress))
            LoadPortraitAsync(_character.ImageAddress).Forget();
    }

    private void RefreshStats()
    {
        if (_character == null) return;
        var stat = _character.Stat;
        if (_statAtk   != null) _statAtk.text   = stat.atk.ToString();
        if (_statDef   != null) _statDef.text    = stat.def.ToString();
        if (_statSpAtk != null) _statSpAtk.text  = stat.Sp_Atk.ToString();
        if (_statSpd   != null) _statSpd.text    = stat.speed.ToString();
        if (_statCrt   != null) _statCrt.text    = $"{stat.crt:P0}%";
        if (_statAcc   != null) _statAcc.text    = $"{stat.acc:P0}%";
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

    private void HandleHpChanged(BaseCharacter c)        => RefreshHpBar(c.Stat.current_Hp, c.Stat.max_Hp);
    private void HandleInsanityChanged(BaseCharacter c)  => RefreshInsanityBar(c.Insanity, 100);
    private void HandleStatRecalculated(BaseCharacter c)
    {
        RefreshStats();
        RefreshHpBar(c.Stat.current_Hp, c.Stat.max_Hp);
    }
    private void HandleRoleChanged(BaseCharacter c)      => RefreshIdentity();

    private void OnDestroy()
    {
        DetachCharacterEvents(_character);
        _equipSlots?.Dispose();
    }

    private void BindSkillSlots(BaseCharacter character)
    {
        for (int i = 0; i < 4; i++)
        {
            var skill = character.Skills?[i];
            if (_skillNames[i] != null)
                _skillNames[i].text = skill?.Data?.DisplayName ?? "-";

            if (_skillIcons[i] != null)
            {
                _skillIcons[i].style.backgroundImage = new StyleBackground();
                string iconAddr = skill?.Data?.IconAddress;
                if (!string.IsNullOrEmpty(iconAddr))
                    LoadSkillIconAsync(_skillIcons[i], iconAddr).Forget();
            }
        }
    }

    // ── 갱신 ────────────────────────────────────────────────────────────

    public void RefreshHpBar(int current, int max)
    {
        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (_hpFill != null)
        {
            _hpFill.style.width = Length.Percent(ratio * 100f);
            _hpFill.EnableInClassList("combat-panel__hp-fill--low", ratio <= 0.3f);
        }

        if (_hpValue != null)
            _hpValue.text = $"{current}/{max}";

        RefreshDangerState(ratio <= 0.3f);
    }

    public void RefreshInsanityBar(int current, int max)
    {
        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (_insanityFill != null)
        {
            _insanityFill.style.width = Length.Percent(ratio * 100f);
            _insanityFill.RemoveFromClassList("combat-panel__insanity-fill--warn");
            _insanityFill.RemoveFromClassList("combat-panel__insanity-fill--crit");

            if (current >= 80)
                _insanityFill.AddToClassList("combat-panel__insanity-fill--crit");
            else if (current >= 50)
                _insanityFill.AddToClassList("combat-panel__insanity-fill--warn");
        }

        if (_insanityValue != null)
            _insanityValue.text = $"{current}/{max}";
    }

    private void RefreshSkillSlots(bool isMyTurn)
    {
        for (int i = 0; i < 4; i++)
        {
            var slot = _skillSlots[i];
            if (slot == null) continue;

            if (isMyTurn)
            {
                slot.RemoveFromClassList("combat-panel__skill-slot--inactive");
                slot.AddToClassList("combat-panel__skill-slot--ready");
            }
            else
            {
                slot.RemoveFromClassList("combat-panel__skill-slot--ready");
                slot.RemoveFromClassList("combat-panel__skill-slot--selected");
                slot.AddToClassList("combat-panel__skill-slot--inactive");
            }
        }
    }

    private void RefreshSkillSelection(int selectedIndex)
    {
        for (int i = 0; i < 4; i++)
        {
            var slot = _skillSlots[i];
            if (slot == null) continue;
            slot.RemoveFromClassList("combat-panel__skill-slot--selected");
        }

        if (selectedIndex >= 0 && selectedIndex < 4 && _skillSlots[selectedIndex] != null)
        {
            _skillSlots[selectedIndex].RemoveFromClassList("combat-panel__skill-slot--ready");
            _skillSlots[selectedIndex].AddToClassList("combat-panel__skill-slot--selected");
        }
    }

    private void RefreshDangerState(bool hpDanger)
    {
        bool danger = hpDanger || (_character != null && _character.Insanity >= 80);
        if (_root != null)
            _root.EnableInClassList("character-combat-panel--danger", danger);
    }

    // ── 이벤트 등록 ─────────────────────────────────────────────────────

    private void RegisterRightClickOnIcon()
    {
        _charIcon?.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 1 || _character == null) return;
            _detailPresenter?.Show(_character, CharacterDetailEditMode.ReadOnly, null);
            evt.StopPropagation();
        });
    }

    private void RegisterSkillSlotClicks()
    {
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            _skillSlots[i]?.RegisterCallback<ClickEvent>(evt =>
            {
                if (_skillSlots[idx].ClassListContains("combat-panel__skill-slot--inactive")) return;
                _controller.SelectSkill(idx);
            });
        }
    }

    // ── Addressables ────────────────────────────────────────────────────

    private async UniTaskVoid LoadPortraitAsync(string address)
    {
        var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
        if (sprite == null || _charIcon == null) return;
        _charIcon.style.backgroundImage = new StyleBackground(sprite);
        if (_iconText != null) _iconText.style.display = DisplayStyle.None;
    }

    #pragma warning disable CS1998
    private async UniTaskVoid LoadSkillIconAsync(VisualElement iconEl, string address)
    {
        // TODO: 실제 이미지 로드로 변경
        iconEl.style.backgroundImage = new StyleBackground(Texture2D.whiteTexture);
        
        // var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
        // if (sprite == null || iconEl == null) return;
        // iconEl.style.backgroundImage = new StyleBackground(sprite);
    }
    #pragma warning restore CS1998

    // ── 유틸 ────────────────────────────────────────────────────────────

    private static string FirstChar(string s) =>
        !string.IsNullOrEmpty(s) ? s[0].ToString() : "?";
}
