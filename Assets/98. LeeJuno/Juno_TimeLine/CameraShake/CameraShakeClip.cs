using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// CameraShakeTrack에 배치되는 클립 에셋.
/// Inspector에서 흔들림 강도(Amplitude), 주파수(Frequency),
/// 페이드인/아웃 시간을 설정한다.
/// 페이드는 ClipCaps.None 방식으로 클립 내부 시간 기반 수동 계산한다.
/// </summary>
[Serializable]
public class CameraShakeClip : PlayableAsset, ITimelineClipAsset
{
    /// <summary>흔들림 강도 배율. Inspector에서 편집 가능. 기본값 1.</summary>
    public float amplitude = 1f;

    /// <summary>흔들림 주파수 배율. Inspector에서 편집 가능. 기본값 1.</summary>
    public float frequency = 1f;

    /// <summary>페이드인 시간(초). 0이면 즉시 최대 강도로 시작. 기본값 0.25.</summary>
    public float fadeInDuration = 0.25f;

    /// <summary>페이드아웃 시간(초). 0이면 즉시 종료. 기본값 0.25.</summary>
    public float fadeOutDuration = 0.25f;

    /// <summary>
    /// 추가 Timeline 기능 미사용.
    /// 페이드는 클립 내부 시간 기반으로 MixerBehaviour에서 수동 계산한다.
    /// </summary>
    public ClipCaps clipCaps => ClipCaps.None;

    /// <summary>
    /// Timeline 런타임에서 호출됨.
    /// CameraShakeClipBehaviour를 생성하고 Inspector 값을 복사해 반환한다.
    /// </summary>
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<CameraShakeClipBehaviour> playable =
            ScriptPlayable<CameraShakeClipBehaviour>.Create(graph);

        CameraShakeClipBehaviour behaviour = playable.GetBehaviour();
        behaviour.amplitude       = amplitude;
        behaviour.frequency       = frequency;
        behaviour.fadeInDuration  = fadeInDuration;
        behaviour.fadeOutDuration = fadeOutDuration;

        return playable;
    }
}
