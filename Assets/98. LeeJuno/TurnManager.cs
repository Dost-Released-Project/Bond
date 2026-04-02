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

            CheckBattleEndCondition();
        }
    }

    private async UniTask PlayRoundAsync(CancellationToken token)
    {
        _turnCount++;
        Debug.Log($"<b>--- {_turnCount} 라운드 시작 ---</b>");

        PrepareTurnQueue(); 

        for (int i = 0; i < _turnQueue.Count; i++)
        {
            var unit = _turnQueue[i];

            if (_isBattleActive == false || unit.IsDead) continue;

            // 유닛이 행동하는 동안에도 취소 토큰을 확인할 수 있도록 처리하는 것이 좋아
            await unit.TakeTurnAsync();
        }
    }

    // 턴 큐 준비 함수
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

    // 무한 루프 방지를 위한 승패 체크 로직 분리
    private void CheckBattleEndCondition()
    {
        //  살아있는 유닛이 없으면 전투 종료
        bool allDead = true;
        for (int i = 0; i < _units.Count; i++)
        {
            if (_units[i].IsDead == false)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            _isBattleActive = false;
            Debug.Log("== 전투 종료 (모든 유닛 사망) ==");
        }
    }
}