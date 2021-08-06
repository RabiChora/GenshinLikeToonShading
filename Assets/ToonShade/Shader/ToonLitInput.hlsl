#pragma once
//The inputs of the shader, includes declaration of Attributes and Varyings, and the Alpha functions
struct Attributes
{
    float3 positionOS: POSITION;
    half4 color: COLOR0;
    half3 normalOS: NORMAL;
    half4 tangentOS: TANGENT;
    float2 texcoord: TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS: POSITION;
    float4 color: COLOR0;
    float4 uv: TEXCOORD0;
    float4 positionWSAndFogFactor: TEXCOORD1;
    float3 positionVS: TEXCOORD2;
    float3 normalWS: TEXCOORD3;
    float lambert: TEXCOORD4;
    float4 shadowCoord: TEXCOORD5;
    float3 viewDirWS : TEXCOORD6;
    float4 tangentWS   : TEXCOORD7;
    float4 positionSS  : TEXCOORD8;
 
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

TEXTURE2D(_BaseMap);                  SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap);                  SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);              SAMPLER(sampler_EmissionMap);
TEXTURE2D(_LightMap);                 SAMPLER(sampler_LightMap);
TEXTURE2D(_FaceShadowMap);            SAMPLER(sampler_FaceShadowMap);
TEXTURE2D(_RampMap);                  SAMPLER(sampler_RampMap);
TEXTURE2D(_BloomMap);                 SAMPLER(sampler_BloomMap);
TEXTURE2D(_Set_HighColorMask);        SAMPLER(sampler_Set_HighColorMask);
TEXTURE2D(_CameraDepthTexture);       SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_RimMask);                  SAMPLER(sampler_RimMask);
sampler2D _OutlineZOffsetMaskTex;

CBUFFER_START(UnityPerMaterial)

float _IsFace;

float4 _BaseMap_ST;
float4 _BumpMap_ST;
float _BumpScale;
float4 _BaseColor;
half _WorldLightInfluence;
float4 _Set_HighColorMask_ST;

float4 _LightMap_ST;
float4 _FaceShadowMap_ST;
float _FaceShadowMapPow;
float _FaceShadowOffset;

uniform float4 _HighColor;
uniform sampler2D _HighColor_Tex; uniform float4 _HighColor_Tex_ST;
uniform float _HighColor_Power;
uniform half _Is_UseTweakHighColorOnShadow;
uniform float _TweakHighColorOnShadow;

float3 _ShadowMultColor;
float _ShadowArea;
half _ShadowSmooth;
float _ShadowColor;

float _EnableSpecular;
float4 _LightSpecColor;
float _Shininess, _SpecSmoothness;
float _SpecMulti;
float _FixDarkShadow;

float _EnableLambert;
float _EnableRim;
half4 _RimColor;
float _RimSmooth;
float _RimWidth;
half4 _RimMask_ST;
float _RimLightBlend;
float _RimLightBlendPoint;

float4 _BloomMap_ST;
float _BloomFactor;
float _EnableEmission;
half3 _EmissionColor;
float _Emission;
float _EmissionBloomFactor;
half _EmissionMulByBaseColor;
half3 _EmissionMapChannelMask;

half _ReceiveShadowMappingAmount;
float _ReceiveShadowMappingPosOffset;
half4 _ShadowMapColor;

float _OutlineWidth;
half4 _OutlineColor;
float _OutlineZOffset;

half _Cutoff;
CBUFFER_END

float3 _LightDirection;

struct ToonSurfaceData
{
    half3   albedo;
    half    alpha;
    half3   emission;
    half4   specular;
    half4   rimLight;
    half3   normalTS;
    half4   shadowCoord;
};

struct LightingData
{
    float3  positionWS;
    half3   normalWS;
    half3   viewDirectionWS;
    float4  shadowCoord;
    half    fogCoord;
    half3   vertexLighting;
    half3   bakedGI;
    float2  normalizedScreenSpaceUV;
    half4   shadowMask;
};

half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    #if !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A) && !defined(_GLOSSINESS_FROM_BASE_ALPHA)
    half alpha = albedoAlpha * color.a;
    #else
    half alpha = color.a;
    #endif

    #if defined(_ALPHATEST_ON)
    clip(alpha - cutoff);
    #endif

    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv);
}

