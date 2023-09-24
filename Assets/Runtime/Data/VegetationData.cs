using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VegetationData : ScriptableObject
{
    [SerializeField]
    public int instanceCount;

    [SerializeField]
    public List<VegetationList> allObj;

    [SerializeField]
    public List<VegetationAsset> assetList;

    [SerializeField]
    public List<InstanceData> instanceData;

    [HideInInspector, SerializeField]
    public List<InstanceBuffer> InstanceBuffers;

    [SerializeField]
    public List<InstanceKindData> instanceKindData;

    #region cluster ‰÷»æ
    [SerializeField]
    public List<ClusterData> clusterData;

    [SerializeField]
    public List<ClusterChunkLODData> clusterChunkLODData;

    [SerializeField]
    public List<VertexBuffer> clusterVertexData;

    [SerializeField]
    public List<int> clusterTriangleData;

    [SerializeField]
    public int chunkCount;
    #endregion

    [SerializeField]
    public VegetationPreDCData preDCCeils;
}

[Serializable]
public enum VegetationGrade
{
    Low = 0,
    Medium,
    High
}