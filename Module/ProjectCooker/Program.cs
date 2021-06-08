﻿using System;

namespace ProjectCooker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello!Titan Cooker!");

            var cfgFile = UCookCommand.FindArgument(args, "CookCfg=");
            //var cfgFile = @"F:\titan3d\content\EngineConfigForCook.cfg";
            EngineNS.UEngine.UGfxDeviceType = typeof(EngineNS.Graphics.Pipeline.UGfxDeviceConsole);
            EngineNS.UEngine.StartEngine(new EngineNS.UEngine(), cfgFile);

            var cmd = UCookCommand.FindArgument(args, "ExeCmd=");
            Action action = async () =>
            {
                switch (cmd)
                {
                    case "SaveAsLastest":
                        {
                            var exe = new Command.USaveAsLastest();
                            await exe.ExecuteCommand(args);
                        }
                        break;
                    case "RenameAsset":
                        {
                            var exe = new Command.URenameAsset();
                            await exe.ExecuteCommand(args);
                        }
                        break;
                }
                EngineNS.UEngine.Instance.PostQuitMessage();
            };
            action();

            while (true)
            {
                if (EngineNS.UEngine.Instance.Tick() == false)
                    break;
            }

            EngineNS.UEngine.Instance.FinalCleanup();
        }
    }
}