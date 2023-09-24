using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


public class HiZGlobelManager
{
    private bool m_isSure = false;

#if UNITY_EDITOR
    public bool DontRunHZBTest = false;

    public bool enableDebugBuffer = false;

    //激活完自动设置为false
    public bool ClearargsDebugBuffer = false;

    private ComputeShader m_testHZBCs;
    private ComputeBuffer m_visibleArgsBuffer;
    private int m_argsDebugCs = -1;
    private int m_csMaxSize = Shader.PropertyToID("_MaxSize");

    private uint[] m_ClearData;
#endif
    #region 多个Pass公用的数据
    private ComputeBuffer m_instanceBuffer;//TODO
    private ComputeBuffer m_instanceDataBuffer;
    private ComputeBuffer m_instanceKindBuffer;

    private ComputeBuffer m_InstanceBuffers;
    #endregion
    private ComputeBuffer m_argsBuffer;

    //正常Object渲染的结果buffer
    private ComputeBuffer m_resultBuffer;

    //Begin阴影
    private ComputeBuffer m_argsShadowBuffer;
    private ComputeBuffer m_resultShadowBuffer;
    private float[] m_cascadeDistances;
    private int m_argsShadowCount;
    private int m_shadowInstanceCount;
    //End阴影

    //Begin Cluster
    private ComputeBuffer m_clusterVertexData;
    private ComputeBuffer m_clusterTriangleData;

    private ComputeBuffer m_clusterDataBuffer;
    private ComputeBuffer m_clusterViewBuffer;
    private ComputeBuffer m_clusterChunkLODDataBuffer;
    private ComputeBuffer m_clusterChunkDataBuffer;

    private ComputeBuffer m_resultTriange;
    //End Cluster
    private VegetationData m_vData;

    public int argsCount;

    public int NumMips = -1;
    public RenderTexture HizTexutreRT;
    public Matrix4x4 LastVp;
    public Vector2Int HzbSize = Vector2Int.zero;

    public void CreateComputeBuffer(VegetationData vData)
    {
        if(vData == null || vData.instanceData == null)
        {
            return;
        }
        DisposeComputeBuffer();

        this.m_vData = vData;
        m_instanceDataBuffer?.Release();
        m_instanceDataBuffer = new ComputeBuffer(vData.instanceCount, Marshal.SizeOf(typeof(InstanceData)));
        m_instanceDataBuffer.SetData(vData.instanceData);

        m_resultBuffer?.Release();
        m_resultBuffer = GeneateResultBuffer(vData);

        CascadeDistances = HiZUtility.GetCascadeDistances();

        RunTimeCreateBuffer(vData);
        GenerateAllClusterBuffer(vData);

        m_isSure = true;
    }
    public void DisposeComputeBuffer()
    {
        if(HizTexutreRT != null)
        {
            HizTexutreRT.Release();
            HizTexutreRT = null;
        }

        if (m_instanceBuffer != null)
        {
            m_instanceBuffer.Dispose();
            m_instanceBuffer = null;
        }

        if (m_instanceDataBuffer != null)
        {
            m_instanceDataBuffer.Dispose();
            m_instanceDataBuffer = null;
        }
        if(m_InstanceBuffers != null)
        {
            m_InstanceBuffers.Dispose();
            m_InstanceBuffers = null;
        }

        if (m_argsBuffer != null)
        {
            m_argsBuffer.Dispose();
            m_argsBuffer = null;
        }

        if(m_resultBuffer != null)
        {
            m_resultBuffer.Dispose();
            m_resultBuffer = null;
        }

        if (m_argsShadowBuffer != null)
        {
            m_argsShadowBuffer.Dispose();
            m_argsShadowBuffer = null;
        }

        if (m_resultShadowBuffer != null)
        {
            m_resultShadowBuffer.Dispose();
            m_resultShadowBuffer = null;
        }


        if (m_clusterVertexData != null)
        {
            m_clusterVertexData.Dispose();
            m_clusterVertexData = null;
        }

        if (m_clusterTriangleData != null)
        {
            m_clusterTriangleData.Dispose();
            m_clusterTriangleData = null;
        }

        if (m_clusterDataBuffer != null)
        {
            m_clusterDataBuffer.Dispose();
            m_clusterDataBuffer = null;
        }

        if (m_clusterViewBuffer != null)
        {
            m_clusterViewBuffer.Dispose();
            m_clusterViewBuffer = null;
        }

        if (m_clusterChunkLODDataBuffer != null)
        {
            m_clusterChunkLODDataBuffer.Dispose();
            m_clusterChunkLODDataBuffer = null;
        }

        if (m_clusterChunkDataBuffer != null)
        {
            m_clusterChunkDataBuffer.Dispose();
            m_clusterChunkDataBuffer = null;
        }

        if (m_resultTriange != null)
        {
            m_resultTriange.Dispose();
            m_resultTriange = null;
        }
        m_cascadeDistances = null;
        m_isSure = false;
    }

    private ComputeBuffer GeneateResultBuffer(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<VegetationAsset> assetList = assetData.assetList;

        int resultNum = 0;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            VegetationAsset asset = assetList[vegetationList.assetId];

            int clusterCount = vegetationList.normalInstanceCount;

            resultNum += (asset.lodAsset.Count * clusterCount);
        }
        if(resultNum == 0)
        {
            return null;
        }

        return new ComputeBuffer(resultNum, sizeof(uint));
    }
    private void RunTimeCreateBuffer(VegetationData assetData)
    {
        List<VegetationList> allVegetation = assetData.allObj;
        List<InstanceKindData> clusterKindData = assetData.instanceKindData;
        List<VegetationAsset> assetList = assetData.assetList;
        List<ClusterData> clusterDataList = assetData.clusterData;

        m_InstanceBuffers = new ComputeBuffer(assetData.InstanceBuffers.Count, Marshal.SizeOf(typeof(InstanceBuffer)));
        m_InstanceBuffers.SetData(assetData.InstanceBuffers);

        //正常渲染args buffer
        List<uint> args = new List<uint>();
        foreach (var vegetationList in allVegetation)
        {
            VegetationAsset asset = assetList[vegetationList.assetId];
            asset.instanceBuffer?.Release();
            asset.instanceBuffer = new ComputeBuffer(vegetationList.instanceDatas.Count, Marshal.SizeOf(typeof(InstanceBuffer)));

            asset.instanceBuffer.SetData(vegetationList.InstanceBufferDatas);

            InstanceData instanceData = vegetationList.instanceDatas[0];
            InstanceKindData cKindData = clusterKindData[instanceData.instanceKindIndex];
            for (int i = 0; i < asset.lodAsset.Count; i++)
            {
                var lod = asset.lodAsset[i];

                lod.materialRun = GameObject.Instantiate<Material>(lod.materialData);
                if(instanceData.clusterIndex == 0)
                {
                    lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, cKindData.kindResultStart + vegetationList.instanceDatas.Count * i);
                    lod.materialRun.SetFloat(HZBMatParameterName._ResultShadowOffset, cKindData.kindShadowResultStart);
                    lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);

                    var mesh = lod.mesh;
                    args.Add(mesh.GetIndexCount(0));
                    args.Add(0);
                    args.Add(mesh.GetIndexStart(0));
                    args.Add(mesh.GetBaseVertex(0));
                    args.Add(0);
                }
                else
                {
                    ClusterData clusterData = clusterDataList[instanceData.clusterIndex - 1];
                    //cluster
                    lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, clusterData.triangleLODResultData[i]);
                    lod.materialRun.SetBuffer(HZBBufferName._ClusterVertexData, ClusterVertexData);
                    lod.materialRun.SetBuffer(HZBBufferName._ClusterTriangleData, ClusterTriangleData);
                    lod.materialRun.SetBuffer(HZBBufferName._ResultTriange, ResultTriange);
                    lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, m_InstanceBuffers);

                    lod.materialRun.SetFloat(HZBMatParameterName._ResultShadowOffset, cKindData.kindShadowResultStart);

                    args.Add(3);
                    args.Add(0);
                    args.Add(0);
                    args.Add(0);
                    args.Add(0);
                }
                
            }
        }
        argsCount = args.Count / 5;
        m_argsBuffer?.Release();
        m_argsBuffer = new ComputeBuffer(argsCount, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
        m_argsBuffer.SetData(args);
        
    }

    private uint[] GetArgs(VegetationLOD lod)
    {
        var mesh = lod.mesh;
        uint[] args = new uint[5];

        args[0] = mesh.GetIndexCount(0);
        args[1] = 0;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;
        return args;
    }

    public void CreateShadowRelevantBuffer(VegetationData assetData, float[] cascadeDistances)
    {
        if(cascadeDistances == null)
        {
            return;
        }
        int lastCsmCount = 0;

        if (m_cascadeDistances == null)
        {
            m_cascadeDistances = cascadeDistances;
        }
        else
        {
            lastCsmCount = m_cascadeDistances.Length;
            m_cascadeDistances = cascadeDistances;
        }
        int csmCount = m_cascadeDistances.Length;
        if (csmCount == lastCsmCount)
        {
            return;
        }
        if(csmCount > 0)
        {
            List<VegetationList> allVegetation = assetData.allObj;
            List<InstanceKindData> instanceKindData = assetData.instanceKindData;
            List<VegetationAsset> assetList = assetData.assetList;


            //3级CSM阴影的args buffer
            List<uint> argsCSM0 = new List<uint>();
            List<uint> argsCSM1 = new List<uint>();
            List<uint> argsCSM2 = new List<uint>();
            int argsIndex = 0;
            int resultNum = 0;
            foreach (var vegetationList in allVegetation)
            {
                VegetationAsset asset = assetList[vegetationList.assetId];
                if(asset.shadowLODLevel < 0)
                {
                    continue;
                }
                //用哪一级LOD作为阴影
                if(asset.lodAsset.Count > 2)
                {
                    asset.lodLevel = asset.lodAsset.Count - 2;
                    asset.lodLevelLow = asset.lodAsset.Count - 1;
                }

                var args = GetArgs(asset.lodAsset[asset.lodLevel]);

                if (csmCount > 0)
                {
                    //0级视椎体
                    argsCSM0.AddRange(args);
                }

                args = GetArgs(asset.lodAsset[asset.lodLevelLow]);
                if (csmCount > 1)
                {
                    //1级视椎体
                    argsCSM1.AddRange(args);
                }

                InstanceKindData cKindData = instanceKindData[vegetationList.instanceDatas[0].instanceKindIndex];
                cKindData.SetShadowData(argsIndex, resultNum);
                instanceKindData[vegetationList.instanceDatas[0].instanceKindIndex] = cKindData;

                resultNum += vegetationList.instanceDatas.Count;
                argsIndex++;
            }
            
            if (csmCount > 2)
            {
                //2级视椎体
                argsCSM2.AddRange(argsCSM1);
            }


            //阴影的argsbuffer
            m_argsShadowCount = argsCSM0.Count / 5;
            if (argsCSM0.Count == 0)
            {
                m_argsShadowBuffer?.Release();
                m_argsShadowBuffer = null;
            }
            else
            {
                m_argsShadowBuffer?.Release();
                m_argsShadowBuffer = new ComputeBuffer(m_argsShadowCount * csmCount, sizeof(uint) * 5, ComputeBufferType.IndirectArguments);
                argsCSM0.AddRange(argsCSM1);
                argsCSM0.AddRange(argsCSM2);
                m_argsShadowBuffer.SetData(argsCSM0);
            }

            //阴影的result buffer
            m_shadowInstanceCount = resultNum ;
            m_resultShadowBuffer?.Release();
            m_resultShadowBuffer = new ComputeBuffer(m_shadowInstanceCount * csmCount, sizeof(uint));
        }


        //种类buffer
        m_instanceKindBuffer?.Release();
        m_instanceKindBuffer = new ComputeBuffer(assetData.allObj.Count, Marshal.SizeOf(typeof(InstanceKindData)));
        m_instanceKindBuffer.SetData(assetData.instanceKindData);

    }

    public void GenerateAllClusterBuffer(VegetationData assetData)
    {
        if(assetData.clusterData == null || assetData.clusterData.Count == 0)
        {
            return;
        }

        m_clusterVertexData = new ComputeBuffer(assetData.clusterVertexData.Count, Marshal.SizeOf(typeof(VertexBuffer)));
        m_clusterVertexData.SetData(assetData.clusterVertexData);

        m_clusterTriangleData = new ComputeBuffer(assetData.clusterTriangleData.Count, sizeof(int));
        m_clusterTriangleData.SetData(assetData.clusterTriangleData);

        m_clusterDataBuffer = new ComputeBuffer(assetData.clusterData.Count, Marshal.SizeOf(typeof(ClusterData)));
        m_clusterDataBuffer.SetData(assetData.clusterData);
        m_clusterViewBuffer = new ComputeBuffer(assetData.clusterData.Count, sizeof(int) * 4);

        m_clusterChunkLODDataBuffer = new ComputeBuffer(assetData.clusterChunkLODData.Count, Marshal.SizeOf(typeof(ClusterChunkLODData)));
        m_clusterChunkLODDataBuffer.SetData(assetData.clusterChunkLODData);

        List<VegetationList> allVegetation = assetData.allObj;
        List<VegetationAsset> assetList = assetData.assetList;
        int chunkLOD0Count = 0;
        for (int i = 0; i < allVegetation.Count; i++)
        {
            var vegetationList = allVegetation[i];
            VegetationAsset asset = assetList[vegetationList.assetId];

            var lod = asset.lodAsset[0];
            chunkLOD0Count += lod.chunkLODData.Count * vegetationList.clusterInstanceCount;
        }

        m_clusterChunkDataBuffer = new ComputeBuffer(chunkLOD0Count, sizeof(int) * 4);

        ClusterData clusterData = assetData.clusterData[assetData.clusterData.Count - 1];

        int triangleCount = 0;
        for(int i = 3; i > 0; i--)
        {
            int value = (int)clusterData.triangleLODResultData[i];
            if(value == 0)
            {
                continue;
            }
            triangleCount = value;
            break;
        }

        var vl = allVegetation[allVegetation.Count - 1];
        VegetationAsset at = assetList[vl.assetId];
        triangleCount += at.lodAsset[at.lodAsset.Count - 1].mesh.triangles.Length / 3 * vl.clusterInstanceCount;

        m_resultTriange = new ComputeBuffer(triangleCount, sizeof(int) * 2);
    }


#if UNITY_EDITOR
    public void DispatchComputeDebug(UnityEngine.Rendering.CommandBuffer cmd, int hzbTestCs, ComputeShader testHZBCs)
    {
        if (m_testHZBCs == null)
        {
            m_testHZBCs = testHZBCs;
            
            m_argsDebugCs = testHZBCs.FindKernel("BakedClearArgs");
        }

        if(ClearargsDebugBuffer)
        {
            ClearargsDebugBuffer = false;

            //清除args
           /* testHZBCs.EnableKeyword("_BAKEDCODE");
            m_argsDebugCs = testHZBCs.FindKernel("BakedClearArgs");
            cmd.SetComputeIntParam(m_testHZBCs, m_csMaxSize, m_visibleArgsBuffer.count);
            cmd.SetComputeBufferParam(m_testHZBCs, m_argsDebugCs, HZBBufferName._VisibleArgsDebugBuffer, m_visibleArgsBuffer);
            cmd.DispatchCompute(m_testHZBCs, m_argsDebugCs, Mathf.CeilToInt(m_visibleArgsBuffer.count / 64.0f), 1, 1);*/
            
            if(m_ClearData != null)
            {
                m_visibleArgsBuffer.SetData(m_ClearData);
            }
        }

        if (DontRunHZBTest)
        {
            testHZBCs.DisableKeyword("_BAKEDCODE");
        }
        else
        {
            testHZBCs.EnableKeyword("_BAKEDCODE");
        }

        cmd.SetComputeBufferParam(m_testHZBCs, hzbTestCs, HZBBufferName._VisibleArgsDebugBuffer, m_visibleArgsBuffer);
    }
    public ComputeBuffer EnableDebugBuffer()
    {
        List<VegetationList> allVegetation = m_vData.allObj;
        List<VegetationAsset> assetList = m_vData.assetList;
        int argsCount = 0;

        foreach (var vegetationList in allVegetation)
        {
            VegetationAsset asset = assetList[vegetationList.assetId];
            argsCount += asset.lodAsset.Count;
        }
        m_ClearData = new uint[argsCount];
        m_visibleArgsBuffer = new ComputeBuffer(argsCount, sizeof(uint), ComputeBufferType.IndirectArguments);
        m_visibleArgsBuffer.SetData(m_ClearData);
        enableDebugBuffer = true;
        ClearargsDebugBuffer = true;
        return m_visibleArgsBuffer;
    }
    public void UnEnableDebugBuffer()
    {
        if(m_testHZBCs != null)
        {
            m_testHZBCs.DisableKeyword("_BAKEDCODE");
        }
        
        enableDebugBuffer = false;
        ClearargsDebugBuffer = false;
        m_argsDebugCs = -1;
        m_visibleArgsBuffer.Dispose();
        m_visibleArgsBuffer = null;
        m_testHZBCs = null;
        m_ClearData = null;
    }
#endif

    private static HiZGlobelManager _Instance = null;
    static public HiZGlobelManager Instance { 
        get 
        { 
            if(_Instance == null)
            {
                _Instance = new HiZGlobelManager();
            }
            return _Instance;
        }
    }
    public ComputeBuffer InstanceDataBuffer { get => m_instanceDataBuffer; }

    public ComputeBuffer InstanceBuffer { get => m_instanceBuffer; }
    public ComputeBuffer ArgsBuffer { get => m_argsBuffer; }
    public ComputeBuffer ResultBuffer { get => m_resultBuffer; }
    public bool IsSure { get { return m_isSure && m_vData != null && HizTexutreRT != null; } }
    public VegetationData VData { get => m_vData; }
    public ComputeBuffer InstanceDataKindBuffer { get => m_instanceKindBuffer; }
    public ComputeBuffer ArgsShadowBuffer { get => m_argsShadowBuffer; set => m_argsShadowBuffer = value; }
    public ComputeBuffer ResultShadowBuffer { get => m_resultShadowBuffer; set => m_resultShadowBuffer = value; }
    public float[] CascadeDistances { get => m_cascadeDistances; set => CreateShadowRelevantBuffer(VData, value); }
    public int ArgsShadowCount { get => m_argsShadowCount; set => m_argsShadowCount = value; }
    public int ShadowClusterCount { get => m_shadowInstanceCount; set => m_shadowInstanceCount = value; }
    public ComputeBuffer ClusterVertexData { get => m_clusterVertexData;  }
    public ComputeBuffer ClusterTriangleData { get => m_clusterTriangleData; }
    public ComputeBuffer ClusterDataBuffer { get => m_clusterDataBuffer; }
    public ComputeBuffer ClusterChunkLODDataBuffer { get => m_clusterChunkLODDataBuffer; }
    public ComputeBuffer ClusterChunkDataBuffer { get => m_clusterChunkDataBuffer; }
    public ComputeBuffer ResultTriange { get => m_resultTriange; }
    public ComputeBuffer InstanceBuffers { get => m_InstanceBuffers; set => m_InstanceBuffers = value; }
    public ComputeBuffer ClusterViewBuffer { get => m_clusterViewBuffer; set => m_clusterViewBuffer = value; }
}
