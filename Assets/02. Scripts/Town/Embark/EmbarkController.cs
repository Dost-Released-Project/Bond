using System;
using System.Collections.Generic;
using Bond.Expedition;

namespace Bond.Embark
{
    public struct EmbarkData
    {
        public List<BaseCharacter> SelectedParty { get; set; }
        public List<InventorySlot> PreparedSupplies { get; set; }

        // Controller가 OnDataChanged 발행 전에 채워서 Presenter에 전달
        public List<InventorySlot> TownInventorySlots { get; set; }
    }

    public class EmbarkController
    {
        private readonly ExpeditionPayload _payload;
        private readonly TotalInventory _totalInventory;
        private readonly ExpeditionInventory _expeditionInventory;
        private readonly IPartyController _partyManager;
        private readonly ExpeditionResultService _expeditionResultService;
        private readonly InventoryTransferService _transferService;

        public event Action OnOverlayOpened;
        public event Action OnOverlayClosed;
        public event Action<EmbarkData> OnDataChanged;

        public EmbarkController(
            ExpeditionPayload payload,
            TotalInventory totalInventory,
            ExpeditionInventory expeditionInventory,
            IPartyController partyManager,
            ExpeditionResultService expeditionResultService,
            InventoryTransferService transferService)
        {
            _payload = payload;
            _totalInventory = totalInventory;
            _expeditionInventory = expeditionInventory;
            _partyManager = partyManager;
            _expeditionResultService = expeditionResultService;
            _transferService = transferService;
        }

        public void Open()
        {
            OnOverlayOpened?.Invoke();
            NotifyChanged();
        }

        public void Close()
        {
            ReturnAllSupplies();
            _partyManager.Clear();
            OnOverlayClosed?.Invoke();
        }

        public void TogglePartyMember(BaseCharacter character)
        {
            bool isChanged = false;
            if (_partyManager.IsInParty(character))
                isChanged = _partyManager.RemoveMember(character);
            else
                isChanged = _partyManager.TryAddMember(character);

            if (isChanged)
                NotifyChanged();
        }

        public void RemovePartyMember(BaseCharacter character)
        {
            if (_partyManager.RemoveMember(character))
                NotifyChanged();
        }

        public void TownToSupp(int index, InventorySlot slot)
        {
            if (index < 0 || slot.IsEmpty) return;

            _transferService.MoveOneFromSlot(_totalInventory, index, _expeditionInventory);
            NotifyChanged();
        }

        public void SuppToTown(int index, InventorySlot slot)
        {
            _transferService.MoveOneFromSlot(_expeditionInventory, index, _totalInventory);
            NotifyChanged();
        }

        public void ConfirmEmbark()
        {
            SavePayload();
            SceneLoader.Load("Test_3_Node");
        }

        public void SavePayload()
        {
            _payload.SetContents(_partyManager.GetCurrentParty(), _expeditionInventory, DungeonType.Forest);
        }

        private void NotifyChanged()
        {
            var data = new EmbarkData
            {
                TownInventorySlots = _totalInventory.GetAll(),
                PreparedSupplies = _expeditionInventory.GetAll(),
                SelectedParty = _partyManager.GetCurrentParty()
            };
            OnDataChanged?.Invoke(data);
        }

        private void ReturnAllSupplies()
        {
            _expeditionResultService.ProcessExpeditionReturn();
        }
    }
}