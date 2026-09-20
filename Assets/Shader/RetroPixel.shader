Shader "Retro/RetroPixel"
{
    Properties
    {
        [Header(Pixelation)]
        _PixelSize ("Pixel Size", Range(1, 32)) = 4

        [Header(Color)]
        _ColorSteps ("Color Steps", Range(2, 64)) = 16
        _DitherStrength ("Dither Strength", Range(0, 1)) = 0.15

        [Header(CRT)]
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.08
        _ScanlineFrequency ("Scanline Frequency", Range(0.25, 4)) = 1

        _ChromaticAberration ("Chromatic Aberration", Range(0, 10)) = 0

        _Curvature ("Screen Curvature", Range(0, 0.5)) = 0

        _Vignette ("Vignette", Range(0, 1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "RetroPixelPass"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            // Unity URP core helpers.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Gives us:
            // - Vert
            // - Varyings
            // - _BlitTexture
            // - sampler_LinearClamp / sampler_LinearRepeat
            // - _BlitMipLevel
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


            // ------------------------------------------------------------
            // MATERIAL PARAMETERS
            // ------------------------------------------------------------

            float _PixelSize;

            float _ColorSteps;
            float _DitherStrength;

            float _ScanlineStrength;
            float _ScanlineFrequency;

            float _ChromaticAberration;
            float _Curvature;
            float _Vignette;


            // ------------------------------------------------------------
            // CRT CURVATURE
            // ------------------------------------------------------------

            float2 ApplyCurvature(float2 uv)
            {
                // Convert:
                //
                // 0 -> 1
                //
                // into:
                //
                // -1 -> 1

                float2 centeredUV = uv * 2.0 - 1.0;

                // Push the image outward based on distance
                // from the centre.

                float2 offset =
                    centeredUV.yx *
                    centeredUV.yx *
                    _Curvature;

                centeredUV *= 1.0 + offset;

                // Convert back to 0 -> 1.

                return centeredUV * 0.5 + 0.5;
            }


            // ------------------------------------------------------------
            // PIXELATION
            // ------------------------------------------------------------

            float2 PixelateUV(float2 uv)
            {
                // _ScreenParams.xy is the screen resolution.
                //
                // Example:
                //
                // 1920 x 1080
                //
                // PixelSize = 4 gives an effective grid around:
                //
                // 480 x 270

                float2 pixelCount =
                    _ScreenParams.xy / max(_PixelSize, 1.0);

                // Snap UV coordinates onto the low-resolution grid.

                float2 pixelUV =
                    (floor(uv * pixelCount) + 0.5)
                    / pixelCount;

                return pixelUV;
            }


            // ------------------------------------------------------------
            // 4x4 BAYER DITHER
            // ------------------------------------------------------------

            float Bayer4x4(float2 position)
            {
                int x = (int)fmod(position.x, 4.0);
                int y = (int)fmod(position.y, 4.0);

                // Bayer 4x4 matrix:
                //
                //  0  8  2 10
                // 12  4 14  6
                //  3 11  1  9
                // 15  7 13  5

                float value = 0.0;

                if (y == 0)
                {
                    if      (x == 0) value = 0.0;
                    else if (x == 1) value = 8.0;
                    else if (x == 2) value = 2.0;
                    else             value = 10.0;
                }
                else if (y == 1)
                {
                    if      (x == 0) value = 12.0;
                    else if (x == 1) value = 4.0;
                    else if (x == 2) value = 14.0;
                    else             value = 6.0;
                }
                else if (y == 2)
                {
                    if      (x == 0) value = 3.0;
                    else if (x == 1) value = 11.0;
                    else if (x == 2) value = 1.0;
                    else             value = 9.0;
                }
                else
                {
                    if      (x == 0) value = 15.0;
                    else if (x == 1) value = 7.0;
                    else if (x == 2) value = 13.0;
                    else             value = 5.0;
                }

                // Convert 0-15 into approximately -0.5 -> +0.5.

                return (value / 16.0) - 0.5;
            }


            // ------------------------------------------------------------
            // COLOR QUANTIZATION + DITHERING
            // ------------------------------------------------------------

            float3 QuantizeColor(
                float3 color,
                float2 pixelPosition
            )
            {
                float steps = max(_ColorSteps, 2.0);

                // Get Bayer threshold.

                float dither = Bayer4x4(pixelPosition);

                // Apply a tiny color offset before quantization.
                //
                // This breaks gradients into patterned pixels instead
                // of large ugly color bands.

                color +=
                    dither *
                    _DitherStrength /
                    steps;

                // Reduce the number of possible colors.

                color =
                    floor(color * steps + 0.5)
                    / steps;

                return saturate(color);
            }


            // ------------------------------------------------------------
            // CHROMATIC ABERRATION
            // ------------------------------------------------------------

            float3 SampleChromaticAberration(float2 uv)
            {
                // Convert pixel offset into UV offset.

                float2 texel =
                    1.0 / _ScreenParams.xy;

                float2 offset =
                    float2(_ChromaticAberration, 0.0)
                    * texel;

                // Clamp so the screen doesn't wrap around at the edges.

                float2 redUV   = saturate(uv + offset);
                float2 greenUV = saturate(uv);
                float2 blueUV  = saturate(uv - offset);


                float r =
                    SAMPLE_TEXTURE2D_X_LOD(
                        _BlitTexture,
                        sampler_LinearClamp,
                        redUV,
                        _BlitMipLevel
                    ).r;


                float g =
                    SAMPLE_TEXTURE2D_X_LOD(
                        _BlitTexture,
                        sampler_LinearClamp,
                        greenUV,
                        _BlitMipLevel
                    ).g;


                float b =
                    SAMPLE_TEXTURE2D_X_LOD(
                        _BlitTexture,
                        sampler_LinearClamp,
                        blueUV,
                        _BlitMipLevel
                    ).b;


                return float3(r, g, b);
            }


            // ------------------------------------------------------------
            // SCANLINES
            // ------------------------------------------------------------

            float3 ApplyScanlines(
                float3 color,
                float2 screenPosition
            )
            {
                // Produce alternating horizontal brightness.

                float scanline =
                    sin(
                        screenPosition.y *
                        3.14159265 *
                        _ScanlineFrequency
                    );

                // Convert -1 -> 1 into 0 -> 1.

                scanline =
                    scanline * 0.5 + 0.5;

                // Darken rows.

                float darkness =
                    1.0 -
                    scanline *
                    _ScanlineStrength;

                return color * darkness;
            }


            // ------------------------------------------------------------
            // VIGNETTE
            // ------------------------------------------------------------

            float3 ApplyVignette(
                float3 color,
                float2 uv
            )
            {
                float2 centered =
                    uv * 2.0 - 1.0;

                float distanceFromCenter =
                    dot(centered, centered);

                float vignette =
                    1.0 -
                    distanceFromCenter *
                    _Vignette;

                return color * saturate(vignette);
            }


            // ------------------------------------------------------------
            // FRAGMENT SHADER
            // ------------------------------------------------------------

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Original fullscreen UV.

                float2 originalUV =
                    input.texcoord.xy;


                // --------------------------------------------------------
                // 1. CRT CURVATURE
                // --------------------------------------------------------

                float2 uv =
                    ApplyCurvature(originalUV);


                // If curvature pushes us outside the screen,
                // render black.

                if (
                    uv.x < 0.0 ||
                    uv.x > 1.0 ||
                    uv.y < 0.0 ||
                    uv.y > 1.0
                )
                {
                    return half4(0, 0, 0, 1);
                }


                // --------------------------------------------------------
                // 2. PIXELATION
                // --------------------------------------------------------

                uv = PixelateUV(uv);


                // --------------------------------------------------------
                // 3. SAMPLE CAMERA IMAGE
                // --------------------------------------------------------

                float3 color =
                    SampleChromaticAberration(uv);


                // --------------------------------------------------------
                // 4. DITHER + COLOR QUANTIZATION
                // --------------------------------------------------------

                float2 virtualPixelPosition =
                    floor(
                        originalUV *
                        (_ScreenParams.xy / max(_PixelSize, 1.0))
                    );

                color =
                    QuantizeColor(
                        color,
                        virtualPixelPosition
                    );


                // --------------------------------------------------------
                // 5. CRT SCANLINES
                // --------------------------------------------------------

                float2 screenPosition =
                    originalUV *
                    _ScreenParams.xy;

                color =
                    ApplyScanlines(
                        color,
                        screenPosition
                    );


                // --------------------------------------------------------
                // 6. VIGNETTE
                // --------------------------------------------------------

                color =
                    ApplyVignette(
                        color,
                        originalUV
                    );


                return half4(color, 1.0);
            }

            ENDHLSL
        }
    }
}