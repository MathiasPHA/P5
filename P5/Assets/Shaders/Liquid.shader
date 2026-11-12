Shader "Custom/Liquid"
{
    Properties
    {
        _Color ("Liquid Color", Color) = (0.96, 0.95, 0.91, 1)
        _TopColor ("Surface Color", Color) = (1, 1, 1, 1)
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.25
        _Fill ("Fill Height (world units from pivot)", Float) = 0
        [HideInInspector] _WobbleX ("Wobble X", Float) = 0
        [HideInInspector] _WobbleZ ("Wobble Z", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            // Draw both sides: the back faces become the flat liquid surface.
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos       : SV_POSITION;
                float  fillEdge  : TEXCOORD0;
                float3 worldNorm : TEXCOORD1;
                float3 viewDir   : TEXCOORD2;
            };

            fixed4 _Color;
            fixed4 _TopColor;
            fixed4 _RimColor;
            float _RimPower;
            float _RimStrength;
            float _Fill;
            float _WobbleX;
            float _WobbleZ;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 pivot    = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float3 rel      = worldPos - pivot;

                // Height above the pivot in world space, tilted by the wobble.
                // World space keeps the surface level when the bottle rotates.
                o.fillEdge = rel.y + rel.x * _WobbleX + rel.z * _WobbleZ;

                o.worldNorm = UnityObjectToWorldNormal(v.normal);
                o.viewDir   = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                // Throw away everything above the liquid level.
                clip(_Fill - i.fillEdge);

                // Back faces seen through the cut = the top of the liquid.
                if (facing < 0)
                    return _TopColor;

                float3 n = normalize(i.worldNorm);
                float shade = 0.85 + 0.15 * n.y;
                float rim = pow(1.0 - saturate(dot(n, normalize(i.viewDir))), _RimPower);

                fixed4 col = _Color * shade;
                col.rgb = lerp(col.rgb, _RimColor.rgb, rim * _RimStrength);
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
