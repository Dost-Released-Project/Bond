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
    public string IdleImageAddress;
    public string AttackImageAddress;
    public string Name;
    
    public Profession Profession;
    public int Level = 0;
    public int Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
    public RoleType RoleType = RoleType.None;
    [field: SerializeReference, SubclassSelector] public AutoBattle battleType { get; set; }

    [SerializeReference] public SkillBase[] Skills = new SkillBase[4];
    public string[] TraitIds = new string[4];
    public Equipment Weapon;
    public Equipment Armor;
    public AccessoryItem[] Accessories = new AccessoryItem[2];

    public Dictionary<BaseCharacter, int> Relation = new Dictionary<BaseCharacter, int>();

    public Reaction[] RoleReactions = new Reaction[2];
    public Reaction[] TraitReactions = new Reaction[4];
    [JsonIgnore] public Reaction[] Reactions => RoleReactions.Concat(TraitReactions).ToArray();

    public void Init()
    {
        // 로드 시엔 OnEquip 이 호출되지 않으므로, 장착 중인 장신구 모디파이어를 CalcStat 전에 재적용한다.
        ReapplyAccessoryModifiers();

        CalcStat();
        // 저장된 HP 가 있으면 복원(max 로 클램프), 없으면(신규 캐릭터) 풀피.
        if (_loadedHp.HasValue)
            Stat.current_Hp = Mathf.Clamp(_loadedHp.Value, 0, Stat.max_Hp);
        else
            SetHpFull();
        SyncTraitReactions();
        Dict[Id] = this;
        void ReapplyAccessoryModifiers()
        {
            if (Accessories == null) return;
            foreach (var acc in Accessories)
                acc?.OnEquip(this);
        }
    }
    
    /// <summary>TraitIds[i] 를 카탈로그에서 해석한 TraitSO. 미설정/미로드면 null.</summary>
    public TraitSO GetTrait(int index)
    {
        if (TraitIds == null || index < 0 || index >= TraitIds.Length) return null;
        var id = TraitIds[index];
        return string.IsNullOrEmpty(id) ? null : DBSORegistry.GetSO<TraitSO>(id);
    }

    /// <summary>
    /// 보유 성향에 맞춰 TraitReactions 동기화. 성향↔리액션 1:1.
    /// 같은 정의면 기존 런타임 리액션(편집값 포함) 보존, 달라졌을 때만 새 인스턴스화.
    /// Id는 있으나 미해석(미로드)인 경우 보존 — 로드 타이밍 유실 방지.
    /// </summary>
    public void SyncTraitReactions()
    {
        if (TraitIds == null) return;
        for (int i = 0; i < TraitReactions.Length && i < TraitIds.Length; i++)
        {
            if (string.IsNullOrEmpty(TraitIds[i])) { TraitReactions[i] = null; continue; }
            var traitSO = GetTrait(i);
            if (traitSO == null) continue;                 // 미로드 — 보존
            var def = traitSO.ReactionDefinition;
            if (def == null) { TraitReactions[i] = null; continue; }
            if (TraitReactions[i] == null || TraitReactions[i].DefinitionId != def.Id)
                TraitReactions[i] = def.CreateRuntimeReaction();
        }
    }

    [JsonIgnore] public Sprite Portrait { get; set; }
    [JsonIgnore] public Texture IdlePortrait { get; set; }
    [JsonIgnore] public Texture AttackPortrait { get; set; }
    
    [JsonIgnore] public string EffectiveIdleImageAddress => 
        !string.IsNullOrEmpty(IdleImageAddress) ? IdleImageAddress : Profession?.IdleImageId;
    
    [JsonIgnore] public string EffectiveAttackImageAddress => 
        !string.IsNullOrEmpty(AttackImageAddress) ? AttackImageAddress : Profession?.BattleImageId;
    
    [JsonIgnore] public Stat Stat { get; } = new Stat();
    [JsonIgnore] public StatController StatController { get; } = new StatController();

    // current_Hp 직렬화 미러. Stat 은 [JsonIgnore] 라 HP 가 안 실리므로,
    // 저장 시엔 런타임 실제값을 굽고(get), 로드 시엔 값을 보관(set)했다가 Init 에서 복원한다.
    // max_Hp 는 CalcStat 으로 재계산되므로 저장하지 않는다. 신규 캐릭터(미저장)는 _loadedHp 가 null.
    [JsonProperty("CurrentHp")]
    private int CurrentHpSerialized
    {
        get => Stat.current_Hp;
        set => _loadedHp = value;
    }
    [JsonIgnore] private int? _loadedHp;
    
    [JsonIgnore] public bool isPlayable { get; set; }

    public BaseCharacter sup_Character { get; set; } // 지원 선택 대상. 대상이 행동할 때 역할군에 따른 지원. 탱커: 피격 시 엄호, 서포터: 피격 후 치유, 딜러: 공격 시 지원 공격.\
    
    // BattleManager가 구독할 이벤트. BattleContext는 공격자, 방어자, 스킬 정보 등을 담는 클래스. BattleManager는 이 이벤트를 구독하여 BattleContext를 받아 처리.
    [JsonIgnore] public Func<BattleContext, UniTask> onBattleAction;
    [JsonIgnore] public Action<BaseCharacter> OnDead; // 사망 시 발송될 이벤트

    // UI 갱신용 상태 변경 이벤트. 데이터 변경 메서드에서 발사된다.
    public event Action<BaseCharacter> OnHpChanged;
    public event Action<BaseCharacter> OnInsanityChanged;
    public event Action<BaseCharacter> OnStatRecalculated;
    public event Action<BaseCharacter> OnRoleChanged;
    public event Action<BaseCharacter> OnAccessoriesChanged;
    public event Action<BaseCharacter> OnEquipmentChanged;   // 무기/방어구 변경 (영속 트리거)
    public event Action<BaseCharacter> OnReactionsChanged;    // 역할/성향 리액션 편집 (영속 트리거)

    public void SetAccessory(int index, AccessoryItem item)
    {
        if (index < 0 || index >= Accessories.Length) return;
        Accessories[index]?.OnUnequip(this);
        Accessories[index] = item;
        Accessories[index]?.OnEquip(this);
        CalcStat();                          // max 변경 시 비율 보존 + OnHpChanged 까지 내부 처리
        OnAccessoriesChanged?.Invoke(this);
    }

    public void SetWeapon(Equipment weapon)
    {
        Weapon = weapon;
        CalcStat();
        OnEquipmentChanged?.Invoke(this);
    }

    public void SetArmor(Equipment armor)
    {
        Armor = armor;
        CalcStat();
        OnEquipmentChanged?.Invoke(this);
    }

    /// <summary>
    /// max_Hp 가 바뀌었을 때 현재 체력 "비율"을 보존하도록 current_Hp 를 재계산한다(+오버플로 클램프).
    /// CalcStat 내부에서 호출되므로 max 를 바꾸는 모든 경로(장비·향후 레벨업/버프 등)가 자동 적용된다.
    /// 로드(Init)는 fresh Stat(max=0)이라 여기선 0 으로 클램프될 뿐이고, 직후 저장된 절대 HP 로 덮어쓴다.
    /// 반환: current_Hp 가 실제로 바뀌었으면 true (CalcStat 이 OnHpChanged 발화 여부 판단).
    /// </summary>
    private bool RescaleCurrentHp(int oldMax)
    {
        if (Stat.max_Hp == oldMax) return false;            // 변화 없음
        int before = Stat.current_Hp;
        if (oldMax <= 0)                                    // 비율 산정 불가 → 클램프만
            Stat.current_Hp = Mathf.Min(Stat.current_Hp, Stat.max_Hp);
        else
        {
            float ratio = (float)Stat.current_Hp / oldMax;
            Stat.current_Hp = Mathf.Clamp(Mathf.RoundToInt(ratio * Stat.max_Hp), 0, Stat.max_Hp);
        }
        return Stat.current_Hp != before;
    }

    /// <summary>역할 슬롯(0~1)에 런타임 리액션 할당</summary>
    public void SetRoleReaction(int index, Reaction reaction)
    {
        if (index < 0 || index >= RoleReactions.Length) return;
        RoleReactions[index] = reaction;
        OnReactionsChanged?.Invoke(this);
    }

    /// <summary>역할 슬롯 비우기</summary>
    public void ClearRoleReaction(int index)
    {
        if (index < 0 || index >= RoleReactions.Length) return;
        RoleReactions[index] = null;
        OnReactionsChanged?.Invoke(this);
    }

    /// <summary>리액션 내부 편집슬롯(관찰대상/행동스킬)을 제자리 수정한 뒤 호출 — 영속 트리거.</summary>
    public void RaiseReactionsChanged() => OnReactionsChanged?.Invoke(this);

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
        OnRoleChanged?.Invoke(this);
    }

    public void CalcStat()
    {
        int oldMax = Stat.max_Hp;
        // Profession에게 데이터와 모디파이어를 전달한 뒤 스탯 계산 요청
        Profession.CalculateStat(this, StatController);
        bool hpChanged = RescaleCurrentHp(oldMax);   // max 가 바뀌면 현재 체력 비율 보존(+오버플로 클램프)
        OnStatRecalculated?.Invoke(this);
        if (hpChanged) OnHpChanged?.Invoke(this);
    }

    public float HpRatio => Stat.max_Hp <= 0 ? 0f : (float)Stat.current_Hp / Stat.max_Hp;

    public void SetHpFull()
    {
        Stat.current_Hp = Stat.max_Hp;
        OnHpChanged?.Invoke(this);
    }

    public void ReduceHP(int amount)
    {
        if (IsDead) return;

        Stat.current_Hp = Mathf.Max(Stat.current_Hp - amount, 0);
        Debug.Log($"<color=orange>[HP 차감] {Name}이(가) {amount}의 피해를 입었습니다. (잔여 HP: {Stat.current_Hp}/{Stat.max_Hp})</color>");
        OnHpChanged?.Invoke(this);

        if (Stat.current_Hp <= 0)
        {
            IsDead = true;
            OnDead?.Invoke(this);
            Debug.Log($"<color=red>[사망] {Name}이(가) 쓰러졌습니다.</color>");
        }
    }

    public void ReduceInsanity(int amount)
    {
        Insanity = Mathf.Min(Insanity + amount, 100); // 스트레스 증가
        OnInsanityChanged?.Invoke(this);
    }

    // 회복 관련 메서드 추가
    public void RecoverHp(int amount)
    {
        Stat.current_Hp = Mathf.Min(Stat.current_Hp + amount, Stat.max_Hp);
        Debug.Log($"<color=lime>[HP 회복] {Name}이(가) {amount}의 체력을 회복했습니다. (현재 HP: {Stat.current_Hp}/{Stat.max_Hp})</color>");
        OnHpChanged?.Invoke(this);
    }
    public void RecoverInsanity(int amount)
    {
        Insanity = Mathf.Max(Insanity - amount, 0);
        OnInsanityChanged?.Invoke(this);
    }

    #region Formaiton

    [JsonIgnore] public CharacterSlot CurrentSlot { get; set; }
    private FormationMask CurrentFormation => CurrentSlot?.rank ?? FormationMask.None;

    private bool[] GetUsableSkills()
    {
        bool[] availability = new bool[Skills.Length];
        for (int i = 0; i < Skills.Length; i++)
        {
            if (Skills[i] == null) continue;
            
            // 시전자 진영과 관계없이 상대적 위치(Rank)를 기반으로 직접 비교
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
    private AutoResetUniTaskCompletionSource<bool> _targetTcs;
    private SkillBase _selectedSkill;
    private BaseCharacter _selectedTarget;

    public event Action<BaseCharacter> onPlayerTurnStarted;
    public event Action<BaseCharacter, SkillBase> onTargetSelectionStarted;

    public void ConfirmSkillSelection(SkillBase skill)
    {
        _selectedSkill = skill;

        // 취소(null) 시 프리젠터에게 알려 타겟팅 모드 해제 유도
        if (skill == null)
        {
            onTargetSelectionStarted?.Invoke(this, null);
        }

        // 스킬 선택 대기 중이면 해제
        if (_tcs != null) _tcs.TrySetResult(true);
        
        // 타겟 선택 대기 중이면 '실패'로 해제하여 루프를 처음으로 되돌림
        if (_targetTcs != null) _targetTcs.TrySetResult(false);
    }

    public void ConfirmTargetSelection(CharacterSlot slot)
    {
        _selectedTarget = slot?.Occupant;
        if (_targetTcs != null) _targetTcs.TrySetResult(true);
    }

    public async UniTask TakeTurnAsync()
    {
        SkillBase skill = null;
        Debug.Log($"<color=green>{Name} 차례</color>");

        try
        {
            if (isPlayable)
            {
                _selectedSkill = null;
                _selectedTarget = null;

                while (true)
                {
                    // 1. 스킬 선택 대기 (이미 스킬이 선택되어 있지 않은 경우에만)
                    if (_selectedSkill == null)
                    {
                        _tcs = AutoResetUniTaskCompletionSource<bool>.Create();
                        onPlayerTurnStarted?.Invoke(this);
                        await _tcs.Task;
                        _tcs = null;
                    }

                    skill = _selectedSkill;
                    
                    // [취소 대응] 스킬이 null이면(취소됨) 다시 루프 처음(스킬 대기)으로 돌아감
                    if (skill == null) continue;

                    // 2. 타겟 선택 대기
                    _selectedTarget = null;
                    _targetTcs = AutoResetUniTaskCompletionSource<bool>.Create();
                    onTargetSelectionStarted?.Invoke(this, skill);
                    
                    // _targetTcs.Task는 ConfirmTargetSelection 시 true, ConfirmSkillSelection 시 false를 반환
                    bool targetConfirmed = await _targetTcs.Task;
                    _targetTcs = null;

                    // 타겟이 정상적으로 선택되었다면 루프 탈출
                    if (targetConfirmed && _selectedTarget != null)
                    {
                        break;
                    }
                    
                    // targetConfirmed가 false이거나 타겟이 없다면 (새 스킬 클릭 또는 취소), 다시 루프 처음으로 이동
                }
            }
            else
            {
                // GetUsableSkills()를 통해 현재 사용할 수 있는 스킬 판별
                bool[] usableFlags = GetUsableSkills();
                var usableSkills = new System.Collections.Generic.List<SkillBase>();
                
                for (int i = 0; i < Skills.Length; i++)
                {
                    if (Skills[i] != null && usableFlags[i])
                    {
                        usableSkills.Add(Skills[i]);
                    }
                }

                // 사용 가능한 스킬이 하나라도 있으면 AI에게 넘겨 판단하게 함
                if (usableSkills.Count > 0)
                {
                    skill = battleType.BattleAction(usableSkills.ToArray());
                    
                    // AI 타겟 결정 로직
                    if (skill.Data.TargetingType == TargetingType.Single)
                    {
                        // 단일 스킬: 사거리 내 유효 타겟 중 무작위 선택
                        var validSlots = m_formationManager.GetValidSlots(this, skill.Data);
                        var validTargets = validSlots
                            .Where(s => !s.IsEmpty && !s.Occupant.IsDead)
                            .Select(s => s.Occupant)
                            .ToList();
                        
                        if (validTargets.Count > 0)
                        {
                            _selectedTarget = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];
                        }
                        else
                        {
                            _selectedTarget = null;
                        }
                    }
                    else
                    {
                        // 광역(AOE) 스킬: 타겟을 비워두어 BattleManager가 범위 내 전원을 타격하게 함
                        _selectedTarget = null;
                    }
                }
                else
                {
                    // [진단 로그] 스킬을 하나도 사용하지 못하는 상황 분석
                    System.Text.StringBuilder debugMsg = new System.Text.StringBuilder();
                    debugMsg.AppendLine($"<color=red>[정밀 진단] {Name} 스킬 사용 불가 상태 분석</color>");
                    debugMsg.AppendLine($"- 시전자 진영(Side): {(CurrentSlot != null ? CurrentSlot.side.ToString() : "Slot Null")}");
                    debugMsg.AppendLine($"- 현재 슬롯 위치(Rank): {(CurrentSlot != null ? CurrentSlot.rank.ToString() : "Slot Null")}");
                    debugMsg.AppendLine("\n[스킬별 가용성 판단 결과]");
                    
                    for (int i = 0; i < Skills.Length; i++)
                    {
                        if (Skills[i] == null) continue;
                        
                        bool rankMatch = (CurrentSlot != null) && ((Skills[i].Data.UseableSlots & (int)CurrentSlot.rank) != 0);
                        bool targetMatch = m_formationManager.HasAnyValidTarget(this, Skills[i].Data);
                        
                        debugMsg.AppendLine($"- 스킬 [{Skills[i].Data.name}]: 위치(Rank) 조건 = {rankMatch}, 타겟 조건 = {targetMatch}");
                    }
                    
                    debugMsg.AppendLine("\n[진영 및 슬롯 상태 확인 완료]");
                    Debug.LogError(debugMsg.ToString());

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
        }
        finally
        {
            await UniTask.Delay(1000); // 턴 종료 딜레이

            Debug.Log($"<color=green>{Name} 행동 완료!</color>");
            
            // 상태 초기화 및 클린업
            _selectedSkill = null;
            _selectedTarget = null;
            _tcs = null;
            _targetTcs = null;
        }
    }
    
    public async UniTask ExecuteReaction(ReactionExecution execution, BattleContext context, IBattleManager battleManager = null)
    {
        if (execution?.Reaction?.Effect == null) return;

        Debug.Log($"<color=lightblue>{Name} 리액션 시작!</color>");

        List<CharacterSlot> targetSlots = new List<CharacterSlot>();
        if (execution.MatchedSubjects != null)
        {
            foreach (var target in execution.MatchedSubjects)
            {
                if (target != null && target.CurrentSlot != null)
                {
                    targetSlots.Add(target.CurrentSlot);
                    target.CurrentSlot.SetTargeted(true);
                    target.CurrentSlot.SetImageType(SlotImageType.Attack);
                }
            }
        }

        if (CurrentSlot != null)
        {
            CurrentSlot.SetActing(true);
            CurrentSlot.SetImageType(SlotImageType.Attack);
        }

        if (battleManager != null)
        {
            await battleManager.StartFocusEffect(CurrentSlot, targetSlots);
            await UniTask.Delay(500); // 연출 감상 대기 (기존 공격 연출 딜레이와 동일하게)
        }

        await execution.Reaction.Effect.Apply(this, execution, context);
        
        Debug.Log($"<color=lightblue>{Name} 리액션 완료!</color>");

        if (CurrentSlot != null)
        {
            CurrentSlot.SetActing(false);
            CurrentSlot.SetImageType(SlotImageType.Idle);
        }

        foreach (var slot in targetSlots)
        {
            if (slot != null)
            {
                slot.SetTargeted(false);
                slot.SetImageType(SlotImageType.Idle);
            }
        }

        if (battleManager != null)
        {
            await battleManager.EndFocusEffect(CurrentSlot, targetSlots);
        }
    }

    private BattleContext CreateBattleContext(SkillBase skill)
    {
        BattleContext battleContext = new BattleContext(
            this, 
            skill, 
            true);
        // 플레이어가 직접 선택한 타겟이 있다면 초기 설정 (없다면 AI용으로 BattleManager에서 처리)
        if (_selectedTarget != null)
        {
            battleContext.target = _selectedTarget;
        }
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
