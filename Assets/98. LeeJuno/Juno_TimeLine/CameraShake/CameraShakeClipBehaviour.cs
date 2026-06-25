using UnityEngine.Playables;

/// <summary>
/// CameraShakeTrack 클립 한 개의 런타임 데이터 컨테이너.
/// CameraShakeClip.CreatePlayable()에서 생성되며,
/// 흔들림 강도(amplitude), 주파수(frequency), 페이드 시간을 보관한다.
/// 실제 적용은 CameraShakeMixerBehaviour가 담당한다.
/// </summary>
public class CameraShakeClipBehaviour : PlayableBehaviour
{
    /// <summary>흔들림 강도 배율. 0이면 흔들림 없음. 기본값 1.</summary>
    public float amplitude = 1f;

    /// <summary>흔들림 주파수 배율. 높을수록 빠른 흔들림. 기본값 1.</summary>
    public float frequency = 1f;

    /// <summary>페이드인 시간(초). 0이면 즉시 전환.</summary>
    public float fadeInDuration = 0f;

    /// <summary>페이드아웃 시간(초). 0이면 즉시 전환.</summary>
    public float fadeOutDuration = 0f;
}
