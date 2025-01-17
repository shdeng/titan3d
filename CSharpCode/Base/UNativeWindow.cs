﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EngineNS
{
    public partial class UNativeWindow
    {
    }

    public interface IEventProcessor
    {
        unsafe bool OnEvent(in Bricks.Input.Event e);
    }
    public partial class UEventProcessorManager
    {
        public List<IEventProcessor> Processors { get; } = new List<IEventProcessor>();
        private List<IEventProcessor> WaitRemoved { get; } = new List<IEventProcessor>();

        public void RegProcessor(IEventProcessor ep)
        {
            lock (this)
            {
                if (Processors.Contains(ep))
                    return;
                Processors.Add(ep);
            }
        }
        public void UnregProcessor(IEventProcessor ep)
        {
            lock (this)
            {
                WaitRemoved.Add(ep);
            }
        }
        partial void OnTickWindow(in Bricks.Input.Event evt);
        public void TickEvent(in Bricks.Input.Event evt)
        {
            if (evt.Type == Bricks.Input.EventType.KEYDOWN && evt.Keyboard.Keysym.Scancode == Bricks.Input.Scancode.SCANCODE_F1)
            {
                unsafe
                {
                    IRenderDocTool.GetInstance().SetGpuDevice(UEngine.Instance.GfxDevice.RenderContext.mCoreObject);
                    //IRenderDocTool.GetInstance().SetActiveWindow(HWindow.ToPointer());
                    UEngine.Instance.GfxDevice.RenderCmdQueue.CaptureRenderDocFrame = true;
                }
            }

            OnTickWindow(in evt);
            
            for (int i = 0; i < Processors.Count; i++)
            {
                if (Processors[i].OnEvent(in evt) == false)
                    break;
            }
            lock (this)
            {
                foreach (var i in WaitRemoved)
                {
                    Processors.Remove(i);
                }
                WaitRemoved.Clear();
            }
        }
    }
}
