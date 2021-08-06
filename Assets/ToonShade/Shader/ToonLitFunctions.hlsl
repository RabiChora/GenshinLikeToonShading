#pragma once

//--------------------------
//Outline
//--------------------------
//Getting camera FOV
float getCameraFOV()
{
    float t = unity_CameraProjection._m11;
    float Rad2Deg = 180 / 3.1415;
    float fov = atan(1.0f / t) * 2.0 * Rad2Deg;
    return fov; 
}

float ApplyOutlineDistanceFadeOut(float input)
{
    return saturate(input);
}

float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVS)
{
    float cameraMultFix;

    if(unity_OrthoParams.w == 0)
    {
        cameraMultFix = abs(positionVS);
        cameraMultFix = ApplyOutlineDistanceFadeOut(cameraMultFix);
        cameraMultFix *= getCameraFOV();
    }
    else
    {
        float othoSize = abs(unity_OrthoParams.y);
        othoSize = ApplyOutlineDistanceFadeOut(othoSize);
        cameraMultFix = othoSize * 50;
    }
    return cameraMultFix * 0.00005;
}

//--------------------------
//ZOffset - adjust z offset for outlines
//--------------------------
float4 GetNewClipPosWithZOffset(float4 originalPositionCS, float viewSpaceZOffsetAmount)
{
    if(unity_OrthoParams.w == 0)
    {
        ////////////////////////////////
        //Perspective camera case
        ////////////////////////////////
        float2 ProjM_ZRow_ZW = UNITY_MATRIX_P[2].zw;
        float modifiedPositionVS_Z = -originalPositionCS.w + -viewSpaceZOffsetAmount; // push imaginary vertex
        float modifiedPositionCS_Z = modifiedPositionVS_Z * ProjM_ZRow_ZW[0] + ProjM_ZRow_ZW[1];
        originalPositionCS.z = modifiedPositionCS_Z * originalPositionCS.w / (-modifiedPositionVS_Z); // overwrite positionCS.z
        return originalPositionCS;    
    }
    else
    {
        ////////////////////////////////
        //Orthographic camera case
        ////////////////////////////////
        originalPositionCS.z += -viewSpaceZOffsetAmount / _ProjectionParams.z; // push imaginary vertex and overwrite positionCS.z
        return originalPositionCS;
    }
}

//--------------------------
// InvLerp, clamp, remap
//--------------------------
half invLerp(half start, half end, half value) 
{
    //linear smoothstep, not clamped
    return (value - start) / (end - start);
}
half invLerpClamp(half start, half end, half value)
{
    return saturate(invLerp(start,end,value));
}
// full control remap, but slower
half remap(half origStart, half origEnd, half targetStart, half targetEnd, half value)
{
    half rel = invLerp(origStart, origEnd, value);
    return lerp(targetStart, targetEnd, rel);
}

//Transform for Outlines
float3 TransformPositionWSToOutlinePositionWS(half vertexColorAlpha, float3 positionWS, float positionVS_Z, float3 normalWS)
{
    float outlineExpandAmount = vertexColorAlpha * _OutlineWidth * GetOutlineCameraFovAndDistanceFixMultiplier(positionVS_Z);
    return positionWS + normalWS * outlineExpandAmount;
}
