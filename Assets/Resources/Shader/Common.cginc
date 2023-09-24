#ifndef COMMON
#define COMMON

struct InstanceBuffer
{
    float4x4 worldMatrix;
    float4x4 worldInverseMatrix;
};

struct InstanceData
{
    half3 center;
    half3 extends;
    uint instanceIndex;
    uint instanceKindIndex;
    uint clusterIndex;
};

struct InstanceKindData
{
    uint argsIndex;
    uint kindResultStart;
    uint lodNum;
    uint elementNum;
    half4 lodRelative;

    uint argsShadowIndex;
    uint shadowLODLevel;
    uint kindShadowResultStart;
};

struct ClusterData
{
    //lod0在ChunkLODData开始位置
    uint chunkLODStartIndex;

    //每级LOD的chunkData有多少个
    uint chunkLODDataCount;

    //lod0在ClusterChunkData开始位置
    uint chunkStartIndex;

    //当前类型instanceData中的位置
    uint instanceIndex;

    //当前类型每种LOD的最终三角形索引buffer的起始位置
    half4 triangleLODResultData;
};

struct ClusterChunkLODData
{
    half3 center;

    half3 extends;

    //三角形索引在buffer开始位置
    uint triangleStart;

    //三角形数量
    uint triangleNum;
};

struct VertexBuffer
{
    half3 vertex;

    half3 normal;

    half4 color;

    half4 tangent;

    half2 uv0;

    half2 uv1;
};

static const half3 axisMuti[8] =
{
    half3(1, 1, 1),
    half3(1, 1, -1),
    half3(1, -1, 1),
    half3(1, -1, -1),
    half3(-1, 1, 1),
    half3(-1, 1, -1),
    half3(-1, -1, 1),
    half3(-1, -1, -1)
};

half angle(half3 a, half3 b)
{
    half dotProduct = dot(normalize(a), normalize(b));
    half angle = acos(dotProduct) * (180.0 / 3.1415926);
    return angle;
}

half FrustumCull(half3 center, half3 extents, float4 _FrustumPlanes[6])
{
    [unroll]
    for (uint i = 0; i < 6; i++)
    {
        half4 plane = _FrustumPlanes[i];
        half3 normal = plane.xyz;
        half dist = dot(normal, center) + plane.w;
        half radius = dot(extents, abs(normal));
        if (dist <= -radius)
        {
            return 1;
        }
    }
    return 0;
}

half GetRelativeHeight(half3 center, half maxSideSize, half3 cameraPos, half fieldOfView, half lodBias)
{
    half preRelative;
    half halfAngle = tan(0.0174532924F * fieldOfView * 0.5f);
    preRelative = 0.5f / halfAngle;
    preRelative = preRelative * lodBias;

    half dis = distance(center, cameraPos);
    half relativeHeight = maxSideSize * preRelative / dis;
    return relativeHeight;
}

uint CalculateLODLevel(half4 lodRelative, half3 center, half maxSideSize, half3 cameraPos, half fieldOfView, half lodBias)
{
    half relative = GetRelativeHeight(center, maxSideSize, cameraPos, fieldOfView, lodBias);

    uint lodLevel = 4;

    lodLevel -= uint(lodRelative.w < relative);
    lodLevel -= uint(lodRelative.z < relative);
    lodLevel -= uint(lodRelative.y < relative);
    lodLevel -= uint(lodRelative.x < relative);

    return lodLevel;
}
#endif