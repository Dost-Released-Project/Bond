using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewAccessory", menuName = "Items/Accessory")]
public class AccessoryItem : BaseItem
{
    public List<StatModifier> specialEffects = new List<StatModifier>(); // 초기화 필수!

    // 장착 시 효과 적용
    public void OnEquip(BaseCharacter target)
    {
        // source를 자기 자신으로 설정하여 나중에 해제할 때 찾기 쉽게 함
        foreach(var effect in specialEffects) effect.source = this;
        target.StatController.AddModifiers(specialEffects);
        Debug.Log($"{itemName} 장착 효과 발동!");
    }

    // 해제 시 효과 제거
    public void OnUnequip(BaseCharacter target)
    {
        target.StatController.RemoveModifiersFromSource(this, specialEffects.Count);
        Debug.Log($"{itemName} 해제 효과 제거!");
    }
}