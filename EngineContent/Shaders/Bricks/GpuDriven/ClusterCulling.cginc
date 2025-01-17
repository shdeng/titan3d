#ifndef	_CLUSTER_CULLING_H_
#define _CLUSTER_CULLING_H_

#include "../../Inc/VertexLayout.cginc"
#include "FRaster.cginc"

struct HZBCullData
{
    float3	RectMin;
    float3	RectMax;

    bool IsVisible;
};

struct FrustumParams
{
    float4 GpuDrivenCameraPlanes[6];

    float3 GpuDrivenFrustumMinPoint;
    float3 GpuDrivenFrustumMaxPoint;
};
cbuffer cbCameraFrustum DX_AUTOBIND
{
    FrustumParams FrustumInfo;
};

ByteAddressBuffer SrcClusterBuffer;
RWByteAddressBuffer VisClusterBuffer;

bool BoxCullFrustum(int clusterId)
{
    FClusterData clusterData = ClusterBuffer[clusterId];

    float3 center = (clusterData.BoundMin + clusterData.BoundMax) / 2;
    float3 extent = (clusterData.BoundMax - clusterData.BoundMin) / 2;

    float3 minPos = clusterData.BoundMin;
    float3 maxPos = clusterData.BoundMax;
    
    float outOfRange = dot(FrustumInfo.GpuDrivenFrustumMinPoint > maxPos, 1) + dot(FrustumInfo.GpuDrivenFrustumMaxPoint < minPos, 1);
    if (outOfRange > 0.5)
        return true;

    for (uint i = 0; i < 6; ++i)
    {
        float4 plane = FrustumInfo.GpuDrivenCameraPlanes[i];
        float3 absNormal = abs(plane.xyz);
        if ((dot(center, plane.xyz) - dot(absNormal, extent)) > -plane.w)
        {
            return true;
        }
    }
    return false;
}

HZBCullData GetScreenRect(int clusterId)
{
    HZBCullData hzbData;


    return hzbData;
}

bool HZBCulling(int clusterId)
{
    HZBCullData hzbData = GetScreenRect(clusterId);


    return true;
}
bool IsVisible(uint clusterId)
{
    bool isFrustumCull = BoxCullFrustum(clusterId);
    
    if (!isFrustumCull)
    {
        return HZBCulling(clusterId);
    }
    
    return false;
}

groupshared uint MaxSrcCount;

[numthreads(DispatchX, DispatchY, DispatchZ)]
void CS_ClusterCullingMain(uint DispatchThreadId : SV_DispatchThreadID, uint3 LocalThreadId : SV_GroupThreadID, uint3 GroupId : SV_GroupID)
{
    if (LocalThreadId.x == 0)
    {
        MaxSrcCount = SrcClusterBuffer.Load(0);
    }
    GroupMemoryBarrierWithGroupSync();
    
    if (DispatchThreadId.x >= MaxSrcCount)
    {
        return;
    }
    
    uint clusterId = SrcClusterBuffer.Load((1 + DispatchThreadId.x) * 4);
    if (!IsVisible(clusterId))
    {
        return;
    }
    // TODO:
    int index = 0;
    VisClusterBuffer.InterlockedAdd(0, 1, index);
    //VisClusterBuffer&ClusterBuffer [0] is the count of array
    VisClusterBuffer.Store((1 + index) * 4, clusterId);
    //VisClusterBuffer.Store(4, 1);
}

#endif//_CLUSTER_CULLING_H_