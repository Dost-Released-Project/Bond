using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using BattleSystem;
using BattleSystem.Interface;
using Bond.Persistence;
using PipeLine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Reactions;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RoleType
{
    None,
    Tanker,   // 활성 트리거: TRG_DEF_TG_IN (피격 시), TRG_SIT_ALLY_CRISIS (아군 위기 시)
    Dealer,   // 활성 트리거: TRG_OFF_KILL (적 처치 시), TRG_OFF_CRIT (치명타 시)
    Supporter // 활성 트리거: TRG_SIT_ALLY_TURN_END (아군 턴 종료 시), TRG_DEF_STATUS (상태이상 시)
}

[Serializable]
public partial class BaseCharacter : ITurnUseUnit
{
    /// <summary>
    /// 테스트 용 객체
    /// </summary>
    public static BaseCharacter Sample => new BaseCharacter();

    public string Id;
    public string ImageAddress;
    public string Name;
    
    public Profession Profession;
    public int Level = 0;
    public int Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
    public RoleType RoleType = RoleType.None;
    [field: SerializeReference, SubclassSelector] public AutoBattle battleType { get; set; }

    [SerializeReference] public SkillBase[] Skills = new SkillBase[4];
    public Trait[] Traits = new Trait[4];
    public Equipment Weapon;
    public Equipment Armor;
    public AccessoryItem[] Accessories = new AccessoryItem[2];

    public Dictionary<BaseCharacter, int> Relation = new Dictionary<BaseCharacter, int>();

    public Reaction[] RoleReactions = new Reaction[2];
    public Reaction[] TraitReactions = new Reaction[4];
    [JsonIgnore] public Reaction[] Reactions => RoleReactions.Concat(TraitReactions).ToArray();
    
    [JsonIgnore] public Stat Stat { get; } = new Stat();
    [JsonIgnore] public StatController StatController { get; } = new StatController();
    
    [JsonIgnore] public bool isPlayable { get; set; }

    public BaseCharacter sup_Character { get; set; } // 지원 선택 대상. 대상이 행동할 때 역할군에 따른 지원. 탱커: 피격 시 엄호, 서포터: 피격 후 치유, 딜러: 공격 시 지원 공격.\
    
    // BattleManager가 구독할 이벤트. BattleContext는 공격자, 방어자, 스킬 정보 등을 담는 클래스. BattleManager는 이 이벤트를 구독하여 BattleContext를 받아 처리.
    [JsonIgnore] public Func<BattleContext, UniTask> onBattleAction;
    private IFormationManager m_formationManager;

    public void SetFormationManager(IFormationManager formationManager)
    {
        m_formationManager = formationManager;
    }

    private BaseCharacter() { }

    public void SetRole(RoleType role)
    {
        RoleType = role;
        battleType = role switch
        {
            RoleType.Dealer => new AutoBattle_Atk(Name),
            RoleType.Tanker => new AutoBattle_Def(Name),
            RoleType.Supporter => new AutoBattle_Sup(Name),
            _ => new AutoBattle_Atk(Name)
        };
    }

    public void CalcStat()
    {
        // Profession에게 "데이터와 모디파이어를 전달한 뒤 스탯 계산 요청
        Profession.CalculateStat(this, StatController);
    }

    public float HpRatio => Stat.current_Hp / Stat.max_Hp;
    
    public void SetHpFull() => Stat.current_Hp = Stat.max_Hp;
    public void ReduceHP(int amount) => Stat.current_Hp = Mathf.Max(Stat.current_Hp - amount, 0); // 체력 감소
    public void ReduceInsanity(int amount) => Insanity = Mathf.Min(Insanity + amount, 100); // 스트레스 증가
    
    // 회복 관련 메서드 추가
    public void RecoverHp(int amount) => Stat.current_Hp = Mathf.Min(Stat.current_Hp + amount, Stat.max_Hp);
    public void RecoverInsanity(int amount) => Insanity = Mathf.Max(Insanity - amount, 0);

    #region Formaiton

    [JsonIgnore] public CharacterSlot CurrentSlot { get; set; }
    private FormationMask CurrentFormation => CurrentSlot?.rank ?? FormationMask.None;

    private bool[] GetUsableSkills()
    {
        bool[] availability = new bool[Skills.Length];
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Skills[i] == null) continue;
            bool rankMatch = (Skills[i].Data.UseableSlots & (int)CurrentSlot.rank) != 0;

            bool targetMatch = m_formationManager.HasAnyValidTarget(this, Skills[i].Data);
            
            availability[i] = rankMatch && targetMatch;
        }
        return availability;
    }

    #endregion
    
    #region ITurnUseUnit

    [JsonIgnore] public int Speed => Stat.speed;
    [JsonIgnore] public bool IsDead { get; private set; } = false;
    string ITurnUseUnit.ImageAddress => ImageAddress;
    public int RandomSpeed { get; set; }
    
    private AutoResetUniTaskCompletionSource<bool> _tcs;
    private SkillBase _selectedSkill;

    public event Action<BaseCharacter> onPlayerTurnStarted;

    public void ConfirmSkillSelection(SkillBase skill)
    {
        _selectedSkill = skill;
        _tcs?.TrySetResult(true);
    }

    public async UniTask TakeTurnAsync()
    {
        SkillBase skill = null;
        Debug.Log($"<color=green>{Name} 차례</color>");

        if (isPlayable)
        {
            _selectedSkill = null;
            _tcs = AutoResetUniTaskCompletionSource<bool>.Create();
            onPlayerTurnStarted?.Invoke(this);
            await _tcs.Task;
            skill = _selectedSkill;
        }
        else
        {
            // GetUsableSkills()를 통해 현재 사용할 수 있는 스킬 판별
            bool[] usableFlags = GetUsableSkills();
            var usableSkills = new System.Collections.Generic.List<SkillBase>();
            
            for (int i = 0; i < Skills.Length; i++)
            {
                Debug.Assert(Skills[i] != null, $"{Name}'s Skills[{i}] is null");
                if (Skills[i] != null && usableFlags[i])
                {
                    usableSkills.Add(Skills[i]);
                }
            }

            // 사용 가능한 스킬이 하나라도 있으면 AI에게 넘겨 판단하게 함
            if (usableSkills.Count > 0)
            {
                skill = battleType.BattleAction(usableSkills.ToArray());
            }
            else
            {
                // TODO: 스킬을 사용할 수 없는 경우 (턴 패스 또는 대기) 예외 처리
                Debug.LogWarning($"<color=yellow>{Name}은(는) 현재 타겟이 없어 스킬을 사용할 수 없습니다.</color>");
            }
        }
        
        // 사용 가능한 스킬이 없어 skill이 null이면 턴 액션을 발생시키지 않음
        if (skill != null)
        {
            BattleContext battleContext = CreateBattleContext(skill);
            if (onBattleAction != null)
            {
                await onBattleAction.Invoke(battleContext);
            }
        }
    
        await UniTask.Delay(1000); // 턴 종료 딜레이

        Debug.Log($"<color=lightblue>{Name} 행동 완료!</color>");
        
        _tcs = null;
    }
    
    public async UniTask ExecuteReaction(Reaction reaction, BattleContext context)
    {
        SkillBase skill = Skills[reaction.SkillIndex];
        
        BattleContext battleContext = CreateBattleContext(skill);
        battleContext.isReaction = true;
        var target = reaction.ReactionSkillTarget == E_TargetFilter.Caster ? context.caster : context.target;
        battleContext.target = target;
        
        if (onBattleAction != null)
        {
            Debug.Log($"<color=lightblue>{Name} 리액션 시작!</color>");
            await onBattleAction.Invoke(battleContext);
            Debug.Log($"<color=lightblue>{Name} 리액션 완료!</color>");
        }
    
        await UniTask.Delay(1000); // 턴 종료 딜레이
    }

    private BattleContext CreateBattleContext(SkillBase skill)
    {
        BattleContext battleContext = new BattleContext(
            this, 
            skill, 
            true);
        // 크리티컬 판단 로직은 따로 추가할 예정)
        return battleContext;
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

    public override string ToString()
    {
        return $"Name: {Name}, Level: {Level}, Profession: {Profession.Name}";
    }

    public static implicit operator string(BaseCharacter character)
    {
        return character.Id;
    }
}
