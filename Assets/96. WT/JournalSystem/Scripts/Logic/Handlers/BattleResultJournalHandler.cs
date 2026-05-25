using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bond.WT.Journal.Handlers
{
    /// <summary>
    /// 일지의 마지막 장에서 "맵으로 복귀" 버튼을 눌렀을 때의 액션을 처리하는 핸들러.
    /// BattleFlowManager가 JournalReport에 심어둔 Metadata를 기반으로 StageResult를 생성하여 맵으로 돌아갑니다.
    /// </summary>
    public class BattleResultJournalHandler : IJournalActionHandler
    {
        private const string ACTION_KEY = "ACTION_RETURN_MAP";

        public bool CanHandle(string actionKey)
        {
            return actionKey == ACTION_KEY;
        }

        public UniTask ExecuteAction(string actionKey, JournalReport report)
        {
            Debug.Log($"<color=green>[BattleResultJournalHandler] '{ACTION_KEY}' 액션 실행: 맵으로 복귀합니다.</color>");

            bool isPlayerWin = false;

            // Metadata에 저장된 승패 정보 파싱
            if (report.Metadata.TryGetValue("IsPlayerWin", out string isWinStr))
            {
                bool.TryParse(isWinStr, out isPlayerWin);
            }
            else
            {
                Debug.LogWarning("[BattleResultJournalHandler] Report Metadata에 'IsPlayerWin' 키가 없습니다. 기본값(false)으로 간주합니다.");
            }

            // 맵으로 복귀할 결과 객체 생성
            StageResult result = new StageResult
            {
                IsSuccess = isPlayerWin,
                IsGameOver = !isPlayerWin,
                IsBattleTriggered = false,
                RewardIds = new List<string>() // 보상 정보는 추후 기획에 따라 Metadata 등을 통해 전달 가능
            };

            // 정적 채널을 통해 맵 씬에 알림
            StageCompletionChannel.Invoke(result);

            return UniTask.CompletedTask;
        }
    }
}
