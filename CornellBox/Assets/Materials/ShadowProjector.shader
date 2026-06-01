Shader "Custom/ShadowProjector"
{
    Properties
    {
        _ShadowTex ("Shadow Texture", 2D) = "white" {}
    }

    SubShader
    {
        // Render after transparent objects so the shadow composites over the photo sprite
        Tags { "Queue"="Transparent+1" }

        Pass
        {
            ZWrite Off
            ColorMask RGB
            Blend DstColor Zero  // multiply: darkens whatever is already rendered
            Offset -1, -1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _ShadowTex;
            float4x4 unity_Projector;
            float4x4 unity_ProjectorClip;

            struct appdata { float4 vertex : POSITION; };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float4 uvShadow : TEXCOORD0;
                float4 uvClip   : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.uvShadow = mul(unity_Projector,     v.vertex);
                o.uvClip   = mul(unity_ProjectorClip, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Behind the projector — no effect
                if (i.uvClip.w < 0) return fixed4(1, 1, 1, 1);

                // Outside the projection rectangle — no effect
                float2 projUV = i.uvShadow.xy / i.uvShadow.w;
                if (any(projUV < 0) || any(projUV > 1)) return fixed4(1, 1, 1, 1);

                return tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.uvShadow));
            }
            ENDCG
        }
    }
}
