Shader "JunoTimeline/SilhouetteOverride"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SilhouetteColor ("Silhouette Color", Color) = (0,0,0,1)
        _BlendFactor ("Blend Factor", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 color      : COLOR;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            half4  color       : COLOR;
            float2 uv          : TEXCOORD0;
        };

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4  _Color;
            half4  _SilhouetteColor;
            half   _BlendFactor;
        CBUFFER_END

        Varyings vert(Attributes v)
        {
            Varyings o;
            o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
            // renderer.color는 vertex color로 전달된다 — 원본 틴트 및 알파 보존
            o.color       = v.color * _Color;
            return o;
        }

        half4 frag(Varyings i) : SV_Target
        {
            half4 texColor    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            half3 originalRGB = texColor.rgb * i.color.rgb;
            // _BlendFactor: 0=원본, 1=실루엣 색상 단색
            half3 finalRGB    = lerp(originalRGB, _SilhouetteColor.rgb, _BlendFactor);
            // 텍스처 알파 x vertex 알파로 스프라이트 모양과 원본 투명도 유지
            return half4(finalRGB, texColor.a * i.color.a);
        }
        ENDHLSL

        // UniversalForward: URP 3D 렌더러 패스
        Pass
        {
            Name "SilhouetteForward"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }

        // Universal2D: URP 2D 렌더러 패스
        Pass
        {
            Name "SilhouetteUniversal2D"
            Tags { "LightMode" = "Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
    }
}
