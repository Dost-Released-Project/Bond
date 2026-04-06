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

    public BaseCharacter sup_Character { get; set; } // 지원 선택 대상. 대상이 행동할 때 역할군에 따른 지원. 탱커: 피격 시 엄호, 서포터: 피격 후 치유, 딜러: 공격 시 지원 공격.\
    private Stat stat;
    
    // BattleManager가 구독할 이벤트. BattleContext는 공격자, 방어자, 스킬 정보 등을 담는 클래스. BattleManager는 이 이벤트를 구독하여 BattleContext를 받아 처리.
    public Action<BattleContext> onBattleAction;
    
    private void Start()
    {
        stat = GetComponent<Stat>();
        Speed = stat.speed;
        _input = new Juno_TestInput();
        _input.Space.space.performed += OnActionButtonClicked;
        _input.Space.FkeyDie.performed += OnDie;
        _input.Enable();

        if (battleType == null)
        {
            switch (Random.Range(0,3))
            {
                case 1:
                    battleType = new AutoBattle_Atk();
                    break;
                case 2:
                    battleType = new AutoBattle_Def();
                    break;
                case 3:
                    battleType = new AutoBattle_Sup();
                    break;
            }
        }
    }

    private void Update()
    {
        // if (Keyboard.current.digit1Key.wasPressedThisFrame)
        // {
        //     battleType = new AutoBattle_Atk();
        //     Debug.Log("넌 딜러야.");
        // }
        // if (Keyboard.current.digit2Key.wasPressedThisFrame)
        // {
        //     battleType = new AutoBattle_Def();
        //     Debug.Log("넌 탱커야.");
        // }
        // if (Keyboard.current.digit3Key.wasPressedThisFrame)
        // {
        //     battleType = new AutoBattle_Sup();
        //     Debug.Log("넌 서포터야.");
        // }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            if (battleType == null)
            {
                Debug.Log("역할군이 비어있습니다.");
                return;
            }
            
            isPlayable = !isPlayable;
            battleType.isPlayable = isPlayable;
            
            if (isPlayable)
            {
                Debug.Log("얘는 내가 조종할게");
            }
            else
            {
                Debug.Log("걔는 네가 조종하렴");
            }
        }
        
        if (stat.current_Hp <= 0)
            IsDead = true;
    }
    
    /*
     스킬 사용 여부 - and계산(플레이어 위치, 사용 가능 칸, 적 진영 아군 진영)
    */

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
        Debug.Log($"<color=green>{unitName} 차례! 플레이어의 명령을 기다립니다...</color>");
    
        _tcs = AutoResetUniTaskCompletionSource<bool>.Create();
        
        // AutoBattle의 BattleAction이 BattleContext를 반환하게 해도될듯 -> 우선 void로 만들어둘테니 나중에 변환할 수 있으면 ㄱㄱ
        battleType.BattleAction(skills[Random.Range(0, skills.Length)]);
        BattleContext battleContext = new BattleContext();
        onBattleAction?.Invoke(battleContext);
    
        await _tcs.Task;
        _tcs = null;
    
        Debug.Log($"{unitName} 행동 완료!");
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
