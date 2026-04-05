using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    public float SkillCooldown { get; private set; }
    
    protected float damage;
    protected int currentLevel;
    
    // 스킬매니저나 스탯같은거 주입받음
    public void Init()
    {
        
    }
    
    public abstract void UseSkill();
}