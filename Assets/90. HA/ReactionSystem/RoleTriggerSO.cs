using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoleTrigger", menuName = "RoleTrigger", order = 0)]
public class RoleTriggerSO : ScriptableObject
{
    public RoleType Role;
    public List<string> TriggerKeys;
}