Shader "UI/SecurityCamera"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Look)]
        _Gray ("Grayscale Amount", Range(0, 1)) = 0.5
        _GrayTint ("Gray Tint", Color) = (0.85, 0.95, 0.92, 1)
        _Vignette ("Vignette", Range(0, 1)) = 0.35
        _ChromaShift ("Colour Split", Range(0, 0.01)) = 0.0015

        [Header(CCTV)]
        _ScanlineCount ("Scanline Count", Float) = 320
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.12
        _RollSpeed ("Rolling Bar Speed", Float) = 0.12
        _RollStrength ("Rolling Bar Strength", Range(0, 0.5)) = 0.06
        _Noise ("Noise", Range(0, 0.5)) = 0.06
        _Flicker ("Flicker", Range(0, 0.3)) = 0.03

        // Standard UI masking support
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float _Gray;
            fixed4 _GrayTint;
            float _Vignette;
            float _ChromaShift;
            float _ScanlineCount;
            float _ScanlineStrength;
            float _RollSpeed;
            float _RollStrength;
            float _Noise;
            float _Flicker;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float hash (float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.texcoord;
                float t = _Time.y;

                // Slight colour split: red and blue sampled a little apart
                float2 shift = float2(_ChromaShift, 0);
                fixed4 col = tex2D(_MainTex, uv) + _TextureSampleAdd;
                col.r = tex2D(_MainTex, uv + shift).r + _TextureSampleAdd.r;
                col.b = tex2D(_MainTex, uv - shift).b + _TextureSampleAdd.b;

                // Partial grayscale with a cold tint
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(col.rgb, lum * _GrayTint.rgb, _Gray);

                // Scanlines
                col.rgb *= 1.0 - _ScanlineStrength * (0.5 + 0.5 * sin(uv.y * _ScanlineCount * 3.14159));

                // Rolling bright bar, moving top to bottom
                float roll = frac(uv.y + t * _RollSpeed) - 0.5;
                col.rgb += _RollStrength * exp(-roll * roll * 400.0);

                // Grain
                col.rgb += (hash(uv * 512.0 + frac(t) * 37.0) - 0.5) * _Noise;

                // Flicker
                col.rgb *= 1.0 - _Flicker * hash(float2(floor(t * 24.0), 3.7));

                // Vignette
                float2 d = uv - 0.5;
                col.rgb *= saturate(1.0 - _Vignette * dot(d, d) * 4.0);

                col.rgb = saturate(col.rgb);
                col *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}