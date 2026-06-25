Shader "Juno/UI/SpeedLineUI"
{
    Properties
    {
        // 스피드라인 색상
        _Color          ("Color",             Color)         = (1, 1, 1, 1)
        // 선 굵기 (UV 공간 기준, 0~1)
        _LineThickness  ("Line Thickness",    Range(0, 0.5)) = 0.02
        // 선 간격 (UV 반복 주기, 값이 작을수록 선이 촘촘)
        _LineSpacing    ("Line Spacing",      Range(0.01, 1)) = 0.1
        // 선 방향 (0 = 가로선, 1 = 세로선)
        _LineDirection  ("Line Direction (0=Horizontal 1=Vertical)", Range(0, 1)) = 0
        // X축 스크롤 속도 (초당 UV 이동량)
        _ScrollX        ("Scroll X Speed",    Float)         = 0.0
        // Y축 스크롤 속도 (초당 UV 이동량)
        _ScrollY        ("Scroll Y Speed",    Float)         = 1.0
        // 전체 알파 (C#에서 애니메이션 용도)
        _Alpha          ("Alpha",             Range(0, 1))   = 1.0
        // 엣지 소프트니스 (선 경계 블렌딩 폭, 0이면 하드엣지)
        _EdgeSoftness   ("Edge Softness",     Range(0, 0.5)) = 0.005

        // C#에서 매 프레임 Time.unscaledTime을 주입한다 (timeScale=0에서도 동작)
        [HideInInspector] _UnscaledTime  ("Unscaled Time",         Float) = 0

        // UI Stencil 지원 (Unity Built-in UI 마스킹 호환)
        [HideInInspector] _StencilComp       ("Stencil Comparison",  Float) = 8
        [HideInInspector] _Stencil           ("Stencil ID",          Float) = 0
        [HideInInspector] _StencilOp         ("Stencil Operation",   Float) = 0
        [HideInInspector] _StencilWriteMask  ("Stencil Write Mask",  Float) = 255
        [HideInInspector] _StencilReadMask   ("Stencil Read Mask",   Float) = 255
        [HideInInspector] _ColorMask         ("Color Mask",          Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "IgnoreProjector"   = "True"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        // UI 마스킹(Mask 컴포넌트)과 호환되는 Stencil 설정
        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull      Off
        Lighting  Off
        ZWrite    Off
        ZTest     [unity_GUIZTestMode]
        Blend     SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "SpeedLineUI"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma target   2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float  _LineThickness;
                float  _LineSpacing;
                float  _LineDirection;
                float  _ScrollX;
                float  _ScrollY;
                float  _UnscaledTime;
                float  _Alpha;
                float  _EdgeSoftness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv          = input.uv;
                output.color       = input.color;
                return output;
            }

            // 밴드 인덱스를 시드로 0~1 의사난수를 반환한다
            float Hash(float n)
            {
                return frac(sin(n * 127.1 + 311.7) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // ScrollX/Y 만큼 UV를 시간에 따라 이동시킨다 (_UnscaledTime은 timeScale=0에서도 진행)
                float2 scrolledUV = uv + float2(_ScrollX, _ScrollY) * _UnscaledTime;

                // LineDirection에 따라 밴드 축과 길이 축을 선택한다
                // 0(가로선): 밴드=Y, 길이=X / 1(세로선): 밴드=X, 길이=Y
                float bandUV   = lerp(scrolledUV.y, scrolledUV.x, _LineDirection);
                float lengthUV = lerp(scrolledUV.x, scrolledUV.y, _LineDirection);

                float bandIndex = floor(bandUV / _LineSpacing);
                float localBand = frac(bandUV / _LineSpacing);

                // 밴드별 랜덤 두께 (0 ~ _LineThickness)
                float thickness = Hash(bandIndex) * _LineThickness;

                // 두께 범위 밖은 투명, EdgeSoftness로 경계 부드럽게 처리
                float lineAlpha = 1.0 - smoothstep(
                    thickness - _EdgeSoftness,
                    thickness + _EdgeSoftness,
                    localBand
                );

                // 밴드별 랜덤 길이와 위상 → frac으로 타일링해 스크롤 시 무한 순환
                float lineLen = 0.3 + Hash(bandIndex + 57.3) * 0.7;
                float phase   = Hash(bandIndex + 113.7);
                float tiledX  = frac(lengthUV + phase);

                // 선 시작/끝을 부드럽게 페이드
                float xFade = 0.03;
                float xMask = smoothstep(0.0,     xFade,           tiledX)
                            * smoothstep(lineLen, lineLen - xFade,  tiledX);

                lineAlpha *= xMask;

                half4 finalColor = _Color * input.color;
                finalColor.a     = finalColor.a * lineAlpha * _Alpha;

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/InternalErrorShader"
}
