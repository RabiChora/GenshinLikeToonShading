#pragma once
#include "ToonLitInput.hlsl"

half3 ShadeGI(ToonSurfaceData surfaceData, LightingData lightingData)
{
    // hide 3D feeling by ignoring all detail SH (leaving only the constant SH term)
    // we just want some average envi indirect color only
    half3 averageSH = SampleSH(0);

    // can prevent result becomes completely black if lightprobe was not baked 
    averageSH = max(_ShadowMultColor,averageSH);

    return averageSH;
}

// Most important part: lighting equation, edit it according to your needs, write whatever you want here, be creative!
// This function will be used by all direct lights (directional/point/spot)
half3 ShadeSingleLight(ToonSurfaceData surfaceData, LightingData lightingData, Light light, bool isAdditionalLight)
{
    half3 N = lightingData.normalWS;
    half3 L = light.direction;

    half NoL = dot(N,L);

    half lightAttenuation = 1;

    // light's distance & angle fade for point light & spot light (see GetAdditionalPerObjectLight(...) in Lighting.hlsl)
    // Lighting.hlsl -> https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl
    half distanceAttenuation = min(4,light.distanceAttenuation); //clamp to prevent light over bright if point/spot light too close to vertex

    // N dot L
    // simplest 1 line cel shade, you can always replace this line by your own method!
    half litOrShadowArea = smoothstep(_ShadowArea-_ShadowSmooth,_ShadowArea+_ShadowSmooth, NoL);

    // face ignore celshade since it is usually very ugly using NoL method
    litOrShadowArea = _IsFace? lerp(0.5,1,litOrShadowArea) : litOrShadowArea;

    // light's shadow map
    litOrShadowArea *= lerp(1,light.shadowAttenuation,_ReceiveShadowMappingAmount);

    half3 litOrShadowColor = lerp(_ShadowMapColor,1, litOrShadowArea);

    half3 lightAttenuationRGB = litOrShadowColor * distanceAttenuation;

    return saturate(light.color) * lightAttenuationRGB * (isAdditionalLight ? 1: 1);
}

half3 ShadeEmission(ToonSurfaceData surfaceData, LightingData lightingData)
{
    half3 emissionResult = lerp(surfaceData.emission, surfaceData.emission * surfaceData.albedo, _EmissionMulByBaseColor); // optional mul albedo
    return emissionResult;
}

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
