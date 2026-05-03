using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class SupplyManager : MonoBehaviour, ISupplyManager
{
    private ResourceManager _resourceManager;
    private ITotalInventory _totalInventory;
    
    // 내부에서 사용할 에셋 리스트 (런타임 로드)
    private List<BaseItem> _normalSupplyPool = new();
    private BaseItem _specialSupplyItem;

    [Inject]
    public void Construct(ResourceManager rm, ITotalInventory total)
    {
        _resourceManager = rm;
        _totalInventory = total;
    }
    
    private void Awake() // 테스트 용도
    {
        // 일반 보급품 풀 구성 (붕대, 진정제 등 ID 기반 로드)
        var oldBandage = Resources.Load<BaseItem>("Data/Items/Consumables/07000000");
        var bandage = Resources.Load<BaseItem>("Data/Items/Consumables/07030000");
        var oldSedative = Resources.Load<BaseItem>("Data/Items/Consumables/07010000");
        var sedative = Resources.Load<BaseItem>("Data/Items/Consumables/07040000");
        if (bandage != null) _normalSupplyPool.Add(oldBandage);
        if (bandage != null) _normalSupplyPool.Add(bandage);
        if (sedative != null) _normalSupplyPool.Add(oldSedative);
        if (sedative != null) _normalSupplyPool.Add(sedative);

        // 특수 보급품 로드
        _specialSupplyItem = Resources.Load<BaseItem>("Data/Items/Consumables/07020000");
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
            if (type == SupplyType.Normal_Supply && _normalSupplyPool.Count > 0)
            {
                // 랜덤하게 하나를 골라서 3개 지급
                int randomIndex = Random.Range(0, _normalSupplyPool.Count);
                _totalInventory.AddItemAuto(_normalSupplyPool[randomIndex], 3);
                Debug.Log($"<color=green>[보급]</color> {_normalSupplyPool[randomIndex].itemName} 3개가 보급되었습니다.");
            }
            else if (type == SupplyType.Special_Supply && _specialSupplyItem != null)
            {
                _totalInventory.AddItemAuto(_specialSupplyItem, 1);
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