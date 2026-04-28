using UnityEngine;

public enum EquipmentType { Base, Accessory }

public class Equipment : MonoBehaviour
{
    public string itemName;
    public EquipmentType type;
    public int upgradeLevel = 0; // 0 ~ 5단계
    public const int MAX_UPGRADE = 5;

    [Header("Base Stat Bonus")]
    public int bonusSTR;
    public int bonusAGI;
    public int bonusINT;

    // 강화 시 호출 (대장간에서 사용)
    public void Upgrade()
    {
        if (upgradeLevel < MAX_UPGRADE)
        {
            upgradeLevel++;
            if (type == EquipmentType.Base)
            {
                // 기본 장비만 스탯을 확정적으로 +1씩 강화
                bonusSTR += 1;
                bonusAGI += 1;
                bonusINT += 1;
            }
            Debug.Log($"{itemName} 강화 성공! 현재 레벨: {upgradeLevel}");
        }
    }
}