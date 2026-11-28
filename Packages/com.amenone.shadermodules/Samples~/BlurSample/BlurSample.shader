Shader "Hidden/amenone_module/blur_sample"
{
    Properties
    {
        _BlurUpsampleOffset("Upsample Offset", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.amenone.shadermodules/Shaders/blur.cginc"

        float _BlurUpsampleOffset;
        ENDHLSL

        Pass
        {
            Name "Downsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDownsample

            float4 FragDownsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return blur_downsample_center5(_BlitTexture, sampler_LinearClamp, input.texcoord, _BlitTexture_TexelSize.xy);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Upsample"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragUpsample

            float4 FragUpsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return blur_upsample(_BlitTexture, sampler_LinearClamp, input.texcoord, _BlitTexture_TexelSize.xy, _BlurUpsampleOffset);
            }
            ENDHLSL
        }
    }
}
