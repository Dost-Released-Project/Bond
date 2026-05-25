using Cysharp.Threading.Tasks;

/// <summary>
/// 이벤트 효과 단위 처리를 담당하는 핸들러 계약 인터페이스.
/// Chain of Responsibility 패턴으로 EventEffectApplier 의 switch 를 대체한다.
/// </summary>
public interface IEventEffectHandler
{
    /// <summary>
    /// 이 핸들러가 해당 효과를 처리할 책임이 있는지 여부를 반환한다.
    /// </summary>
    bool CanHandle(EffectType effectType);

    /// <summary>
    /// 효과를 비동기로 적용한다.
    /// </summary>
    UniTask HandleAsync(EventEffectData effect);
}
