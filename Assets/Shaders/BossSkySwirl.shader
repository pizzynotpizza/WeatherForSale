// Weather for Sale - storm sky of the Tempest arena (story 008), drawn on the inside of a big sphere.
// Matches the "Tempest appears" CGI: a dark purple storm. When _StormTex is set (a storm-cloud painting
// with its vortex in the middle) it is projected from straight overhead, so the vortex sits above the arena;
// the texture's wrap mode should be Mirror so its edges never show a seam. Towards the horizon and below it
// the colour fades into the storm gradient. _Calm 0 = storm, 1 = clear blue sky (set when the boss is sealed).
// Target: URP, WebGL / mobile; one texture sample per pixel, no branches.
Shader "WeatherForSale/BossSkySwirl"
{
    Properties
    {
        [Header(Storm)]
        _StormTex ("Storm panorama (wide)", 2D) = "black" {}
        _StormTexStrength ("Panorama strength (0 = gradient only)", Range(0, 1)) = 1
        _StormTop ("Storm colour overhead", Color) = (0.10, 0.05, 0.20, 1)
        _StormHorizon ("Storm colour at the horizon", Color) = (0.42, 0.22, 0.58, 1)
        _StormBelow ("Storm colour below the horizon", Color) = (0.08, 0.05, 0.14, 1)
        _Spread ("Painting spread (bigger = vortex smaller)", Range(0.05, 2)) = 0.42
        _HorizonFade ("Sky height where the painting fades out", Range(0, 0.5)) = 0.06
        [Header(Calm)]
        _Calm ("Calm (0 storm, 1 clear)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" "RenderPipeline" = "UniversalPipeline" }
        Cull Front
        ZWrite Off

        Pass
        {
            Name "Sky"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_StormTex);
            SAMPLER(sampler_StormTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _StormTex_ST;
                half _StormTexStrength;
                half4 _StormTop;
                half4 _StormHorizon;
                half4 _StormBelow;
                float _Spread;
                float _HorizonFade;
                half _Calm;
            CBUFFER_END

            // Keeps the overhead projection finite at the horizon (it would run to infinity at d.y = 0).
            static const float HorizonLift = 0.28;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.dir = input.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);

                // Storm gradient: dark overhead, purple glow at the horizon, near-black below.
                half up = saturate(d.y);
                half3 gradient = lerp(_StormHorizon.rgb, _StormTop.rgb, up * up * (3.0 - 2.0 * up));
                gradient = lerp(gradient, _StormBelow.rgb, saturate(-d.y * 4.0));

                // Overhead projection: straight up is the middle of the painting (the vortex).
                float2 uv = 0.5 + d.xz / (max(d.y, 0.0) + HorizonLift) * _Spread;
                half3 pano = SAMPLE_TEXTURE2D(_StormTex, sampler_StormTex, uv * _StormTex_ST.xy + _StormTex_ST.zw).rgb;
                half band = smoothstep(0.0, _HorizonFade + 0.2, d.y) * _StormTexStrength;
                half3 storm = lerp(gradient, pano, band);

                half3 clear = lerp(half3(0.78, 0.92, 1.0), half3(0.10, 0.50, 1.0), saturate(d.y * 1.4 + 0.15)); // bright blue
                return half4(lerp(storm, clear, _Calm), 1);
            }
            ENDHLSL
        }
    }
}
