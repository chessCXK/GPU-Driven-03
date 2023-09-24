using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct InstanceBuffer
{
    public Matrix4x4 worldMatrix;
    public Matrix4x4 worldInverseMatrix;
    public InstanceBuffer(Matrix4x4 worldMatrix)
    {
        this.worldMatrix = worldMatrix;
        this.worldInverseMatrix = worldMatrix.inverse;
    }
}

[Serializable]
public struct InstanceKindData
{
    public int argsIndex;

    //该种类型的result起始位置
    public int kindResultStart;

    //有多少LOD
    public int lodNum;

    //该类型的Cluster有多少个
    public int elementNum;

    //只运行4级LOD
    public Vector4 lodRelative;

    //阴影
    public int argsShadowIndex;

    //阴影到LOD第几级;
    public int shadowLODLevel;

    //阴影的ResultBuffer开始位置
    public int kindShadowResultStart;

    public InstanceKindData(int argsIndex, int kindResultStart, int elementNum, int lodNum, Vector4 lodRelative, int shadowLODLevel)
    {
        this.argsIndex = argsIndex;
        this.kindResultStart = kindResultStart;
        this.elementNum = elementNum;
        this.lodNum = lodNum;
        this.lodRelative = lodRelative;
        this.shadowLODLevel = shadowLODLevel;
        this.argsShadowIndex = 0;
        this.kindShadowResultStart = 0;
    }

    public void SetShadowData(int argsShadowIndex, int kindShadowResultStart)
    {
        this.argsShadowIndex = argsShadowIndex;
        this.kindShadowResultStart = kindShadowResultStart;
    }
}

[Serializable]
public struct InstanceData
{
    public Vector3 center;
    public Vector3 extends;
    //当前实例的变换矩阵数组的引用
    public int instanceIndex;

    public int instanceKindIndex;

    public int clusterIndex;

    public InstanceData(Bounds bound, int clusterIndex = 0)
    {
        center = bound.center;
        extends = bound.extents;
        instanceIndex = instanceKindIndex = -1;
        this.clusterIndex = clusterIndex;
    }

    public InstanceData(InstanceData other, int instanceIndex, int instanceKindIndex)
    {
        center = other.center;
        extends = other.extends;
        this.instanceIndex = instanceIndex;
        this.instanceKindIndex = instanceKindIndex;
        this.clusterIndex = other.clusterIndex;
    }
    public void SetClusterIndex(int clusterIndex)
    {
        this.clusterIndex = clusterIndex;
    }
}

[Serializable]
public struct ClusterData
{
    //lod0在ChunkLODData开始位置
    public int chunkLODStartIndex;

    //每级LOD的chunkData有多少个
    public int chunkLODDataCount;

    //lod0在ClusterChunkData开始位置
    public int chunkStartIndex;

    //当前类型instanceData中的位置
    public int instanceIndex;

    //当前类型每种LOD的最终三角形索引buffer的起始位置
    public Vector4 triangleLODResultData;

    public ClusterData(int chunkLODStartIndex, int chunkLODDataCount, int instanceIndex, int chunkStartIndex, Vector4 triangleLODResultData)
    {
        this.chunkLODStartIndex = chunkLODStartIndex;
        this.chunkLODDataCount = chunkLODDataCount;
        this.instanceIndex = instanceIndex;
        this.chunkStartIndex = chunkStartIndex;
        this.triangleLODResultData = triangleLODResultData;
    }
}

[Serializable]
public struct ClusterChunkLODData
{
    public Vector3 center;
    public Vector3 extends;

    //三角形索引在buffer开始位置
    public int triangleStart;

    //三角形数量
    public int triangleNum;

    public ClusterChunkLODData(Vector3 center, Vector3 extends, int triangleNum)
    {
        this.center = center;
        this.extends = extends;
        this.triangleNum = triangleNum;
        triangleStart = 0;
    }
    public void SetData(int triangleStart)
    {
        this.triangleStart = triangleStart;
    }

}

[Serializable]
public struct VertexBuffer
{
    public Vector3 vertex;

    public Vector3 normal;

    public Vector4 color;

    public Vector4 tangent;

    public Vector2 uv0;

    public Vector2 uv1;

    public VertexBuffer(Vector3 vertex, Vector3 normal, Vector4 color, Vector4 tangent, Vector2 uv0, Vector2 uv1)
    {
        this.vertex = vertex;
        this.normal = normal;
        this.color = color;
        this.tangent = tangent;
        this.uv0 = uv0;
        this.uv1 = uv1;
    }
}


