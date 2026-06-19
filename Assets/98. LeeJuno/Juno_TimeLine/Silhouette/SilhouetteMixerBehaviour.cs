using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// SilhouetteTrack의 믹서. SpriteRenderer 전용.
/// static 공유 머티리얼 1개 + MaterialPropertyBlock으로 per-renderer 프로퍼티 오버라이드.
/// 공유 머티리얼을 직접 수정하지 않으므로 여러 SilhouetteTrack이 동시에 동작해도 안전하다.
/// </summary>
public class SilhouetteMixerBehaviour : PlayableBehaviour
{
    /// <summary>모든 인스턴스가 공유하는 실루엣 셰이더 머티리얼. 씬 단위 1회 생성.</summary>
    private static Material _sharedSilhouetteMaterial;

    private SpriteRenderer _spriteRenderer;
    private Material _originalMaterial;
    private MaterialPropertyBlock _propertyBlock;
    private bool _originalColorSaved;
    private bool _isSilhouetteActive;

    /// <summary>
    /// 매 프레임 Timeline이 호출한다. playerData는 트랙에 바인딩된 SpriteRenderer다.
    /// </summary>
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        SpriteRenderer boundRenderer = playerData as SpriteRenderer;

        if (boundRenderer == null)
        {
            return;
        }

        if (_originalColorSaved == false)
        {
            CacheTarget(boundRenderer);
        }

        int inputCount = playable.GetInputCount();
        float totalWeight = 0f;
        SilhouetteColorMode activeColorMode = SilhouetteColorMode.Black;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);

            if (weight <= 0f)
            {
                continue;
            }

            ScriptPlayable<SilhouetteClipBehaviour> inputPlayable =
                (ScriptPlayable<SilhouetteClipBehaviour>)playable.GetInput(i);

            SilhouetteClipBehaviour clipBehaviour = inputPlayable.GetBehaviour();

            float clipTime = (float)inputPlayable.GetTime();
            float clipDuration = (float)inputPlayable.GetDuration();
            float fadeWeight = CalculateFadeWeight(clipTime, clipDuration, clipBehaviour.fadeInDuration, clipBehaviour.fadeOutDuration);

            activeColorMode = clipBehaviour.colorMode;
            totalWeight += weight * fadeWeight;
        }

        if (totalWeight > 0f)
        {
            ApplyColor(activeColorMode, totalWeight);
        }
        else
        {
            RestoreOriginalColor();
        }
    }

    /// <summary>
    /// Timeline 정지 또는 트랙 바인딩 해제 시 호출된다.
    /// 원본 머티리얼 복원, PropertyBlock 초기화, 참조 해제를 수행한다.
    /// 공유 머티리얼은 해제하지 않는다.
    /// </summary>
    public override void OnPlayableDestroy(Playable playable)
    {
        RestoreOriginalColor();

        _spriteRenderer = null;
        _originalMaterial = null;
        _propertyBlock = null;
        _originalColorSaved = false;
        _isSilhouetteActive = false;
    }

    /// <summary>
    /// 바인딩 SpriteRenderer의 원본 sharedMaterial을 저장하고 PropertyBlock을 초기화한다.
    /// 공유 셰이더 머티리얼이 null이면 이 시점에 1회 생성한다.
    /// </summary>
    private void CacheTarget(SpriteRenderer target)
    {
        _spriteRenderer = target;
        _originalMaterial = target.sharedMaterial;

        if (_sharedSilhouetteMaterial == null)
        {
            // static 필드 — 모든 인스턴스 공유, 씬 전체에서 1번만 생성된다
            _sharedSilhouetteMaterial = new Material(Shader.Find("JunoTimeline/SilhouetteOverride"));
        }

        _propertyBlock = new MaterialPropertyBlock();
        _originalColorSaved = true;
    }

    /// <summary>
    /// 공유 머티리얼로 교체하고 MaterialPropertyBlock으로 색상·BlendFactor를 설정한다.
    /// 머티리얼 교체는 비활성 상태일 때 1회만 수행한다. per-frame은 SetPropertyBlock만 호출한다.
    /// </summary>
    private void ApplyColor(SilhouetteColorMode colorMode, float totalWeight)
    {
        if (_isSilhouetteActive == false)
        {
            // 공유 머티리얼로 교체 — 인스턴스 생성 없음
            _spriteRenderer.material = _sharedSilhouetteMaterial;
            _isSilhouetteActive = true;
        }

        Color silhouetteColor = colorMode == SilhouetteColorMode.Black ? Color.black : Color.white;

        // GetPropertyBlock으로 현재 블록을 읽고 값을 덮어쓴 뒤 SetPropertyBlock으로 적용한다
        // 다른 시스템이 설정한 기존 프로퍼티를 유지하기 위해 Get → Set 패턴을 사용한다
        _spriteRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_SilhouetteColor", silhouetteColor);
        // totalWeight(0~1)을 BlendFactor로 사용해 원본↔실루엣 색상을 보간한다
        _propertyBlock.SetFloat("_BlendFactor", totalWeight);
        _spriteRenderer.SetPropertyBlock(_propertyBlock);
    }

    /// <summary>
    /// 원본 머티리얼로 복원하고 PropertyBlock을 제거한다.
    /// 클립 구간 밖(totalWeight == 0)에서 호출된다.
    /// </summary>
    private void RestoreOriginalColor()
    {
        if (_isSilhouetteActive == false)
        {
            return;
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.material = _originalMaterial;
            _spriteRenderer.SetPropertyBlock(null);
        }

        _isSilhouetteActive = false;
    }

    /// <summary>
    /// 클립 내부 시간 기반으로 페이드 weight를 계산한다.
    /// fadeIn/fadeOut이 0이면 즉시 전환(1 반환).
    /// </summary>
    private float CalculateFadeWeight(float clipTime, float clipDuration, float fadeIn, float fadeOut)
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
