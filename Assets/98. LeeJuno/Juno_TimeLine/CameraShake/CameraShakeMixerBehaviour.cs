using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// CameraShakeTrack의 믹서 PlayableBehaviour.
/// 바인딩된 CinemachineBasicMultiChannelPerlin 컴포넌트의
/// AmplitudeGain과 FrequencyGain을 클립 구간 동안 오버라이드한다.
///
/// 처리 흐름:
///   1. ProcessFrame() 첫 호출 — 원본 AmplitudeGain, FrequencyGain 저장
///   2. 클립 활성(totalWeight > 0) — fadeWeight 기반으로 값 적용
///   3. 클립 비활성(totalWeight == 0) — 원본 값 복원
///   4. OnPlayableDestroy() — 원본 값 복원 후 상태 초기화
/// </summary>
public class CameraShakeMixerBehaviour : PlayableBehaviour
{
    private CinemachineBasicMultiChannelPerlin _noiseComponent;
    private float _originalAmplitude;
    private float _originalFrequency;
    private bool _initialized;
    private bool _isShakeActive;

    /// <summary>
    /// 매 프레임 Timeline이 호출한다.
    /// playerData는 트랙에 바인딩된 CinemachineBasicMultiChannelPerlin이다.
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        CinemachineBasicMultiChannelPerlin noiseComponent =
            playerData as CinemachineBasicMultiChannelPerlin;

        if (noiseComponent == null)
        {
            return;
        }

        if (_initialized == false)
        {
            CacheTarget(noiseComponent);
        }

        int inputCount         = playable.GetInputCount();
        float totalWeight      = 0f;
        float blendedAmplitude = 0f;
        float blendedFrequency = _originalFrequency;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);

            if (weight <= 0f)
            {
                continue;
            }

            ScriptPlayable<CameraShakeClipBehaviour> inputPlayable =
                (ScriptPlayable<CameraShakeClipBehaviour>)playable.GetInput(i);

            CameraShakeClipBehaviour clipBehaviour = inputPlayable.GetBehaviour();

            float clipTime     = (float)inputPlayable.GetTime();
            float clipDuration = (float)inputPlayable.GetDuration();
            float fadeWeight   = CalculateFadeWeight(
                clipTime,
                clipDuration,
                clipBehaviour.fadeInDuration,
                clipBehaviour.fadeOutDuration
            );

            // 여러 클립이 동시에 활성화될 경우 weight 가중 평균으로 블렌딩한다
            blendedAmplitude += clipBehaviour.amplitude * fadeWeight * weight;
            // Frequency는 목표값과 원본값 사이를 fadeWeight로 Lerp한다
            blendedFrequency  = Mathf.Lerp(_originalFrequency, clipBehaviour.frequency, fadeWeight * weight);
            totalWeight       += weight;
        }

        if (totalWeight > 0f)
        {
            ApplyShake(blendedAmplitude, blendedFrequency);
        }
        else
        {
            RestoreOriginalValues();
        }
    }

    /// <summary>
    /// Timeline 정지 또는 에디터 스크럽으로 PlayableGraph가 재빌드될 때 호출된다.
    /// 원본 값을 복원하고 상태를 초기화한다.
    /// </summary>
    public override void OnPlayableDestroy(Playable playable)
    {
        if (_noiseComponent != null && _initialized)
        {
            _noiseComponent.AmplitudeGain = _originalAmplitude;
            _noiseComponent.FrequencyGain = _originalFrequency;
        }

        _isShakeActive  = false;
        _noiseComponent = null;
        _initialized    = false;
    }

    /// <summary>
    /// 바인딩된 CinemachineBasicMultiChannelPerlin의 원본 값을 저장한다.
    /// ProcessFrame 첫 호출 시 1회만 실행된다.
    /// </summary>
    private void CacheTarget(CinemachineBasicMultiChannelPerlin target)
    {
        _noiseComponent    = target;
        _originalAmplitude = target.AmplitudeGain;
        _originalFrequency = target.FrequencyGain;
        _initialized       = true;
    }

    /// <summary>
    /// 계산된 흔들림 값을 노이즈 컴포넌트에 적용한다.
    /// </summary>
    private void ApplyShake(float amplitude, float frequency)
    {
        _noiseComponent.AmplitudeGain = amplitude;
        _noiseComponent.FrequencyGain = frequency;
        _isShakeActive                = true;
    }

    /// <summary>
    /// 원본 AmplitudeGain과 FrequencyGain을 복원한다.
    /// 클립 구간 밖(totalWeight == 0)에서 호출된다.
    /// </summary>
    private void RestoreOriginalValues()
    {
        if (_isShakeActive == false)
        {
            return;
        }

        if (_noiseComponent != null)
        {
            _noiseComponent.AmplitudeGain = _originalAmplitude;
            _noiseComponent.FrequencyGain = _originalFrequency;
        }

        _isShakeActive = false;
    }

    /// <summary>
    /// 클립 내부 시간 기반으로 페이드 weight를 계산한다.
    /// fadeIn/fadeOut이 0이면 즉시 전환(1 반환).
    /// SilhouetteMixerBehaviour의 동일 로직을 적용한다.
    /// </summary>
    private float CalculateFadeWeight(
        float clipTime,
        float clipDuration,
        float fadeIn,
        float fadeOut)
    {
        if (fadeIn > 0f && clipTime < fadeIn)
        {
            return Mathf.Clamp01(clipTime / fadeIn);
        }

        if (fadeOut > 0f && clipTime > clipDuration - fadeOut)
        {
            return Mathf.Clamp01((clipDuration - clipTime) / fadeOut);
        }

        return 1f;
    }
}
