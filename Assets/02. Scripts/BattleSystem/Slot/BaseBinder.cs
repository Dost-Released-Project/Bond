using UnityEngine;

/// <summary>
/// 로직과 비주얼을 서로 연결(Binding)해주는 베이스 클래스입니다.
/// 제네릭 제약 조건을 통해 각 타입이 Bind 메서드를 가지고 있음을 보장합니다.
/// </summary>
public class BaseBinder<TBinderTarget, TBinder> : MonoBehaviour
    where TBinder : INeedBind<TBinderTarget>
{
    protected void Bind(TBinderTarget bind1, TBinder bind2)
    {
        if (bind2 == null) return;
        bind2.Bind(bind1);
    }
}
