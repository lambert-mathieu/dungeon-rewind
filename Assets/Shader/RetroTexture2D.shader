Shader "Retro/SpriteRetro"
{
    Properties
    {
        [PerRendererData] [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Pixelation)]
        _PixelResolutionX ("Pixel Resolution X", Range(8, 1024)) = 128
        _PixelResolutionY ("Pixel Resolution Y", Range(8, 1024)) = 128

        [Header(Color)]
        _ColorSteps ("Color Steps", Range(2, 64)) = 12
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.25

        [Header(Optional Retro Effects)]
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 2)) = 1

        [Header(Transparency)]
        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0

        [Header(Sprite Tint)]
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SpriteRetro"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)

                float4 _MainTex_ST;

                float _PixelResolutionX;
                float _PixelResolutionY;

                float _ColorSteps;
                float _DitherStrength;

                float _ScanlineStrength;

                float _Brightness;
                float _Contrast;

                float _AlphaClip;

                float4 _Color;

            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            // --------------------------------------------------------
            // VERTEX SHADER
            // --------------------------------------------------------

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                // Keep the sprite's supplied UVs.
                // This is important for SpriteRenderer and Sprite Atlas use.
                output.uv = input.uv;

                // SpriteRenderer color/tint comes through vertex color.
                output.color = input.color * _Color;

                return output;
            }


            // --------------------------------------------------------
            // 4x4 BAYER DITHER
            // --------------------------------------------------------

            float Bayer4x4(float2 pixelPosition)
            {
                int x = (int)fmod(pixelPosition.x, 4.0);
                int y = (int)fmod(pixelPosition.y, 4.0);

                float value = 0.0;

                if (y == 0)
                {
                    if      (x == 0) value = 0;
                    else if (x == 1) value = 8;
                    else if (x == 2) value = 2;
                    else             value = 10;
                }
                else if (y == 1)
                {
                    if      (x == 0) value = 12;
                    else if (x == 1) value = 4;
                    else if (x == 2) value = 14;
                    else             value = 6;
                }
                else if (y == 2)
                {
                    if      (x == 0) value = 3;
                    else if (x == 1) value = 11;
                    else if (x == 2) value = 1;
                    else             value = 9;
                }
                else
                {
                    if      (x == 0) value = 15;
                    else if (x == 1) value = 7;
                    else if (x == 2) value = 13;
                    else             value = 5;
                }

                return value / 16.0 - 0.5;
            }


            // --------------------------------------------------------
            // PIXELATION
            // --------------------------------------------------------

            float2 PixelateUV(float2 uv)
            {
                float2 resolution =
                    float2(
                        max(_PixelResolutionX, 1.0),
                        max(_PixelResolutionY, 1.0)
                    );

                // Snap UVs to a virtual low-resolution grid.
                float2 pixelUV =
                    (floor(uv * resolution) + 0.5)
                    / resolution;

                return pixelUV;
            }


            // --------------------------------------------------------
            // COLOR QUANTIZATION
            // --------------------------------------------------------

            float3 QuantizeColor(
                float3 color,
                float2 pixelPosition
            )
            {
                float steps =
                    max(_ColorSteps, 2.0);

                float dither =
                    Bayer4x4(pixelPosition);

                color +=
                    dither *
                    _DitherStrength /
                    steps;

                color =
                    floor(
                        color * steps + 0.5
                    )
                    / steps;

                return saturate(color);
            }


            // --------------------------------------------------------
            // CONTRAST
            // --------------------------------------------------------

            float3 ApplyContrast(float3 color)
            {
                return
                    (color - 0.5) *
                    _Contrast +
                    0.5;
            }


            // --------------------------------------------------------
            // FRAGMENT SHADER
            // --------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                float2 originalUV =
                    input.uv;


                // ------------------------------------------------
                // 1. PIXELATE UV
                // ------------------------------------------------

                float2 pixelUV =
                    PixelateUV(originalUV);


                // ------------------------------------------------
                // 2. SAMPLE SPRITE TEXTURE
                // ------------------------------------------------

                half4 col =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        pixelUV
                    );


                // ------------------------------------------------
                // 3. APPLY SPRITERENDERER TINT
                // ------------------------------------------------

                col *= input.color;


                // ------------------------------------------------
                // 4. TRANSPARENCY
                // ------------------------------------------------

                clip(col.a - _AlphaClip);


                // ------------------------------------------------
                // 5. PIXEL COORDINATE FOR DITHERING
                // ------------------------------------------------

                float2 pixelPosition =
                    floor(
                        originalUV *
                        float2(
                            _PixelResolutionX,
                            _PixelResolutionY
                        )
                    );


                // ------------------------------------------------
                // 6. DITHER + COLOR QUANTIZATION
                // ------------------------------------------------

                col.rgb =
                    QuantizeColor(
                        col.rgb,
                        pixelPosition
                    );


                // ------------------------------------------------
                // 7. CONTRAST
                // ------------------------------------------------

                col.rgb =
                    ApplyContrast(col.rgb);


                // ------------------------------------------------
                // 8. BRIGHTNESS
                // ------------------------------------------------

                col.rgb *= _Brightness;


                // ------------------------------------------------
                // 9. OPTIONAL SCANLINES
                // ------------------------------------------------

                float scanline =
                    sin(
                        pixelPosition.y *
                        3.14159265
                    );

                scanline =
                    scanline * 0.5 + 0.5;

                col.rgb *=
                    1.0 -
                    scanline *
                    _ScanlineStrength;


                col.rgb =
                    saturate(col.rgb);


                return col;
            }

            ENDHLSL
        }
    }
}