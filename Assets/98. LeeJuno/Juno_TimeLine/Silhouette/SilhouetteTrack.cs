using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline에 Silhouette Track을 추가하는 커스텀 TrackAsset.
/// 바인딩 대상: SpriteRenderer. Timeline 창 우클릭 → Add Track에서 추가한다.
/// </summary>
[TrackColor(0.15f, 0.15f, 0.15f)]
[TrackClipType(typeof(SilhouetteClip))]
[TrackBindingType(typeof(SpriteRenderer))]
public class SilhouetteTrack : TrackAsset
{
    /// <summary>
    /// Timeline 런타임이 믹서를 생성할 때 호출한다.
    /// SilhouetteMixerBehaviour를 Playable로 래핑해 반환한다.
    /// </summary>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<SilhouetteMixerBehaviour>.Create(graph, inputCount);
    }
}
