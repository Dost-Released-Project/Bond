using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Test Logic] IJournalActionHandler 구현 테스트용 모크 핸들러
    /// </summary>
    public class MockJournalActionHandler : IJournalActionHandler
    {
        public bool CanHandle(string actionKey)
        {
            if (string.IsNullOrEmpty(actionKey)) return false;
            
            // 실제 프로덕션에서 동작해야 하는 구체적인 액션 키들은 가로채지 않도록 예외 처리
            if (actionKey == "ACTION_RETURN_MAP") return false;

            // 그 외 테스트용이거나 미구현된 키들에 대해서만 반응하여 로그 출력
            return true;
        }

        public async UniTask ExecuteAction(string actionKey, JournalReport report)
        {
            string itemIdInfo = report.Metadata.ContainsKey("ItemId") ? $", ItemId: {report.Metadata["ItemId"]}" : "";
            string quantityInfo = report.Metadata.ContainsKey("Quantity") ? $", Quantity: {report.Metadata["Quantity"]}" : "";
            Debug.Log($"<color=cyan>[MockJournalActionHandler]</color> 외부 시스템에서 액션 실행: <b>{actionKey}</b>{itemIdInfo}{quantityInfo}");

            // 실제 프로젝트에서는 여기서 InventorySystem.AddItem() 등을 호출하게 됩니다.
            await UniTask.Yield(); // 비동기 구문을 맞추기 위해 1 프레임 대기
        }
    }
}

