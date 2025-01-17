﻿using EngineNS.Support;
using NPOI.SS.Formula.Functions;
using NPOI.SS.Formula.PTG;
using NPOI.Util;
using Org.BouncyCastle.Asn1.Crmf;
using Org.BouncyCastle.Asn1.Mozilla;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace EngineNS.Bricks.Font
{
    [Rtti.Meta]
    public class TtFontSDFAMeta : IO.IAssetMeta
    {
        public override string GetAssetExtType()
        {
            return TtFontSDF.AssetExt;
        }
        public override string GetAssetTypeName()
        {
            return "FONTSDF";
        }
        public override async System.Threading.Tasks.Task<IO.IAsset> LoadAsset()
        {
            //return await UEngine.Instance.GfxDevice.MeshPrimitiveManager.GetMeshPrimitive(GetAssetName());
            return null;
        }
        public override bool CanRefAssetType(IO.IAssetMeta ameta)
        {
            return false;
        }
        protected override Color GetBorderColor()
        {
            return Color.LightGray;
        }
    }
    [Rtti.Meta]
    [TtFontSDF.Import]
    [IO.AssetCreateMenu(MenuName = "FontSDF")]
    public class TtFontSDF : AuxPtrType<Canvas.FTFont>, IO.IAsset
    {
        public const string AssetExt = ".fontsdf";
        public class TtFontDesc : IO.BaseSerializer
        {
            public TtFontDesc()
            {
                
            }
            [Rtti.Meta]
            public int OriginPixelSize { get; set; } = 2048;
            [Rtti.Meta]
            public int FontSize { get; set; } = 64;
            [Rtti.Meta]
            public byte Spread { get; set; } = 5;
            [Rtti.Meta]
            public byte PixelColored { get; set; } = 127;
            public TtFontCharFilter CharFilters { get; set; } = new TtFontCharFilter();
        }
        public class ImportAttribute : IO.IAssetCreateAttribute
        {
            bool bPopOpen = false;
            bool bFileExisting = false;
            RName mDir;
            string mName;
            string mSourceFile;
            public TtFontDesc mDesc = new TtFontDesc();
            ImGui.ImGuiFileDialog mFileDialog = UEngine.Instance.EditorInstance.FileDialog.mFileDialog;
            EGui.Controls.PropertyGrid.PropertyGrid PGAsset = new EGui.Controls.PropertyGrid.PropertyGrid();
            public override void DoCreate(RName dir, Rtti.UTypeDesc type, string ext)
            {
                mDir = dir;
                var noused = PGAsset.Initialize();
                PGAsset.Target = mDesc;
            }
            public override unsafe bool OnDraw(EGui.Controls.UContentBrowser ContentBrowser)
            {
                if (bPopOpen == false)
                    ImGuiAPI.OpenPopup($"Import font", ImGuiPopupFlags_.ImGuiPopupFlags_None);
                bool retValue = false;
                var visible = true;
                ImGuiAPI.SetNextWindowSize(new Vector2(200, 500), ImGuiCond_.ImGuiCond_FirstUseEver);
                if (ImGuiAPI.BeginPopupModal($"Import font", &visible, ImGuiWindowFlags_.ImGuiWindowFlags_None))
                {
                    if (string.IsNullOrEmpty(ContentBrowser.CurrentImporterFile))
                    {
                        var sz = new Vector2(-1, 0);
                        if (ImGuiAPI.Button("Select Font", in sz))
                        {
                            mFileDialog.OpenModal("ChooseFileDlgKey", "Choose File", ".ttf", ".");
                        }
                        // display
                        if (mFileDialog.DisplayDialog("ChooseFileDlgKey"))
                        {
                            // action if OK
                            if (mFileDialog.IsOk() == true)
                            {
                                mSourceFile = mFileDialog.GetFilePathName();
                                mName = IO.TtFileManager.GetPureName(mSourceFile);
                            }
                            // close
                            mFileDialog.CloseDialog();
                        }
                    }
                    else if (string.IsNullOrEmpty(mSourceFile))
                    {
                        mSourceFile = ContentBrowser.CurrentImporterFile;
                        mName = IO.TtFileManager.GetPureName(mSourceFile);
                    }
                    if (bFileExisting)
                    {
                        var clr = new Vector4(1, 0, 0, 1);
                        ImGuiAPI.TextColored(in clr, $"Source:{mSourceFile}");
                    }
                    else
                    {
                        var clr = new Vector4(1, 1, 1, 1);
                        ImGuiAPI.TextColored(in clr, $"Source:{mSourceFile}");
                    }
                    ImGuiAPI.Separator();

                    using (var buffer = BigStackBuffer.CreateInstance(64))
                    {
                        buffer.SetTextUtf8(mName);
                        ImGuiAPI.InputText("##in_rname", buffer.GetBuffer(), (uint)buffer.GetSize(), ImGuiInputTextFlags_.ImGuiInputTextFlags_None, null, (void*)0);
                        var name = buffer.AsTextUtf8();
                        if (mName != name)
                        {
                            mName = name;
                            bFileExisting = IO.TtFileManager.FileExists(mDir.Address + mName + NxRHI.USrView.AssetExt);
                        }
                    }

                    var btSz = Vector2.Zero;
                    if (bFileExisting == false)
                    {
                        if (ImGuiAPI.Button("Create Asset", in btSz))
                        {
                            if (ImportFont())
                            {
                                ImGuiAPI.CloseCurrentPopup();
                                retValue = true;
                            }
                        }
                        ImGuiAPI.SameLine(0, 20);
                    }
                    if (ImGuiAPI.Button("Cancel", in btSz))
                    {
                        ImGuiAPI.CloseCurrentPopup();
                        retValue = true;
                    }

                    ImGuiAPI.Separator();

                    PGAsset.OnDraw(false, false, false);

                    ImGuiAPI.EndPopup();
                }
                if (!visible)
                    retValue = true;
                return retValue;
            }
            private bool ImportFont()
            {
                var rn = RName.GetRName(mDir.Name + mName + TtFontSDF.AssetExt, mDir.RNameType);

                mDesc.CharFilters.Excludes.Add(new Vector2ui(0, 65535));
                TtFontSDF.SaveFont(rn, UEngine.Instance.FontModule.FontManager, mSourceFile, mDesc);

                var ameta = new TtFontSDFAMeta();
                ameta.SetAssetName(rn);
                ameta.AssetId = Guid.NewGuid();
                ameta.TypeStr = Rtti.UTypeDescManager.Instance.GetTypeStringFromType(typeof(TtFontSDFAMeta));
                ameta.Description = $"This is a {typeof(TtFontSDFAMeta).FullName}\n";
                ameta.SaveAMeta();

                UEngine.Instance.AssetMetaManager.RegAsset(ameta);
                return true;
            }
        }

        #region IAsset
        public override void Dispose()
        {
            base.Dispose();
        }
        public IO.IAssetMeta CreateAMeta()
        {
            var result = new TtFontSDFAMeta();
            return result;
        }
        public IO.IAssetMeta GetAMeta()
        {
            return UEngine.Instance.AssetMetaManager.GetAssetMeta(AssetName);
        }
        public void UpdateAMetaReferences(IO.IAssetMeta ameta)
        {
            ameta.RefAssetRNames.Clear();
        }
        public void SaveAssetTo(RName name)
        {
            var ameta = this.GetAMeta();
            if (ameta != null)
            {
                UpdateAMetaReferences(ameta);
                ameta.SaveAMeta();
            }
            
        }
        [Rtti.Meta]
        public RName AssetName
        {
            get;
            set;
        }
        #endregion
        public TtFontSDF()
        {
            mCoreObject = Canvas.FTFont.CreateInstance();
        }
        public TtFontSDF(Canvas.FTFont self)
        {
            mCoreObject = self;
            this.Core_AddRef();
        }
        public string Name
        {
            get { return mCoreObject.GetName(); }
        }
        public int FontSize
        {
            get { return mCoreObject.GetFontSize(); }
        }
        public class TtFontCharFilter : IO.BaseSerializer
        {
            public TtFontCharFilter()
            {
                IncludeChars = "abc中国1A，!,";
            }
            [Rtti.Meta]
            public uint CharBegin { get; set; } = 0;
            [Rtti.Meta]
            public uint CharEnd { get; set; } = ushort.MaxValue;
            string mIncludeChars;
            [Rtti.Meta]
            public string IncludeChars 
            { 
                get
                {
                    return mIncludeChars;
                }
                set
                {
                    mIncludeChars = value;

                    unsafe
                    {
                        IncludeUnicodes.Clear();
                        var buffer = Encoding.UTF32.GetBytes(value);
                        if (buffer.Length == 0)
                            return;
                        int num = buffer.Length / 4;
                        fixed (byte* p = &buffer[0])
                        {
                            var pUnicodes = (uint*)p;
                            for (int i = 0; i < num; i++)
                            {
                                IncludeUnicodes.Add(pUnicodes[i]);
                            }
                        }
                    }
                }
            }
            public List<uint> IncludeUnicodes = new List<uint>();
            [Rtti.Meta]
            public List<Vector2ui> Includes { get; set; } = new List<Vector2ui>();
            [Rtti.Meta]
            public List<Vector2ui> Excludes { get; set; } = new List<Vector2ui>();
            public bool IsInclude(uint c)
            {
                if (IncludeUnicodes.Contains(c))
                    return true;
                foreach (var i in Includes)
                {
                    if (c >= i.X && c <= i.Y)
                        return true;
                }
                return false;
            }
            public bool IsExclude(uint c)
            {
                foreach (var i in Excludes)
                {
                    if (c >= i.X && c <= i.Y)
                        return true;
                }
                return false;
            }
        }
        public static unsafe void SaveFont(RName name, TtFontManager manager, string fontName, TtFontDesc desc)
        {
            TtFontCharFilter filter = desc.CharFilters;
            var font = new TtFontSDF();
            font.mCoreObject.Init(UEngine.Instance.GfxDevice.RenderContext.mCoreObject,
                manager.mCoreObject, fontName, desc.OriginPixelSize, desc.OriginPixelSize, desc.OriginPixelSize, false);

            var xnd = new IO.TtXndHolder(fontName, 0, (uint)desc.FontSize);
            var wordPairs = new List<KeyValuePair<uint, ulong>>();
            using (var attrWords = xnd.NewAttribute("Words", 0, 0))
            {
                attrWords.BeginWrite(0);
                for (uint unicode = filter.CharBegin; unicode < filter.CharEnd; unicode++)
                {
                    if (font.mCoreObject.GetCharIndex(unicode) == 0)
                        continue;
                    if (filter.IsInclude(unicode) == false && filter.IsExclude(unicode))
                        continue;
                    var word = font.mCoreObject.GetWord(unicode);
                    if (word.IsValidPointer == false)
                        continue;
                    var sdfWord = word.BuildAsSDFFast(desc.FontSize, desc.FontSize, desc.FontSize, desc.PixelColored, desc.Spread);

                    var offset = attrWords.GetWriterPosition();
                    wordPairs.Add(new KeyValuePair<uint, ulong>(unicode, offset));
                    attrWords.Write(sdfWord.UniCode);
                    attrWords.Write(sdfWord.PixelX);
                    attrWords.Write(sdfWord.PixelY);
                    attrWords.Write(sdfWord.PixelWidth);
                    attrWords.Write(sdfWord.PixelHeight);
                    attrWords.Write(sdfWord.Advance);
                    attrWords.Write(sdfWord.SdfScale);
                    var size = (uint)(sdfWord.PixelWidth * sdfWord.PixelHeight);
                    if (size > 0)
                    {
                        var len = CoreSDK.CompressBound_ZSTD(size) + 5;
                        using(var d = BigStackBuffer.CreateInstance((int)len))
                        {
                            var wSize = (uint)CoreSDK.Compress_ZSTD(d.GetBuffer(), len, sdfWord.GetBufferPtr(), size, 1);
                            attrWords.Write(wSize);
                            attrWords.Write(d.GetBuffer(), wSize);
                        }
                    }
                    //attrWords.Write(sdfWord.GetBufferPtr(), size);

                    sdfWord.NativeSuper.Release();
                    font.mCoreObject.ResetWords();
                }
                attrWords.EndWrite();
                xnd.RootNode.AddAttribute(attrWords);
            }
            using (var attrWords = xnd.NewAttribute("UniCode", 0, 0))
            {
                wordPairs.Sort((x, y) =>
                {
                    return x.Key.CompareTo(y.Key);
                });
                attrWords.BeginWrite((ulong)(wordPairs.Count * 12));
                attrWords.Write(desc.FontSize);
                attrWords.Write(wordPairs.Count);
                foreach (var i in wordPairs)
                {
                    attrWords.Write(i.Key);
                    attrWords.Write(i.Value);
                }
                attrWords.EndWrite();
                xnd.RootNode.AddAttribute(attrWords);
            }
            using (var attrDesc = xnd.NewAttribute("ImportDesc", 0, 0))
            {
                using (var ar = attrDesc.GetWriter(512))
                {
                    ar.Write(desc);
                }
                xnd.RootNode.AddAttribute(attrDesc);
            }
            xnd.SaveXnd(name.Address);
        }

        public unsafe uint GetWords(EngineNS.Canvas.FTWord** pWords, uint count, wchar_t* text, uint numOfChar)
        {
            return mCoreObject.GetWords(pWords, count, text, numOfChar);
        }
        public unsafe uint GetWords(UNativeArray<EngineNS.Canvas.FTWord> words, string text)
        {
            var count = text.Length;
            words.SetSize(count);
            return GetWords((EngineNS.Canvas.FTWord**)words.UnsafeGetElementAddress(0), (uint)count, text);
        }
        public unsafe uint GetWords(EngineNS.Canvas.FTWord** pWords, uint count, string text)
        {
#if PWindow
            fixed(char* p = text)
            {
                return mCoreObject.GetWords(pWords, count, (wchar_t*)p, (uint)text.Length);
            }
#else
            fixed (char* p = text)
            {
                using (var buffer = BigStackBuffer.CreateInstance(text.Length * sizeof(wchar_t)))
                {
                    var numOfUtf32 = System.Text.Encoding.UTF32.GetBytes(p, text.Length, (byte*)buffer.GetBuffer(), text.Length * sizeof(wchar_t));
                    return mCoreObject.GetWords(pWords, count, (wchar_t*)buffer.GetBuffer(), (uint)numOfUtf32);
                }
            }
#endif
        }
    }

    public class TtFontManager : AuxPtrType<Canvas.FTFontManager>
    {
        public const string FontSDFAssetExt = ".fontsdf";
        public const string FontAssetExt = ".font";

        public TtFontManager()
        {
            mCoreObject = Canvas.FTFontManager.CreateInstance();
            mCoreObject.Init();
        }
        public override void Dispose()
        {
            base.Dispose();
        }
        public TtFontSDF GetFontSDF(RName font, int fontSize, int texSizeX, int texSizeY)
        {
            var result = new TtFontSDF(mCoreObject.GetFont(UEngine.Instance.GfxDevice.RenderContext.mCoreObject, font.Address, fontSize, texSizeX, texSizeY, true));
            return result;
        }
        public void Tick(UEngine host)
        {
            mCoreObject.Update(host.GfxDevice.RenderContext.mCoreObject, false);
        }
        public static unsafe void Test()
        {
            var font = new TtFontSDF();
            font.mCoreObject.Init(UEngine.Instance.GfxDevice.RenderContext.mCoreObject,
                UEngine.Instance.FontModule.FontManager.mCoreObject, "F:/TitanEngine/enginecontent/fonts/Roboto-Regular.ttf", 32, 32, 32, false);
            uint index = 0;
            uint unicode = font.mCoreObject.GetFirstUnicode(ref index);
            string t = "中";
            while (index != 0)
            {
                if(unicode == t[0])
                {
                    break;
                }
                unicode = font.mCoreObject.GetNextUnicode(unicode, ref index);
            }
        }
    }

    [Rtti.Meta]
    [Bricks.CodeBuilder.ShaderNode.Control.TtHLSLProvider(Include = "@Engine/Shaders/Bricks/TextFont/FontSDF.cginc")]
    public partial class TtFontHLSLMethod
    {
        [Rtti.Meta]
        [Bricks.CodeBuilder.ShaderNode.Control.TtHLSLProvider(Name = "GetFontSDF")]
        [Bricks.CodeBuilder.ContextMenu("font", "Bricks\\Font\\GetFontSDF", Bricks.CodeBuilder.ShaderNode.UMaterialGraph.MaterialEditorKeyword)]
        public static Vector4 GetFontSDF(int effect, Vector3 baseColor, Vector3 borderColor, float alpha, float lowThreshold = 0, float highThreshold = 0.8f, float smoothValue = 0.5f)
        {
            return Vector4.Zero;
        }
    }

    public class UFontModule : UModule<UEngine>
    {
        TtFontManager mFontManager = new TtFontManager();
        public TtFontManager FontManager { get => mFontManager; }
        public override Task<bool> PostInitialize(UEngine host)
        {
            //var font = FontManager.GetFontSDF(RName.GetRName("fonts/roboto-regular.fontsdf", RName.ERNameType.Engine), 0, 1024, 512);
            //using (var words = UNativeArray<EngineNS.FTWord>.CreateInstance())
            //{
            //    font.GetWords(words, "abc中国1A，!,");
            //}
            return base.PostInitialize(host);
        }
        public override void TickModule(UEngine host)
        {
            FontManager.Tick(host);
            base.TickModule(host);
        }
        public override void Cleanup(UEngine host)
        {
            CoreSDK.DisposeObject(ref mFontManager);
            base.Cleanup(host);
        }
    }
}

namespace EngineNS
{
    partial class UEngine
    {
        public Bricks.Font.UFontModule FontModule { get; } = new Bricks.Font.UFontModule();
    }
}
