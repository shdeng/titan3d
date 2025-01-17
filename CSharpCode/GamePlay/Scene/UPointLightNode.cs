﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.GamePlay.Scene
{
    [Bricks.CodeBuilder.ContextMenu("PointLight", "PointLight", UNode.EditorKeyword)]
    [UNode(NodeDataType = typeof(UPointLightNode.ULightNodeData), DefaultNamePrefix = "PointLight")]
    public partial class UPointLightNode : USceneActorNode
    {
        public override void Dispose()
        {
            CoreSDK.DisposeObject(ref mDebugMesh);
            base.Dispose();
        }
        public class ULightNodeData : UNodeData
        {
            internal UPointLightNode HostNode;
            Vector3 mColor;
            [Rtti.Meta]
            [EGui.Controls.PropertyGrid.Color3PickerEditor()]
            public Vector3 Color 
            { 
                get=> mColor;
                set
                {
                    mColor = value;
                    HostNode?.OnLightColorChanged();
                }
            }
            [Rtti.Meta]
            public float Intensity { get; set; }
            [Rtti.Meta]
            public float Radius { get; set; }
        }
        public override async System.Threading.Tasks.Task<bool> InitializeNode(GamePlay.UWorld world, UNodeData data, EBoundVolumeType bvType, Type placementType)
        {
            if (data == null)
            {
                data = new ULightNodeData();
            }
            
            var ret = await base.InitializeNode(world, data, bvType, placementType);
            GetNodeData<ULightNodeData>().HostNode = this;
            return ret;
        }
        public static async System.Threading.Tasks.Task<UPointLightNode> AddPointLightNode(UWorld world, UNode parent, ULightNodeData data, DVector3 pos)
        {
            var scene = parent.GetNearestParentScene();
            var scale = new Vector3(data.Radius);

            var meshNode = await scene.NewNode(world, typeof(UPointLightNode), data, EBoundVolumeType.Box, typeof(UPlacement)) as UPointLightNode;            
            meshNode.Parent = parent;
            
            meshNode.Placement.SetTransform(in pos, in scale, in Quaternion.Identity);

            return meshNode;
        }
        public int IndexInGpuScene = -1;
        internal void OnLightColorChanged()
        {
            if (mDebugMesh != null)
            {
                var colorVar = mDebugMesh.MaterialMesh.Materials[0].FindVar("clr4_0");
                if (colorVar != null)
                {
                    Vector4 clr4 = new Vector4(GetNodeData<ULightNodeData>().Color, 1);
                    colorVar.SetValue(in clr4);
                }
            }
        }
        Graphics.Mesh.UMesh mDebugMesh;
        public Graphics.Mesh.UMesh DebugMesh
        {
            get
            {
                if (mDebugMesh == null)
                {
                    var cookedMesh = UEngine.Instance.GfxDevice.MeshPrimitiveManager.UnitSphere;
                    var materials1 = new Graphics.Pipeline.Shader.UMaterialInstance[1];
                    materials1[0] = UEngine.Instance.GfxDevice.MaterialInstanceManager.WireColorMateria.CloneMaterialInstance();
                    var mesh2 = new Graphics.Mesh.UMesh();
                    var ok1 = mesh2.Initialize(cookedMesh, materials1,
                        Rtti.UTypeDescGetter<Graphics.Mesh.UMdfStaticMesh>.TypeDesc);
                    if (ok1)
                    {
                        mesh2.IsAcceptShadow = false;
                        mDebugMesh = mesh2;

                        mDebugMesh.HostNode = this;

                        BoundVolume.LocalAABB = mDebugMesh.MaterialMesh.Mesh.mCoreObject.mAABB;

                        this.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.Root;

                        UpdateAbsTransform();
                        UpdateAABB();
                        Parent?.UpdateAABB();

                        OnLightColorChanged();
                    }
                }
                return mDebugMesh;
            }
        }
        public override void OnNodeLoaded(UNode parent)
        {
            base.OnNodeLoaded(parent);
            UpdateAbsTransform();
        }
        public override void GetHitProxyDrawMesh(List<Graphics.Mesh.UMesh> meshes)
        {
            meshes.Add(mDebugMesh);
            foreach (var i in Children)
            {
                if (i.HitproxyType == Graphics.Pipeline.UHitProxy.EHitproxyType.FollowParent)
                    i.GetHitProxyDrawMesh(meshes);
            }
        }
        public override void OnGatherVisibleMeshes(UWorld.UVisParameter rp)
        {
            if (rp.VisibleNodes != null)
            {//灯光比较特殊，无论是否debug都要加入visnode列表，否则Tiling过程得不到可见灯光集
                rp.VisibleNodes.Add(this);
            }

            //if (UEngine.Instance.EditorInstance.Config.IsFilters(GamePlay.UWorld.UVisParameter.EVisCullFilter.LightDebug) == false)
            //    return;
            if ((rp.CullFilters & GamePlay.UWorld.UVisParameter.EVisCullFilter.LightDebug) == 0)
                return;

            if (DebugMesh != null)
                rp.VisibleMeshes.Add(mDebugMesh);
        }
        protected override void OnAbsTransformChanged()
        {
            var lightData = NodeData as ULightNodeData;
            if (lightData != null)
            {
                lightData.Radius = Placement.Scale.X;
            }
            if (mDebugMesh == null)
                return;

            var world = this.GetWorld();
            mDebugMesh.SetWorldTransform(in Placement.AbsTransform, world, false);
        }
        public override void OnHitProxyChanged()
        {
            if (mDebugMesh == null)
                return;
            if (this.HitProxy == null)
            {
                mDebugMesh.IsDrawHitproxy = false;
                return;
            }

            if (HitproxyType != Graphics.Pipeline.UHitProxy.EHitproxyType.None)
            {
                mDebugMesh.IsDrawHitproxy = true;
                var value = HitProxy.ConvertHitProxyIdToVector4();
                mDebugMesh.SetHitproxy(in value);
            }
            else
            {
                mDebugMesh.IsDrawHitproxy = false;
            }
        }
        public override bool OnTickLogic(GamePlay.UWorld world, Graphics.Pipeline.URenderPolicy policy)
        {
            //test temp code 
            LightData.Intensity = 120 * (float)Math.Sin(UEngine.Instance.TickCountSecond * 0.005f);

            return true;
        }
        public ULightNodeData LightData
        {
            get
            {
                return NodeData as ULightNodeData;
            }
        }

        public override bool IsAcceptShadow
        {
            get => false;
            set { }
        }
        public override bool IsCastShadow
        {
            get { return false; }
            set { }
        }
    }
}
