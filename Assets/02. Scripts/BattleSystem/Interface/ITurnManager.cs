using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface ITurnManager
{
    // --- 데이터 관리 ---
    /// <summary>전투에 참여할 유닛을 등록합니다.</summary>
    void RegisterUnit(IEnumerable<ITurnUseUnit> unit);

    /// <summary>현재 턴 큐에 대기 중인 유닛 목록입니다.</summary>
    IReadOnlyList<ITurnUseUnit> TurnQueue { get; }

    // --- 흐름 제어 ---
    /// <summary>등록된 유닛들을 바탕으로 전투 루프를 시작합니다.</summary>
    UniTask StartBattleAsync(CancellationToken cancellation);

    /// <summary>전투 종료.</summary>
    void BattleEnd();

    // --- 이벤트 ---
    /// <summary>턴 큐가 갱신될 때마다 발생합니다. (UI 연동용)</summary>
    event Action OnTurnQueueUpdated;
}
