using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public enum SilhouetteColorMode
{
    Black,
    White
}

/// <summary>
/// SilhouetteTrack에 배치되는 클립 에셋. Inspector에서 색상 모드와 페이드 시간을 설정한다.
/// </summary>
[Serializable]
public class SilhouetteClip : PlayableAsset, ITimelineClipAsset
{
    /// <summary>전환 색상. Inspector에서 편집 가능하며 기본값은 Black.</summary>
    public SilhouetteColorMode colorMode = SilhouetteColorMode.Black;

    /// <summary>페이드인 시간(초). 0이면 즉시 전환.</summary>
    public float fadeInDuration = 0.25f;

    /// <summary>페이드아웃 시간(초). 0이면 즉시 전환.</summary>
    public float fadeOutDuration = 0.25f;

    /// <summary>추가 Timeline 기능 미사용. 페이드는 클립 내부 시간 기반으로 수동 계산한다.</summary>
    public ClipCaps clipCaps => ClipCaps.None;

    /// <summary>
    /// Timeline 런타임에서 호출됨.
    /// SilhouetteClipBehaviour를 생성하고 Inspector 값을 복사해 반환한다.
    /// </summary>
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<SilhouetteClipBehaviour> playable =
            ScriptPlayable<SilhouetteClipBehaviour>.Create(graph);

        SilhouetteClipBehaviour behaviour = playable.GetBehaviour();
        behaviour.colorMode = colorMode;
        behaviour.fadeInDuration = fadeInDuration;
        behaviour.fadeOutDuration = fadeOutDuration;

        return playable;
    }
}
