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

    private VisualElement _chipWeapon;
    private VisualElement _chipArmor;
    private readonly VisualElement[] _chipAcc = new VisualElement[2];

    private BaseCharacter _character;

    [Inject]
    public void Construct(CharacterDetailPresenter detailPresenter, ICharacterSelector selector)
    {
        _detailPresenter = detailPresenter;
        _selector        = selector;
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
            _chipWeapon = equipMount.Q("equip-chip-weapon");
            _chipArmor  = equipMount.Q("equip-chip-armor");
            _chipAcc[0] = equipMount.Q("equip-chip-acc0");
            _chipAcc[1] = equipMount.Q("equip-chip-acc1");
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
        _character = character;

        if (_charName != null) _charName.text = character.Name ?? "";
        if (_charJob  != null) _charJob.text  = character.Profession?.Name ?? "";
        if (_charRole != null) _charRole.text = character.RoleType.ToString();

        if (_iconText != null)
            _iconText.text = FirstChar(character.Profession?.Name);
        if (!string.IsNullOrEmpty(character.ImageAddress))
            LoadPortraitAsync(character.ImageAddress).Forget();

        RefreshHpBar(character.Stat.current_Hp, character.Stat.max_Hp);
        RefreshInsanityBar(character.Insanity, 100);

        var stat = character.Stat;
        if (_statAtk   != null) _statAtk.text   = stat.atk.ToString();
        if (_statDef   != null) _statDef.text    = stat.def.ToString();
        if (_statSpAtk != null) _statSpAtk.text  = stat.Sp_Atk.ToString();
        if (_statSpd   != null) _statSpd.text    = stat.speed.ToString();
        if (_statCrt   != null) _statCrt.text    = $"{stat.crt:0.#}%";
        if (_statAcc   != null) _statAcc.text    = $"{stat.acc:0.#}%";

        BindSkillSlots(character);
        RefreshEquipSlots(character);
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

    private void RefreshEquipSlots(BaseCharacter character)
    {
        SetChipData(_chipWeapon, character.Weapon?.itemName, character.Weapon != null);
        SetChipData(_chipArmor,  character.Armor?.itemName,  character.Armor  != null);
        for (int i = 0; i < 2; i++)
        {
            var acc = character.Accessories?[i];
            SetChipData(_chipAcc[i], acc?.itemName, acc != null);
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
            _detailPresenter?.Show(_character, CharacterDetailViewMode.ReadOnly, null);
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

    private async UniTaskVoid LoadSkillIconAsync(VisualElement iconEl, string address)
    {
        iconEl.style.backgroundImage = new StyleBackground(Texture2D.whiteTexture);
        return;
        
        var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
        if (sprite == null || iconEl == null) return;
        iconEl.style.backgroundImage = new StyleBackground(sprite);
    }

    // ── 유틸 ────────────────────────────────────────────────────────────

    private static string FirstChar(string s) =>
        !string.IsNullOrEmpty(s) ? s[0].ToString() : "?";

    private static string Truncate(string s, int max) =>
        s != null && s.Length > max ? s[..max] + "…" : s ?? "";

    private static void SetChipData(VisualElement chip, string itemName, bool equipped)
    {
        if (chip == null) return;
        var nameLabel = chip.Q<Label>(className: "equip-slots__chip-name");
        if (nameLabel != null)
            nameLabel.text = equipped ? Truncate(itemName, 8) : "비어있음";

        chip.EnableInClassList("equip-slots__chip--equipped", equipped);
        chip.EnableInClassList("equip-slots__chip--empty",    !equipped);
    }
}
