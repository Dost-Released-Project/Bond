using UnityEngine;

public enum TutorialSequence { Sequence_A_Town, Sequence_B_Expedition }

public class TutorialStepSO : BaseSO
{
    [Header("튜토리얼 고유 설정")] [SerializeField]
    private TutorialSequence _sequence;

    [SerializeField] private string _targetUiKey;

    public TutorialSequence Sequence => _sequence;
    public string TargetUiKey => _targetUiKey;

    public void SetData(string id, string displayName, string description, TutorialSequence sequence, string targetUiKey)
    {
        Initialize(id, displayName, description);
        _sequence = sequence;
        _targetUiKey = targetUiKey;
    }
}


// 직렬화에 사용될 순수 데이터 세이브 래퍼
[System.Serializable]
public class TutorialRawSaveData
{
    public bool isTutorialCleared;
    public string currentStepId;
}