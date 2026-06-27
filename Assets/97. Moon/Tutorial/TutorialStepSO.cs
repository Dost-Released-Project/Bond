using UnityEngine;

public enum TutorialSequence { Sequence_A_Town, Sequence_B_Expedition, Sequence_C_Camp, Sequence_D_Battle }

public enum TutorialClickType
{
    LeftClick = 0,      // 반드시 좌클릭
    RightClick = 1,     // 반드시 우클릭
    AnyClick = 2        // 좌/우 관계없음
}

public class TutorialStepSO : BaseSO
{
    [Header("튜토리얼 고유 설정")]
    [SerializeField] private TutorialSequence _sequence;
    [SerializeField] private string _targetUiKey;
    [SerializeField] private TutorialClickType _clickType;

    public TutorialSequence Sequence => _sequence;
    public string TargetUiKey        => _targetUiKey;
    public TutorialClickType ClickType     => _clickType;

    [Header("🎁 단계 처리 보상 설정 (0 또는 공백이면 무시)")]
    [SerializeField] private int _rewardFrontier;
    [SerializeField] private int _rewardWood;
    [SerializeField] private int _rewardOre;
        
    [Tooltip("콤마(,)로 구분하여 여러 ID 기입 가능. 예: 08000000,08010000,08020000")]
    [SerializeField] private string _rewardItemIds;
        
    [Tooltip("콤마(,)로 구분하여 각 ID에 매칭될 수량 기입. 예: 1,1,1")]
    [SerializeField] private string _rewardItemCounts;

    public int RewardFrontier => _rewardFrontier;
    public int RewardWood     => _rewardWood;
    public int RewardOre      => _rewardOre;
    public string RewardItemIds => _rewardItemIds;
    public string RewardItemCounts => _rewardItemCounts;

    public void SetData(string id, string displayName, string description, TutorialSequence sequence, string targetUiKey,
        int frontier, int wood, int ore, string itemIds, string itemCounts, TutorialClickType clickType)
    {
        base.Initialize(id, displayName, description);
        _sequence = sequence;
        _targetUiKey = targetUiKey;
        _rewardFrontier = frontier;
        _rewardWood = wood;
        _rewardOre = ore;
        _rewardItemIds = itemIds;
        _rewardItemCounts = itemCounts;
        _clickType = clickType;
    }
}

// 직렬화에 사용될 순수 데이터 세이브 래퍼
[System.Serializable]
public class TutorialRawSaveData
{
    public bool isTutorialCleared;
    public string currentStepId;
}