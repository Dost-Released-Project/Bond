using UnityEngine;

public class BuildingInteractionCounter : MonoBehaviour
{
    private BuildingObject _owner;
    public int CurrentTurnUses { get; private set; } = 0;

    public void OnInitialize(BuildingObject owner)
    {
        _owner = owner;
    }

    public void UseBuilding()
    {
        CurrentTurnUses++;
    }

    public void ResetTurnUses()
    {
        CurrentTurnUses = 0;
    }

    public bool IsUseLimitReached()
    {
        if (_owner == null || _owner.Data == null) return false;
        
        var levelData = _owner.Data.GetLevelData(_owner.CurrentLevel);
        if (levelData.level == 0) return false;

        // 창고/대장간 등 maxUses가 테이블에 0 이하로 세팅된 건물은 제한 면제
        if (levelData.maxUses <= 0) return false;

        return CurrentTurnUses >= levelData.maxUses;
    }
}