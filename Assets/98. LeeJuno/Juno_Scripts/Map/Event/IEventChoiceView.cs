using System;
using System.Collections.Generic;
using Bond.WT.Journal;

/// <summary>
/// 이벤트 선택지 UI의 시각적 표현을 추상화하는 인터페이스.
/// UI 프레임워크(UGUI, UI Toolkit 등)에 독립적으로 로직을 구성하기 위해 사용한다.
/// JournalSystem 의 IJournalVisualizer 패턴을 참고해 설계했다.
/// </summary>
public interface IEventChoiceView
{
    /// <summary>이벤트 설명 텍스트를 표시한다.</summary>
    void ShowDescription(string description);

    /// <summary>선택지 버튼들을 생성 및 표시한다.</summary>
    void ShowChoices(IReadOnlyList<EventChoice> choices);

    /// <summary>모든 선택지 버튼의 인터랙션 가능 여부를 설정한다.</summary>
    void SetInteractable(bool interactable);

    /// <summary>선택지 버튼을 모두 제거한다.</summary>
    void ClearChoices();

    /// <summary>플레이어가 선택지를 클릭했을 때 발생하는 이벤트 핸들러.</summary>
    Action<EventChoice> OnChoiceSelected { get; set; }

    /// <summary>
    /// 2차 선택지 화면으로 전환한다.
    /// DescriptionLabel 에 paragraphs 를 표시하고 ChoiceContainer 에 JournalOption 버튼을 생성한다.
    /// </summary>
    /// <param name="paragraphs">표시할 문단 목록. 개행으로 연결해 DescriptionLabel 에 표시한다.</param>
    /// <param name="options">2차 선택지 목록.</param>
    void ShowSecondaryPhase(IReadOnlyList<string> paragraphs, IReadOnlyList<JournalOption> options);

    /// <summary>2차 선택지 클릭 이벤트 핸들러.</summary>
    Action<JournalOption> OnSecondaryOptionSelected { get; set; }

    /// <summary>
    /// 효과 적용 후 결과 화면을 표시하고 확인 클릭 시 onConfirm 을 호출한다.
    /// outcomeText 가 null 이거나 비어있는 경우 호출하지 않는다.
    /// </summary>
    /// <param name="outcomeText">선택 결과 설명 텍스트.</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 호출할 콜백.</param>
    void ShowOutcome(string outcomeText, Action onConfirm);

    /// <summary>
    /// HpChange ChooseOne 효과 처리 시 파티원 선택 버튼을 표시한다.
    /// 플레이어가 버튼을 클릭하면 onSelected 에 해당 파티 인덱스를 전달한다.
    /// </summary>
    /// <param name="characterNames">선택 가능한 파티원 이름 목록.</param>
    /// <param name="onSelected">선택된 캐릭터의 파티 인덱스를 인자로 받는 콜백.</param>
    void ShowCharacterSelection(IReadOnlyList<string> characterNames, Action<int> onSelected);
}
