﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Graphics.Pipeline.Common
{
    public class UCopyNode : Graphics.Pipeline.Common.URenderGraphNode
    {
        public Common.URenderGraphPin SrcPinIn = Common.URenderGraphPin.CreateInput("Src");
        public Common.URenderGraphPin DestPinOut = Common.URenderGraphPin.CreateOutput("Dest", false, EPixelFormat.PXF_UNKNOWN);
        public UCopyNode()
        {
            Name = "CopyNode";
        }
        public override void InitNodePins()
        {
            AddInput(SrcPinIn, NxRHI.EBufferType.BFT_SRV);
            AddOutput(DestPinOut, NxRHI.EBufferType.BFT_SRV);
        }
        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            var rc = UEngine.Instance.GfxDevice.RenderContext;

            await base.Initialize(policy, debugName);
            BasePass.Initialize(rc, debugName + ".BasePass");

            mCopyDrawcall = UEngine.Instance.GfxDevice.RenderContext.CreateCopyDraw();
        }
        public UAttachBuffer ResultBuffer;
        public bool IsCpuAceesResult { get; set; } = false;
        public NxRHI.UCopyDraw mCopyDrawcall;
        public override void FrameBuild(Graphics.Pipeline.URenderPolicy policy)
        {
            var attachement = RenderGraph.AttachmentCache.ImportAttachment(DestPinOut);
            if (SrcPinIn.Attachement.Format != DestPinOut.Attachement.Format ||
                SrcPinIn.Attachement.Width != DestPinOut.Attachement.Width ||
                SrcPinIn.Attachement.Height != DestPinOut.Attachement.Height)
            {
                ResultBuffer = attachement.Clone();
                attachement.Buffer = ResultBuffer.Buffer;
                attachement.Srv = ResultBuffer.Srv;
                attachement.Rtv = ResultBuffer.Rtv;
                attachement.Dsv = ResultBuffer.Dsv;
                attachement.Uav = ResultBuffer.Uav;
                DestPinOut.Attachement.Format = SrcPinIn.Attachement.Format;
                DestPinOut.Attachement.Width = SrcPinIn.Attachement.Width;
                DestPinOut.Attachement.Height = SrcPinIn.Attachement.Height;
            }
            if (ResultBuffer != null)
            {
                attachement.Buffer = ResultBuffer.Buffer;
                attachement.Srv = ResultBuffer.Srv;
                attachement.Rtv = ResultBuffer.Rtv;
                attachement.Dsv = ResultBuffer.Dsv;
                attachement.Uav = ResultBuffer.Uav;
            }
        }
        [ThreadStatic]
        private static Profiler.TimeScope ScopeTick = Profiler.TimeScopeManager.GetTimeScope(typeof(UCopyNode), nameof(TickLogic));
        public override unsafe void TickLogic(GamePlay.UWorld world, URenderPolicy policy, bool bClear)
        {
            using (new Profiler.TimeScopeHelper(ScopeTick))
            {
                if (mCopyDrawcall == null)
                    return;
                var cmdlist = BasePass.DrawCmdList;
                cmdlist.BeginCommand();

                {
                    var srcPin = GetAttachBuffer(SrcPinIn);
                    var tarPin = GetAttachBuffer(DestPinOut);

                    if (srcPin.Buffer.GetType() == typeof(NxRHI.UBuffer) && tarPin.Buffer.GetType() == typeof(NxRHI.UBuffer))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Buffer2Buffer;
                    }
                    else if (srcPin.Buffer.GetType() == typeof(NxRHI.UTexture) && tarPin.Buffer.GetType() == typeof(NxRHI.UTexture))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Texture2Texture;
                    }
                    else if (srcPin.Buffer.GetType() == typeof(NxRHI.UTexture) && tarPin.Buffer.GetType() == typeof(NxRHI.UBuffer))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Texture2Buffer;
                    }
                    else if (srcPin.Buffer.GetType() == typeof(NxRHI.UTexture) && tarPin.Buffer.GetType() == typeof(NxRHI.UBuffer))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Buffer2Texture;
                    }
                    mCopyDrawcall.BindSrc(srcPin.Buffer);
                    mCopyDrawcall.BindDest(tarPin.Buffer);

                    //if (SrcPinIn.Attachement.Format == EPixelFormat.PXF_UNKNOWN)
                    //{
                    //    //SetCopyBuffer(srcPin.Buffer.mCoreObject, 0, tarPin.Buffer.mCoreObject, 0, SrcPinIn.Attachement.Width * SrcPinIn.Attachement.Height);
                    //}
                    //else
                    //{   
                    //    mCopyDrawcall.SetCopyTexture2D(srcPin.Buffer.mCoreObject, 0, 0, 0, tarPin.Buffer.mCoreObject, 0, 0, 0, SrcPinIn.Attachement.Width, SrcPinIn.Attachement.Height);
                    //}

                    cmdlist.PushGpuDraw(mCopyDrawcall);
                }
                cmdlist.FlushDraws();
                cmdlist.EndCommand();
                UEngine.Instance.GfxDevice.RenderCmdQueue.QueueCmdlist(cmdlist);
            }   
        }
    }

    public class UCopy2NextFrameNode : Graphics.Pipeline.Common.URenderGraphNode
    {
        public Common.URenderGraphPin SrcPinIn = Common.URenderGraphPin.CreateInput("Src");
        public Common.URenderGraphPin PrevPinOut = Common.URenderGraphPin.CreateOutput("Prev", false, EPixelFormat.PXF_UNKNOWN);

        public UCopy2NextFrameNode()
        {
            Name = "Copy2NextFrameNode";
        }
        public override void InitNodePins()
        {
            AddInput(SrcPinIn, NxRHI.EBufferType.BFT_SRV);
            AddOutput(PrevPinOut, NxRHI.EBufferType.BFT_SRV);
        }
        public override async System.Threading.Tasks.Task Initialize(URenderPolicy policy, string debugName)
        {
            var rc = UEngine.Instance.GfxDevice.RenderContext;

            await base.Initialize(policy, debugName);
            BasePass.Initialize(rc, debugName + ".BasePass");

            mCopyDrawcall = UEngine.Instance.GfxDevice.RenderContext.CreateCopyDraw();
        }
        public UAttachBuffer[] ResultBuffer = new UAttachBuffer[2];
        public UAttachBuffer Current { get => ResultBuffer[0]; }
        public UAttachBuffer Previos { get => ResultBuffer[1]; }
        public bool IsCpuAceesResult { get; set; } = false;
        public NxRHI.UCopyDraw mCopyDrawcall;
        public override void FrameBuild(Graphics.Pipeline.URenderPolicy policy)
        {
            var attachement = RenderGraph.AttachmentCache.ImportAttachment(PrevPinOut);
            if (SrcPinIn.Attachement.Format != PrevPinOut.Attachement.Format ||
                SrcPinIn.Attachement.Width != PrevPinOut.Attachement.Width ||
                SrcPinIn.Attachement.Height != PrevPinOut.Attachement.Height)
            {
                PrevPinOut.Attachement.Format = SrcPinIn.Attachement.Format;
                PrevPinOut.Attachement.Width = SrcPinIn.Attachement.Width;
                PrevPinOut.Attachement.Height = SrcPinIn.Attachement.Height;

                CoreSDK.DisposeObject(ref ResultBuffer[0]);
                CoreSDK.DisposeObject(ref ResultBuffer[1]);
                ResultBuffer[0] = new UAttachBuffer();
                ResultBuffer[1] = new UAttachBuffer();
                ResultBuffer[0].BufferDesc = SrcPinIn.Attachement.BufferDesc;
                ResultBuffer[0].CreateBufferViews(in ResultBuffer[0].BufferDesc);

                ResultBuffer[1].BufferDesc = SrcPinIn.Attachement.BufferDesc;
                ResultBuffer[1].CreateBufferViews(in ResultBuffer[0].BufferDesc);
            }
            attachement.Buffer = Previos.Buffer;
            attachement.Srv = Previos.Srv;
            attachement.Rtv = Previos.Rtv;
            attachement.Dsv = Previos.Dsv;
            attachement.Uav = Previos.Uav;
        }
        [ThreadStatic]
        private static Profiler.TimeScope ScopeTick = Profiler.TimeScopeManager.GetTimeScope(typeof(UCopyNode), nameof(TickLogic));
        public override unsafe void TickLogic(GamePlay.UWorld world, URenderPolicy policy, bool bClear)
        {
            using (new Profiler.TimeScopeHelper(ScopeTick))
            {
                if (mCopyDrawcall == null)
                    return;
                var cmdlist = BasePass.DrawCmdList;
                cmdlist.BeginCommand();
                {
                    var srcPin = GetAttachBuffer(SrcPinIn);

                    if (srcPin.Buffer.GetType() == typeof(NxRHI.UBuffer) && Current.Buffer.GetType() == typeof(NxRHI.UBuffer))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Buffer2Buffer;
                    }
                    else if (srcPin.Buffer.GetType() == typeof(NxRHI.UTexture) && Current.Buffer.GetType() == typeof(NxRHI.UTexture))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Texture2Texture;
                    }
                    else if (srcPin.Buffer.GetType() == typeof(NxRHI.UTexture) && Current.Buffer.GetType() == typeof(NxRHI.UBuffer))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Texture2Buffer;
                    }
                    else if (srcPin.Buffer.GetType() == typeof(NxRHI.UTexture) && Current.Buffer.GetType() == typeof(NxRHI.UBuffer))
                    {
                        mCopyDrawcall.Mode = NxRHI.ECopyDrawMode.CDM_Buffer2Texture;
                    }
                    mCopyDrawcall.BindSrc(srcPin.Buffer);
                    mCopyDrawcall.BindDest(Current.Buffer);

                    //var fp = new NxRHI.FSubResourceFootPrint();
                    //fp.SetDefault();
                    //mCopyDrawcall.mCoreObject.FootPrint = fp;
                    
                    cmdlist.PushGpuDraw(mCopyDrawcall);
                }
                cmdlist.FlushDraws();
                cmdlist.EndCommand();
                UEngine.Instance.GfxDevice.RenderCmdQueue.QueueCmdlist(cmdlist);
            }
        }

        public override unsafe void TickSync(URenderPolicy policy)
        {
            base.TickSync(policy);
            MathHelper.Swap(ref ResultBuffer[0], ref ResultBuffer[1]);
        }
    }
}
