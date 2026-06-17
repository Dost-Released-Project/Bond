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
    private readonly CharacterSelector _selector;
    private CancellationTokenSource m_cts;
    
    public TurnManager(IBattleFlowManager expeditionFlowManager, CharacterSelector selector) {
        _battleFlowManager = expeditionFlowManager;
        _selector = selector;
    }
    
    void IStartable.Start()
    {
        _battleFlowManager.OnBattle += SwitchBattle;
    }

    void IDisposable.Dispose()
    {
        _battleFlowManager.OnBattle -= SwitchBattle;
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

    public void SwitchBattle(BaseCharacter[] characters, BaseCharacter[] targets)
    {
        if (m_cts != null)
        {
            BattleEnd();
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
        
        //2라운드 강제 종료 테스트
        // if (_turnCount >= 2)
        // {
        //     _battleFlowManager.BattleSwitch();  
        // }

        PrepareTurnQueue();
        OnTurnQueueUpdated?.Invoke();

        while (_turnQueue.Count > 0)
        {
            ITurnUseUnit unit = _turnQueue[0];

            if (_isBattleActive && unit.IsDead == false)
            {
                if (token.IsCancellationRequested) break;

                await UniTask.Delay(150, cancellationToken: token);
                if (unit is BaseCharacter chara && chara.isPlayable)
                {
                    _selector.Select(chara);
                }
                else
                {
                    _selector.Deselect(); // 플레이어가 아닌 유닛 턴일 때 기존 선택 해제
                }
                
                // 자기 턴 시작: 버프·봉인 지속(자기 턴 수)을 1 감소시키고 만료분을 제거.
                // 이어서 강제 휴식(턴 스킵) → 돌발 행동(성향) 순으로 판정. 둘 중 하나라도 발동하면 계획 행동을 생략.
                bool skipNormalTurn = false;
                if (unit is BaseCharacter ownerChar)
                {
                    ownerChar.TickBuffs();
                    ownerChar.TickSeals();
                    ownerChar.TickDistrust(); // 불협조(특정 아군 비협조) 지속 감소 (봉인과 동일 타이밍)
                    ownerChar.ClearRecentAnomaly(); // 자기 턴 도달 → '최근 돌발' 플래그 리셋 (아군 돌발 관찰 창)

                    if (ownerChar.ConsumeSkipTurn())
                    {
                        Debug.Log($"<color=gray>[강제 휴식]</color> {ownerChar.Name} 이(가) 이번 턴 행동하지 않습니다.");
                        skipNormalTurn = true;
                    }
                    else
                    {
                        skipNormalTurn = await ownerChar.TryRunSelfTurnAnomalyAsync();
                    }

                    ownerChar.ResetReactionCount(); // 자기 턴 도달 → '연속' 리액션 카운트 리셋 (돌발 판정 이후)
                }

                if (!skipNormalTurn) await unit.TakeTurnAsync();
                
                // 턴이 완전히 끝나면 선택 상태를 초기화하여 다음 턴(혹은 다음 라운드의 동일 캐릭터 턴)에 이벤트가 정상 발생하도록 함
                _selector.Deselect();
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
        m_cts.Cancel();
        m_cts.Dispose();
        m_cts = null;
        _isBattleActive = false;
        _units.Clear();
        Debug.Log("== 전투 종료 ==");
    }
}
