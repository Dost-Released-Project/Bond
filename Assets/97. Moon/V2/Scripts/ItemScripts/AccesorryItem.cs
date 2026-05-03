using UnityEngine;

[CreateAssetMenu(fileName = "NewAccessory", menuName = "Items/Accessory")]
public class AccessoryItem : BaseItem
{
    public Equipment equipmentData; // 실제 적용될 스탯 정보

    // 인스펙터에서 값이 수정될 때 실행됩니다.
    private void OnValidate()
    {
        category = ItemCategory.Accessories;
        if (equipmentData != null)
        {
            // 자기 자신(SO)을 데이터 내부의 원본 참조로 넣어둠
            equipmentData.originItem = this; 
        }
    }

    // 장착 시 효과 적용
    public void OnEquip(BaseCharacter target)
    {
        Debug.Log($"{itemName} 장착 효과 발동!");
        // 여기에 특수 로직 작성 (예: 패시브 스킬 등록 등)
    }

    // 해제 시 효과 제거
    public void OnUnequip(BaseCharacter target)
    {
        Debug.Log($"{itemName} 해제 효과 제거!");
        // 여기에 효과 제거 로직 작성
    }
}