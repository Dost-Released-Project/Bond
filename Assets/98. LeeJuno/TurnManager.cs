using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TurnManager
{
    private List<ITurnUseUnit> _units = new List<ITurnUseUnit>();
    private List<ITurnUseUnit> _turnQueue = new List<ITurnUseUnit>(20);
    private bool _isBattleActive;
    private int _turnCount;

    //이부분이 인자로 배열이나 리스트로 유닛들 추가
    public void RegisterUnit(IEnumerable<ITurnUseUnit> unit)
    {
        _units.AddRange(unit);
    }

    public async UniTask StartBattleAsync(CancellationToken token = default)
    {
        _turnCount = 0;
        _isBattleActive = true;

        while (_isBattleActive)
        {
            // 외부에서 취소 요청이 오면 루프 탈출
            if (token.IsCancellationRequested) break;

            await PlayRoundAsync(token);
        }
    }

    private async UniTask PlayRoundAsync(CancellationToken token)
    {
        _turnCount++;
        Debug.Log($"<b>--- {_turnCount} 라운드 시작 ---</b>");

        // 라운드마다 살아있는 유닛만 큐에 추가
        PrepareTurnQueue();

        for (int i = 0; i < _turnQueue.Count; i++)
        {
            ITurnUseUnit unit = _turnQueue[i];

            if (_isBattleActive == false || unit.IsDead) continue;

            // 유닛이 행동하는 동안에도 취소 토큰을 확인할 수 있도록 처리
            await unit.TakeTurnAsync();
        }
    }

    private void PrepareTurnQueue()
    {
        _turnQueue.Clear();

        // 살아있는 유닛만 큐에
        for (int i = 0; i < _units.Count; i++)
        {
            var unit = _units[i];
            if (unit.IsDead == false)
            {
                _turnQueue.Add(unit);
            }
        }

        // 내림차순 정렬
        _turnQueue.Sort();
    }

    // 해당 로직을 외부에서 승패 판정 후 호출 
    private void CheckBattleEndCondition()
    {
        _isBattleActive = false;
        _units.Clear();
        Debug.Log("== 전투 종료 ==");
    }
}