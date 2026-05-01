using System;
using BattleSystem;
using BattleSystem.Interface;
using PipeLine;
using Cysharp.Threading.Tasks;
using Reactions;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class BaseCharacter : ITurnUseUnit
{
    /// <summary>
    /// 테스트 용 객체
    /// </summary>
    public static BaseCharacter Sample => new BaseCharacter(BaseCharacterData.Sample);
    
    public BaseCharacterData Data;
    public Stat Stat { get; } = new Stat();
    
    // 읽는 쪽에서 편하라고 일단 만들어두긴 했는데 너무 길어지면 지우는게 나을지도
    public string Name => Data.Name;
    public int Level => Data.Level;
    public Profession Profession => Data.Profession;
    public SkillBase[] Skills => Data.Skills;
    public Trait[] Traits => Data.Traits;
    public Reaction[] RoleReactions => Data.RoleReactions;
    public Reaction[] TraitReactions => Data.TraitReactions;
    public RoleType RoleType => Data.RoleType;
    public int Insanity => Data.Insanity;
    
    public bool isPlayable { get; set; }
    public AutoBattle battleType { get; set; }

    public BaseCharacter sup_Character { get; set; } // 지원 선택 대상. 대상이 행동할 때 역할군에 따른 지원. 탱커: 피격 시 엄호, 서포터: 피격 후 치유, 딜러: 공격 시 지원 공격.\
    
    // BattleManager가 구독할 이벤트. BattleContext는 공격자, 방어자, 스킬 정보 등을 담는 클래스. BattleManager는 이 이벤트를 구독하여 BattleContext를 받아 처리.
    public Action<BattleContext> onBattleAction;
    private IFormationManager m_formationManager;

    public BaseCharacter(BaseCharacterData data)
    {
        Data = data;
    }

    public void SetRole(RoleType role)
    {
        Data.RoleType = role;
        battleType = role switch
        {
            RoleType.Dealer => new AutoBattle_Atk(Name),
            RoleType.Tanker => new AutoBattle_Def(Name),
            RoleType.Supporter => new AutoBattle_Sup(Name)
        };
    }
    
    public void ReduceHP(int amount) => Stat.current_Hp = Mathf.Max(Stat.current_Hp - amount, 0); // 체력 감소
    public void ReduceInsanity(int amount) => Data.Insanity = Mathf.Min(Data.Insanity + amount, 100); // 스트레스 증가
    
    // 회복 관련 메서드 추가
    public void RecoverHp(int amount) => Stat.current_Hp = Mathf.Min(Stat.current_Hp + amount, Stat.max_Hp);
    public void RecoverInsanity(int amount) => Data.Insanity = Mathf.Max(Data.Insanity - amount, 0);

    #region Formaiton

    public CharacterSlot CurrentSlot { get; set; }
    private FormationMask CurrentFormation => CurrentSlot?.rank ?? FormationMask.None;

    private bool[] GetUsableSkills()
    {
        bool[] availability = new bool[Skills.Length];
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Data.Skills[i] == null) continue;
            bool rankMatch = (Data.Skills[i].Data.UseableSlots & (int)CurrentSlot.rank) != 0;

            bool targetMatch = m_formationManager.HasAnyValidTarget(this, Data.Skills[i].Data);
            
            availability[i] = rankMatch && targetMatch;
        }
        return availability;
    }

    #endregion
    
    #region ITurnUseUnit

    public int Speed => Stat.speed;
    public bool IsDead { get; private set; } = false;
    public string ImageAddress => Data.ImageAddress;
    public int RandomSpeed { get; set; }
    
    private AutoResetUniTaskCompletionSource<bool> _tcs;
    
    public async UniTask TakeTurnAsync()
    {
        Debug.Log($"<color=green>{Name} 차례! 역할군: {battleType} 플레이어의 명령을 기다립니다...</color>");
        if (isPlayable)
        {
            _tcs = AutoResetUniTaskCompletionSource<bool>.Create();
            // 플레이어 입력으로 스킬을 결정하고
            await _tcs.Task;
        }
        else
        {
            // 배틀액션에서 스킬을 직접 실행하는데 아마 직접 실행이 아닌 스킬을 선택해 반환하고 여기서 사용하는 방식으로 변경이 필요할거임.
            battleType.BattleAction(Skills);
        }
        
        BattleContext battleContext = new BattleContext();
        onBattleAction?.Invoke(battleContext);
    
        Debug.Log($"<color=lightblue>{Name} 행동 완료!</color>");
        
        _tcs = null;
    }
    
    private void OnDie(InputAction.CallbackContext context)
    {
        if (_tcs == null) return;
        IsDead = true;
        Debug.Log($"<color=red>[테스트] {Name} 강제 사망!</color>");
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
    #endregion
}
