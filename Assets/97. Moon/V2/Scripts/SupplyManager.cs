using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class SupplyManager : MonoBehaviour, ISupplyManager
{
    private ResourceManager _resourceManager;
    private ITotalInventory _totalInventory;
    private StageCoach _stageCoach;

    private BaseItem _specialSupplyItem;
    private SupplyDataBaseSO _supplyDataBase; 

    [Inject] private Roster _roster;

    [Inject]
    public void Construct(ResourceManager rm, ITotalInventory total, StageCoach stageCoach)
    {
        _resourceManager = rm;
        _totalInventory = total;
        _stageCoach = stageCoach;
    }
    
    private void Awake()
    {
        _supplyDataBase = DBSORegistry.GetDb<SupplyDataBaseSO>();
        if (_supplyDataBase == null)
        {
            Debug.LogError("[SupplyManager] SupplyDataBaseSO가 레지스트리에 존재하지 않습니다.");
        }

        // 특수 보급품 고정 로드 규칙 보존
        _specialSupplyItem = DBSORegistry.GetSO<BaseItem>("07020000");
    }

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
            _roster.Hire(_stageCoach.GetRandomCharacter());
            Debug.Log("<color=green>[보급]</color> 증원 요청 완료.");
        });
    }
    
    public void RequestSupply(SupplyType type)
    {
        TryProcessSupply(type, () => 
        {
            if (type == SupplyType.Normal_Supply)
            {
                ExecuteNormalSupplyLottery();
            }
            else if (type == SupplyType.Special_Supply && _specialSupplyItem != null)
            {
                int count = CalculateSpecialSupplyCount();
                _totalInventory.AddItemAuto(_specialSupplyItem, count);
                Debug.Log($"<color=green>[보급]</color> 특수 보급품 {_specialSupplyItem.itemName} {count}개가 창고에 추가되었습니다.");
            }
        });
    }

    private void ExecuteNormalSupplyLottery()
    {
        if (_supplyDataBase == null) return;

        // 💥 [교정] 없는 함수(GetAllList) 대신 레지스트리가 제공하는 QuerySO API를 통해 
        // 이미 메모리에 로드된 모든 SupplyDataSO 리스트를 직접 쿼리하여 가져옵니다.
        var queryResult = DBSORegistry.QuerySO<SupplyDataSO>(so => true);
        
        List<SupplyDataSO> supplyList = new List<SupplyDataSO>();
        float totalRate = 0f;

        foreach (var so in queryResult)
        {
            if (so != null)
            {
                supplyList.Add(so);
                totalRate += so.Rate;
            }
        }

        if (supplyList.Count == 0) return;

        // 난수 주사위 생성 (0 ~ 총합)
        float diceValue = Random.Range(0f, totalRate);
        float currentWeightSum = 0f;
        SupplyDataSO selectedBundle = null;

        // 누적 검증 스캔
        foreach (var so in supplyList)
        {
            currentWeightSum += so.Rate;
            if (diceValue <= currentWeightSum)
            {
                selectedBundle = so;
                break;
            }
        }

        // 선정된 번들 내부 품목 전량 자동 안착 정산
        if (selectedBundle != null)
        {
            Debug.Log($"<color=yellow>[보급 테이블 추첨 완료]</color> 당첨 번들: {selectedBundle.DisplayName} (ID: {selectedBundle.Id})");
            
            foreach (var pair in selectedBundle.BundleItems)
            {
                BaseItem targetItem = DBSORegistry.GetSO<BaseItem>(pair.itemId);
                if (targetItem != null)
                {
                    _totalInventory.AddItemAuto(targetItem, pair.count);
                    Debug.Log($"   - 보급 수령: {targetItem.itemName} x{pair.count}개 완료.");
                }
            }
        }
    }

    private int CalculateSpecialSupplyCount()
    {
        int roll = Random.Range(0, 100);

        if (roll < 75) return 1;       // 0 ~ 74 (75% 확률)
        if (roll < 95) return 2;       // 75 ~ 94 (20% 확률)
        return 3;                      // 95 ~ 99 (5% 확률)
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