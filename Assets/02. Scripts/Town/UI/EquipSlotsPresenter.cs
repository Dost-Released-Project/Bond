using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI
{
    public class EquipSlotsPresenter : IDisposable
    {
        private readonly List<IDisposable> _tooltipHandles = new List<IDisposable>();

        // 인벤토리 열기 / 부속품 해제 요청은 상위 Presenter가 처리한다
        public event Action OnInventoryOpenRequested;
        public event Action<int> OnUnequipRequested;
        // 기존 이벤트 선언부 아래에 추가
        public event Action<int> OnDragStartRequested; // 장신구 슬롯에서 드래그를 시작했음을 프레젠터에 알림
        public event Action<int> OnDragDropRequested;  // 가방에서 장신구 슬롯 위로 아이템을 떨어뜨렸음을 알림

        private VisualElement _chipWeapon;
        private VisualElement _chipArmor;
        private readonly VisualElement[] _chipAcc = new VisualElement[2];

        private BaseCharacter _character;
        private bool _editable;

        // tooltipRoot: (구) 툴팁 부착 컨테이너. 이제 TooltipPopup이 chip의 패널에서 자동 해석하므로 미사용(호환 위해 시그니처만 유지).
        public EquipSlotsPresenter(VisualElement root, VisualElement tooltipRoot = null)
        {
            _chipWeapon = root.Q("equip-chip-weapon");
            _chipArmor  = root.Q("equip-chip-armor");
            _chipAcc[0] = root.Q("equip-chip-acc0");
            _chipAcc[1] = root.Q("equip-chip-acc1");

            RegisterEvents();
        }

        public void SetCharacter(BaseCharacter character)
        {
            DetachCharacterEvents(_character);
            _character = character;
            AttachCharacterEvents(_character);
            RefreshAll();
        }

        public void Dispose()
        {
            foreach (var handle in _tooltipHandles) handle.Dispose();
            _tooltipHandles.Clear();

            DetachCharacterEvents(_character);
            _character = null;
        }

        private void AttachCharacterEvents(BaseCharacter character)
        {
            if (character == null) return;
            character.OnAccessoriesChanged += HandleAccessoriesChanged;
        }

        private void DetachCharacterEvents(BaseCharacter character)
        {
            if (character == null) return;
            character.OnAccessoriesChanged -= HandleAccessoriesChanged;
        }

        private void HandleAccessoriesChanged(BaseCharacter c) => RefreshAccessories();

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

        private void RegisterEvents()
        {
            // 무기·방어구: 툴팁만 (클릭 이벤트 없음). 배치·flip·경계 clamp는 TooltipPopup이 처리.
            _tooltipHandles.Add(TooltipPopup.Attach(_chipWeapon, () => BuildTooltip(GetWeaponTooltip())));
            _tooltipHandles.Add(TooltipPopup.Attach(_chipArmor,  () => BuildTooltip(GetArmorTooltip())));

            // 부속품: 클릭 → 인벤토리 열기, 우클릭 → 해제 요청
            for (int i = 0; i < _chipAcc.Length; i++)
            {
                int idx = i;
                _tooltipHandles.Add(TooltipPopup.Attach(_chipAcc[idx], () => BuildTooltip(GetAccTooltip(idx))));

                _chipAcc[idx].RegisterCallback<ClickEvent>(evt =>
                {
                    if (!_editable) return;
                    OnInventoryOpenRequested?.Invoke();
                });
                
                // PointerDownEvent: 우클릭 해제는 유지하되, 좌클릭 시에는 마우스 캡처를 준비합니다.
                _chipAcc[idx].RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (!_editable) return;

                    if (evt.button == 1) // 우클릭 즉시 해제 (기존 유지)
                    {
                        OnUnequipRequested?.Invoke(idx);
                        evt.StopPropagation();
                    }
                    else if (evt.button == 0) // 좌클릭 시 마우스 포인터 캡처 (드래그 준비)
                    {
                        _chipAcc[idx].CapturePointer(evt.pointerId);
                    }
                });

                // PointerMoveEvent 신규 등록: 드래그 해제 마비 해결의 핵심
                // 클릭한 상태로 마우스를 슬롯 밖으로 움직이기 시작할 때 상위 레이어에 드래그 시작 신호를 보냅니다.
                _chipAcc[idx].RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!_editable || !_chipAcc[idx].HasPointerCapture(evt.pointerId)) return;

                    var accItem = _character?.Accessories?[idx];
                    if (accItem != null)
                    {
                        // 캡처를 해제하여 마우스가 슬롯 영역 밖의 가방 포인터를 인식할 수 있도록 풀어줍니다.
                        _chipAcc[idx].ReleasePointer(evt.pointerId); 
        
                        // 상위 프레젠터로 드래그 시작(해제 프로세스) 이벤트를 쏘아 올립니다.
                        OnDragStartRequested?.Invoke(idx); 
                    }
                });

                // PointerUpEvent 신규 등록: 가방에서 장신구 슬롯 위로 드롭했을 때 (장착 및 스왑)
                _chipAcc[idx].RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!_editable) return;

                    // 만약 드래그 시작 후 슬롯 내부에서 그냥 마우스를 뗐다면 캡처만 안전하게 정리합니다.
                    if (_chipAcc[idx].HasPointerCapture(evt.pointerId))
                    {
                        _chipAcc[idx].ReleasePointer(evt.pointerId);
                        return;
                    }

                    // 가방 인벤토리 등에서 장신구 슬롯 위로 아이템을 가져와 드롭했을 때 정산 요청
                    OnDragDropRequested?.Invoke(idx);
                    _chipAcc[idx].ReleasePointer(evt.pointerId); // 캡처 잔여물 누수 원천 차단
                    evt.StopPropagation(); // 드롭 처리가 성공했으므로 전파 중단
                });
            }
        }

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
        // 배치(부착·실측·flip·경계 clamp)는 TooltipPopup이 담당. 여기선 기존 룩(equip-slots__tooltip)만 구성.

        private static VisualElement BuildTooltip(string text)
        {
            var label = new Label(text);
            label.AddToClassList("equip-slots__tooltip");
            return label;
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
