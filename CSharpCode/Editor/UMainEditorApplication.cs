﻿//#define UseWindowTest

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using NPOI.SS.Formula.Functions;
using EngineNS.UI.Editor;
using EngineNS.Macross;
using System.Runtime.InteropServices;
//using SDL2;

namespace EngineNS.Editor
{
    public partial class UMainEditorApplication : USlateApplication, ITickable
    {
        public int GetTickOrder()
        {
            return 0;
        }
        public UAssetEditorManager AssetEditorManager { get; } = new UAssetEditorManager();
        
        public UMainEditorApplication()
        {
            mLogWatcher = new EGui.Controls.ULogWatcher();
            mCpuProfiler = new Editor.Forms.UCpuProfiler();
            mGpuProfiler = new Editor.Forms.TtGpuProfiler();
            mMemProfiler = new Forms.TtMemoryProfiler();
            mMainInspector = new Forms.UInspector();
            //WorldViewportSlate = new UEditorWorldViewportSlate(true);
            //mWorldOutliner = new Editor.Forms.UWorldOutliner(WorldViewportSlate);

            mBrickManager = new Bricks.ProjectGen.UBrickManager();
            mEditorSettings = new Forms.UEditorSettings();
            mPIEController = new UPIEController();
        }
        private bool IsVisible = true;
        //public Editor.Forms.UWorldOutliner mWorldOutliner;
        public EGui.Controls.ULogWatcher mLogWatcher;
        public Editor.Forms.UCpuProfiler mCpuProfiler;
        public Editor.Forms.TtGpuProfiler mGpuProfiler;
        public Editor.Forms.TtMemoryProfiler mMemProfiler;
        public Editor.Forms.UInspector mMainInspector;
        public Bricks.ProjectGen.UBrickManager mBrickManager = null;
        public Editor.Forms.UEditorSettings mEditorSettings;
        public UPIEController mPIEController;

        //public UEditorWorldViewportSlate WorldViewportSlate = null;
        //public override EGui.Slate.UWorldViewportSlate GetWorldViewportSlate()
        //{
        //    return WorldViewportSlate;
        //}        
        ///////////////////////////////////////////
        public EGui.Controls.UContentBrowser ContentBrowser = new EGui.Controls.UContentBrowser();
        public override void Cleanup()
        {
            UEngine.Instance?.TickableManager.RemoveTickable(this);
#if (UseWindowTest)
            mWinTest.Cleanup();
#endif
            base.Cleanup();
        }
        public override async System.Threading.Tasks.Task<bool> InitializeApplication(NxRHI.UGpuDevice rc, RName rpName)
        {
            await base.InitializeApplication(rc, rpName);

            await ContentBrowser.Initialize();
            UEngine.RootFormManager.RegRootForm(ContentBrowser);

            //await WorldViewportSlate.Initialize(this, rpName, 0, 1);

            await mMainInspector.Initialize();

            await mEditorSettings.Initialize();
            //UEngine.Instance.Config.PlayGameName = RName.GetRName("utest/test_game01.macross");

            mMainInspector.PropertyGrid.PGName = "MainInspector";
            mMainInspector.PropertyGrid.Target = EGui.UIProxy.StyleConfig.Instance;// WorldViewportSlate;

            UEngine.Instance.TickableManager.AddTickable(this);

            EGui.UIProxy.StyleConfig.Instance.ResetStyle();
            /////////////////////////////////
#if (UseWindowTest)
            mWinTest.Initialize();
            Editor.UMainEditorApplication.RegRootForm(mWinTest);
#endif
            /////////////////////////////////

            InitMainMenu();

            return true;
        }
#if PWindow
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
#endif
        public void InitMainMenu()
        {
            mMenuItems = new List<EGui.UIProxy.MenuItemProxy>()
            {
                new EGui.UIProxy.MenuItemProxy()
                {
                    MenuName = "File",
                    IsTopMenuItem = true,
                    SubMenus = new List<EGui.UIProxy.IUIProxyBase>()
                    {
                        new EGui.UIProxy.NamedMenuSeparator()
                        {
                            Name = "OPEN",
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "Load...",
                            Shortcut = "Ctrl+L",
                            Icon = new EGui.UIProxy.ImageProxy()
                            {
                                ImageFile = RName.GetRName("icons/icons.srv", RName.ERNameType.Engine),
                                ImageSize = new Vector2(16, 16),
                                UVMin = new Vector2(140.0f/1024, 265.0f/1024),
                                UVMax = new Vector2(156.0f/1024, 281.0f/1024),
                            },
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                // Do sth
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "Save...",
                            Shortcut = "Ctrl+S",
                            Icon = new EGui.UIProxy.ImageProxy()
                            {
                                ImageFile = RName.GetRName("icons/icons.srv", RName.ERNameType.Engine),
                                ImageSize = new Vector2(16, 16),
                                UVMin = new Vector2(124.0f/1024, 305.0f/1024),
                                UVMax = new Vector2(140.0f/1024, 321.0f/1024),
                            },
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                // Do sth
                            },
                        },
                    },
                },
                new EGui.UIProxy.MenuItemProxy()
                {
                    MenuName = "Tools",
                    IsTopMenuItem = true,
                    SubMenus = new List<EGui.UIProxy.IUIProxyBase>()
                    {
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "CompileMacross",
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                var csFilesPath = UEngine.Instance.FileManager.GetRoot(IO.TtFileManager.ERootDir.Game);
                                var projectFile = UEngine.Instance.FileManager.GetRoot(IO.TtFileManager.ERootDir.EngineSource) + UEngine.Instance.EditorInstance.Config.GameProject;
                                var assemblyFile = UEngine.Instance.FileManager.GetRoot(IO.TtFileManager.ERootDir.EngineSource) + UEngine.Instance.EditorInstance.Config.GameAssembly;

                                if (UMacrossModule.CompileGameProject(csFilesPath, projectFile, assemblyFile))
                                {
                                    UEngine.Instance.MacrossModule.ReloadAssembly(assemblyFile);
                                }
                                //var gameAssembly = UEngine.Instance.FileManager.GetRoot(IO.FileManager.ERootDir.Root) + UEngine.Instance.EditorInstance.Config.GameAssembly;

                                //UEngine.Instance.MacrossModule.ReloadAssembly(gameAssembly);
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "PrintAttachmentPool",
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                UEngine.Instance.GfxDevice.AttachBufferManager.PrintCachedBuffer = true;
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "Cap",
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                unsafe
                                {
                                    IRenderDocTool.GetInstance().SetGpuDevice(UEngine.Instance.GfxDevice.RenderContext.mCoreObject);
                                    IRenderDocTool.GetInstance().SetActiveWindow(this.NativeWindow.HWindow.ToPointer());
                                    UEngine.Instance.GfxDevice.RenderCmdQueue.CaptureRenderDocFrame = true;
                                }
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "CapMem",
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                var save = PrevMemCapture;
                                PrevMemCapture = new EngineNS.Profiler.TtNativeMemCapture();
                                PrevMemCapture.CaptureNativeMemoryState();
                                if (save != null)
                                    PrevMemCapture.GetIncreaseTypes(save);
                                    
                                //UEngine.Instance.EventPoster.RunOn((state)=>
                                //{
                                    //return true;
                                //}, Thread.Async.EAsyncTarget.Main);
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "ShowConsole",
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                item.Selected = !item.Selected;
#if PWindow
                                var handle = GetConsoleWindow();
                                ShowWindow(handle, item.Selected ? 1 : 0);
#endif
                            },
                        },
                    },
                },
                new EGui.UIProxy.MenuItemProxy()
                {
                    MenuName = "Windows",
                    IsTopMenuItem = true,
                    SubMenus = new List<EGui.UIProxy.IUIProxyBase>()
                    {
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "GC",
                            Selected = false,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                System.GC.Collect();
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "LogWatcher",
                            Selected = true,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mLogWatcher.Visible = !mLogWatcher.Visible;
                                var application = UEngine.Instance.GfxDevice.SlateApplication as EngineNS.Editor.UMainEditorApplication;
                                item.Selected = mLogWatcher.Visible;
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "CpuProfiler",
                            Selected = true,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mCpuProfiler.Visible = !mCpuProfiler.Visible;
                                var application = UEngine.Instance.GfxDevice.SlateApplication as EngineNS.Editor.UMainEditorApplication;
                                //mCpuProfiler.DockId = application.CenterDockId;
                                item.Selected = mCpuProfiler.Visible;
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "GpuProfiler",
                            Selected = true,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mGpuProfiler.Visible = !mGpuProfiler.Visible;
                                var application = UEngine.Instance.GfxDevice.SlateApplication as EngineNS.Editor.UMainEditorApplication;
                                //mCpuProfiler.DockId = application.CenterDockId;
                                item.Selected = mGpuProfiler.Visible;
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "MemProfiler",
                            Selected = true,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mMemProfiler.Visible = !mMemProfiler.Visible;
                                var application = UEngine.Instance.GfxDevice.SlateApplication as EngineNS.Editor.UMainEditorApplication;
                                item.Selected = mMemProfiler.Visible;
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "MainInspector",
                            Selected = true,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mMainInspector.Visible = !mMainInspector.Visible;
                                item.Selected = mMainInspector.Visible;
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "BrickManager",
                            Selected = false,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mBrickManager.Visible = !mBrickManager.Visible;
                                item.Selected = mBrickManager.Visible;
                                if(mBrickManager.Visible)
                                {
                                    var task = mBrickManager.Initialize();
                                }
                            },
                        },
                        new EGui.UIProxy.MenuItemProxy()
                        {
                            MenuName = "PIE Controller",
                            Selected = false,
                            Action = (EGui.UIProxy.MenuItemProxy item, Support.UAnyPointer data)=>
                            {
                                mPIEController.Visible = !mPIEController.Visible;
                                item.Selected = mPIEController.Visible;
                                if(mPIEController.Visible)
                                    _ = mPIEController.Initialize();
                            },
                        },
                    },
                },
            };
        }

        #region DrawGui
        bool _showDemoWindow = false;
        public float LeftWidth = 0;
        public float CenterWidth = 0;

        ////////////////////////
#if (UseWindowTest)
        UIWindowsTest mWinTest = new UIWindowsTest();
#endif
        ////////////////////////

        //public uint LeftDockId { get; private set; } = 0;
        //public uint CenterDockId { get; private set; } = 0;

        public UInt32 ActiveViewportId = UInt32.MaxValue;
        protected unsafe override void OnDrawUI()
        {
            {
                var count = ImGuiAPI.PlatformIO_Viewports_Size(ImGuiAPI.GetPlatformIO());
                for (int i = 0; i < count; i++)
                {
                    var pViewport = ImGuiAPI.PlatformIO_Viewports_Get(ImGuiAPI.GetPlatformIO(), i);
                    var nativeWindow = pViewport->PlatformHandle;
                    if (UNativeWindow.IsInputFocus((IntPtr)nativeWindow))
                    {
                        ActiveViewportId = pViewport->ID;
                    }
                }
            }
            if (IsVisible == false)
            {
                AssetEditorManager.CloseAll();
                var num = ImGuiAPI.PlatformIO_Viewports_Size(ImGuiAPI.GetPlatformIO());
                if (num == 1)
                {//只剩下被特意隐藏的主Viewport了
                    UEngine.Instance.PostQuitMessage();
                }
                return;
            }

//            if (LeftDockId == 0)
//            {
//                LeftDockId = ImGuiAPI.GetID("LeftDocker");
//                CenterDockId = ImGuiAPI.GetID("CenterDocker");

//                mCpuProfiler.DockId = CenterDockId;
//#if PWindow
//                mClrProfiler.DockId = CenterDockId;
//#endif
//                //WorldViewportSlate.DockId = CenterDockId;
//                //mWorldOutliner.DockId = LeftDockId;
//                ContentBrowser.DockId = CenterDockId;
//                mBrickManager.DockId = CenterDockId;
//                mEditorSettings.DockId = LeftDockId;
//            }

            var io = ImGuiAPI.GetIO();
            if ((io.ConfigFlags & ImGuiConfigFlags_.ImGuiConfigFlags_DockingEnable) != 0)
            {
                ImGuiAPI.DockSpaceOverViewport(ImGuiAPI.GetMainViewport(), ImGuiDockNodeFlags_.ImGuiDockNodeFlags_None, null);
                
                //var dockspace_id = ImGuiAPI.GetID("MyDockSpace");
                //ImGuiDockNodeFlags_ dockspace_flags = ImGuiDockNodeFlags_.ImGuiDockNodeFlags_None;
                //var winClass = new ImGuiWindowClass();
                //winClass.UnsafeCallConstructor();
                //var sz = new Vector2(0.0f, 0.0f);                
                //ImGuiAPI.DockSpace(dockspace_id, ref sz, dockspace_flags, ref winClass);
                //winClass.UnsafeCallDestructor();
            }
            try
            {
                var mainPos = new Vector2(0);
                ImGuiAPI.SetNextWindowPos(in mainPos, ImGuiCond_.ImGuiCond_FirstUseEver, in mainPos);
                var wsz = new Vector2(1290, 800);
                ImGuiAPI.SetNextWindowSize(in wsz, ImGuiCond_.ImGuiCond_FirstUseEver);
                uint dockId = 0;
                if (EGui.UIProxy.DockProxy.BeginMainForm(UEngine.Instance.Config.ConfigName, ref IsVisible, ref dockId, //ImGuiWindowFlags_.ImGuiWindowFlags_NoMove |
                    //ImGuiWindowFlags_.ImGuiWindowFlags_NoResize |
                    ImGuiWindowFlags_.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags_.ImGuiWindowFlags_NoTitleBar |
                    ImGuiWindowFlags_.ImGuiWindowFlags_MenuBar))
                {
                    wsz = ImGuiAPI.GetWindowSize();
                    //DrawToolBar();
                    DrawMainMenu();

                    //ImGuiAPI.Separator();
                    //ImGuiAPI.Columns(2, null, true);
                    //if (LeftWidth == 0)
                    //{
                    //    ImGuiAPI.SetColumnWidth(0, wsz.X * 0.15f);
                    //}
                    //var min = ImGuiAPI.GetWindowContentRegionMin();
                    //var max = ImGuiAPI.GetWindowContentRegionMax();

                    //DrawLeft(ref min, ref max);
                    //LeftWidth = ImGuiAPI.GetColumnWidth(0);
                    //ImGuiAPI.NextColumn();

                    //DrawCenter(ref min, ref max);
                    //CenterWidth = ImGuiAPI.GetColumnWidth(1);
                    //ImGuiAPI.NextColumn();

                    //ImGuiAPI.Columns(1, null, true);
                    if (ImGuiAPI.BeginChild("CenterWindow", in Vector2.MinusOne, false, ImGuiWindowFlags_.ImGuiWindowFlags_None))
                    {
                        ImGuiDockNodeFlags_ dockspace_flags = ImGuiDockNodeFlags_.ImGuiDockNodeFlags_None;
                        var sz = new Vector2(0.0f, 0.0f);
                        //var winClass = new ImGuiWindowClass();
                        //winClass.UnsafeCallConstructor();
                        //ImGuiAPI.DockSpace(CenterDockId, ref sz, dockspace_flags, ref winClass);
                        //winClass.UnsafeCallDestructor();

                        fixed(ImGuiWindowClass* dockclsPtr = &EGui.UIProxy.DockProxy.MainFormDockClass)
                        {
                            ImGuiAPI.DockSpace(EGui.UIProxy.DockProxy.MainFormDockClass.m_ClassId, &sz, dockspace_flags, dockclsPtr);
                        }
                    }
                    ImGuiAPI.EndChild();
                }
                EGui.UIProxy.DockProxy.EndMainForm();

                //WorldViewportSlate.DockId = CenterDockId;

                UEngine.RootFormManager.DrawRootForms();
#if (UseWindowTest)
                mWinTest.OnDraw();
#endif

                AssetEditorManager.OnDraw();
            }
            catch
            {

            }

            if (_showDemoWindow)
            {
                // Normally user code doesn't need/want to call this because positions are saved in .ini file anyway.
                // Here we just want to make the demo initial state a bit more friendly!
                var pos = new Vector2(650, 20);
                var pivot = new Vector2(0, 0);
                ImGuiAPI.SetNextWindowPos(in pos, ImGuiCond_.ImGuiCond_FirstUseEver, in pivot);
                ImGuiAPI.ShowDemoWindow(ref _showDemoWindow);
            }
        }
        List<EGui.UIProxy.MenuItemProxy> mMenuItems = new List<EGui.UIProxy.MenuItemProxy>();
        private void DrawMainMenu()
        {
            if (ImGuiAPI.BeginMenuBar())
            {
                var drawList = ImGuiAPI.GetWindowDrawList();
                for (int i = 0; i < mMenuItems.Count; i++)
                {
                    mMenuItems[i].OnDraw(in drawList, in Support.UAnyPointer.Default);
                }
                ImGuiAPI.EndMenuBar();
            }
            //if (ImGuiAPI.BeginMenuBar())
            //{
            //    if (ImGuiAPI.BeginMenu("File", true))
            //    {
            //        if (ImGuiAPI.MenuItem("Load", null, false, true))
            //        {
                        
            //        }
            //        if (ImGuiAPI.MenuItem("Save", null, false, true))
            //        {
                        
            //        }
            //        ImGuiAPI.EndMenu();
            //    }
            //    if (ImGuiAPI.BeginMenu("Windows", true))
            //    {
            //        var check = mWorldOutliner.Visible;
            //        ImGuiAPI.Checkbox("##mWorldOutliner", ref check);
            //        ImGuiAPI.SameLine(0, -1);
            //        if ( ImGuiAPI.MenuItem("WorldOutliner", null, false, true))
            //        {
            //            mWorldOutliner.Visible = !mWorldOutliner.Visible;
            //        }
            //        check = mCpuProfiler.Visible;
            //        ImGuiAPI.Checkbox("##mCpuProfiler", ref check);
            //        ImGuiAPI.SameLine(0, -1);
            //        if (ImGuiAPI.MenuItem("CpuProfiler", null, false, true))
            //        {
            //            mCpuProfiler.Visible = !mCpuProfiler.Visible;
            //        }
            //        check = mMainInspector.Visible;
            //        ImGuiAPI.Checkbox("##mMainInspector", ref check);
            //        ImGuiAPI.SameLine(0, -1);
            //        if (ImGuiAPI.MenuItem("MainInspector", null, false, true))
            //        {
            //            mMainInspector.Visible = !mMainInspector.Visible;
            //        }
            //        check = mMetaViewer.Visible;
            //        ImGuiAPI.Checkbox("##mMetaViewer", ref check);
            //        ImGuiAPI.SameLine(0, -1);
            //        if (ImGuiAPI.MenuItem("MetaViewer", null, false, true))
            //        {
            //            mMetaViewer.Visible = !mMetaViewer.Visible;
            //        }
            //        ImGuiAPI.EndMenu();
            //    }
            //    if (ImGuiAPI.BeginMenu("View", true))
            //    {
            //        var check = this.WorldViewportSlate.RenderPolicy.DisableShadow;
            //        ImGuiAPI.Checkbox("##DisableShadow", ref check);
            //        ImGuiAPI.SameLine(0, -1);
            //        if (ImGuiAPI.MenuItem("DisableShadow", null, false, true))
            //        {
            //            this.WorldViewportSlate.RenderPolicy.DisableShadow = !check;
            //        }

            //        check = this.WorldViewportSlate.RenderPolicy.DisableAO;
            //        ImGuiAPI.Checkbox("##DisableAO", ref check);
            //        ImGuiAPI.SameLine(0, -1);
            //        if (ImGuiAPI.MenuItem("DisableAO", null, false, true))
            //        {
            //            this.WorldViewportSlate.RenderPolicy.DisableAO = !check;
            //        }

            //        ImGuiAPI.EndMenu();
            //    }
            //    ImGuiAPI.EndMenuBar();
            //}
        }
        //bool[] mToolBtn_IsMouseDown = new bool[4];
        //bool[] mToolBtn_IsMouseHover = new bool[4];
        //private unsafe void DrawToolBar()
        //{
        //    var drawList = ImGuiAPI.GetWindowDrawList();
        //    EGui.UIProxy.Toolbar.BeginToolbar(drawList);

        //    if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(drawList, ref mToolBtn_IsMouseDown[1], ref mToolBtn_IsMouseHover[1], null, "Play"))
        //    {
        //        var task = OnPlayGame(UEngine.Instance.Config.PlayGameName);
        //    }
        //    if (EGui.UIProxy.ToolbarIconButtonProxy.DrawButton(drawList, ref mToolBtn_IsMouseDown[2], ref mToolBtn_IsMouseHover[2], null, "Stop"))
        //    {

        //    }

        //    EGui.UIProxy.Toolbar.EndToolbar();
        //}
        //protected unsafe void DrawLeft(ref Vector2 min, ref Vector2 max)
        //{
        //    var size = new Vector2(-1, -1);
        //    if (ImGuiAPI.BeginChild("LeftWindow", in size, false, ImGuiWindowFlags_.ImGuiWindowFlags_None))
        //    {
        //        ImGuiDockNodeFlags_ dockspace_flags = ImGuiDockNodeFlags_.ImGuiDockNodeFlags_None;
        //        var winClass = new ImGuiWindowClass();
        //        winClass.UnsafeCallConstructor();
        //        var sz = new Vector2(0.0f, 0.0f);
        //        ImGuiAPI.DockSpace(LeftDockId, in sz, dockspace_flags, in winClass);
        //        winClass.UnsafeCallDestructor();
        //    }
        //    ImGuiAPI.EndChild();
        //}
        //protected unsafe void DrawCenter(ref Vector2 min, ref Vector2 max)
        //{
        //    var size = new Vector2(-1, -1);
        //    if (ImGuiAPI.BeginChild("CenterWindow", in size, false, 
        //        ImGuiWindowFlags_.ImGuiWindowFlags_None))
        //    {
        //        ImGuiDockNodeFlags_ dockspace_flags = ImGuiDockNodeFlags_.ImGuiDockNodeFlags_None;
        //        var sz = new Vector2(0.0f, 0.0f);
        //        //var winClass = new ImGuiWindowClass();
        //        //winClass.UnsafeCallConstructor();
        //        //ImGuiAPI.DockSpace(CenterDockId, ref sz, dockspace_flags, ref winClass);
        //        //winClass.UnsafeCallDestructor();

        //        ImGuiAPI.DockSpace(CenterDockId, &sz, dockspace_flags, (ImGuiWindowClass*)0);
        //    }
        //    ImGuiAPI.EndChild();
        //}
        //protected unsafe void DrawRight(ref Vector2 min, ref Vector2 max)
        //{
        //    //var size = new Vector2(-1, -1);
        //    //if (ImGuiAPI.BeginChild("RightWindow", in size, false, ImGuiWindowFlags_.ImGuiWindowFlags_None))
        //    //{
        //    //    ImGuiDockNodeFlags_ dockspace_flags = ImGuiDockNodeFlags_.ImGuiDockNodeFlags_None;
        //    //    var winClass = new ImGuiWindowClass();
        //    //    winClass.UnsafeCallConstructor();
        //    //    var sz = new Vector2(0.0f, 0.0f);
        //    //    ImGuiAPI.DockSpace(RightDockId, in sz, dockspace_flags, in winClass);
        //    //    winClass.UnsafeCallDestructor();
        //    //}
        //    //ImGuiAPI.EndChild();
        //}
        #endregion

        //async System.Threading.Tasks.Task OnPlayGame(RName assetName)
        //{
        //    await UEngine.Instance.StartPlayInEditor(UEngine.Instance.GfxDevice.SlateApplication, assetName);
        //}
        #region TestCode
        public static async System.Threading.Tasks.Task TestCreateScene(Graphics.Pipeline.UViewportSlate vpSlate,GamePlay.UWorld world, GamePlay.Scene.UNode root, bool hideTerrain = false)
        {
            var materials = new Graphics.Pipeline.Shader.UMaterialInstance[2];
            materials[0] = await UEngine.Instance.GfxDevice.MaterialInstanceManager.GetMaterialInstance(RName.GetRName("utest/ddd.uminst"));
            materials[1] = await UEngine.Instance.GfxDevice.MaterialInstanceManager.GetMaterialInstance(RName.GetRName("utest/ground.uminst"));
            if (materials[0] == null)
                return;
            {
                var meshData = new GamePlay.Scene.UMeshNode.UMeshNodeData();
                meshData.MeshName = RName.GetRName("utest/mesh/skysphere001.ums");
                var meshNode = new GamePlay.Scene.USkyNode();
                await meshNode.InitializeNode(world, meshData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                meshNode.Parent = root;
                meshNode.Placement.Scale = new Vector3(800.0f);
                meshNode.Placement.Position = DVector3.Zero;
                meshNode.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.None;
                meshNode.NodeData.Name = "SkySphere";
                meshNode.IsAcceptShadow = false;
                meshNode.IsCastShadow = false;
            }

            {
                var meshData = new GamePlay.Scene.UMeshNode.UMeshNodeData();
                meshData.MeshName = RName.GetRName("utest/puppet/mesh/puppet.ums");
                meshData.CollideName = RName.GetRName("utest/puppet/mesh/puppet.vms");
                var meshNode = new GamePlay.Scene.UMeshNode();
                await meshNode.InitializeNode(world, meshData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                meshNode.Parent = root;
                meshNode.Placement.SetTransform(new DVector3(0, 0, 0), new Vector3(0.01f), Quaternion.Identity);
                meshNode.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.Root;
                meshNode.NodeData.Name = "Robot0";
                meshNode.IsAcceptShadow = false;
                meshNode.IsCastShadow = true;

                {
                    var mesh1 = new Graphics.Mesh.UMesh();
                    await mesh1.Initialize(RName.GetRName("utest/puppet/mesh/puppet.ums"), Rtti.UTypeDesc.TypeOf(typeof(Graphics.Mesh.UMdfSkinMesh)));
                    var meshData1 = new GamePlay.Scene.UMeshNode.UMeshNodeData();
                    var meshNode1 = new GamePlay.Scene.UMeshNode();
                    await meshNode1.InitializeNode(world, meshData1, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                    meshNode1.Mesh = mesh1;
                    meshNode1.NodeData.Name = "Robot1";
                    meshNode1.Parent = meshNode;
                    meshNode1.Placement.SetTransform(new DVector3(3, 3, 3), new Vector3(0.01f), Quaternion.RotationAxis(Vector3.UnitY, (float)Math.PI / 4));
                    meshNode1.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.FollowParent;
                    meshNode1.IsAcceptShadow = false;
                    meshNode1.IsCastShadow = true;

                    (meshNode1.NodeData as GamePlay.Scene.UMeshNode.UMeshNodeData).MeshName = RName.GetRName("utest/puppet/mesh/puppet.ums");
                    (meshNode1.NodeData as GamePlay.Scene.UMeshNode.UMeshNodeData).MdfQueueType = Rtti.UTypeDesc.TypeStr(typeof(Graphics.Mesh.UMdfSkinMesh));
                    (meshNode1.NodeData as GamePlay.Scene.UMeshNode.UMeshNodeData).AtomType = Rtti.UTypeDesc.TypeStr(typeof(Graphics.Mesh.UMesh.UAtom));

                    var gameplayMacrossNodeData = new EngineNS.GamePlay.GamePlayMacross.UGamePlayMacrossNode.UGamePlayMacrossNodeData();
                    gameplayMacrossNodeData.MacrossName = RName.GetRName("utest/puppet/testgameplay.macross");
                    
                    {
                        var gamePlayMacrossNode = new GamePlay.GamePlayMacross.UGamePlayMacrossNode();
                        await gamePlayMacrossNode.InitializeNode(world, gameplayMacrossNodeData, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
                        gamePlayMacrossNode.Parent = meshNode1;
                        //await gameplayMacrossNodeData.McGamePlay.Get().ConstructAnimGraph(meshNode1);
                    }

                    //await EngineNS.GamePlay.GamePlayMacross.UGamePlayMacrossNode.AddGamePlayMacrossNodeNode(world, meshNode, gameplayMacrossNodeData, EngineNS.GamePlay.Scene.EBoundVolumeType.Box, typeof(EngineNS.GamePlay.UPlacement));
                }
            }

            {
                var meshData = new GamePlay.Scene.UMeshNode.UMeshNodeData();
                meshData.MeshName = RName.GetRName("utest/brdf_test/chair2.ums");
                //meshData.CollideName = RName.GetRName("utest/puppet/mesh/puppet.vms");
                var meshNode = new GamePlay.Scene.UMeshNode();
                await meshNode.InitializeNode(world, meshData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                meshNode.Parent = root;
                meshNode.Placement.SetTransform(new DVector3(0, 10, 0), new Vector3(1.0f), Quaternion.Identity);
                meshNode.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.Root;
                meshNode.NodeData.Name = "Robot_Chair";
                meshNode.IsAcceptShadow = false;
                meshNode.IsCastShadow = true;
            }

            {
                var meshData = new GamePlay.Scene.UMeshNode.UMeshNodeData();
                meshData.MeshName = RName.GetRName("utest/brdf_test/chair2.ums");
                //meshData.CollideName = RName.GetRName("utest/puppet/mesh/puppet.vms");
                var meshNode = new GamePlay.Scene.UMeshNode();
                await meshNode.InitializeNode(world, meshData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                meshNode.Parent = root;
                meshNode.Placement.SetTransform(new DVector3(0, 10, 0), new Vector3(1.0f), Quaternion.Identity);
                meshNode.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.Root;
                meshNode.NodeData.Name = "Robot_Chair_Far";
                meshNode.IsAcceptShadow = false;
                meshNode.IsCastShadow = true;

                meshNode.Placement.Position = new DVector3(1024 * 100, 0, 0);
            }

            //var materials1 = new Graphics.Pipeline.Shader.UMaterialInstance[1];
            //materials1[0] = await UEngine.Instance.GfxDevice.MaterialInstanceManager.GetMaterialInstance(RName.GetRName("utest/ddd.uminst"));

            ////var cookedMesh = Graphics.Mesh.UMeshDataProvider.MakeBoxWireframe(0, 0, 0, 5, 5, 5).ToMesh();
            ////var cookedMesh = Graphics.Mesh.UMeshDataProvider.MakeSphere(2.5f, 100, 100, 0xfffffff).ToMesh();
            //var cookMeshProvider = Graphics.Mesh.UMeshDataProvider.MakeCylinder(2.0f, 0.5f, 3.0f, 100, 100, 0xfffffff);
            //var cookedMesh = cookMeshProvider.ToMesh();
            ////var cookedMesh = Graphics.Mesh.UMeshDataProvider.MakeTorus(2.0f, 3.0f, 100, 300, 0xfffffff).ToMesh(); 
            ////var cookedMesh = Graphics.Mesh.UMeshDataProvider.MakeCapsule(1.0f, 4.0f, 100, 100, 100, Graphics.Mesh.UMeshDataProvider.ECapsuleUvProfile.Aspect, 0xfffffff).ToMesh();
            //{
            //    var mesh2 = new Graphics.Mesh.UMesh();
            //    var colorVar = materials1[0].FindVar("clr4_0");
            //    if (colorVar != null)
            //    {
            //        colorVar.SetValue(new Vector4(1, 0, 1, 1));
            //    }
            //    var ok1 = mesh2.Initialize(cookedMesh, materials1, Rtti.UTypeDesc.TypeOf(typeof(Graphics.Mesh.UMdfStaticMesh)));
            //    if (ok1)
            //    {
            //        var boxNode = await GamePlay.Scene.UMeshNode.AddMeshNode(world, root, new GamePlay.Scene.UMeshNode.UMeshNodeData(), typeof(GamePlay.UPlacement), mesh2,
            //            DVector3.Zero, Vector3.UnitXYZ, Quaternion.Identity);
            //        boxNode.NodeData.Name = "MakeMeshNode";
            //        boxNode.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.Root;
            //        boxNode.IsCastShadow = true;
            //        boxNode.IsAcceptShadow = true;

            //        //var pxNode = new Bricks.PhysicsCore.URigidBodyNode();
            //        //var rgData = new Bricks.PhysicsCore.URigidBodyNode.URigidBodyNodeData();
            //        //rgData.PxActorType = EPhyActorType.PAT_Static;
            //        //var pc = UEngine.Instance.PhyModue.PhyContext;

            //        //var pxTriMesh = pc.CookTriMesh(cookMeshProvider, null, null, null);
            //        //var pxMtls = new Bricks.PhysicsCore.UPhyMaterial[1];
            //        //pxMtls[0] = pc.PhyMaterialManager.DefaultMaterial;
            //        ////var pxShape = pc.CreateShapeTriMesh(pxMtls, pxTriMesh, Vector3.UnitXYZ, Quaternion.Identity);
            //        //var pxShape = pc.CreateShapeBox(pxMtls[0], new Vector3(20,30,10));
            //        //pxShape.mCoreObject.SetFlag(EPhysShapeFlag.eVISUALIZATION, true);
            //        //pxShape.mCoreObject.SetFlag(EPhysShapeFlag.eSCENE_QUERY_SHAPE, true);
            //        //pxShape.mCoreObject.SetFlag(EPhysShapeFlag.eSIMULATION_SHAPE, true);
            //        //rgData.PxShapes.Add(pxShape);
            //        //await pxNode.InitializeNode(world, rgData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacementBase));
            //        //pxNode.Parent = boxNode;

            //        boxNode.Placement.Position = new DVector3(0, 0, 0);
            //    }
            //}

            {
                var nebulaData = new Bricks.Particle.Simple.USimpleNebulaNode.USimpleNebulaNodeData();
                //nebulaData.MeshName = RName.GetRName("utest/mesh/unit_sphere.ums");
                nebulaData.NebulaName = RName.GetRName("utest/particle001.nebula");
                var meshNode = new Bricks.Particle.Simple.USimpleNebulaNode();
                await meshNode.InitializeNode(world, nebulaData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                meshNode.Parent = root;
                meshNode.Placement.Position = DVector3.Zero;
                meshNode.HitproxyType = Graphics.Pipeline.UHitProxy.EHitproxyType.None;
                meshNode.NodeData.Name = "NebulaParticle";
                meshNode.IsAcceptShadow = false;
                meshNode.IsCastShadow = false;
            }

            {
                var lightData = new GamePlay.Scene.UPointLightNode.ULightNodeData();
                lightData.Name = "PointLight0";
                lightData.Intensity = 100.0f;
                lightData.Radius = 20.0f;
                lightData.Color = new Vector3(1, 0, 0);
                var lightNode = GamePlay.Scene.UPointLightNode.AddPointLightNode(world, root, lightData, new DVector3(10, 10, 10));
            }

            if (hideTerrain == false)
            {
                var terrainNode = new Bricks.Terrain.CDLOD.UTerrainNode();
                var terrainData = new Bricks.Terrain.CDLOD.UTerrainNode.UTerrainData();
                terrainData.Name = "TerrainGen";
                terrainData.PgcName = RName.GetRName("UTest/terraingen.pgc");
                await terrainNode.InitializeNode(world, terrainData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
                terrainNode.Parent = root;
                terrainNode.Placement.Position = DVector3.Zero;
                terrainNode.IsAcceptShadow = true;
                terrainNode.SetActiveCenter(in DVector3.Zero);
            }

            var gridNode = await GamePlay.Scene.UGridNode.AddGridNode(world, root);
            //gridNode.SetStyle(GamePlay.Scene.UNode.ENodeStyles.Invisible);

            gridNode.ViewportSlate = vpSlate;

        }

        public static async System.Threading.Tasks.Task TestCreateCharacter(GamePlay.UWorld world, GamePlay.Scene.UNode root, bool hideTerrain = false)
        {
            var characterController = new GamePlay.Controller.UCharacterController();
            var player = new GamePlay.Player.UPlayer();
            var playerData = new GamePlay.Player.UPlayer.UPlayerData() { CharacterController = characterController };
            await player.InitializeNode(world, playerData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
            player.Parent = root;

            var character = new GamePlay.Character.UCharacter();
            await character.InitializeNode(world, new GamePlay.Character.UCharacter.UCharacterData(), GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
            characterController.ControlledCharacter = character;
            var movement = new GamePlay.Movemnet.UMovement();
            movement.Parent = character;

        }
        #endregion

        #region Tick
        public void TickLogic(float ellapse)
        {
            //WorldViewportSlate.TickLogic(ellapse);
        }
        public void TickRender(float ellapse)
        {
            //WorldViewportSlate.TickRender(ellapse);
        }
        public void TickBeginFrame(float ellapse)
        {

        }
        public void TickSync(float ellapse)
        {
            //WorldViewportSlate.TickSync(ellapse);

            //OnDrawSlate();
        }
        #endregion

        public EngineNS.Profiler.TtNativeMemCapture PrevMemCapture;
    }
}
