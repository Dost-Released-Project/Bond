using Cysharp.Threading.Tasks;

/// <summary>
/// 이벤트 선택지 효과를 실제 게임 시스템에 적용하는 계약 인터페이스.
/// 캐릭터/파티/아이템 시스템이 미구현인 경우 Stub 구현체를 등록해
/// 컴파일 오류 없이 사용할 수 있다.
/// </summary>
public interface IEventEffectApplier
{
    /// <summary>
    /// 효과 적용을 시작한다.
    /// ChooseOne 타입이면 UI 로 대상 선택을 기다린 뒤 적용한다.
    /// </summary>
    /// <param name="effect">적용할 효과 데이터.</param>
    UniTask ApplyAsync(EventEffectData effect);
}
