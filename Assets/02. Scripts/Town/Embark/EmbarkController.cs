using System;
using System.Collections.Generic;

namespace Bond.Embark
{
    public class EmbarkController
    {
        private readonly EmbarkData _data;
        private readonly TotalInventory _totalInventory;
        private readonly ExpeditionInventory _expeditionInventory;
        private readonly IPartyController _partyManager;
        private readonly EmbarkManager _embarkManager;

        public event Action OnOverlayOpened;
        public event Action OnOverlayClosed;
        public event Action<EmbarkData> OnDataChanged;
        public event Action<IReadOnlyList<BaseCharacter>, IReadOnlyList<InventorySlot>> OnEmbark;

        public EmbarkController(
            EmbarkData data,
            TotalInventory totalInventory,
            ExpeditionInventory expeditionInventory,
            IPartyController partyManager,
            EmbarkManager embarkManager)
        {
            _data = data;
            _totalInventory = totalInventory;
            _expeditionInventory = expeditionInventory;
            _partyManager = partyManager;
            _embarkManager = embarkManager;
        }

        public void Open()
        {
            _data.SelectedParty.Clear();
            _data.PreparedSupplies.Clear();
            OnOverlayOpened?.Invoke();
            NotifyChanged();
        }

        public void Close()
        {
            ReturnAllSupplies();
            _data.SelectedParty.Clear();
            OnOverlayClosed?.Invoke();
        }

        public void AddPartyMember(BaseCharacter character)
        {
            if (_data.SelectedParty.Contains(character) || _data.SelectedParty.Count >= 4) return;
            _data.SelectedParty.Add(character);
            NotifyChanged();
        }

        public void RemovePartyMember(BaseCharacter character)
        {
            if (_data.SelectedParty.Remove(character))
                NotifyChanged();
        }

        public void MoveToSupplies(InventorySlot slot)
        {
            var all = _totalInventory.GetAll();
            int idx = all.IndexOf(slot);
            if (idx < 0 || slot.IsEmpty) return;

            var item = slot.item;
            int qty = slot.quantity;
            _totalInventory.RemoveFromSlot(idx, qty);

            var existing = _data.PreparedSupplies.Find(s => !s.IsEmpty && s.item.id == item.id);
            if (existing != null)
                existing.quantity += qty;
            else
                _data.PreparedSupplies.Add(new InventorySlot { item = item, quantity = qty });

            NotifyChanged();
        }

        public void ReturnToTown(InventorySlot slot)
        {
            if (!_data.PreparedSupplies.Remove(slot)) return;
            _totalInventory.AddItemAuto(slot.item, slot.quantity);
            NotifyChanged();
        }

        public void ConfirmEmbark()
        {
            _partyManager.Clear();
            foreach (var member in _data.SelectedParty)
                _partyManager.TryAddMember(member);

            // ExpeditionInventory 초기화 후 보급품 복사
            var expSlots = _expeditionInventory.GetAll();
            for (int i = 0; i < expSlots.Count; i++)
                _expeditionInventory.ClearSlot(i);
            foreach (var s in _data.PreparedSupplies)
                if (!s.IsEmpty) _expeditionInventory.AddItemAuto(s.item, s.quantity);

            // TODO: 씬 전환 로직은 추후 연결. 현재는 Payload 저장 후 OnEmbark 이벤트만 발행.
            _embarkManager.SavePayload();
            OnEmbark?.Invoke(_data.SelectedParty.AsReadOnly(), _data.PreparedSupplies.AsReadOnly());
        }

        private void NotifyChanged()
        {
            _data.TownInventorySlots = _totalInventory.GetAll();
            OnDataChanged?.Invoke(_data);
        }

        private void ReturnAllSupplies()
        {
            foreach (var slot in _data.PreparedSupplies)
                if (!slot.IsEmpty) _totalInventory.AddItemAuto(slot.item, slot.quantity);
            _data.PreparedSupplies.Clear();
        }
    }
}
