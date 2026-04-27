using UnityEngine;

public interface INeedBind<TargetClass>
{
    public void Bind(TargetClass targetClass);
}
