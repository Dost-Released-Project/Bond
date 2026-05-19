using Bond.Expedition;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Logic] 일지에서 "Take" 등의 액션이 발생했을 때 인벤토리에 아이템을 추가하는 핸들러
    /// </summary>
    public class JournalInventoryActionHandler : IJournalActionHandler
    {
        private readonly ExpeditionPayload _payload;

        [Inject]
        public JournalInventoryActionHandler(ExpeditionPayload payload)
        {
            _payload = payload;
        }

        public bool CanHandle(string actionKey)
        {
            // 이 핸들러는 "Take" 액션만 처리합니다.
            return actionKey == "Take";
        }

        public async UniTask ExecuteAction(string actionKey, JournalReport report)
        {
            if (report.Metadata.TryGetValue("ItemId", out string itemId))
            {
                int quantity = 1; // 기본값
                if (report.Metadata.TryGetValue("Quantity", out string quantityStr) && int.TryParse(quantityStr, out int parsedQuantity))
                {
                    quantity = parsedQuantity;
                }

                // 탐사용 인벤토리에 아이템을 추가 (비동기 대기)
                await _payload.Supplies.AddItemId(itemId, quantity);
                Debug.Log($"<color=green>[JournalInventoryActionHandler]</color> 일지 선택에 의해 탐사용 인벤토리에 아이템 추가 완료: {itemId} x {quantity}");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[JournalInventoryActionHandler]</color> 'Take' 액션이 호출되었으나, 리포트 메타데이터에 'ItemId'가 없습니다. Title: {report.Title}");
            }
        }
    }
}
