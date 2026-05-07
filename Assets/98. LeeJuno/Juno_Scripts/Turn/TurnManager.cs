using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BattleSystem.Interface;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public class TurnManager : ITurnManager, IStartable, IDisposable
{
    // 외부용
    public event Action OnTurnQueueUpdated;
    public int TurnCount => _turnCount;
    public IReadOnlyList<ITurnUseUnit> TurnQueue => _turnQueue;
    
    
    private readonly IBattleFlowManager _battleFlowManager;
    private CancellationTokenSource m_cts;
    
    public TurnManager(IBattleFlowManager expeditionFlowManager) {
        _battleFlowManager = expeditionFlowManager;
    }
    
    void IStartable.Start()
    {
        _battleFlowManager.OnBattle += StartBattle;
    }

    void IDisposable.Dispose()
    {
        _battleFlowManager.OnBattle -= StartBattle;
    }

    // 전체 유닛
    private List<ITurnUseUnit> _units = new List<ITurnUseUnit>();

    // 현재 턴순서
    private List<ITurnUseUnit> _turnQueue = new List<ITurnUseUnit>(20);

    // 몇번째 턴인지 / UI는 턴바뀔때마다 이벤트 발송하면 될듯 / 스킬중 몇턴마다는 여기서 턴수 가져가서 연산하게 하면될듯
    private int _turnCount;
    private bool _isBattleActive;

    // 이부분이 인자로 배열이나 리스트로 유닛들 추가
    public void RegisterUnit(IEnumerable<ITurnUseUnit> unit)
    {
        _units.AddRange(unit);
    }

    public void StartBattle(BaseCharacter[] characters, BaseCharacter[] targets)
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
            return;
        }
        
        m_cts = new CancellationTokenSource();
        if (characters == null || targets == null)
        {
            Debug.LogError("유닛 데이터가 설정되지 않았습니다! SetPlayerUnits를 먼저 호출하세요.");
            return;
        }
        _units.Clear();
        _units.AddRange(characters.Where(c => c != null));
        _units.AddRange(targets.Where(c => c != null));
        Debug.Log($"TurnManager received battle start event with {characters.Length} characters and {targets.Length} targets.");
        StartBattleAsync(m_cts.Token).Forget();
    }

    public async UniTask StartBattleAsync(CancellationToken token = default)
    {
        if (_isBattleActive)
        {
            Debug.LogWarning("[TurnManager] 이미 배틀이 진행 중입니다.");
            return;
        }

        _turnCount = 0;
        _isBattleActive = true;

        while (_isBattleActive)
        {
            // 외부에서 취소 요청이 오면 루프 탈출
            if (token.IsCancellationRequested) break;

            await PlayRoundAsync(token);
            await UniTask.Delay(1000, cancellationToken: token);
        }
    }

    private async UniTask PlayRoundAsync(CancellationToken token)
    {
        _turnCount++;
        Debug.Log($"<b>--- {_turnCount} 라운드 시작 ---</b>");

        PrepareTurnQueue();
        OnTurnQueueUpdated?.Invoke();

        while (_turnQueue.Count > 0)
        {
            ITurnUseUnit unit = _turnQueue[0];

            if (_isBattleActive && unit.IsDead == false)
            {
                if (token.IsCancellationRequested) break;

                await UniTask.Delay(150, cancellationToken: token);
                await unit.TakeTurnAsync();
            }
            if (_turnQueue.Count > 0 && _turnQueue[0] == unit)
            {
                _turnQueue.RemoveAt(0);
            }
            RemoveDeadUnitsFromQueue();
            OnTurnQueueUpdated?.Invoke();
        }

    }

    private void RemoveDeadUnitsFromQueue()
    {
        for (int i = _turnQueue.Count - 1; i >= 0; i--)
        {
            if (_turnQueue[i].IsDead)
            {
                _turnQueue.RemoveAt(i);
            }
        }
    }

    private void PrepareTurnQueue()
    {
        _turnQueue.Clear();

        // 살아있는 유닛만 큐에
        for (int i = 0; i < _units.Count; i++)
        {
            ITurnUseUnit unit = _units[i];
            if (unit.IsDead == false)
            {
                // 속도 같을때 섞기용
                unit.RandomSpeed = UnityEngine.Random.Range(0, 10000);
                _turnQueue.Add(unit);
            }
        }

        // 내림차순 정렬
        _turnQueue.Sort();
    }

    // 해당 로직을 외부에서 승패 판정 후 호출
    public void BattleEnd()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
        }
        _isBattleActive = false;
        _units.Clear();
        Debug.Log("== 전투 종료 ==");
    }
}
