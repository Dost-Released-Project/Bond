using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Bond.UI.PartySelection
{
    public class SlotState
    {
        public bool          IsEmpty           => AssignedCharacter == null;
        public BaseCharacter AssignedCharacter;
    }

    public class PartyPanelPresenter
    {
        public event Action<BaseCharacter> OnMemberAssigned;
        public event Action<int>           OnMemberRemoved;

        private const int SlotCount = 3;

        private readonly VisualElement _memberSlotsContainer;
        private readonly Label         _countLabel;
        private readonly Button        _btnNext;

        private readonly List<SlotState>     _slots      = new();
        private readonly List<VisualElement> _slotRoots  = new();

        public IReadOnlyList<SlotState> Slots => _slots;

        public PartyPanelPresenter(VisualElement root)
        {
            _memberSlotsContainer = root.Q<VisualElement>("memberSlots");
            _countLabel           = root.Q<Label>("partyCountLabel");
            _btnNext              = root.Q<Button>("btnNext");

            for (int i = 0; i < SlotCount; i++)
            {
                _slots.Add(new SlotState());
                _slotRoots.Add(BuildEmptySlot(i));
                _memberSlotsContainer.Add(_slotRoots[i]);
            }

            RefreshFooter();
        }

        // 빈 슬롯 중 가장 앞 번호에 캐릭터 배정
        public bool TryAssign(BaseCharacter character)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (!_slots[i].IsEmpty) continue;
                Assign(i, character);
                return true;
            }
            return false;
        }

        // 이미 배정된 캐릭터인지 확인
        public bool IsAssigned(BaseCharacter character)
        {
            foreach (var s in _slots)
                if (s.AssignedCharacter == character) return true;
            return false;
        }

        // 캐릭터가 배정된 슬롯을 찾아 해제
        public bool TryRelease(BaseCharacter character)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i].AssignedCharacter == character)
                {
                    Release(i);
                    return true;
                }
            }
            return false;
        }

        private void Assign(int index, BaseCharacter character)
        {
            _slots[index].AssignedCharacter = character;
            RebuildSlot(index);
            RefreshFooter();
            OnMemberAssigned?.Invoke(character);
        }

        private void Release(int index)
        {
            var character = _slots[index].AssignedCharacter;
            _slots[index].AssignedCharacter = null;
            RebuildSlot(index);
            RefreshFooter();
            OnMemberRemoved?.Invoke(index);
            _ = character; // suppress unused warning
        }

        private void RebuildSlot(int index)
        {
            var old = _slotRoots[index];
            _memberSlotsContainer.Remove(old);

            VisualElement newSlot = _slots[index].IsEmpty
                ? BuildEmptySlot(index)
                : BuildFilledSlot(index);

            _memberSlotsContainer.Insert(index, newSlot);
            _slotRoots[index] = newSlot;
        }

        private VisualElement BuildEmptySlot(int index)
        {
            var root = new VisualElement();
            root.AddToClassList("party-slot");
            root.AddToClassList("party-slot--empty");

            var label = new Label($"대원 미배정") ;
            label.AddToClassList("party-slot__name");
            root.Add(label);

            return root;
        }

        private VisualElement BuildFilledSlot(int index)
        {
            var character = _slots[index].AssignedCharacter;
            var stat      = character.Stat;

            bool isDanger = stat != null &&
                            ((float)stat.current_Hp / stat.max_Hp <= 0.3f ||
                             character.Insanity >= 80);

            var root = new Button(() => Release(index));
            root.AddToClassList("party-slot");
            root.AddToClassList("party-slot--filled");
            if (isDanger) root.AddToClassList("party-slot--danger");

            var name = new Label(character.UnitName);
            name.AddToClassList("party-slot__name");

            var cls = new Label(stat != null
                ? $"{character.Profession}  Lv.{character.Level}"
                : $"Lv.{character.Level}");
            cls.AddToClassList("party-slot__class");

            root.Add(name);
            root.Add(cls);

            return root;
        }

        private void RefreshFooter()
        {
            int filled = 0;
            foreach (var s in _slots) if (!s.IsEmpty) filled++;

            _countLabel.text = $"{filled} / {SlotCount}";

            bool full = filled == SlotCount;
            if (full)
            {
                _btnNext.RemoveFromClassList("btn-next--disabled");
                _btnNext.SetEnabled(true);
            }
            else
            {
                _btnNext.AddToClassList("btn-next--disabled");
                _btnNext.SetEnabled(false);
            }
        }

        public void BindNextButton(Action onNext) =>
            _btnNext.clicked += onNext;
    }
}