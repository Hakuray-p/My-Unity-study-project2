Shader "New Gamer Card/Card Pearlescent Surface"
{
    Properties
    {
        _MainTex ("Card Alpha", 2D) = "white" {}
        _PearlTint ("Pearl Tint", Color) = (0.98, 0.99, 1, 0.98)
        _RainbowTint ("Rainbow Tint", Color) = (0.86, 0.97, 1, 0.9)
        _FrameIntensity ("Frame Intensity", Range(0, 1.5)) = 1.08
        _SurfaceIntensity ("Surface Intensity", Range(0, 1.5)) = 0.78
        _ArtIntensity ("Art Intensity", Range(0, 0.75)) = 0.3
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.82
        _TextureScale ("Texture Scale", Range(4, 80)) = 22
        _BandWidth ("Band Width", Range(0.05, 0.65)) = 0.4
        _SpectrumFrequency ("Spectrum Frequency", Range(0.25, 3)) = 1
        _ReflectionSharpness ("Reflection Sharpness", Range(0.5, 6)) = 1.35
        _IdleStrength ("Idle Strength", Range(0, 0.75)) = 0.38
        _EffectMode ("Effect Mode", Float) = 2
        _Rotation ("Rotation", Vector) = (0, 0, 0, 0)
        _LayerMode ("Layer Mode", Range(0, 1)) = 0
        _HueOffset ("Hue Offset", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PearlescentSurface"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _PearlTint;
            fixed4 _RainbowTint;
            float4 _Rotation;
            float _FrameIntensity;
            float _SurfaceIntensity;
            float _ArtIntensity;
            float _TextureStrength;
            float _TextureScale;
            float _BandWidth;
            float _SpectrumFrequency;
            float _ReflectionSharpness;
            float _IdleStrength;
            float _EffectMode;
            float _LayerMode;
            float _HueOffset;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed3 Spectrum(float hue)
            {
                float3 phase = float3(0.0, 0.333333, 0.666667);
                return 0.5 + 0.5 * cos(6.283185 * (hue + phase));
            }

            float2 Rotate2D(float2 value, float angle)
            {
                float sine;
                float cosine;
                sincos(angle, sine, cosine);
                return float2(
                    value.x * cosine - value.y * sine,
                    value.x * sine + value.y * cosine);
            }

            float PearlescentTexture(float2 uv, float phase)
            {
                float2 scaledUv = uv * max(_TextureScale, 1.0);
                float diagonal = sin((scaledUv.x * 1.07 + scaledUv.y * 0.53 + phase * 2.1) * 6.283185);
                float cross = sin((scaledUv.x * -0.31 + scaledUv.y * 0.93 - phase * 1.4) * 6.283185);
                return saturate(0.58 + diagonal * cross * 0.28);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 card = tex2D(_MainTex, input.uv);
                float frameMask = _LayerMode;
                float artMask = 1.0 - _LayerMode;
                float surfaceMask = (1.0 - _LayerMode) * 0.55;
                clip(card.a - 0.001);

                float2 rotation = clamp(_Rotation.xy, -1.0, 1.0);
                float active = saturate(_Rotation.z);
                float tiltAmount = smoothstep(0.01, 0.42, length(rotation));
                float response = saturate(max(_IdleStrength, max(active, tiltAmount)));

                float2 twirlCenter = float2(0.12, 0.5 + rotation.y * 0.45);
                float2 twirlDelta = input.uv - twirlCenter;
                float twirlAngle = length(twirlDelta) * 4.0 + rotation.x * 1.3;
                float2 twirledUv = Rotate2D(twirlDelta, twirlAngle) + twirlCenter + rotation * 0.2;
                float idlePhase = _Time.y * (0.055 + _EffectMode * 0.035);
                float spectrumPhase = (twirledUv.x + twirledUv.y * 0.86) * _SpectrumFrequency + idlePhase;

                float2 bandDirection = normalize(float2(0.72, 0.69) + float2(rotation.y, -rotation.x) * 0.32);
                float bandPosition = dot(input.uv - 0.5, bandDirection);
                float bandCenter = (rotation.x - rotation.y) * 0.34;
                float bandDistance = abs(bandPosition - bandCenter);
                float band = saturate(1.0 - bandDistance / max(_BandWidth, 0.001));
                band = pow(band, max(_ReflectionSharpness, 0.01));

                float luma = dot(card.rgb, fixed3(0.2126, 0.7152, 0.0722));
                float sourceResponse = lerp(0.72, 1.18, saturate((luma - 0.2) * 1.35));
                float texturePattern = PearlescentTexture(input.uv, spectrumPhase);
                float textureResponse = lerp(1.0, texturePattern, _TextureStrength);

                fixed3 spectrum = Spectrum(spectrumPhase + _HueOffset + luma * 0.12);
                spectrum = saturate(spectrum * _RainbowTint.rgb * 1.18 + 0.08);
                fixed3 pearlColor = lerp(_PearlTint.rgb, spectrum, saturate(_RainbowTint.a));

                float reflection = lerp(0.22, 1.0, band) * response;
                float frameAlpha = frameMask * _FrameIntensity * lerp(0.5, 1.0, band) * response;
                float surfaceAlpha = surfaceMask * _SurfaceIntensity * reflection * textureResponse;
                float artAlpha = artMask * _ArtIntensity * reflection * sourceResponse;

                if (_EffectMode < 1.5)
                {
                    pearlColor = lerp(_PearlTint.rgb, pearlColor, 0.62);
                }
                else
                {
                    float broadField = 0.5 + 0.5 * sin((spectrumPhase + _HueOffset + rotation.x * 0.35) * 6.283185);
                    float polychromeBody = lerp(0.58, 1.15, broadField);
                    surfaceAlpha *= polychromeBody;
                    artAlpha *= lerp(0.72, 1.1, broadField);
                    frameAlpha *= lerp(0.78, 1.15, broadField);
                }

                float alpha = saturate(frameAlpha + surfaceAlpha + artAlpha);
                alpha *= card.a * _PearlTint.a;
                return fixed4(pearlColor, alpha);
            }
            ENDCG
        }
    }
}
