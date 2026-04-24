using System;
using System.Collections.Generic;
using _03._PipeLine;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class BaseCharacter : MonoBehaviour, ITurnUseUnit
{
    public Equipment[] equip = new Equipment[2]; // 2개
    public Trait[] traits = new Trait[4]; // 4개
    public SkillBase[] skills = new SkillBase[4]; // 4개
    public Class classType { get; set; }
    public AutoBattle battleType { get; set; }
    public bool isPlayable { get; set; }
    
    private Stat stat;
    
    // BattleManager가 구독할 이벤트. BattleContext는 공격자, 방어자, 스킬 정보 등을 담는 클래스. BattleManager는 이 이벤트를 구독하여 BattleContext를 받아 처리.
    public Action<BattleContext> onBattleAction;

    public GameObject TestSkillManager;
    private ISkillManager _skillManager;

    private void Awake()
    {
        if (battleType == null) AssignDefaultBattleType(); // 역할군 랜덤 지정
    }

    private void Start()
    {
        _skillManager = TestSkillManager.GetComponent<ISkillManager>();
        
        stat = GetComponent<Stat>(); // 스탯 컴포넌트 가져오기
        stat.StatCalculate(unitName);
        Speed = stat.speed; // ITurnUseUnit에 포함된 Speed 변수를 스탯 스피트로 맞춰주기
        _input = new Juno_TestInput();
        _input.Space.space.performed += OnActionButtonClicked;
        _input.Space.FkeyDie.performed += OnDie;
        _input.Enable();
    }
    
    // 역할군 랜덤 지정 로직
    private void AssignDefaultBattleType()
    {
        int rand = Random.Range(0, 3);
        battleType = rand switch
        {
            0 => new AutoBattle_Atk(unitName),
            1 => new AutoBattle_Def(unitName),
            _ => new AutoBattle_Sup(unitName)
        };
        battleType.isPlayable = this.isPlayable;
    }
    
    /*
     스킬 사용 여부 - and계산(플레이어 위치, 사용 가능 칸, 적 진영 아군 진영) -> 다른 스크립트에서 처리
    */

    // 주노 테스트 플레이어 및 ITurnUseUnit 인터페이스 변수들
    public int Speed { get; private set; }
    
    [SerializeField] private string unitName;
    [SerializeField] private string imageAddress;
    private Juno_TestInput _input;
    
    public bool IsDead { get; private set; } = false;
    public string ImageAddress => imageAddress;
    public int RandomSpeed { get; set; }
    
    private AutoResetUniTaskCompletionSource<bool> _tcs;
    
    private void OnDestroy()
    {
        _input.Space.space.performed -= OnActionButtonClicked;
        _input.Space.FkeyDie.performed -= OnDie;
        _input.Disable();
    }
    
    public async UniTask TakeTurnAsync()
    {
        Debug.Log($"<color=green>{unitName} 차례! 역할군: {battleType} 플레이어의 명령을 기다립니다...</color>");
    
        _tcs = AutoResetUniTaskCompletionSource<bool>.Create();
        
        // [수정됨] 랜덤으로 하나를 뽑지 않고, 스킬 배열 전체를 넘겨서 역할군 클래스가 판단하게 합니다.
        battleType.BattleAction(skills);
        
        BattleContext battleContext = new BattleContext();
        onBattleAction?.Invoke(battleContext);
    
        Debug.Log($"<color=lightblue>{unitName} 행동 완료!</color>");
        
        await _tcs.Task;
        _tcs = null;
    }
    
    private void OnDie(InputAction.CallbackContext context)
    {
        if (_tcs == null) return;
        IsDead = true;
        Debug.Log($"<color=red>[테스트] {unitName} 강제 사망!</color>");
        _tcs?.TrySetResult(true);
    }
    
    private void OnActionButtonClicked(InputAction.CallbackContext context)
    {
        if (_tcs == null) return;
        _tcs?.TrySetResult(true);
    }
    
    public int CompareTo(ITurnUseUnit other)
    {
        if (other == null) return 1;
        // 1. 먼저 스피드를 비교
        int speedComparison = other.Speed.CompareTo(this.Speed);
    
        // 2. 만약 스피드가 완전히 똑같다면 
        if (speedComparison == 0)
        {
            // 3. 매니저가 나누어준 랜덤 번호표로 순서를 결정
            return other.RandomSpeed.CompareTo(this.RandomSpeed);
        }
    
        // 스피드가 다르면 그냥 스피드 비교 결과를 반환
        return speedComparison;
    }
}
