#ifndef CLUSTERCULL
#define CLUSTERCULL

void ClusterCull(uint2 id)
{
    uint4 clusterView = _ClusterViewBuffer[id.y];
    ClusterData clusterData = _ClusterDataBuffer[id.y];
    if (clusterView.x < 0.5 || id.x >= clusterData.chunkLODDataCount)
    {
        return;
    }
    uint lodIndex = clusterView.y;

    uint chunkLODIndex = lodIndex * clusterData.chunkLODDataCount + clusterData.chunkLODStartIndex + id.x;
    ClusterChunkLODData chunkLODData = _ClusterChunkLODDataBuffer[chunkLODIndex];
    if (chunkLODData.triangleNum < 0.5)
    {
        return;
    }

    InstanceBuffer iData = _InstanceBuffer[clusterData.instanceIndex];

    half4 worldPos = mul(iData.worldMatrix, half4(chunkLODData.center, 1));
    worldPos /= worldPos.w;


    if (FrustumCull(worldPos.xyz, chunkLODData.extends, _FrustumPlanes) > 0.5)
    {
        return;
    }

    if (HizCull_4x4(worldPos.xyz, chunkLODData.extends) > 0.5)
    {
        return;
    }

    uint chunkIndex = clusterData.chunkStartIndex + id.x;
    _ClusterChunkDataBuffer[chunkIndex] = uint4(1, lodIndex, id.y, chunkLODIndex);
}

void TriangleCull(uint2 id)
{
    if (id.y >= _MaxSize)
    {
        return;
    }

    uint4 chunkData = _ClusterChunkDataBuffer[id.y];
    if (chunkData.x < 0.5)
    {
        return;
    }
    uint instanceIndex = chunkData.z;
    uint chunkLODIndex = chunkData.w;
    uint4 clusterView = _ClusterViewBuffer[instanceIndex];

    uint argsIndex = clusterView.z;
    uint lodIndex = clusterView.y;

    ClusterData clusterData = _ClusterDataBuffer[instanceIndex];
    ClusterChunkLODData chunkLODData = _ClusterChunkLODDataBuffer[chunkLODIndex];

    if (chunkLODData.triangleNum <= id.x)
    {
        return;
    }

    uint startIndex = chunkLODData.triangleStart + id.x * 3;
    uint index1 = _ClusterTriangleData[startIndex];
    uint index2 = _ClusterTriangleData[startIndex + 1];
    uint index3 = _ClusterTriangleData[startIndex + 2];

    VertexBuffer vert1 = _ClusterVertexData[index1];
    VertexBuffer vert2 = _ClusterVertexData[index2];
    VertexBuffer vert3 = _ClusterVertexData[index3];

    InstanceBuffer iBuffer = _InstanceBuffer[clusterData.instanceIndex];

    half4 worldVert1 = mul(iBuffer.worldMatrix, half4(vert1.vertex, 1));
    worldVert1 /= worldVert1.w;
    half4 worldVert2 = mul(iBuffer.worldMatrix, half4(vert2.vertex, 1));
    worldVert2 /= worldVert2.w;
    half4 worldVert3 = mul(iBuffer.worldMatrix, half4(vert3.vertex, 1));
    worldVert3 /= worldVert3.w;

    half3 minVertex;
    half3 maxVertex;

    minVertex.xy = min(min(worldVert1.xy, worldVert2.xy), worldVert3.xy);
    maxVertex.xy = max(max(worldVert1.xy, worldVert2.xy), worldVert3.xy);
    minVertex .z= min(min(worldVert1.z, worldVert2.z), worldVert3.z);
    maxVertex.z = max(max(worldVert1.z, worldVert2.z), worldVert3.z);

    half3 center = (minVertex + maxVertex) / 2;
    half3 extents = (maxVertex - minVertex) / 2;

    if (FrustumCull(center, extents, _FrustumPlanes) > 0.5)
    {
        return;
    }

    if (HizCull_4x4(center, extents) > 0.5)
    {
        return;
    }
    
    argsIndex = argsIndex + lodIndex;
   
    uint currentIndex;
    InterlockedAdd(_ArgsBuffer[argsIndex * 5 + 1], 1, currentIndex);
    currentIndex = clusterData.triangleLODResultData[lodIndex] + currentIndex;
    _ResultTriange[currentIndex] = uint2(startIndex, clusterData.instanceIndex);
}

#endif