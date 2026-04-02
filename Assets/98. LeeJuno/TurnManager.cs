using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TurnManager
{
    // 전체 유닛
    private List<ITurnUseUnit> _units = new List<ITurnUseUnit>();
    
    // 현재 턴순서
    private List<ITurnUseUnit> _turnQueue = new List<ITurnUseUnit>(20);
    
    // 몇번째 턴인지 / UI는 턴바뀔때마다 이벤트 발송하면 될듯 / 스킬중 몇턴마다는 여기서 턴수 가져가서 연산하게 하면될듯
    private int _turnCount;

    private bool _isBattleActive;
    
    
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
            
            // 라운드 사이에 1초 정도 대기
            await UniTask.Delay(1000, cancellationToken: token);
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
    private void BattleEnd()
    {
        _isBattleActive = false;
        _units.Clear();
        Debug.Log("== 전투 종료 ==");
    }
}