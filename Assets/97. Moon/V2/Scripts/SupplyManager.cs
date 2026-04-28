using UnityEngine;
using VContainer;

public class SupplyManager : MonoBehaviour, ISupplyManager
{
    private ResourceManager _resourceManager;
    private ITotalInventory _totalInventory;

    // 인스펙터에서 보급될 실제 아이템 에셋 할당
    public BaseItem normalSupplyPackage; // 붕대 등 묶음
    public BaseItem specialSupplyItem;   // 정신 각성제

    [Inject]
    public void Construct(ResourceManager rm, ITotalInventory total)
    {
        _resourceManager = rm;
        _totalInventory = total;
    }

    public virtual void RequestReinforcement()
    {
        int cost = GetRequiredData(SupplyType.Reinforcements);
        if (_resourceManager.ConsumeResources(cost, 0, 0))
        {
            // TODO: 팀원이 작업 중인 캐릭터 생성(증원) 로직 호출
            Debug.Log("<color=green>[보급]</color> 증원 요청 완료.");
        }
    }

    public virtual void RequestSupply(SupplyType type)
    {
        int cost = GetRequiredData(type);
        if (!_resourceManager.ConsumeResources(cost, 0, 0))
        {
            Debug.LogWarning("개척 데이터가 부족합니다.");
            return;
        }

        if (type == SupplyType.Normal_Supply)
        {
            //_totalInventory.AddItemAuto(normalSupplyPackage, 5);
            //Debug.Log("<color=green>[보급]</color> 일반 보급품(붕대 등) 3개가 창고에 추가되었습니다.");
        }
        else if (type == SupplyType.Special_Supply)
        {
            //_totalInventory.AddItemAuto(specialSupplyItem, 5);
            //Debug.Log("<color=green>[보급]</color> 특수 보급품(정신 각성제) 1개가 창고에 추가되었습니다.");
        }
    }

    public virtual int GetRequiredData(SupplyType type)
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