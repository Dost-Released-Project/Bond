using UnityEngine;
using UnityEngine.UI;

public enum ItemCategory
{
    Resource, 
    Consume, 
    Accessories
}
public class BaseItem : ScriptableObject
{
    public string ID { get; private set; } // 아이템 ID
    public string itemName; // 아이템 이름
    public ItemCategory itemCategory; // 아이템 카테고리
    public Image icon; // 아이콘 이미지
    public string description; // 설명

    public int maxStack; // 최대 적재량
    public bool isConsume; // 소모품 여부

    // 사용 효과
    public void Effect(BaseCharacter bc)
    {
        
    }
}