using Bond.Embark;
using VContainer;

namespace Bond.Expedition
{
    public class EmbarkManager
    {
        [Inject] private PartyManager partyManager;
        [Inject] private IInventory expeditionInventory;

        public ExpeditionPayload GetPayload()
        {
            var reVal = new ExpeditionPayload();

            reVal.SetContents(partyManager.GetCurrentPartyDataOnly(), expeditionInventory, "mollu");
            
            return reVal;
        }
    }
}