using UnityEngine;

[CreateAssetMenu(fileName = "NewBaseEquipment", menuName = "Items/BaseEquipment")]
public class BaseEquipmentItem : ScriptableObject
{
    public string id;
    public ClassType classType;
    public int STR;
    public int AGI;
    public int INT;
    public string weaponId;
    public string armorId;
    public Sprite icon;
}
