using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public class GenerateHizDataEditor
{
    static DebugDrawScene s_debugScrpit = null;
    static string runtimeDataDir = "Assets";

    static int s_ceilSize = 16;

    [MenuItem("Chess/Build")]

    public static void Test()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        var runtimeDataPath = $"{runtimeDataDir}/VegetationData_{activeScene.name}.asset";
        VegetationData runtimeData = AssetDatabase.LoadAssetAtPath<VegetationData>(runtimeDataPath);
        if (runtimeData == null)
        {
            runtimeData = ScriptableObject.CreateInstance<VegetationData>();
            if (!Directory.Exists(runtimeDataDir))
            {
                Directory.CreateDirectory(runtimeDataDir);
            }
            AssetDatabase.CreateAsset(runtimeData, runtimeDataPath);
        }

        runtimeData.allObj = new List<VegetationList>();
        runtimeData.assetList = new List<VegetationAsset>();

        runtimeData.instanceCount = 0;
        Dictionary<Mesh, int> editorAssetsIndex = UpdateVegetationAreaData(runtimeData);

        UpdateClusterVegetation(runtimeData, editorAssetsIndex);

        UpdateTriangleResultIndex(runtimeData);

        var terrain = GameObject.FindObjectOfType<Terrain>();
        BulidRenderData(runtimeData, terrain.terrainData.bounds);

        HiZDataLoader loader = GameObject.FindObjectOfType<HiZDataLoader>();
        if (loader)
        {
            loader.data = runtimeData;
            loader.OnEnable();
        }
        EditorUtility.SetDirty(runtimeData);
    }

    public static void BulidRenderData(VegetationData assetData, Bounds maxBounds)
    {
        CalculateInstanceTypeCount(assetData);
        GeneateClusterData(assetData);
        GeneateRenderData(assetData);

        assetData.preDCCeils = GenerateTool.BuildDCCeil(maxBounds, s_ceilSize);
        DebugDrawScene debugDraw = FindDebugDrawScenea();
        debugDraw.preDCCeils = assetData.preDCCeils;
        debugDraw.b = maxBounds;

        RunBakedEditor.RunBakeDCCeil(assetData);
    }

    public static void UpdateClusterVegetation(VegetationData assetData, Dictionary<Mesh, int> editorAssetsIndex)
    {
        LODGroup[] lodgs = GameObject.FindObjectsOfType<LODGroup>();

        if (lodgs.Length == 0)
        {
            return;
        }

        editorAssetsIndex = UpdateVegetationAsset(assetData, lodgs.ToList(), editorAssetsIndex, true);

        int count = assetData.instanceCount;
        int ClusterCount = 1;
        foreach (var lodg in lodgs)
        {
            if (AddVegetation(assetData.allObj, editorAssetsIndex, lodg, ClusterCount))
            {
                count++;
                ClusterCount++;
            }
        }
        assetData.instanceCount = count;
    }
    public static Dictionary<Mesh, int> UpdateVegetationAreaData(VegetationData assetData)
    {
        Dictionary<Mesh, int> editorAssetsIndex = new Dictionary<Mesh, int>();
        var terrain = GameObject.FindObjectOfType<Terrain>();
        List<LODGroup> lodgs = GenerateTool.ConvertTerrainData(terrain);
        if (lodgs.Count == 0)
        {
            return editorAssetsIndex;
        }

        editorAssetsIndex = UpdateVegetationAsset(assetData, lodgs, editorAssetsIndex);
        int count = 0;
        foreach (var lodg in lodgs)
        {
            if (AddVegetation(assetData.allObj, editorAssetsIndex, lodg))
            {
                count++;
            }
        }
        assetData.instanceCount = count;

        foreach (var item in lodgs)
        {
            GameObject.DestroyImmediate(item.gameObject);
        }
        return editorAssetsIndex;
    }

    public static void UpdateTriangleResultIndex(VegetationData assetData)
    {
        /*int index = 0;
        foreach (var vAsset in assetData.assetList)
        {
            List<VegetationLOD> lodAssetList = vAsset.lodAsset;
            foreach(var lodAsset in lodAssetList)
            {
                lodAsset.triangleStart = index;
                index += lodAsset.trianglesList.
            }
        }*/
    }

    public static Dictionary<Mesh, int> UpdateVegetationAsset(VegetationData assetData, List<LODGroup> lodgs, Dictionary<Mesh, int> editorAssetsIndex, bool generateCluster = false)
    {
        foreach (var lodg in lodgs)
        {
            LOD[] lods = lodg.GetLODs();
            if (lods.Length == 0)
            {
                continue;
            }
            Renderer[] rds = lods[0].renderers;
            if (rds.Length == 0)
            {
                continue;
            }
            Renderer rd = rds[0];
            MeshFilter meshfilter = rd.GetComponent<MeshFilter>();
            Mesh mesh = meshfilter.sharedMesh;
            int index = 0;
            if (!editorAssetsIndex.TryGetValue(mesh, out index))
            {
                index = editorAssetsIndex.Count;

                int lodLevel = -1;
                for (int j = 0; j < lods.Length; j++)
                {
                    var lod = lods[j];
                    if (lods[0].renderers.Length == 0 || lods[0].renderers[0] == null)
                    {
                        continue;
                    }
                    if (lods[0].renderers[0].shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                    {
                        lodLevel = j;
                        break;
                    }
                }

                VegetationAsset vAsset = new VegetationAsset(index, lodg, lodLevel);
                vAsset.GenerateCluster();
                assetData.assetList.Add(vAsset);

                editorAssetsIndex.Add(mesh, index);
            }
        }
        return editorAssetsIndex;
    }

    public static DebugDrawScene FindDebugDrawScenea()
    {
        if (s_debugScrpit != null)
        {
            return s_debugScrpit;
        }
        s_debugScrpit = GameObject.FindObjectOfType<DebugDrawScene>();
        if (s_debugScrpit == null)
        {
            var obj = new GameObject();
            obj.name = "!DebugScrpitObject";
            s_debugScrpit = obj.AddComponent<DebugDrawScene>();
        }
        return s_debugScrpit;
    }
    private static bool AddVegetation(List<VegetationList> allObjMatrix, Dictionary<Mesh, int> editorAssetsIndex, LODGroup lodGroup, int clusterIndex = 0)
    {
        LOD[] lods = lodGroup.GetLODs();

        if (lods.Length == 0)
        {
            return false;
        }
        Renderer[] rds = lods[0].renderers;
        if (rds.Length == 0)
        {
            return false;
        }

        MeshFilter meshfilter = rds[0].GetComponent<MeshFilter>();
        Mesh mesh = meshfilter.sharedMesh;
        if (!editorAssetsIndex.TryGetValue(mesh, out var index))
        {
            return false;
        }
        VegetationList vegetation = allObjMatrix.Find(t => t.assetId == index);
        if (vegetation == null)
        {
            vegetation = new VegetationList();
            vegetation.assetId = index;
            allObjMatrix.Add(vegetation);
        }

        Matrix4x4 matrix = Matrix4x4.TRS(lodGroup.transform.position, lodGroup.transform.rotation, lodGroup.transform.localScale);
        InstanceData iData = new InstanceData(rds[0].bounds, clusterIndex);

        vegetation.AddData(matrix, iData);
        return true;
    }
    private static void CalculateInstanceTypeCount(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            int instanceCount = vegetationList.instanceDatas.Count;

            int normalInstanceCount = 0;
            int clusterInstanceCount = 0;
            for (int j = 0; j < instanceCount; j++)
            {
                InstanceData instanceData = vegetationList.instanceDatas[j];
                if (instanceData.clusterIndex == 0)
                {
                    normalInstanceCount++;
                }
                else
                {
                    clusterInstanceCount++;
                }
            }
            vegetationList.clusterInstanceCount = clusterInstanceCount;
            vegetationList.normalInstanceCount = normalInstanceCount;
        }
    }
    
    private static void GeneateRenderData(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<VegetationAsset> assetList = assetData.assetList;

        //cluster
        List<InstanceBuffer> instanceBuffers = new List<InstanceBuffer>();

        var instances = new NativeArray<InstanceData>(assetData.instanceCount, Allocator.Temp);
        var instanceKindData = new List<InstanceKindData>();
        int instanceOffset = 0;
        int kindResultStart = 0;
        int argsIndex = 0;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            VegetationAsset asset = assetList[vegetationList.assetId];

            List<InstanceData> instanceDatas = new List<InstanceData>();
            int instanceCount = vegetationList.instanceDatas.Count;

            for (int j = 0; j < instanceCount; j++)
            {
                InstanceData instanceData = vegetationList.instanceDatas[j];
                int instanceIndex = j;
                if (instanceData.clusterIndex != 0)
                {
                    instanceIndex = instanceBuffers.Count + j;
                }
                instanceData = new InstanceData(instanceData, instanceIndex, i);
                instanceDatas.Add(instanceData);
            }
            instanceBuffers.AddRange(vegetationList.InstanceBufferDatas);

            instanceKindData.Add(new InstanceKindData(argsIndex, kindResultStart, instanceCount, asset.lodAsset.Count, asset.lodRelative, asset.shadowLODLevel));
            NativeArray<InstanceData>.Copy(instanceDatas.ToArray(), 0, instances, instanceOffset, instanceCount);
            vegetationList.instanceDatas = instanceDatas;

            argsIndex += asset.lodAsset.Count;

            instanceOffset += instanceCount;
            kindResultStart += (asset.lodAsset.Count * instanceCount);
        }
        assetData.instanceData = instances.ToList();
        assetData.instanceKindData = instanceKindData;
        assetData.InstanceBuffers = instanceBuffers;

    }

    private static void GeneateClusterData(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<VegetationAsset> assetList = assetData.assetList;

        List<ClusterData> clusterData = new List<ClusterData>();
        List<ClusterChunkLODData> clusterChunkLODData = new List<ClusterChunkLODData>();
        List<VertexBuffer> clusterVertexData = new List<VertexBuffer>();
        List<int> clusterTriangleData = new List<int>();

        assetData.clusterChunkLODData = clusterChunkLODData;
        assetData.clusterData = clusterData;
        assetData.clusterVertexData = clusterVertexData;
        assetData.clusterTriangleData = clusterTriangleData;

        int chunkCount = 0;
        int vbBufferOffset = 0;
        Vector4 triangleLODResultData = Vector4.zero;
        int triangleLastStart = -1;
        int instanceOffset = 0;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            VegetationAsset asset = assetList[vegetationList.assetId];

            int clusterInstanceCount = vegetationList.clusterInstanceCount;

            if (clusterInstanceCount == 0)
            {
                instanceOffset += vegetationList.InstanceBufferDatas.Count;
                continue;
            }
            else
            {
                int chunkLODStartIndex = 0;
                for (int k = 0; k < asset.lodAsset.Count; k++)
                {
                    if (k == 0)
                    {
                        chunkLODStartIndex = clusterChunkLODData.Count;
                    }
                    var lod = asset.lodAsset[k];
                    //clusterVertexData构建
                    List<VertexBuffer> vertexData = new List<VertexBuffer>();
                    for (int index = 0; index < lod.mesh.vertexCount; index++)
                    {
                        Mesh mesh = lod.mesh;
                        Vector4 color = new Vector4(0, 0, 0, 0);
                        if (mesh.colors.Length > index)
                        {
                            color = mesh.colors[index];
                        }
                        VertexBuffer vb = new VertexBuffer(mesh.vertices[index], mesh.normals[index], color, mesh.tangents[index], mesh.uv[index], mesh.uv2[index]);
                        vertexData.Add(vb);
                    }
                    clusterVertexData.AddRange(vertexData);

                    for (int index = 0; index < lod.chunkLODData.Count; index++)
                    {
                        var chunk = lod.chunkLODData[index];
                        var trianglesList = lod.trianglesList[index];
                        //triangleData构建
                        int[] triangles = new int[trianglesList.Count * 3];

                        //映射三角形索引
                        int tIndex = 0;
                        for (int tCount = 0; tCount < trianglesList.Count; tCount++)
                        {
                            Triangle trg = trianglesList[tCount];
                            triangles[tIndex++] = vbBufferOffset + trg.Index1;
                            triangles[tIndex++] = vbBufferOffset + trg.Index2;
                            triangles[tIndex++] = vbBufferOffset + trg.Index3;
                        }
                        chunk.SetData(clusterTriangleData.Count);
                        lod.chunkLODData[index] = chunk;
                        clusterTriangleData.AddRange(triangles);
                    }

                    //chunkData构建
                    clusterChunkLODData.AddRange(lod.chunkLODData);

                    vbBufferOffset += lod.mesh.vertexCount;

                    //计算三角形结果buffer偏移
                    int trianglesNum = lod.mesh.triangles.Length / 3;
                    if (triangleLastStart == -1)
                    {
                        triangleLastStart = trianglesNum * clusterInstanceCount;
                    }
                    else
                    {
                        triangleLODResultData[k] = triangleLastStart;
                        triangleLastStart = triangleLastStart + trianglesNum * clusterInstanceCount;
                    }
                }

                for (int j = 0; j < clusterInstanceCount; j++)
                {
                    InstanceData instanceData = vegetationList.instanceDatas[j];
                    if (instanceData.clusterIndex == 0)
                    {
                        continue;
                    }

                    //clusterData构建
                    ClusterData data = new ClusterData(chunkLODStartIndex, asset.lodAsset[0].chunkLODData.Count, j + instanceOffset, chunkCount, triangleLODResultData);
                    chunkCount += asset.lodAsset[0].chunkLODData.Count;
                    clusterData.Add(data);

                    instanceData.SetClusterIndex(clusterData.Count);
                    vegetationList.instanceDatas[j] = instanceData;
                }
                instanceOffset += vegetationList.InstanceBufferDatas.Count;
            }
        }
        assetData.chunkCount = chunkCount;
    }
}
