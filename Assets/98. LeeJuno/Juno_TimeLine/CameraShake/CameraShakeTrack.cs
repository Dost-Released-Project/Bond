using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline에 카메라 흔들림 트랙을 추가하는 커스텀 TrackAsset.
/// 바인딩 대상: CinemachineBasicMultiChannelPerlin
///   - CinemachineCamera와 같은 게임오브젝트에 부착된 노이즈 컴포넌트를 연결한다.
/// 허용 클립: CameraShakeClip
/// Timeline 창 → 빈 영역 우클릭 → Add Track → JunoTimeline/Camera Shake Track
/// </summary>
[TrackColor(0.4f, 0.2f, 0.6f)]
[TrackClipType(typeof(CameraShakeClip))]
[TrackBindingType(typeof(CinemachineBasicMultiChannelPerlin))]
public class CameraShakeTrack : TrackAsset
{
    /// <summary>
    /// Timeline 런타임이 믹서를 생성할 때 호출한다.
    /// CameraShakeMixerBehaviour를 Playable로 래핑해 반환한다.
    /// </summary>
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CameraShakeMixerBehaviour>.Create(graph, inputCount);
    }
}
