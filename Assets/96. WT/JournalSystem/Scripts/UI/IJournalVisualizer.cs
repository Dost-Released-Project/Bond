using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Visual Interface] 일지 시스템의 시각적 표현을 추상화하는 인터페이스.
    /// UI 프레임워크(UGUI, UI Toolkit 등)에 독립적으로 로직을 구성하기 위해 사용합니다.
    /// </summary>
    public interface IJournalVisualizer
    {
        /// <summary>
        /// 일지 텍스트를 출력 (타이핑 연출 여부 포함)
        /// </summary>
        void ShowText(string text, bool isTyping = true);
        
        /// <summary>
        /// 사건 아이콘 설정
        /// </summary>
        void SetIcon(Sprite icon);
        
        /// <summary>
        /// 선택지 버튼들을 생성 및 표시
        /// </summary>
        void SetOptions(IReadOnlyList<JournalOption> options);
        
        /// <summary>
        /// UI를 초기 상태로 되돌림
        /// </summary>
        void ClearUI();

        /// <summary>
        /// 전투 결과 전용 UI 요소를 동적으로 세팅
        /// </summary>
        void SetBattleResult(
            BattleSystem.Interface.BattleEndStatus status, 
            IReadOnlyList<BaseCharacter> party, 
            Dictionary<string, Sprite> portraits, 
            int frontier, 
            int wood, 
            int ore);

        /// <summary>
        /// 세팅된 전투 결과 UI 요소를 정리
        /// </summary>
        void ClearBattleResult();

        /// <summary>
        /// UI 창의 활성화/비활성화 제어
        /// </summary>
        void SetVisible(bool isVisible);

        /// <summary>
        /// 뷰에서 발생하는 이벤트 핸들러
        /// </summary>
        Action OnNextClicked { get; set; }
        Action OnPrevClicked { get; set; }
        Action<JournalOption> OnOptionSelected { get; set; }
        
        void SetPrevButtonEnabled(bool isEnabled);
        void SetNextButtonEnabled(bool isEnabled);
        void SetNextButtonText(string text);
    }
}
