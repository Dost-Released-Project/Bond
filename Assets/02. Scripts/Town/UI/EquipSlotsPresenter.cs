using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI
{
    public class EquipSlotsPresenter
    {
        private readonly CharacterDetailController _controller;
        private readonly InventoryTransferService _transferService;
        private readonly VisualElement _tooltipRoot;

        // 인벤토리 열기는 직접 처리하지 않고 상위에 위임한다
        public event Action OnInventoryOpenRequested;

        private VisualElement _chipWeapon;
        private VisualElement _chipArmor;
        private VisualElement[] _chipAcc = new VisualElement[2];

        private BaseCharacter _character;
        private bool _editable;

        // tooltipRoot: 툴팁을 붙일 클리핑 없는 상위 컨테이너 (null이면 chip 자체에 붙임)
        public EquipSlotsPresenter(
            VisualElement root,
            CharacterDetailController controller,
            InventoryTransferService transferService = null,
            VisualElement tooltipRoot = null)
        {
            _controller      = controller;
            _transferService = transferService;
            _tooltipRoot     = tooltipRoot ?? root;

            _chipWeapon  = root.Q("equip-chip-weapon");
            _chipArmor   = root.Q("equip-chip-armor");
            _chipAcc[0]  = root.Q("equip-chip-acc0");
            _chipAcc[1]  = root.Q("equip-chip-acc1");

            RegisterEvents();

            _controller.OnAccessoryChanged += () => RefreshAccessories();
        }

        public void SetCharacter(BaseCharacter character)
        {
            _character = character;
            RefreshAll();
        }

        public void SetEditable(bool editable)
        {
            _editable = editable;

            // 무기·방어구는 상호작용 없으므로 disabled 처리 불필요
            for (int i = 0; i < _chipAcc.Length; i++)
            {
                if (editable)
                {
                    _chipAcc[i].RemoveFromClassList("equip-slots__chip--disabled");
                    _chipAcc[i].pickingMode = PickingMode.Position;
                }
                else
                {
                    _chipAcc[i].AddToClassList("equip-slots__chip--disabled");
                    _chipAcc[i].pickingMode = PickingMode.Ignore;
                }
            }
        }

        // 드롭 이벤트 수신 (AccessoryBagView 드래그 → 이 칩에 드롭)
        public void OnItemDropped(int accIndex, AccessoryItem item)
        {
            _controller.EquipAccessory(item);
        }

        private void RegisterEvents()
        {
            // 무기·방어구: 툴팁만 (클릭 이벤트 없음)
            RegisterTooltip(_chipWeapon, GetWeaponTooltip);
            RegisterTooltip(_chipArmor, GetArmorTooltip);

            // 부속품: 클릭 → 인벤토리 열기, 우클릭 → 즉시 해제
            for (int i = 0; i < _chipAcc.Length; i++)
            {
                int idx = i;
                RegisterTooltip(_chipAcc[idx], () => GetAccTooltip(idx));

                _chipAcc[idx].RegisterCallback<ClickEvent>(evt =>
                {
                    if (!_editable) return;
                    OnInventoryOpenRequested?.Invoke();
                });

                _chipAcc[idx].RegisterCallback<PointerUpEvent>(evt =>
                {
                    // AccessoryBagView에서 드래그 중인 아이템을 이 칩에 드롭
                    if (_transferService != null && _transferService.IsDragging && _editable)
                    {
                        var slot = _transferService.CurrentSourceInventory.GetSlot(_transferService.CurrentDraggingIndex);
                        if (slot.item is AccessoryItem acc)
                        {
                            OnItemDropped(idx, acc);
                            _transferService.ResetDrag();
                        }
                    }
                    evt.StopPropagation();
                });

                _chipAcc[idx].RegisterCallback<PointerEnterEvent>(evt =>
                {
                    if (_editable && _transferService != null && _transferService.IsDragging)
                        _chipAcc[idx].AddToClassList("equip-slots__chip--drop-target");
                });

                _chipAcc[idx].RegisterCallback<PointerLeaveEvent>(evt =>
                {
                    _chipAcc[idx].RemoveFromClassList("equip-slots__chip--drop-target");
                });

                // 우클릭 즉시 해제 — 목적지 인벤토리는 CharacterDetailPresenter가 _currentInventory로 관리
                // EquipSlotsPresenter는 해제 요청 이벤트를 발행하고 상위가 처리한다
                _chipAcc[idx].RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1 && _editable)
                    {
                        OnUnequipRequested?.Invoke(idx);
                        evt.StopPropagation();
                    }
                });
            }
        }

        // 해제 요청 이벤트: 상위(CharacterDetailPresenter)가 IInventory 선택 후 Controller 호출
        public event Action<int> OnUnequipRequested;

        private void RefreshAll()
        {
            RefreshWeapon();
            RefreshArmor();
            RefreshAccessories();
        }

        private void RefreshWeapon()
        {
            if (_chipWeapon == null) return;
            var weapon = _character?.Weapon;
            SetChipData(_chipWeapon, weapon?.itemName, weapon != null);
        }

        private void RefreshArmor()
        {
            if (_chipArmor == null) return;
            var armor = _character?.Armor;
            SetChipData(_chipArmor, armor?.itemName, armor != null);
        }

        private void RefreshAccessories()
        {
            for (int i = 0; i < _chipAcc.Length; i++)
            {
                if (_chipAcc[i] == null) continue;
                var acc = _character?.Accessories?[i];
                SetChipData(_chipAcc[i], acc?.itemName, acc != null);
                SetChipIcon(_chipAcc[i], acc?.icon);
                _chipAcc[i].RemoveFromClassList("equip-slots__chip--drop-target");
            }
        }

        private static void SetChipIcon(VisualElement chip, Sprite icon)
        {
            var iconEl = chip.Q<Label>(className: "equip-slots__chip-icon");
            if (iconEl == null) return;

            if (icon != null)
            {
                iconEl.text = "";
                iconEl.style.backgroundImage = new StyleBackground(icon);
            }
            else
            {
                iconEl.style.backgroundImage = new StyleBackground();
                iconEl.text = "◆";
            }
        }

        private void SetChipData(VisualElement chip, string itemName, bool equipped)
        {
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

        // ── 툴팁 ──────────────────────────────────────────────────────

        private void RegisterTooltip(VisualElement chip, Func<string> getText)
        {
            var tooltip = new Label();
            tooltip.AddToClassList("equip-slots__tooltip");
            tooltip.pickingMode    = PickingMode.Ignore;
            tooltip.style.display  = DisplayStyle.None;
            tooltip.style.position = Position.Absolute;
            _tooltipRoot.Add(tooltip);

            chip.RegisterCallback<MouseEnterEvent>(evt =>
            {
                tooltip.text = getText();
                // chip의 월드 좌표를 _tooltipRoot 로컬 좌표로 변환하여 clip 없이 표시
                var chipBounds = chip.worldBound;
                var localPos   = _tooltipRoot.WorldToLocal(new Vector2(chipBounds.x, chipBounds.yMax + 4));
                tooltip.style.left = localPos.x;
                tooltip.style.top  = localPos.y;
                tooltip.style.display = DisplayStyle.Flex;
                tooltip.BringToFront();
            });
            chip.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                tooltip.style.display = DisplayStyle.None;
            });
        }

        private string GetWeaponTooltip()
        {
            var w = _character?.Weapon;
            if (w == null) return "무기\n장착된 아이템 없음";
            return $"무기\n{w.itemName}\nSTR +{w.bonusSTR}  AGI +{w.bonusAGI}  INT +{w.bonusINT}";
        }

        private string GetArmorTooltip()
        {
            var a = _character?.Armor;
            if (a == null) return "방어구\n장착된 아이템 없음";
            return $"방어구\n{a.itemName}\nSTR +{a.bonusSTR}  AGI +{a.bonusAGI}  INT +{a.bonusINT}";
        }

        private string GetAccTooltip(int idx)
        {
            var acc = _character?.Accessories?[idx];

            if (acc == null)
            {
                string hint = _editable ? "\n클릭하여 인벤토리 열기" : "";
                return $"부속품 {idx + 1}\n장착된 아이템 없음{hint}";
            }

            string result = $"부속품 {idx + 1}\n{acc.itemName}";

            if (!string.IsNullOrEmpty(acc.Description))
                result += $"\n{acc.Description}";

            if (acc.specialEffects != null && acc.specialEffects.Count > 0)
            {
                result += "\n[장착 효과]";
                foreach (var effect in acc.specialEffects)
                {
                    string sign = effect.value >= 0 ? "+" : "";
                    string val  = effect.mode == ModifierMode.Flat
                        ? $"{sign}{effect.value:0.#}"
                        : $"{sign}{effect.value:P0}";
                    result += $"\n{effect.name}  {val}";
                }
            }

            if (_editable)
                result += "\n우클릭으로 해제";

            return result;
        }

        private static string Truncate(string s, int max) =>
            s != null && s.Length > max ? s[..max] + "…" : s ?? "";
    }
}
