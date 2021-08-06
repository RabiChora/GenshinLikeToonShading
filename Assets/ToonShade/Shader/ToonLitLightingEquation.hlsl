#pragma once
#include "ToonLitInput.hlsl"


half3 ShadeGI(ToonSurfaceData surfaceData, LightingData lightingData)
{
    half3 averageSH = SampleSH(0);

    averageSH = max(_ShadowMultColor,averageSH);
    return averageSH;
}

half MainLightRealtimeShadow_custom(float4 shadowCoord)
{
    #if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    return 1.0h;
    #endif

    #if SHADOWS_SCREEN
    return SampleScreenSpaceShadowmap(shadowCoord);
    #else
        ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
        half4 shadowParams = GetMainLightShadowParams();
        return SampleShadowmap(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowCoord, shadowSamplingData, shadowParams, false);
    #endif
}

// Lighting Equations, will be used for all directional lights
half3 ShadeSingleLight(ToonSurfaceData surfaceData, LightingData lightingData, Light light, bool isAdditionalLight)
{
    half3 N = lightingData.normalWS;
    half3 L = light.direction;

    half NoL = dot(N,L);

    half lightAttenuation = 1;

    // light's distance & angle fade for point light & spot light
    half distanceAttenuation = min(4,light.distanceAttenuation); 

    // N dot L applied
    half litOrShadowArea = smoothstep(_ShadowArea-_ShadowSmooth,_ShadowArea+_ShadowSmooth, NoL);

    // face ignore normalShade since it is usually very ugly using NoL on faces
    litOrShadowArea = _IsFace? lerp(0.5,1,litOrShadowArea) : litOrShadowArea;

    // Shadow map
    litOrShadowArea *= lerp(1,light.shadowAttenuation,_ReceiveShadowMappingAmount);

    half3 litOrShadowColor = lerp(_ShadowMapColor,1, litOrShadowArea);

    half3 lightAttenuationRGB = litOrShadowColor * distanceAttenuation;

    return saturate(light.color) * lightAttenuationRGB * (isAdditionalLight ? 1: 1);
}

half3 ShadeEmission(ToonSurfaceData surfaceData, LightingData lightingData)
{
    half3 emissionResult = lerp(surfaceData.emission, surfaceData.emission * surfaceData.albedo, _EmissionMulByBaseColor);
    return emissionResult;
}

// Putting everything together
half3 CompositeAllLightResults(half3 indirectResult, half3 mainLightResult, half3 additionalLightSumResult, half3 emissionResult, ToonSurfaceData surfaceData, LightingData lightingData)
{
    half3 rawLightSum = max(indirectResult, mainLightResult + additionalLightSumResult);
    half4 FinalColor;
    FinalColor.rgb = surfaceData.albedo * rawLightSum + emissionResult;

    Light light = GetMainLight();
    half3 N = lightingData.normalWS;
    half3 L = light.direction;

    half NoL = dot(N,L);
    half litOrShadowArea = smoothstep(_ShadowArea-_ShadowSmooth,_ShadowArea+_ShadowSmooth, NoL);
    half4 spec = surfaceData.specular * litOrShadowArea;
    half4 SpecDiffuse;
    SpecDiffuse.rgb = spec.rgb + FinalColor.rgb;
    SpecDiffuse.rgb *= _BaseColor.rgb;
    SpecDiffuse.a = spec.a * _BloomFactor * 10;

    FinalColor.rgb = SpecDiffuse + surfaceData.rimLight;
    FinalColor = _WorldLightInfluence * _MainLightColor * FinalColor + (1 - _WorldLightInfluence) * FinalColor;

    return FinalColor;
}
