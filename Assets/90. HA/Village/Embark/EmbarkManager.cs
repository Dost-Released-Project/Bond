using Bond.Expedition;
using VContainer;

namespace Bond.Embark
{
    public class EmbarkManager
    {
        [Inject] private IPartyProvider partyManager;
        [Inject] private IExpeditionInventory expeditionInventory;
        [Inject] private ExpeditionPayload payload;

        public void SavePayload()
        {
            payload.SetContents(partyManager.GetCurrentParty(), expeditionInventory, "mollu");
        }
    }
}