using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Serialization;

public static class HZBBufferName
{
    public static int _InstanceBuffer = Shader.PropertyToID("_InstanceBuffer");

    public static int _InstanceDataBuffer = Shader.PropertyToID("_InstanceDataBuffer");

    public static int _InstanceDataKindBuffer = Shader.PropertyToID("_InstanceDataKindBuffer");

    public static int _ArgsBuffer = Shader.PropertyToID("_ArgsBuffer");

    public static int _ResultBuffer = Shader.PropertyToID("_ResultBuffer");

    public static int _HizTexutreRT = Shader.PropertyToID("_HizTexutreRT");

    public static int _VisibleArgsDebugBuffer = Shader.PropertyToID("_VisibleArgsIndexBuffer");

    public static int _ResultShadowBuffer = Shader.PropertyToID("_ResultShadowBuffer");

    public static int _ArgsShadowBuffer = Shader.PropertyToID("_ArgsShadowBuffer");

    #region Cluster Name
    public static int _ClusterDataBuffer = Shader.PropertyToID("_ClusterDataBuffer");

    public static int _ClusterViewBuffer = Shader.PropertyToID("_ClusterViewBuffer");

    public static int _ClusterChunkLODDataBuffer = Shader.PropertyToID("_ClusterChunkLODDataBuffer");

    public static int _ClusterChunkDataBuffer = Shader.PropertyToID("_ClusterChunkDataBuffer");

    public static int _ClusterVertexData = Shader.PropertyToID("_ClusterVertexData");

    public static int _ClusterTriangleData = Shader.PropertyToID("_ClusterTriangleData");

    public static int _ResultTriange = Shader.PropertyToID("_ResultTriange");
    #endregion
}

public static class HZBMatParameterName
{

    public static int _ResultOffset = Shader.PropertyToID("_ResultOffset");

    public static int _ResultShadowOffset = Shader.PropertyToID("_ResultShadowOffset");

    public static int _CSMOffset0 = Shader.PropertyToID("_CSMOffset0");
    public static int _CSMOffset1 = Shader.PropertyToID("_CSMOffset1");
    public static int _CSMOffset2 = Shader.PropertyToID("_CSMOffset2");
}
