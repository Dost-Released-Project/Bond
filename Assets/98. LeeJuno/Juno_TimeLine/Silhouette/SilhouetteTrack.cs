using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline에 Silhouette Track을 추가하는 커스텀 TrackAsset.
/// 바인딩 대상: SpriteRenderer. Timeline 창 우클릭 → Add Track에서 추가한다.
/// silhouetteMaterial은 Inspector에서 직접 연결한다. 에셋 참조 방식이므로 빌드에 자동 포함된다.
/// </summary>
[TrackColor(0.15f, 0.15f, 0.15f)]
[TrackClipType(typeof(SilhouetteClip))]
[TrackBindingType(typeof(SpriteRenderer))]
public class SilhouetteTrack : TrackAsset
{
    /// <summary>실루엣 효과에 사용할 머티리얼. Inspector에서 SilhouetteOverride 머티리얼을 연결한다.</summary>
    public Material silhouetteMaterial;

    /// <summary>
    /// Timeline 런타임이 믹서를 생성할 때 호출한다.
    /// Inspector에서 연결한 머티리얼을 믹서에 주입한 뒤 반환한다.
    /// </summary>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        ScriptPlayable<SilhouetteMixerBehaviour> playable =
            ScriptPlayable<SilhouetteMixerBehaviour>.Create(graph, inputCount);

        playable.GetBehaviour().SetMaterial(silhouetteMaterial);
        return playable;
    }
}
