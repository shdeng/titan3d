﻿using System;
using System.Collections.Generic;

namespace EngineNS
{
    public interface ITickable
    {
        //int GetTickOrder();
        void TickLogic(float ellapse);
        void TickRender(float ellapse);
        void TickBeginFrame(float ellapse);
        void TickSync(float ellapse);
    }
    public interface IMemberTickable
    {
        System.Threading.Tasks.Task<bool> Initialize(object host);
        void Cleanup(object host);
        void TickLogic(object host, float ellapse);
        void TickRender(object host, float ellapse);
        void TickSync(object host, float ellapse);
    }
    public class UMemberTickables
    {
        public List<IMemberTickable> Members = new List<IMemberTickable>();
        public void CollectMembers(object host)
        {
            var props = host.GetType().GetProperties();
            foreach (var i in props)
            {
                if (i.PropertyType.GetInterface("IMemberTickable") != null)
                {
                    var member = i.GetValue(host) as IMemberTickable;
                    if (member != null)
                        Members.Add(member);
                }
            }
        }
        public async System.Threading.Tasks.Task InitializeMembers(object host)
        {
            foreach (var i in Members)
            {
                if (await i.Initialize(host) == false)
                {
                    Profiler.Log.WriteLine(Profiler.ELogTag.Error, "UMemberTickables", $"UMemberTickables:{i.GetType().FullName} Initialize failed");
                }
            }
        }
        public void CleanupMembers(object host)
        {
            foreach (var i in Members)
            {
                i.Cleanup(host);
            }
        }
        public void TickLogic(object host, float ellapse)
        {
            foreach(var i in Members)
            {
                i.TickLogic(host, ellapse);
            }
        }
        public void TickRender(object host, float ellapse)
        {
            foreach (var i in Members)
            {
                i.TickRender(host, ellapse);
            }
        }
        public void TickSync(object host, float ellapse)
        {
            foreach (var i in Members)
            {
                i.TickSync(host, ellapse);
            }
        }
    }

    public partial class UTickableManager
    {
        public List<WeakReference<ITickable>> Tickables { get; } = new List<WeakReference<ITickable>>();
        public void Cleanup()
        {
            Tickables.Clear();
        }
        [Rtti.Meta]
        public void AddTickable(ITickable tickable)
        {
            lock(this)
            {
                foreach(var i in Tickables)
                {
                    ITickable cur;
                    if(i.TryGetTarget(out cur))
                    {
                        if (tickable == cur)
                            return;
                    }
                }
                Tickables.Add(new WeakReference<ITickable>(tickable));
                for (int i = 0; i < Tickables.Count; i++)
                {
                    ITickable xx;
                    Tickables[i].TryGetTarget(out xx);
                    if (xx == null)
                    {
                        Tickables.RemoveAt(i);
                        i--;
                    }
                }
                //Tickables.Sort((x, y) =>
                //{
                //    ITickable xx, yy;
                //    x.TryGetTarget(out xx);
                //    y.TryGetTarget(out yy);
                //    return xx.GetTickOrder().CompareTo(yy.GetTickOrder());
                //}); 
            }
        }
        [Rtti.Meta]
        public void RemoveTickable(ITickable tickable)
        {
            lock (this)
            {
                foreach (var i in Tickables)
                {
                    ITickable cur;
                    if (i.TryGetTarget(out cur))
                    {
                        if (tickable == cur)
                        {
                            Tickables.Remove(i);
                            return;
                        }
                    }
                }
            }
        }
    }

    
}
