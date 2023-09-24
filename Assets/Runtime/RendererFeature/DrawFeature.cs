using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DrawInstanceDirectFeature : ScriptableRendererFeature
{
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

    private DrawInstanceDirectPass m_pass;
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_pass);
    }

    public override void Create()
    {
        m_pass = new DrawInstanceDirectPass(renderPassEvent);
        ShadowUtils.CustomRenderShadowSlice -= DrawInstanceDirectPass.RenderShadowmap;
        ShadowUtils.CustomRenderShadowSlice += DrawInstanceDirectPass.RenderShadowmap;
    }
}
public class DrawInstanceDirectPass : ScriptableRenderPass
{
    private static ProfilingSampler s_profilingSampler = new ProfilingSampler("HZBDrawPass");

    public DrawInstanceDirectPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        RenderInstance(cmd, renderingData.cameraData.camera);
        //执行
        context.ExecuteCommandBuffer(cmd);

        //回收
        CommandBufferPool.Release(cmd);
    }
    private static void RenderInstance(CommandBuffer cmd, Camera camera)
    {
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        if (!m_gManager.IsSure)
        {
            return;
        }

        List<uint> args = new List<uint>();
        List<VegetationList> allVegetation = m_gManager.VData.allObj;
        List<VegetationAsset> assetList = m_gManager.VData.assetList;
        List<InstanceKindData> clusterKindData = m_gManager.VData.instanceKindData;
        List<ClusterData> clusterDataList = m_gManager.VData.clusterData;

        using (new ProfilingScope(cmd, s_profilingSampler))
        {
            if(m_gManager.ResultBuffer != null)
            {
                Shader.SetGlobalBuffer(HZBBufferName._ResultBuffer, m_gManager.ResultBuffer);
            }
            //TODO Cluster的预烘焙没有做
#if UNITY_EDITOR
            if (m_gManager.enableDebugBuffer || camera.name == "SceneCamera" || true)
            {
                int instanceMatrixIndex = 0;
                foreach (var vegetationList in allVegetation)
                {
                    VegetationAsset asset = assetList[vegetationList.assetId];

                    InstanceData instanceData = vegetationList.instanceDatas[0];

                    for (int lodIndex = 0; lodIndex < asset.lodAsset.Count; lodIndex++)
                    {
                        var lod = asset.lodAsset[lodIndex];
                        InstanceKindData cKindData = clusterKindData[vegetationList.instanceDatas[0].instanceKindIndex];
                        if (instanceData.clusterIndex == 0)
                        {
#if UNITY_EDITOR
                            lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);
                            
                            lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, cKindData.kindResultStart + vegetationList.instanceDatas.Count * lodIndex);

#endif
                            cmd.DrawMeshInstancedIndirect(lod.mesh, 0, lod.materialRun, 0, m_gManager.ArgsBuffer, sizeof(uint) * 5 * (cKindData.argsIndex + lodIndex));
                        }
                        else
                        {
#if UNITY_EDITOR
                            //cluster
                            ClusterData clusterData = clusterDataList[instanceData.clusterIndex - 1];
                            lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, clusterData.triangleLODResultData[lodIndex]);
                            lod.materialRun.SetBuffer(HZBBufferName._ClusterVertexData, m_gManager.ClusterVertexData);
                            lod.materialRun.SetBuffer(HZBBufferName._ClusterTriangleData, m_gManager.ClusterTriangleData);
                            lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, m_gManager.InstanceBuffers);
                            lod.materialRun.SetBuffer(HZBBufferName._ResultTriange, m_gManager.ResultTriange);
#endif
                            int[] chunkDataw = new int[m_gManager.ArgsBuffer.count * 5];
                            m_gManager.ArgsBuffer.GetData(chunkDataw);
                            // 绘制三角形
                            cmd.DrawProceduralIndirect(Matrix4x4.identity, lod.materialRun, 0, MeshTopology.Triangles, m_gManager.ArgsBuffer, sizeof(uint) * 5 * (cKindData.argsIndex + lodIndex));
                        }
                        //break;
                    }
                }
            }
            else
#endif
            {
                VegetationCeilGather ceilGather = m_gManager.VData.preDCCeils.ceilGather;
                VegetationCeil column = ceilGather.GetCeil(camera.transform.position);
                if(column != null)
                {
                    foreach (var drawIndex in column.dcIndexList)
                    {
                        int vegetationIndex = drawIndex.x;
                        int lodIndex = drawIndex.y;

                        var vegetationList = allVegetation[vegetationIndex];
                        VegetationAsset asset = assetList[vegetationList.assetId];
                        var lod = asset.lodAsset[lodIndex];

                        InstanceKindData cKindData = clusterKindData[vegetationList.instanceDatas[0].instanceKindIndex];
                        int argsCount = cKindData.argsIndex + lodIndex;
                        InstanceData instanceData = vegetationList.instanceDatas[0];
                        if (instanceData.clusterIndex == 0)
                        {
#if UNITY_EDITOR
                            lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);
                            lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, cKindData.kindResultStart + vegetationList.instanceDatas.Count * lodIndex);
#endif
                            cmd.DrawMeshInstancedIndirect(lod.mesh, 0, lod.materialRun, 0, m_gManager.ArgsBuffer, sizeof(uint) * 5 * (cKindData.argsIndex + lodIndex));
                        }
                        else
                        {
#if UNITY_EDITOR
                            //cluster
                            ClusterData clusterData = clusterDataList[instanceData.clusterIndex - 1];
                            lod.materialRun.SetFloat(HZBMatParameterName._ResultOffset, clusterData.triangleLODResultData[lodIndex]);
                            lod.materialRun.SetBuffer(HZBBufferName._ClusterVertexData, m_gManager.ClusterVertexData);
                            lod.materialRun.SetBuffer(HZBBufferName._ClusterTriangleData, m_gManager.ClusterTriangleData);
                            lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, m_gManager.InstanceBuffers);
                            lod.materialRun.SetBuffer(HZBBufferName._ResultTriange, m_gManager.ResultTriange);
#endif
                            // 绘制三角形
                            cmd.DrawProceduralIndirect(Matrix4x4.identity, lod.materialRun, 0, MeshTopology.Triangles, m_gManager.ArgsBuffer, sizeof(uint) * 5 * (cKindData.argsIndex + lodIndex));
                        }

                    }
                }
                
            }
        }
    }

    private static void RenderShadowInstance(CommandBuffer cmd, Camera camera, int cascadeIndex)
    {
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
        if (!m_gManager.IsSure)
        {
            return;
        }

        List<uint> args = new List<uint>();
        List<VegetationList> allVegetation = m_gManager.VData.allObj;
        List<VegetationAsset> assetList = m_gManager.VData.assetList;
        List<InstanceKindData> clusterKindData = m_gManager.VData.instanceKindData;

        using (new ProfilingScope(cmd, s_profilingSampler))
        {
            int passIndex = 1;
            switch(cascadeIndex)
            {
                case 0:
                    Shader.SetGlobalFloat(HZBMatParameterName._CSMOffset0, m_gManager.ShadowClusterCount * cascadeIndex);
                    break;
                case 1:
                    Shader.SetGlobalFloat(HZBMatParameterName._CSMOffset1, m_gManager.ShadowClusterCount * cascadeIndex);
                    passIndex = 2;
                    break;
                case 2:
                    Shader.SetGlobalFloat(HZBMatParameterName._CSMOffset2, m_gManager.ShadowClusterCount * cascadeIndex);
                    passIndex = 3;
                    break;
            }
            
            Shader.SetGlobalBuffer(HZBBufferName._ResultShadowBuffer, m_gManager.ResultShadowBuffer);
#if UNITY_EDITOR
            if (m_gManager.enableDebugBuffer)
            {
                int argsCount = -1;
                foreach (var vegetationList in allVegetation)
                {
                    VegetationAsset asset = assetList[vegetationList.assetId];
                    int lodLevel = cascadeIndex == 0 ? asset.lodLevel : asset.lodLevelLow;

                    var lod = asset.lodAsset[lodLevel];
                    InstanceKindData cKindData = clusterKindData[vegetationList.instanceDatas[0].instanceKindIndex];
                    argsCount = cKindData.argsShadowIndex;
#if UNITY_EDITOR
                    lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);
                    lod.materialRun.SetFloat(HZBMatParameterName._ResultShadowOffset, cKindData.kindShadowResultStart);
#endif
                    int oneArgsSize = sizeof(uint) * 5;
                    int argsOffset = oneArgsSize * argsCount + m_gManager.ArgsShadowCount * oneArgsSize * cascadeIndex;
                    cmd.DrawMeshInstancedIndirect(lod.mesh, 0, lod.materialRun, passIndex, m_gManager.ArgsShadowBuffer, argsOffset);
                }
            }
            else
#endif
            {
                VegetationCeilGather ceilGather = m_gManager.VData.preDCCeils.ceilGather;
                VegetationCeil column = ceilGather.GetCeil(camera.transform.position);
                if (column != null)
                {
                    int vegetationIndex = -1;
                    foreach (var drawIndex in column.dcIndexList)
                    {
                        if(drawIndex.x == vegetationIndex)
                        {
                            continue;
                        }
                        vegetationIndex = drawIndex.x;
                        int lodIndex = drawIndex.y;

                        var vegetationList = allVegetation[vegetationIndex];
                        VegetationAsset asset = assetList[vegetationList.assetId];

                        int lodLevel = cascadeIndex == 0 ? asset.lodLevel : asset.lodLevelLow;
                        var lod = asset.lodAsset[lodLevel];
                        InstanceKindData cKindData = clusterKindData[vegetationList.instanceDatas[0].instanceKindIndex];
                        int argsCount = cKindData.argsShadowIndex;
#if UNITY_EDITOR
                        lod.materialRun.SetBuffer(HZBBufferName._InstanceBuffer, asset.instanceBuffer);
                        lod.materialRun.SetFloat(HZBMatParameterName._ResultShadowOffset, cKindData.kindShadowResultStart);
#endif
                        int oneArgsSize = sizeof(uint) * 5;
                        int argsOffset = oneArgsSize * argsCount + m_gManager.ArgsShadowCount * oneArgsSize * cascadeIndex;
                        cmd.DrawMeshInstancedIndirect(lod.mesh, 0, lod.materialRun, passIndex, m_gManager.ArgsShadowBuffer, argsOffset);

                    }
                }
            }
        }
    }

    public static void RenderShadowmap(CommandBuffer cmd, Camera camera, CameraRenderType renderType, int cascadeIndex)
    {
        //TODO
        //return;
        if (camera == null)
        {
            return;
        }
        if(renderType != CameraRenderType.Base)
        {
            //只有base类型相机才渲染阴影
            return;
        }
        HiZGlobelManager m_gManager = HiZGlobelManager.Instance;
#if UNITY_EDITOR
        if (m_gManager.enableDebugBuffer)
        {
            return;
        }
#endif
        float[] CascadeDistances = m_gManager.CascadeDistances;
        if (CascadeDistances == null || CascadeDistances.Length - 1 < cascadeIndex)
        {
            return;
        }

        RenderShadowInstance(cmd, camera, cascadeIndex);
    }
}