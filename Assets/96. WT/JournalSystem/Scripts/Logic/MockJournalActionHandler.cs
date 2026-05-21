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
            // 모든 액션 키를 처리하거나, 특정 프리픽스를 검사할 수 있습니다.
            return !string.IsNullOrEmpty(actionKey);
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
