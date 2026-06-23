using UnityEngine.Playables;

/// <summary>
/// SilhouetteTrack 클립 한 개의 런타임 데이터 컨테이너.
/// SilhouetteClip.CreatePlayable()에서 생성되며 전환 색상 모드와 페이드 시간을 보관한다.
/// 실제 색상 적용은 SilhouetteMixerBehaviour가 담당한다.
/// </summary>
public class SilhouetteClipBehaviour : PlayableBehaviour
{
    /// <summary>전환할 색상 모드. 기본값 Black.</summary>
    public SilhouetteColorMode colorMode = SilhouetteColorMode.Black;

    /// <summary>페이드인 시간(초). 0이면 즉시 전환.</summary>
    public float fadeInDuration = 0f;

    /// <summary>페이드아웃 시간(초). 0이면 즉시 전환.</summary>
    public float fadeOutDuration = 0f;
}
