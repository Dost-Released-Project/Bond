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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;
            fixed4    _SilhouetteColor;
            fixed     _BlendFactor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                // renderer.color는 vertex color로 전달된다 — 원본 틴트 및 알파 보존
                o.color    = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor   = tex2D(_MainTex, i.texcoord);
                fixed3 originalRGB = texColor.rgb * i.color.rgb;
                // _BlendFactor: 0=원본, 1=실루엣 색상
                fixed3 finalRGB   = lerp(originalRGB, _SilhouetteColor.rgb, _BlendFactor);
                // 텍스처 알파 × vertex 알파로 스프라이트 모양과 원본 투명도 유지
                return fixed4(finalRGB, texColor.a * i.color.a);
            }
            ENDCG
        }
    }
}
