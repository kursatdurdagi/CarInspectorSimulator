#ifndef BCG_TEXTURE_ARRAY_HDRP_INCLUDED
#define BCG_TEXTURE_ARRAY_HDRP_INCLUDED

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    float arrayIndex = round(input.texCoord1.x);
    float4 texColor = SAMPLE_TEXTURE2D_ARRAY(_TexArray, sampler_TexArray, input.texCoord0.xy, arrayIndex) * _Color;

    float3 normalWS = normalize(input.tangentToWorld[2]);
    float3 tangentWS = normalize(input.tangentToWorld[0]);

    ZERO_INITIALIZE(SurfaceData, surfaceData);
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
    surfaceData.baseColor = texColor.rgb;
    surfaceData.specularOcclusion = 1.0;
    surfaceData.normalWS = normalWS;
    surfaceData.perceptualSmoothness = _Glossiness;
    surfaceData.ambientOcclusion = 1.0;
    surfaceData.metallic = _Metallic;
    surfaceData.coatMask = 0.0;
    surfaceData.specularColor = DEFAULT_SPECULAR_VALUE.xxx;
    surfaceData.diffusionProfileHash = 0;
    surfaceData.subsurfaceMask = 0.0;
    surfaceData.thickness = 1.0;
    surfaceData.transmissionMask = 1.0;
    surfaceData.tangentWS = tangentWS;
    surfaceData.anisotropy = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;
    surfaceData.geomNormalWS = normalWS;
    surfaceData.ior = 1.5;
    surfaceData.transmittanceColor = 1.0;
    surfaceData.atDistance = 1.0;
    surfaceData.transmittanceMask = 0.0;

#ifdef DEBUG_DISPLAY
    ApplyDebugToSurfaceData(input.tangentToWorld, surfaceData);
#endif

    InitBuiltinData(posInput, texColor.a, normalWS, -normalWS, input.texCoord1, input.texCoord2, builtinData);
    builtinData.emissiveColor = 0.0;
    builtinData.depthOffset = 0.0;
    PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}

#endif
