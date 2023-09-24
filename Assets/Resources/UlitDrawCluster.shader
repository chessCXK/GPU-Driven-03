Shader "Universal Render Pipeline/NPR" {
    Properties{
        _Color("Color Tint", Color) = (1, 1, 1, 1)
        [HDR] _BaseMap("_BaseMap", 2D) = "white" {}
        _Cutoff("_Cutoff", Range(0,1)) = 0.5
    }

        SubShader{
            Tags
            {
                "RenderPipeline" = "UniversalPipeline"
                "Queue" = "Transparent" 
                "RenderType" = "Transparent"
            }

            Pass {
                Cull Back
                ZWrite On

                Tags{"LightMode" = "UniversalForward"}
                HLSLPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
                //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariables.hlsl"
                //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInstancing.hlsl"
                #include "Shader/Common.cginc"

                float _ResultOffset;
                StructuredBuffer<InstanceBuffer> _InstanceBuffer;
                StructuredBuffer<VertexBuffer> _ClusterVertexData;
                Buffer<uint> _ClusterTriangleData;
                Buffer<uint2> _ResultTriange;//x：_ClusterTriangleData三角形第一个索引，y:_InstanceBuffer索引

                struct appdata {
                    uint id : SV_VertexID;
                    uint idx : SV_InstanceID;
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float3 worldPos : TEXCOORD0;
                    float3 worldNormal : TEXCOORD1;
                    float2 uv : TEXCOORD2;
                };
                half4 _Color;
                v2f vert(appdata i) {
                    v2f o;

                    uint2 resultTriange = _ResultTriange[_ResultOffset + i.idx];
                    uint index = _ClusterTriangleData[resultTriange.x + i.id];
                    VertexBuffer vb = _ClusterVertexData[index];
                    InstanceBuffer ib = _InstanceBuffer[resultTriange.y];

                    float3 vertex = vb.vertex;
                    float3 normal = vb.normal;
                    half2 uv0 = vb.uv0;
                    unity_ObjectToWorld = ib.worldMatrix;

                    //normal = normalize(normal);

                    VertexPositionInputs positionInputs = GetVertexPositionInputs(vertex);

                    o.vertex = positionInputs.positionCS;
                    o.worldPos = mul(unity_ObjectToWorld, float4(vertex, 1)).xyz;
                    o.worldNormal = mul(unity_ObjectToWorld, float4(normal, 0)).xyz;
                    o.uv = TRANSFORM_TEX(uv0, _BaseMap);
                    return o;
                }

                half4 frag(v2f i) : SV_Target{

                    half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _Color;
                    clip(baseMap.a - _Cutoff);
                    return baseMap;
                }

                ENDHLSL
            }
        }
            FallBack "Diffuse"
}