#pragma once


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "ToonLitInput.hlsl"
#include "SimpleToonFunctions.hlsl"


float3 TransformPositionWSToOutlinePositionWS(half vertexColorAlpha, float3 positionWS, float positionVS_Z, float3 normalWS)
{
    float outlineExpandAmount = vertexColorAlpha * _OutlineWidth * GetOutlineCameraFovAndDistanceFixMultiplier(positionVS_Z);
    return positionWS + normalWS * outlineExpandAmount;
}

#include "ToonLitLightingEquation.hlsl"

Varyings OutlinePassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    output.color = input.color;
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);

    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    output.normalWS = vertexNormalInput.normalWS;
    float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    float3 positionWS = vertexInput.positionWS;
    
    output.positionWSAndFogFactor = float4(positionWS, fogFactor);
    output.positionVS = TransformWorldToView(output.positionWSAndFogFactor.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWSAndFogFactor.xyz);

    // #ifdef ToonShaderIsOutline
    output.positionWSAndFogFactor.xyz = TransformPositionWSToOutlinePositionWS(input.color.a, output.positionWSAndFogFactor.xyz, output.positionVS.z, output.normalWS);
    // #endif
    output.positionCS = TransformWorldToHClip(output.positionWSAndFogFactor.xyz);
    float outlineZOffsetMaskTexExplictMipLevel = 0;
    float outlineZOffsetMask = tex2Dlod(_OutlineZOffsetMaskTex, float4(input.texcoord,0,outlineZOffsetMaskTexExplictMipLevel)).r; //we assume it is a Black/White texture

    // [Remap ZOffset texture value]
    // flip texture read value so default black area = apply ZOffset, because usually outline mask texture are using this format(black = hide outline)
    outlineZOffsetMask = 1-outlineZOffsetMask;
    output.positionCS = GetNewClipPosWithZOffset(output.positionCS, _OutlineZOffset * outlineZOffsetMask + 0.03 * _IsFace);

    
    float3 lightDirWS = normalize(_MainLightPosition.xyz);

    float lambert = dot(output.normalWS, lightDirWS);
    output.lambert = lambert * 0.5f + 0.5f;

    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.uv.zw = TRANSFORM_TEX(input.texcoord, _BloomMap);
    

    output.shadowCoord = TransformWorldToShadowCoord(output.positionWSAndFogFactor.xyz);
    return output;
}

            
float4 OutlinePassFragment(Varyings input): COLOR
{
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);
    half4 FinalColor = _OutlineColor * baseColor;
            
    return FinalColor;
}

Varyings ToonPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    output.color = input.color;
 
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);

    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    
    output.normalWS = vertexNormalInput.normalWS;
    float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    float3 positionWS = vertexInput.positionWS;
    
    output.positionWSAndFogFactor = float4(positionWS, fogFactor);
    output.positionVS = TransformWorldToView(output.positionWSAndFogFactor.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWSAndFogFactor.xyz);
    
    float3 lightDirWS = normalize(_MainLightPosition.xyz);

    float lambert = dot(output.normalWS, lightDirWS);
    output.lambert = lambert * 0.5f + 0.5f;

    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.uv.zw = TRANSFORM_TEX(input.texcoord, _BloomMap);

    output.shadowCoord = TransformWorldToShadowCoord(output.positionWSAndFogFactor.xyz);
    
    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    output.viewDirWS = viewDirWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(vertexNormalInput.tangentWS.xyz,sign);
    return output;
    
}
float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif

    return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);

    output.uv.xy = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    return 0;
}

half4 FragmentAlphaClip(Varyings input): SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    #if ENABLE_ALPHA_CLIPPING
        clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).b - _Cutoff);
    #endif
    return 0;
}

half4 GetFinalBaseColor(Varyings input)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
}

half3 GetFinalEmissionColor(Varyings input)
{    
    half4 Emission;
    Emission.rgb = _Emission * _EmissionColor.rgb;
    Emission.a = _EmissionBloomFactor;
    return Emission * SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv);
}

half4 GetFinalSpecular(Varyings input)
{
    Light mainLight = GetMainLight(input.shadowCoord);
    half4 LightMapColor = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, input.uv.xy);
    // Blinn-Phong
    half3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWSAndFogFactor.xyz);
    half3 halfViewLightWS = normalize(viewDirWS + mainLight.direction.xyz);
    half spec = pow(saturate(dot(input.normalWS, halfViewLightWS)), _Shininess);
    spec = smoothstep(0, _SpecSmoothness, spec - (1.0f - LightMapColor.b));
    half4 specularColor = _EnableSpecular * _LightSpecColor * _SpecMulti * LightMapColor.r * spec;
    
    #if ENABLE_SPECULAR_MAP
        half4 specularMap = SAMPLE_TEXTURE2D(_Set_HighColorMask, sampler_Set_HighColorMask, input.uv);
        specularColor *= specularMap;
    #endif
    return specularColor;
}

half4 GetFinalRimLight(Varyings input)
{
    Light mainLight = GetMainLight(input.shadowCoord);
    half3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - input.positionWSAndFogFactor.xyz);
    // Rim Light
    float lambertF = dot(mainLight.direction, input.normalWS);
    lambertF = max(0, lambertF);
    float rim = 1 - saturate(dot(viewDirWS, input.normalWS));
    
    float rimDot = pow(rim, _RimPow);
    rimDot = _EnableLambert * lambertF * rimDot + (1 - _EnableLambert) * rimDot;
    float rimIntensity = smoothstep(0, _RimSmooth, rimDot);
    half4 Rim = _EnableRim * pow(rimIntensity, 5) * _RimColor;
    Rim.a =  _RimColor.a;
    
    return Rim;
}

void DoClipTestToTargetAlphaValue(half alpha) 
{
    #if _UseAlphaClipping
    clip(alpha - _Cutoff);
    #endif
}

half3 SampleNormal(float2 uv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = 1.0h)
{
    #ifdef _NORMALMAP
    half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
    #if BUMP_SCALE_NOT_SUPPORTED
    return UnpackNormal(n);
    #else
    return UnpackNormalScale(n, scale);
    #endif
    #else
    return half3(0.0h, 0.0h, 1.0h);
    #endif
}

ToonSurfaceData InitializeSurfaceData(Varyings input)
{
    ToonSurfaceData output;

    // albedo & alpha
    float4 baseColorFinal = GetFinalBaseColor(input);
    output.albedo = baseColorFinal.rgb;
    output.alpha = baseColorFinal.a;
    DoClipTestToTargetAlphaValue(output.alpha);// early exit if possible

    // emission
    output.emission = GetFinalEmissionColor(input);

    output.specular = GetFinalSpecular(input);
    output.rimLight = GetFinalRimLight(input);
    output.normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);

    return output;
}

LightingData InitializeLightingData(Varyings input, half3 normalTS)
{
    LightingData lightingData;
    lightingData.positionWS = input.positionWSAndFogFactor.xyz;
    half3 viewDirWS = SafeNormalize(input.viewDirWS);
    lightingData.viewDirectionWS = viewDirWS;
    lightingData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - lightingData.positionWS);  
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    lightingData.normalWS =
        TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS
    .xyz));

    return lightingData;
}

half3 ShadeAllLight(Varyings input, ToonSurfaceData surfaceData, LightingData lightingData)
{
    half3 indirectResult = ShadeGI(surfaceData, lightingData);
    Light mainLight = GetMainLight(input.shadowCoord);

    float shadowMapPos = _ReceiveShadowMappingPosOffset + _IsFace;
    float3 shadowTestPosWS = lightingData.positionWS + mainLight.direction * shadowMapPos;
    #ifdef _MAIN_LIGHT_SHADOWS
        float4 shadowCoord = TransformWorldToShadowCoord(shadowTestPosWS);
        mainLight.shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
    #endif

    half3 mainLightResult = ShadeSingleLight(surfaceData, lightingData, mainLight, false);

    half3 additionalLightSumResult = 0;

    uint additionalLightsCount = GetAdditionalLightsCount();
    for (uint i = 0u; i < additionalLightsCount; ++i)
    {
        int perObjectLightIndex = GetPerObjectLightIndex(i);
        Light light = GetAdditionalPerObjectLight(perObjectLightIndex, lightingData.positionWS); // use original positionWS for lighting
        light.shadowAttenuation = AdditionalLightRealtimeShadow(perObjectLightIndex, shadowTestPosWS); // use offseted positionWS for shadow test

        additionalLightSumResult += ShadeSingleLight(surfaceData, lightingData, light, true);
    }

    half3 emission = ShadeEmission(surfaceData, lightingData);

    return CompositeAllLightResults(indirectResult, mainLightResult, additionalLightSumResult, emission, surfaceData, lightingData);
}

half3 SurfaceToOutlineColor(half3 originalSurfColor)
{
    return originalSurfColor * _OutlineColor;
}

half3 ApplyFog(half3 color, Varyings input)
{
    half fogFactor = input.positionWSAndFogFactor.w;
    color = MixFog(color, fogFactor);

    return color;
}

half4 ToonPassFragment(Varyings input): SV_TARGET{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    
    ToonSurfaceData surfaceData = InitializeSurfaceData(input);

    LightingData lightingData = InitializeLightingData(input, surfaceData.normalTS);

    half3 color = ShadeAllLight(input, surfaceData, lightingData);

    color = ApplyFog(color, input);

    return half4(color, surfaceData.alpha);
    
}

