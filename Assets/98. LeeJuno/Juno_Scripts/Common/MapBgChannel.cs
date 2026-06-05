using UnityEngine;

/// <summary>
/// MapInitializer 에서 로드한 배경 Sprite 를 각 씬의 SceneBgLoader 로 전달하는 단방향 정적 채널.
///
/// Addressables.LoadSceneAsync 는 씬 로드 시 파라미터 직접 전달 불가하고,
/// VContainer 스코프는 씬 경계를 넘지 않으므로 StageCompletionChannel 과 동일한
/// 정적 채널 패턴으로 Sprite 참조를 보관한다.
///
/// 배틀씬, 이벤트씬 등 모든 씬에서 SceneBgLoader 가 공통으로 읽는다.
///
/// 사용 흐름:
///   1. MapInitializer.StartAsync() → Set(sprite) 호출
///   2. SceneBgLoader.Start() → Sprite 수신 후 Image 에 적용
///   3. MapInitializer.Dispose() → Clear() 호출 (핸들 해제와 함께 수행)
/// </summary>
public static class MapBgChannel
{
    private static Sprite _sprite;

    /// <summary>현재 보관 중인 배경 Sprite. 설정되지 않았으면 null.</summary>
    public static Sprite Sprite => _sprite;

    /// <summary>
    /// MapInitializer 에서 로드 완료 후 호출한다.
    /// 기존 값을 덮어쓴다.
    /// </summary>
    public static void Set(Sprite sprite)
    {
        _sprite = sprite;
    }

    /// <summary>
    /// MapInitializer.Dispose() 에서 Addressables 핸들 해제 직전 호출한다.
    /// Sprite 참조를 null 로 초기화해 다음 런 이전 잔류 참조를 제거한다.
    /// </summary>
    public static void Clear()
    {
        _sprite = null;
    }
}
