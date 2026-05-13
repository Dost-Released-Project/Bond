using System.Collections.Generic;

namespace Bond.Embark
{
    public class EmbarkData
    {
        public List<BaseCharacter> SelectedParty { get; } = new(4);
        public List<InventorySlot> PreparedSupplies { get; } = new();

        // Controller가 OnDataChanged 발행 전에 채워서 Presenter에 전달
        public List<InventorySlot> TownInventorySlots { get; set; } = new();
    }
}
