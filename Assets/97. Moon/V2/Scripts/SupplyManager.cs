using UnityEngine;
using VContainer;

public class SupplyManager : MonoBehaviour, ISupplyManager
{
    private ResourceManager _resourceManager;
    private ITotalInventory _totalInventory;

    [Header("보급 아이템 에셋")]
    public BaseItem normalSupplyPackage; // 붕대 등 묶음
    public BaseItem specialSupplyItem;   // 정신 각성제

    [Inject]
    public void Construct(ResourceManager rm, ITotalInventory total)
    {
        _resourceManager = rm;
        _totalInventory = total;
    }

    // [리팩토링] 공통 실행 로직: 비용 확인 및 소모 처리를 하나로 통합
    private bool TryProcessSupply(SupplyType type, System.Action onSuccess)
    {
        int cost = GetRequiredData(type);
        Debug.Log($"<color=white>[보급 요청]</color> {type} (비용: {cost})");

        if (_resourceManager.ConsumeResources(cost, 0, 0))
        {
            onSuccess?.Invoke();
            Debug.Log($"<color=green>[보급 완료]</color> {type} 처리가 성공적으로 끝났습니다.");
            return true;
        }

        Debug.LogWarning($"<color=red>[보급 실패]</color> 개척 데이터가 부족합니다. (보유: {_resourceManager.FrontierData} / 필요: {cost})");
        return false;
    }

    public void RequestReinforcement()
    {
        TryProcessSupply(SupplyType.Reinforcements, () => 
        {
            // 기존 기능 유지: AdminTestTool 참조 및 빌더 패턴 유지
            AdminTestTool.testHero.Data = new BaseCharacterData.Builder().Build(); 
            Debug.Log("<color=green>[보급]</color> 증원 요청 완료.");
        });
    }

    public void RequestSupply(SupplyType type)
    {
        TryProcessSupply(type, () => 
        {
            if (type == SupplyType.Normal_Supply)
            {
                _totalInventory.AddItemAuto(normalSupplyPackage, 3);
                Debug.Log("<color=green>[보급]</color> 일반 보급품 3개가 창고에 추가되었습니다.");
            }
            else if (type == SupplyType.Special_Supply)
            {
                _totalInventory.AddItemAuto(specialSupplyItem, 1);
                Debug.Log("<color=green>[보급]</color> 특수 보급품 1개가 창고에 추가되었습니다.");
            }
        });
    }

    public int GetRequiredData(SupplyType type)
    {
        return type switch
        {
            SupplyType.Reinforcements => 300,
            SupplyType.Normal_Supply => 100,
            SupplyType.Special_Supply => 500,
            _ => 0
        };
    }
}