Shader "Custom/TransparentReceiveShadow"
{
    Properties
    {
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.6
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Cull Off
        ZWrite Off

        CGPROGRAM
        #pragma surface surf ShadowOnly alpha:fade fullforwardshadows
        #pragma target 3.0

        float _ShadowIntensity;
        fixed4 _ShadowColor;

        struct Input { float2 uv_MainTex; };

        // atten = shadow attenuation from Unity (1 = lit, 0 = fully in shadow)
        half4 LightingShadowOnly(SurfaceOutput s, half3 lightDir, half atten)
        {
            return half4(_ShadowColor.rgb, (1.0 - atten) * _ShadowIntensity);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = 0;
            o.Alpha = 0;
        }
        ENDCG
    }

    Fallback "Diffuse"
}
