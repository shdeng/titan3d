﻿using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Rtti
{
    public class AssemblyEntry
    {
        public class ULevelServerAssemblyDesc : UAssemblyDesc
        {
            public ULevelServerAssemblyDesc()
            {
                Profiler.Log.WriteLine(Profiler.ELogTag.Info, "Core", "Plugins:LevelServer AssemblyDesc Created");
            }
            ~ULevelServerAssemblyDesc()
            {
                Profiler.Log.WriteLine(Profiler.ELogTag.Info, "Core", "Plugins:LevelServer AssemblyDesc Destroyed");
            }
            public override string Name { get => "LevelServer"; }
            public override string Service { get { return "Plugins"; } }
            public override bool IsGameModule { get { return false; } }
            public override string Platform { get { return "Global"; } }
        }
        static ULevelServerAssemblyDesc AssmblyDesc = new ULevelServerAssemblyDesc();
        public static UAssemblyDesc GetAssemblyDesc()
        {
            return AssmblyDesc;
        }
    }
}


namespace EngineNS.Plugins.ClientRobot
{
    public class UPluginLoader
    {
        public static ULevelServerPlugin mPluginObject = new ULevelServerPlugin();
        public static Bricks.AssemblyLoader.UPlugin GetPluginObject()
        {
            return mPluginObject;
        }
    }
    public class ULevelServerPlugin : Bricks.AssemblyLoader.UPlugin
    {
        RobotClient.URobot Robot;
        public override void OnLoadedPlugin()
        {
            Robot = new RobotClient.URobot();
            UEngine.Instance.TickableManager.AddTickable(Robot);
            var nu = Robot.Initialize();
        }
        public override void OnUnloadPlugin()
        {
            UEngine.Instance.TickableManager.RemoveTickable(Robot);
            Robot = null;
        }
    }
}