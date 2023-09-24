using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class VegetationList
{
    [SerializeField]
    public int assetId;

    [HideInInspector, SerializeField]
    public List<InstanceBuffer> InstanceBufferDatas = new List<InstanceBuffer>();

    [SerializeField]
    public List<InstanceData> instanceDatas = new List<InstanceData>();

    [SerializeField]
    public int normalInstanceCount;

    [SerializeField]
    public int clusterInstanceCount;

    public void AddData(Matrix4x4 worldMatrix, InstanceData instanceData)
    {
        InstanceBuffer instance = new InstanceBuffer(worldMatrix);
        this.InstanceBufferDatas.Add(instance);
        this.instanceDatas.Add(instanceData);
    }
}

[Serializable]
public class VegetationLOD
{
    [SerializeField] [FormerlySerializedAs("mesh")] 
    public Mesh mesh;

    //todo
    [SerializeField] [FormerlySerializedAs("materialPath")] 
    public Material materialData;

    [SerializeField]
    public List<ClusterChunkLODData> chunkLODData;

    [HideInInspector, NonSerialized]
    public List<List<Triangle>> trianglesList;

    [HideInInspector, NonSerialized]
    public int triangleStart;

    [HideInInspector,NonSerialized]
    public Material materialRun;

}

/// <summary>
/// 植被资源
/// </summary>
[Serializable]
public class VegetationAsset
{
    public int id;
    public List<VegetationLOD> lodAsset = new List<VegetationLOD>();

    [HideInInspector, NonSerialized]
    public int lodLevel;
    [HideInInspector, NonSerialized]
    public int lodLevelLow;

    //阴影到LOD第几级, -1表示没阴影;
    public int shadowLODLevel;

    [NonSerialized]
    public ComputeBuffer instanceBuffer;

#if UNITY_EDITOR
    public Vector4 lodRelative;

    
    public VegetationAsset(int id, LODGroup lodGroup, int shadowLODLevel)
    {
        this.id = id;
        this.shadowLODLevel = shadowLODLevel;
        LOD[] lods = lodGroup.GetLODs();
        if (lods.Length == 0)
        {
            return;
        }

        int i = 0;
        foreach(var lod in lods)
        {
            Renderer[] rds = lod.renderers;
            if (rds.Length == 0)
            {
                return;
            }
            Renderer rd = rds[0];
            MeshFilter meshfilter = rd.GetComponent<MeshFilter>();

            VegetationLOD vLOD = new VegetationLOD();
            vLOD.mesh = meshfilter.sharedMesh;
            vLOD.materialData = rd.sharedMaterial;
            lodAsset.Add(vLOD);

            switch (i)
            {
                case 0:
                    lodRelative.x = lod.screenRelativeTransitionHeight;
                    break;
                case 1:
                    lodRelative.y = lod.screenRelativeTransitionHeight;
                    break;
                case 2:
                    lodRelative.z = lod.screenRelativeTransitionHeight;
                    break;
                case 3:
                    lodRelative.w = lod.screenRelativeTransitionHeight;
                    break;
            } 
            i++;
        }


    }

    public void GenerateCluster()
    {
        Action<VegetationLOD, int> clusterFunc = (VegetationLOD vLOD, int capacity) =>
        {
            //生成cluster
            List<KDNode> nodes = ClusterSplit.Split(vLOD.mesh, 6, 128);
            if (nodes == null && nodes.Count == 0)
            {
                return;
            }
            List<ClusterChunkLODData> chunkLODData = new List<ClusterChunkLODData>();
            List<List<Triangle>> trianglesList = new List<List<Triangle>>();

            foreach (var node in nodes)
            {
                chunkLODData.Add(new ClusterChunkLODData(node.Bounding.center, node.Bounding.extents, node.Triangles.Count));
                trianglesList.Add(node.Triangles);
            }


            while (chunkLODData.Count < capacity)
            {
                chunkLODData.Add(new ClusterChunkLODData(Vector3.zero, Vector3.zero, 0));
                trianglesList.Add(new List<Triangle>());
            }
            vLOD.chunkLODData = chunkLODData;
            vLOD.trianglesList = trianglesList;
        };
        int capacity = 0;
        for (int i = 0; i < lodAsset.Count; i++)
        {
            var vLOD = lodAsset[i];
            if (i == 0)
            {
                clusterFunc(vLOD, -1);
                capacity = vLOD.chunkLODData.Count;
            }
            else
            {
                clusterFunc(vLOD, capacity);
            }
        }
    }
#endif
}
