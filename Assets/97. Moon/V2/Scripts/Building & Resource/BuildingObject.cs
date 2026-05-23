using UnityEngine;

public class BuildingObject : MonoBehaviour
{
    public BuildingData Data { get; private set; }
    public int CurrentLevel { get; private set; } = 1;

    private ISettlementManager _manager;
    
    // 🔗 나에게 동적으로 붙을 하위 부품 컴포넌트들 레퍼런스
    public BuildingVisualAnims Visuals { get; private set; }
    public BuildingInteractionCounter Counter { get; private set; }

    public void Initialize(BuildingData data, ISettlementManager manager)
    {
        Data = data;
        _manager = manager;

        // 나에게 붙은 비주얼 관리자와 횟수 카운터 부품을 찾아서 등록
        Visuals = GetComponent<BuildingVisualAnims>();
        Counter = GetComponent<BuildingInteractionCounter>();

        if (Visuals != null) Visuals.OnInitialize(this);
        if (Counter != null) Counter.OnInitialize(this);
    }

    public void Upgrade()
    {
        if (Data != null && CurrentLevel < Data.levels.Count)
        {
            CurrentLevel++;
            
            // 비주얼 컴포넌트에게 레벨이 올랐으니 겉모습 바꾸고 두근거리라고 명령
            if (Visuals != null)
            {
                Visuals.RefreshVisual(CurrentLevel);
                Visuals.TriggerConstructionPopping(CurrentLevel);
            }
        }
    }

    public void LoadLevelForce(int targetLevel)
    {
        CurrentLevel = targetLevel;
        
        // 로드 시에는 두근 연출 없이 겉모습만 조용히 세팅 명령
        if (Visuals != null) Visuals.RefreshVisual(CurrentLevel);
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_manager != null) _manager.OnBuildingClicked(this);
    }
}