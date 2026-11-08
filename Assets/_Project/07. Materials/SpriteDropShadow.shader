// 스프라이트 모양만 한 색으로 칠하는 그림자 셰이더. 겹친 그림자는 스텐실로 한 번만 칠해 더 진해지지 않는다
// 울타리처럼 조각을 이어 붙인 벽은 조각마다 그림자를 그리면 이음매가 진하게 겹쳐 보여서다.
// 스텐실은 맨 위 비트(128)만 쓴다. 프로젝트에 스프라이트 마스크가 없지만, 생겨도 아래 비트와 섞이지 않게 하려는 것.
// URP 2D 기본 스프라이트 셰이더(Sprite-Unlit-Default)와 같은 방식으로 SpriteRenderer 색, 뒤집기를 받는다.
// (Unity 6은 렌더러 색을 정점 색이 아니라 unity_SpriteColor로 넘겨, 정점 색만 쓰면 그림자가 흰색으로 나왔다)
Shader "ExplodeIt/Sprite Drop Shadow"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        // 테두리를 흐리게 하는 폭 (화면 픽셀). 0이면 딱 떨어진다. 풀 울타리 그림자용(사용자 요청)
        _Softness ("Edge Softness (screen px)", Range(0, 16)) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Stencil
        {
            Ref 128
            ReadMask 128
            WriteMask 128
            Comp NotEqual
            Pass Replace
        }

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 ditherPos : TEXCOORD5;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Softness;
            CBUFFER_END

            // 흐린 테두리는 반투명 대신 점 무늬(4x4 디더)로 그린다. 반투명으로 그리면 스텐실이 먼저 그린 조각의 옅은 테두리를 지켜
            // 울타리 조각이 겹친 이음매에 밝은 줄이 생긴다. 점 무늬는 칠하거나 안 칠하거나 둘 중 하나라 겹쳐도 고르게 덮인다.
            // 무늬는 월드 좌표에 붙여, 카메라가 움직여도 점이 기어 다니지 않게 한다.
            static const float Bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            // 월드 1유닛에 찍는 점 수. 화면 픽셀(카메라 크기 11에서 1유닛 약 49px)에 가깝게 둔다.
            #define DITHER_PER_UNIT 48.0

            Varyings ShadowVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                o.ditherPos = TransformObjectToWorld(input.positionOS).xy * DITHER_PER_UNIT;
                return o;
            }

            float Coverage(float2 uv)
            {
                return step(0.5, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a);
            }

            half4 ShadowFragment(Varyings input) : SV_Target
            {
                // 반투명 가장자리까지 스텐실을 쓰면 테두리가 깎여 보여서, 절반 넘게 칠한 칸만 그림자로 친다
                float coverage = Coverage(input.uv);
                if (_Softness > 0)
                {
                    // 그림 안쪽 둘레 두 겹(16곳)을 더 봐서 가장자리일수록 덜 덮인 것으로 친다. 그림 밖으로는 그릴 수 없어 안쪽으로 흐려진다.
                    float2 dx = ddx(input.uv) * _Softness;
                    float2 dy = ddy(input.uv) * _Softness;
                    float sum = coverage;
                    [unroll] for (int k = 0; k < 8; k++)
                    {
                        float angle = k * 0.785398;
                        float2 dir = float2(cos(angle), sin(angle));
                        sum += Coverage(input.uv + dir.x * dx + dir.y * dy);
                        sum += Coverage(input.uv + (dir.x * dx + dir.y * dy) * 0.5);
                    }

                    coverage *= sum / 17.0;
                    int2 cell = (int2)floor(input.ditherPos) & 3;
                    clip(coverage - (Bayer[cell.y * 4 + cell.x] + 0.5) / 16.0);
                }
                else
                {
                    clip(coverage - 0.5);
                }

                return input.color;
            }
            ENDHLSL
        }
    }
}
