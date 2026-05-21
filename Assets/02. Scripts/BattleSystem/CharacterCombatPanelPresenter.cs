using Bond.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

public class CharacterCombatPanelPresenter
{
    private readonly VisualElement _root;
    private readonly CharacterCombatPanelController _controller;
    private readonly CharacterDetailPresenter _detailPresenter;

    private readonly VisualElement _charIcon;
    private readonly Label _iconText;
    private readonly Label _charName;
    private readonly Label _charJob;
    private readonly Label _charRole;

    private readonly VisualElement _hpFill;
    private readonly Label _hpValue;
    private readonly VisualElement _insanityFill;
    private readonly Label _insanityValue;

    private readonly Label _statAtk;
    private readonly Label _statDef;
    private readonly Label _statSpAtk;
    private readonly Label _statSpd;
    private readonly Label _statCrt;
    private readonly Label _statAcc;

    private readonly VisualElement[] _skillSlots = new VisualElement[4];
    private readonly Label[] _skillNames = new Label[4];
    private readonly VisualElement[] _skillIcons = new VisualElement[4];

    private readonly VisualElement _chipWeapon;
    private readonly VisualElement _chipArmor;
    private readonly VisualElement[] _chipAcc = new VisualElement[2];

    private BaseCharacter _character;

    public CharacterCombatPanelPresenter(
        VisualElement root,
        CharacterCombatPanelController controller,
        CharacterDetailPresenter detailPresenter)
    {
        _root = root;
        _controller = controller;
        _detailPresenter = detailPresenter;

        _charIcon  = root.Q("combat-panel__char-icon");
        _iconText  = root.Q<Label>("combat-panel__icon-text");
        _charName  = root.Q<Label>("combat-panel__char-name");
        _charJob   = root.Q<Label>("combat-panel__char-job");
        _charRole  = root.Q<Label>("combat-panel__char-role");

        _hpFill         = root.Q("combat-panel__hp-fill");
        _hpValue        = root.Q<Label>("combat-panel__hp-value");
        _insanityFill   = root.Q("combat-panel__insanity-fill");
        _insanityValue  = root.Q<Label>("combat-panel__insanity-value");

        _statAtk   = root.Q<Label>("stat-atk-value");
        _statDef   = root.Q<Label>("stat-def-value");
        _statSpAtk = root.Q<Label>("stat-spatk-value");
        _statSpd   = root.Q<Label>("stat-spd-value");
        _statCrt   = root.Q<Label>("stat-crt-value");
        _statAcc   = root.Q<Label>("stat-acc-value");

        for (int i = 0; i < 4; i++)
        {
            _skillSlots[i] = root.Q($"skill-slot-{i}");
            if (_skillSlots[i] != null)
            {
                _skillNames[i] = _skillSlots[i].Q<Label>(className: "combat-panel__skill-name");
                _skillIcons[i] = _skillSlots[i].Q(className: "combat-panel__skill-icon");
            }
        }

        var equipMount = root.Q("combat-panel__equip-mount");
        if (equipMount != null)
        {
            _chipWeapon  = equipMount.Q("equip-chip-weapon");
            _chipArmor   = equipMount.Q("equip-chip-armor");
            _chipAcc[0]  = equipMount.Q("equip-chip-acc0");
            _chipAcc[1]  = equipMount.Q("equip-chip-acc1");
        }

        _controller.OnCharacterUpdated += BindCharacter;
        _controller.OnTurnStateChanged += RefreshSkillSlots;
        _controller.OnSkillSelected    += RefreshSkillSelection;

        RegisterRightClickOnIcon();
        RegisterSkillSlotClicks();
    }

    public void BindCharacter(BaseCharacter character)
    {
        _character = character;

        if (_charName != null)  _charName.text = character.Name ?? "";
        if (_charJob  != null)  _charJob.text  = character.Profession?.Name ?? "";
        if (_charRole != null)  _charRole.text = character.RoleType.ToString();

        // TODO: 캐릭터 초상화 에셋 연결 필요
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

    public void RefreshHpBar(int current, int max)
    {
        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        if (_hpFill != null)
        {
            _hpFill.style.width = Length.Percent(ratio * 100f);

            if (ratio <= 0.3f)
            {
                _hpFill.AddToClassList("combat-panel__hp-fill--low");
            }
            else
            {
                _hpFill.RemoveFromClassList("combat-panel__hp-fill--low");
            }
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

    public void RefreshSkillSlots(bool isMyTurn)
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

    public void RefreshSkillSelection(int selectedIndex)
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

    public void RegisterRightClickOnIcon()
    {
        _charIcon?.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 1) return;
            if (_character == null) return;
            _detailPresenter?.Show(_character, CharacterDetailViewMode.ReadOnly, null);
            evt.StopPropagation();
        });
    }

    // ── 내부 ────────────────────────────────────────────────────────────

    private void RegisterSkillSlotClicks()
    {
        for (int i = 0; i < 4; i++)
        {
            int idx = i;
            _skillSlots[i]?.RegisterCallback<ClickEvent>(evt =>
            {
                // Inactive 상태이면 Controller에 전달하지 않는다
                if (_skillSlots[idx].ClassListContains("combat-panel__skill-slot--inactive")) return;
                _controller.SelectSkill(idx);
            });
        }
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
        SetChipData(_chipArmor, character.Armor?.itemName, character.Armor != null);
        for (int i = 0; i < 2; i++)
        {
            var acc = character.Accessories?[i];
            SetChipData(_chipAcc[i], acc?.itemName, acc != null);
        }
    }

    private void RefreshDangerState(bool hpDanger)
    {
        bool danger = hpDanger || (_character != null && _character.Insanity >= 80);
        if (danger)
            _root.AddToClassList("character-combat-panel--danger");
        else
            _root.RemoveFromClassList("character-combat-panel--danger");
    }

    private static void SetChipData(VisualElement chip, string itemName, bool equipped)
    {
        if (chip == null) return;

        var nameLabel = chip.Q<Label>(className: "equip-slots__chip-name");
        if (nameLabel != null)
            nameLabel.text = equipped ? Truncate(itemName, 8) : "비어있음";

        if (equipped)
        {
            chip.RemoveFromClassList("equip-slots__chip--empty");
            chip.AddToClassList("equip-slots__chip--equipped");
        }
        else
        {
            chip.RemoveFromClassList("equip-slots__chip--equipped");
            chip.AddToClassList("equip-slots__chip--empty");
        }
    }

    private async UniTaskVoid LoadPortraitAsync(string address)
    {
        var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
        if (sprite == null || _charIcon == null) return;
        _charIcon.style.backgroundImage = new StyleBackground(sprite);
        if (_iconText != null) _iconText.style.display = DisplayStyle.None;
    }

    private async UniTaskVoid LoadSkillIconAsync(VisualElement iconEl, string address)
    {
        var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
        if (sprite == null || iconEl == null) return;
        iconEl.style.backgroundImage = new StyleBackground(sprite);
    }

    private static string FirstChar(string s) =>
        !string.IsNullOrEmpty(s) ? s[0].ToString() : "?";

    private static string Truncate(string s, int max) =>
        s != null && s.Length > max ? s[..max] + "…" : s ?? "";
}
