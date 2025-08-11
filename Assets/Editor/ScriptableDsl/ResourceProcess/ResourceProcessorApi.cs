using UnityEngine;
//using UnityEngine.UI;
using UnityEditor;
//using UnityEditor.UI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.MemoryProfiler;
using UnityEditorInternal;
using UnityEditor.Profiling;
using UnityEditor.Profiling.Memory.Experimental;
using Unity.MemoryProfilerExtension.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using StoryScript;
using StoryScript.DslExpression;
using UnityEngine.Profiling;

internal enum ProfilerViewType
{
    Hierarchy,
    Timeline,
    RawHierarchy
}

internal static class ResourceEditUtility
{
    internal class BatchProcessInfo
    {
        internal string ResPath = string.Empty;
        internal string DslPath = string.Empty;
    }
    internal class ParamInfo
    {
        internal string Name;
        internal Type Type;
        internal BoxedValue Value;
        internal string StringValue = string.Empty;
        internal string Caption;
        internal string Tooltip;

        internal string Encoding;
        internal BoxedValue MinValue;
        internal BoxedValue MaxValue;
        internal List<string> OptionNames = new List<string>();
        internal Dictionary<string, string> Options = new Dictionary<string, string>();
        internal string OptionStyle = string.Empty;
        internal string FileExts = string.Empty;
        internal string FileInitDir = string.Empty;
        internal string Script = string.Empty;
        internal double NextRunScriptTime = 0;
        //temp
        internal string[] PopupOptionNames = null;
        internal List<string> MultipleOldValues = null;
        internal List<string> MultipleNewValues = null;
    }
    internal class ItemInfo
    {
        internal string AssetPath;
        internal string ScenePath;
        internal AssetImporter Importer;
        internal UnityEngine.Object Object;
        internal string Info;
        internal double Order;
        internal double Value;
        internal string Group;
        internal IList<KeyValuePair<string, BoxedValue>> ExtraList = null;
        internal BoxedValue ExtraObject = BoxedValue.NullObject;
        internal string ExtraListBuildScript = string.Empty;
        internal string ExtraListClickScript = string.Empty;
        internal string RedirectDsl = string.Empty;
        internal IDictionary<string, string> RedirectArgs = new Dictionary<string, string>();
        internal bool Selected;

        internal void PrepareShowInfo()
        {
            if (string.IsNullOrEmpty(Info)) {
                Info = string.Format("{0},{1}", AssetPath, ScenePath);
                Value = 0;
            }
        }
        internal bool IsEqual(string assetPath, string scenePath, string info, double order, double value)
        {
            if (AssetPath == assetPath && ScenePath == scenePath && Info == info && Math.Abs(Order - order) < double.Epsilon && Math.Abs(Value - value) < double.Epsilon)
                return true;
            else
                return false;
        }
    }
    internal class GroupInfo
    {
        internal string Group;
        internal List<ItemInfo> Items = new List<ItemInfo>();
        internal double Sum;
        internal double Max;
        internal double Min;

        internal int Count;
        internal double Avg;
        internal double Order;
        internal double Value;

        internal string AssetPath;
        internal string ScenePath;
        internal string Info;
        internal IList<KeyValuePair<string, BoxedValue>> ExtraList = null;
        internal BoxedValue ExtraObject = BoxedValue.NullObject;
        internal string ExtraListBuildScript = string.Empty;
        internal string ExtraListClickScript = string.Empty;
        internal string RedirectDsl = string.Empty;
        internal IDictionary<string, string> RedirectArgs = null;
        internal bool Selected;

        internal void CopyFrom(GroupInfo other)
        {
            Group = other.Group;
            Items = other.Items;
            Sum = other.Sum;
            Max = other.Max;
            Min = other.Min;

            Count = other.Count;
            Avg = other.Avg;
            Order = other.Order;
            Value = other.Value;

            AssetPath = other.AssetPath;
            ScenePath = other.ScenePath;
            Info = other.Info;
            ExtraList = other.ExtraList;
            ExtraObject = other.ExtraObject;
            ExtraListBuildScript = other.ExtraListBuildScript;
            ExtraListClickScript = other.ExtraListClickScript;
            RedirectDsl = other.RedirectDsl;
            RedirectArgs = other.RedirectArgs;
        }
        internal void PrepareShowInfo()
        {
            if (string.IsNullOrEmpty(Info)) {
                Count = Items.Count;
                Avg = Sum / Count;

                var sb = new StringBuilder();
                sb.AppendFormat("{0}=>Sum:{1},Max:{2},Min:{3},Avg:{4},Count:{5}", Group, Sum, Max, Min, Avg, Count);
                /*
                for (int i = 0; i < Items.Count; ++i) {
                    sb.Append(",");
                    sb.Append(Items[i].Info);
                }
                */
                Order = Items.Count;
                Value = Sum;
                AssetPath = Items[0].AssetPath;
                ScenePath = Items[0].ScenePath;
                Info = sb.ToString();
            }
        }
    }
    internal class SceneDepInfo
    {
        internal HashSet<string> deps = new HashSet<string>();
        internal Dictionary<string, string> name2paths = new Dictionary<string, string>();

        internal void Clear()
        {
            deps.Clear();
            name2paths.Clear();
        }
    }
    internal class MemoryInfo
    {
        internal ulong instanceId;
        internal string name;
        internal string className;
        internal long size;
        internal int refCount;
        internal ulong address;
        internal bool isManaged;
        internal int sortedObjectIndex;
        internal ObjectData objectData;
    }
    internal class MemoryGroupInfo
    {
        internal string group;
        internal int count = 0;
        internal long size = 0;
        internal List<MemoryInfo> memories = new List<MemoryInfo>();
    }
    internal class InstrumentThreadInfo
    {
        internal int theadIndex;
        internal ulong threadId;
        internal string threadName;
        internal string threadGroup;
    }
    internal class InstrumentRecord
    {
        internal int frame;
        internal int threadIndex;
        internal int sampleCount;
        internal int depth;
        internal string name;
        internal string layerPath;
        internal float calls;
        internal float gcMemory;
        internal float totalTime;
        internal float totalPercent;
        internal float selfTime;
        internal float selfPercent;
        internal int markerId;
    }
    internal class InstrumentInfo
    {
        internal int frame;
        internal float fps;
        internal float totalGcMemory;
        internal float totalCpuTime;
        internal float totalGpuTime;
        internal float batch;
        internal float triangle;
        internal SortedDictionary<int, InstrumentThreadInfo> threads = new SortedDictionary<int, InstrumentThreadInfo>();
        internal List<InstrumentRecord> records = new List<InstrumentRecord>();
    }
    internal class uTraceThreadInfo
    {
        internal int timelineIndex;
        internal int theadId;
        internal string threadName;
        internal string threadGroup;
    }
    internal class uTraceTimeline
    {
        internal int frame;
        internal int timelineIndex;
        internal int threadId;
        internal string threadGroup;
        internal string threadName;
        internal int depth;
        internal double startTime;
        internal double endTime;
        internal double time;
        internal string name;
    }
    internal class uTraceFrame
    {
        internal int frame;
        internal double startTime;
        internal double endTime;
        internal double time;
        internal SortedDictionary<int, uTraceThreadInfo> threads = new SortedDictionary<int, uTraceThreadInfo>();
        internal List<uTraceTimeline> records = new List<uTraceTimeline>();
    }
    internal class SectionInfo
    {
        internal ulong vm_start = 0;
        internal ulong vm_end = 0;
        internal ulong size = 0;
    }
    internal class MapsInfo
    {
        internal ulong vm_start = 0;
        internal ulong vm_end = 0;
        internal ulong size = 0;
        internal string flags = string.Empty;
        internal string offset = string.Empty;
        internal string file1 = string.Empty;
        internal string file2 = string.Empty;
        internal string module = string.Empty;
    }
    internal class SmapsInfo
    {
        internal ulong vm_start = 0;
        internal ulong vm_end = 0;
        internal ulong size = 0;
        internal string flags = string.Empty;
        internal string offset = string.Empty;
        internal string file1 = string.Empty;
        internal string file2 = string.Empty;
        internal string module = string.Empty;

        internal ulong sizeKB = 0;
        internal ulong rss = 0;
        internal ulong pss = 0;
        internal ulong shared_clean = 0;
        internal ulong shared_dirty = 0;
        internal ulong private_clean = 0;
        internal ulong private_dirty = 0;
        internal ulong referenced = 0;
        internal ulong anonymous = 0;
        internal ulong swap = 0;
        internal ulong swappss = 0;
    }
    internal class SymbolInfo
    {
        internal ulong Addr;
        internal string Name;
    }
    internal class StifHeader
    {
        internal int Tag1;
        internal int Tag2;
        internal int Tag3;
        internal int Count;
    }
    internal class StifSymbolInfo
    {
        internal int Begin;
        internal int End;
        internal int NameOffset;

        internal static int ReverseEndian(int value)
        {
            s_buffer[3] = (byte)value;
            s_buffer[2] = (byte)(value >> 8);
            s_buffer[1] = (byte)(value >> 16);
            s_buffer[0] = (byte)(value >> 24);
            return BitConverter.ToInt32(s_buffer, 0);
        }
        internal static uint ReverseEndian(uint value)
        {
            s_buffer[3] = (byte)value;
            s_buffer[2] = (byte)(value >> 8);
            s_buffer[1] = (byte)(value >> 16);
            s_buffer[0] = (byte)(value >> 24);
            return BitConverter.ToUInt32(s_buffer, 0);
        }

        private static byte[] s_buffer = new byte[c_max_buffer];
        private const int c_max_buffer = 8;
    }
    internal class DataRow
    {
        internal int RowIndex
        {
            get { return m_RowIndex; }
        }
        internal int CellCount
        {
            get { return m_Fields.Length; }
        }
        internal string this[int ix]
        {
            get {
                return GetCell(ix);
            }
        }
        internal string GetCell(int ix)
        {
            if (ix >= 0 && ix < m_Fields.Length)
                return m_Fields[ix];
            else
                return string.Empty;
        }
        internal string GetLine()
        {
            return GetLine(0);
        }
        internal string GetLine(int skipCols)
        {
            if (skipCols == m_SkipCols && null == m_ColIndexes) {
                return m_Line;
            }
            m_Line = string.Join(m_Delimiter, m_Fields, skipCols, m_Fields.Length - skipCols);
            m_SkipCols = skipCols;
            m_ColIndexes = null;
            return m_Line;
        }
        internal string GetLine(int skipCols, IList<int> colIndexes)
        {
            if (null == colIndexes)
                return GetLine(skipCols);
            if (skipCols == m_SkipCols && null != m_ColIndexes && colIndexes.Count == m_ColIndexes.Count) {
                bool same = true;
                for (int i = 0; i < colIndexes.Count && i < m_ColIndexes.Count; ++i) {
                    if (colIndexes[i] != m_ColIndexes[i]) {
                        same = false;
                        break;
                    }
                }
                if (same)
                    return m_Line;
            }
            var strs = new string[colIndexes.Count];
            int curIx = 0;
            foreach (var ix in colIndexes) {
                if (ix >= skipCols) {
                    strs[curIx++] = m_Fields[ix];
                }
            }
            m_Line = string.Join(m_Delimiter, strs);
            m_SkipCols = skipCols;
            m_ColIndexes = colIndexes;
            return m_Line;
        }
        internal DataRow(int rowIndex, string line, char delimiter)
        {
            m_RowIndex = rowIndex;
            m_Line = line;
            m_SkipCols = 0;
            m_ColIndexes = null;
            m_Delimiter = delimiter.ToString();
            m_Fields = line.Split(delimiter);
        }

        private int m_RowIndex = 0;
        private string m_Line = null;
        private int m_SkipCols = 0;
        private IList<int> m_ColIndexes = null;
        private string m_Delimiter = string.Empty;
        private string[] m_Fields = null;
    }
    internal class DataTable
    {
        internal int RowCount
        {
            get { return m_Rows.Length; }
        }
        internal DataRow this[int ix]
        {
            get {
                return GetRow(ix);
            }
        }
        internal DataRow GetRow(int ix)
        {
            if (ix >= 0 && ix < m_Rows.Length)
                return m_Rows[ix];
            else
                return null;
        }
        internal void Load(string path, Encoding encoding, char delimiter)
        {
            var lines = File.ReadAllLines(path, encoding);
            if (null != lines && lines.Length > 0) {
                m_Rows = new DataRow[lines.Length];
                for (int ix = 0; ix < lines.Length; ++ix) {
                    var line = lines[ix];
                    m_Rows[ix] = new DataRow(ix, line, delimiter);
                }
            }
        }

        private DataRow[] m_Rows = null;
    }
    internal static void InitCalculator(DslCalculator calc)
    {
        calc.OnLog = msg => { UnityEngine.Debug.LogError(msg); };
        calc.Init();
        calc.Register("setparamstomodel", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetParamsToModelExp>());
        calc.Register("getparamsfrommodel", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetParamsFromModelExp>());
        calc.Register("setparamstotexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetParamsToTextureExp>());
        calc.Register("getparamsfromtexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetParamsFromTextureExp>());
        calc.Register("setparamstoprefab", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetParamsToPrefabExp>());
        calc.Register("getparamsfromprefab", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetParamsFromPrefabExp>());
        calc.Register("updatemodeldb", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.UpdateModelDbExp>());
        calc.Register("updatetexturedb", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.UpdateTextureDbExp>());
        calc.Register("updateprefabdb", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.UpdatePrefabDbExp>());
        calc.Register("callscript", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CallScriptExp>());
        calc.Register("setredirect", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetRedirectExp>());
        calc.Register("newitem", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.NewItemExp>());
        calc.Register("setextraobject", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetExtraObjectExp>());
        calc.Register("getextraobject", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetExtraObjectExp>());
        calc.Register("calcextraobjectfieldcount", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcExtraObjectFieldCountExp>());
        calc.Register("newextralist", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.NewExtraListExp>());
        calc.Register("extralistadd", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ExtraListAddExp>());
        calc.Register("extralistclear", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ExtraListClearExp>());
        calc.Register("getreferenceassets", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetReferenceAssetsExp>());
        calc.Register("getreferencebyassets", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetReferenceByAssetsExp>());
        calc.Register("calcrefcount", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcRefCountExp>());
        calc.Register("calcrefbycount", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcRefByCountExp>());
        calc.Register("findasset", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindAssetExp>());
        calc.Register("selectframe", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SelectFrameExp>());
        calc.Register("filtercpusample", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FilterCpuSampleExp>());
        calc.Register("filtergpusample", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FilterGpuSampleExp>());
        calc.Register("selectsample", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SelectSampleExp>());
        calc.Register("selectpropertypath", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SelectPropertyPathExp>());
        calc.Register("setmarkerfilter", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetMarkerFilterExp>());
        calc.Register("setmaxrefbynumperobj", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetMaxRefByNumPerObjExp>());
        calc.Register("findshortestpathtoroot", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindShortestPathToRootExp>());
        calc.Register("getrefbyobjdata", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetRefByObjectDataExp>());
        calc.Register("getrefobjdata", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetRefObjectDataExp>());
        calc.Register("objdatafromaddress", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ObjectDataFromAddressExp>());
        calc.Register("objdatafromunifiedindex", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ObjectDataFromUnifiedObjectIndexExp>());
        calc.Register("objdatafromnativeindex", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ObjectDataFromNativeObjectIndexExp>());
        calc.Register("objdatafrommanagedindex", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ObjectDataFromManagedObjectIndexExp>());
        calc.Register("loadidaprosymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadIdaproSymbolsExp>());
        calc.Register("loadxcodesymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadXcodeSymbolsExp>());
        calc.Register("loadbuglyandroidsymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadBuglyAndroidSymbolsExp>());
        calc.Register("loadbuglyiossymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadBuglyIosSymbolsExp>());
        calc.Register("converttorelativeaddrs", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ConvertToRelativeAddrsExp>());
        calc.Register("mapbuglyandroidsymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MapBuglyAndroidSymbolsExp>());
        calc.Register("mapbuglyiossymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MapBuglyIosSymbolsExp>());
        calc.Register("mapmyhooksymbols", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MapMyhookSymbolsExp>());
        calc.Register("grep", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GrepExp>());
        calc.Register("subst", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SubstExp>());
        calc.Register("setclipboard", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetClipboardExp>());
        calc.Register("getclipboard", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetClipboardExp>());
        calc.Register("selectobject", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SelectObjectExp>());
        calc.Register("selectprojectobject", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SelectProjectObjectExp>());
        calc.Register("selectsceneobject", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SelectSceneObjectExp>());
        calc.Register("saveandreimport", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SaveAndReimportExp>());
        calc.Register("setdirty", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetDirtyExp>());
        calc.Register("getdefaulttexturesetting", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetDefaultTextureSettingExp>());
        calc.Register("gettexturesetting", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetTextureSettingExp>());
        calc.Register("settexturesetting", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetTextureSettingExp>());
        calc.Register("textureisnotastc", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.TextureIsNotAstcExp>());
        calc.Register("isastctexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.IsAstcTextureExp>());
        calc.Register("setastctexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetAstcTextureExp>());
        calc.Register("issceneastctexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.IsSceneAstcTextureExp>());
        calc.Register("setsceneastctexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetSceneAstcTextureExp>());
        calc.Register("istexturenoalphasource", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.IsTextureNoAlphaSourceExp>());
        calc.Register("doestexturehavealpha", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.DoesTextureHaveAlphaExp>());
        calc.Register("correctnonealphatexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CorrectNoneAlphaTextureExp>());
        calc.Register("setnonealphatexture", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetNoneAlphaTextureExp>());
        calc.Register("gettexturecompression", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetTextureCompressionExp>());
        calc.Register("settexturecompression", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetTextureCompressionExp>());
        calc.Register("gettexturestorage", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetTextureStorageMemorySizeExp>());
        calc.Register("gettexturememory", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetTextureRuntimeMemorySizeExp>());
        calc.Register("getruntimememory", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetRuntimeMemorySizeExp>());
        calc.Register("setmaxboundingbox", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetMaxBoundingBoxExp>());
        calc.Register("resetboundingbox", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ResetBoundingBoxExp>());
        calc.Register("mergeboundingbox", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MergeBoundingBoxExp>());
        calc.Register("getboundingbox", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetBoundingBoxExp>());
        calc.Register("addorupdateboundingbox", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.AddOrUpdateBoundingBoxExp>());
        calc.Register("getmeshcompression", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetMeshCompressionExp>());
        calc.Register("setmeshcompression", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetMeshCompressionExp>());
        calc.Register("setmeshimportexternalmaterials", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetMeshImportExternalMaterialsExp>());
        calc.Register("setmeshimportinprefabmaterials", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetMeshImportInPrefabMaterialsExp>());
        calc.Register("closemeshanimationifnoanimation", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CloseMeshAnimationIfNoAnimationExp>());
        calc.Register("collectmeshes", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CollectMeshesExp>());
        calc.Register("collectmeshinfo", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CollectMeshInfoExp>());
        calc.Register("collectanimatorcontrollerinfo", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CollectAnimatorControllerInfoExp>());
        calc.Register("collectprefabinfo", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CollectPrefabInfoExp>());
        calc.Register("getanimationclipinfo", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetAnimationClipInfoExp>());
        calc.Register("getanimationcompression", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetAnimationCompressionExp>());
        calc.Register("setanimationcompression", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetAnimationCompressionExp>());
        calc.Register("getanimationtype", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetAnimationTypeExp>());
        calc.Register("setanimationtype", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetAnimationTypeExp>());
        calc.Register("setextraexposedtransformpaths", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetExtraExposedTransformPathsExp>());
        calc.Register("clearanimationscalecurve", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ClearAnimationScaleCurveExp>());
        calc.Register("getaudiosetting", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetAudioSettingExp>());
        calc.Register("setaudiosetting", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SetAudioSettingExp>());
        calc.Register("splitanimationreference", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.SplitAnimationReferenceExp>());
        calc.Register("calcmeshvertexcomponentcount", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcMeshVertexComponentCountExp>());
        calc.Register("calcmeshtexratio", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcMeshTexRatioExp>());
        calc.Register("calcassetmd5", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcAssetMd5Exp>());
        calc.Register("calcassetsize", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcAssetSizeExp>());
        calc.Register("deleteasset", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.DeleteAssetExp>());
        calc.Register("getshaderutil", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetShaderUtilExp>());
        calc.Register("getshaderpropertycount", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetShaderPropertyCountExp>());
        calc.Register("getshaderpropertynames", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetShaderPropertyNamesExp>());
        calc.Register("removeyamlleafproperties", "removeyamlleafproperties(asset_path,property1,property2,...)", new ExpressionFactoryHelper<ResourceEditApi.RemoveYamlLeafPropertiesExp>());
        calc.Register("checkyaml", "checkyaml(asset_path[,int_start,int_count,bool_partial_conflict])", new ExpressionFactoryHelper<ResourceEditApi.CheckYamlExp>());
        calc.Register("ispathtoolong", "ispathtoolong(asset_path)", new ExpressionFactoryHelper<ResourceEditApi.IsPathTooLongExp>());
        calc.Register("getshadervariants", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetShaderVariantsExp>());
        calc.Register("addshadertocollection", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.AddShaderToCollectionExp>());
        calc.Register("getalldslfiles", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetAllDslFilesExp>());
        calc.Register("buildassetstringlist", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.BuildAssetStringListExp>());
        calc.Register("createrefasset", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CreateRefAssetExp>());
        calc.Register("findrowindex", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindRowIndexExp>());
        calc.Register("findrowindexes", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindRowIndexesExp>());
        calc.Register("findcellindex", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindCellIndexExp>());
        calc.Register("findcellindexes", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindCellIndexesExp>());
        calc.Register("getcellvalue", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetCellValueExp>());
        calc.Register("getcellstring", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetCellStringExp>());
        calc.Register("getcellnumeric", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.GetCellNumericExp>());
        calc.Register("rowtoline", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.RowToLineExp>());
        calc.Register("tabletohashtable", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.TableToHashtableExp>());
        calc.Register("findrowfromhashtable", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindRowFromHashtableExp>());
        calc.Register("loadmanagedheaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadManagedHeapsExp>());
        calc.Register("findmanagedheaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindManagedHeapsExp>());
        calc.Register("matchmanagedheaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MatchManagedHeapsExp>());
        calc.Register("calcmatchedmanagedheapsdiff", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcMatchedManagedHeapsDiffExp>());
        calc.Register("loadmaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadMapsExp>());
        calc.Register("findmaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindMapsExp>());
        calc.Register("matchmaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MatchMapsExp>());
        calc.Register("calcmatchedmapsdiff", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcMatchedMapsDiffExp>());
        calc.Register("loadsmaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadSmapsExp>());
        calc.Register("findsmaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FindSmapsExp>());
        calc.Register("matchsmaps", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.MatchSmapsExp>());
        calc.Register("calcmatchedsmapsdiff", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.CalcMatchedSmapsDiffExp>());
        calc.Register("loadaddrs", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LoadAddrsExp>());
        calc.Register("escapeurl", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.EscapeUrlExp>());
        calc.Register("unescapeurl", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.UnEscapeUrlExp>());
        calc.Register("parseurlargs", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ParseUrlArgsExp>());
        calc.Register("parsebuglyinfo", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.ParseBuglyInfoExp>());
        calc.Register("inthashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.IntHashContainsExp>());
        calc.Register("uinthashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.UintHashContainsExp>());
        calc.Register("longhashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.LongHashContainsExp>());
        calc.Register("ulonghashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.UlongHashContainsExp>());
        calc.Register("floathashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.FloatHashContainsExp>());
        calc.Register("doublehashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.DoubleHashContainsExp>());
        calc.Register("stringhashcontains", string.Empty, new ExpressionFactoryHelper<ResourceEditApi.StringHashContainsExp>());

        UnityEditorApi.Register(calc);
    }
    internal static BoxedValue Filter(ItemInfo item, Dictionary<string, BoxedValue> addVars, List<ItemInfo> results, DslCalculator calc, int indexCount, Dictionary<string, ParamInfo> args, SceneDepInfo sceneDeps, Dictionary<string, HashSet<string>> refDict, Dictionary<string, HashSet<string>> refByDict)
    {
        try {
            item.PrepareShowInfo();
            results.Clear();
            var ret = BoxedValue.NullObject;
            if (null != calc) {
                calc.SetGlobalVariable("assetpath", item.AssetPath);
                calc.SetGlobalVariable("scenepath", item.ScenePath);
                calc.SetGlobalVariable("importer", BoxedValue.FromObject(item.Importer));
                calc.SetGlobalVariable("object", BoxedValue.FromObject(item.Object));
                calc.SetGlobalVariable("info", item.Info);
                calc.SetGlobalVariable("order", item.Order);
                calc.SetGlobalVariable("value", item.Value);
                calc.SetGlobalVariable("selected", item.Selected);
                calc.SetGlobalVariable("results", BoxedValue.FromObject(results));
                calc.SetGlobalVariable("scenedeps", BoxedValue.FromObject(sceneDeps));
                calc.SetGlobalVariable("refdict", BoxedValue.FromObject(refDict));
                calc.SetGlobalVariable("refbydict", BoxedValue.FromObject(refByDict));
                if (null != addVars) {
                    foreach (var pair in addVars) {
                        calc.SetGlobalVariable(pair.Key, pair.Value);
                    }
                }
                calc.SetGlobalVariable("params", BoxedValue.FromObject(args));
                foreach (var pair in args) {
                    var p = pair.Value;
                    calc.SetGlobalVariable(p.Name, BoxedValue.FromObject(p.Value));
                }
                calc.SetGlobalVariable("group", BoxedValue.NullObject);
                calc.SetGlobalVariable("extralist", BoxedValue.NullObject);

                for (int i = 0; i < indexCount; ++i) {
                    ret = calc.Calc(i.ToString());
                }

                BoxedValue v;
                if (calc.TryGetGlobalVariable("assetpath", out v)) {
                    var path = v.AsString;
                    if (!string.IsNullOrEmpty(path))
                        item.AssetPath = path;
                }
                if (calc.TryGetGlobalVariable("scenepath", out v)) {
                    var path = v.AsString;
                    if (!string.IsNullOrEmpty(path))
                        item.ScenePath = path;
                }
                if (calc.TryGetGlobalVariable("importer", out v)) {
                    if (!v.IsNullObject) {
                        item.Importer = v.As<AssetImporter>();
                    }
                }
                if (calc.TryGetGlobalVariable("object", out v)) {
                    if (!v.IsNullObject) {
                        item.Object = v.As<UnityEngine.Object>();
                    }
                }
                if (calc.TryGetGlobalVariable("info", out v)) {
                    item.Info = v.AsString;
                    if (null == item.Info) {
                        item.Info = string.Empty;
                    }
                }
                if (calc.TryGetGlobalVariable("order", out v)) {
                    if (!v.IsNullObject) {
                        item.Order = v.GetDouble();
                    }
                }
                if (calc.TryGetGlobalVariable("value", out v)) {
                    if (!v.IsNullObject) {
                        item.Value = v.GetDouble();
                    }
                }
                if (calc.TryGetGlobalVariable("selected", out v)) {
                    if (!v.IsNullObject) {
                        item.Selected = v.GetBool();
                    }
                }
                if (calc.TryGetGlobalVariable("group", out v)) {
                    var g = v.AsString;
                    if (null != g) {
                        item.Group = g;
                    }
                }
                if (calc.TryGetGlobalVariable("extralist", out v)) {
                    var list = v.As<IList>();
                    if (null != list) {
                        var pairList = new List<KeyValuePair<string, BoxedValue>>();
                        foreach (var pair in list) {
                            var keyObj = (KeyValuePair<string, BoxedValue>)pair;
                            pairList.Add(keyObj);
                        }
                        item.ExtraList = pairList;
                    }
                    else {
                        item.ExtraList = null;
                    }
                }
                if (calc.TryGetGlobalVariable("extraobject", out v)) {
                    item.ExtraObject = v;
                }
                if (calc.TryGetGlobalVariable("extralistbuild", out v)) {
                    string scp = v.AsString;
                    if (!string.IsNullOrEmpty(scp)) {
                        item.ExtraListBuildScript = scp;
                    }
                    else {
                        item.ExtraListBuildScript = string.Empty;
                    }
                }
                if (calc.TryGetGlobalVariable("extralistclick", out v)) {
                    string scp = v.AsString;
                    if (!string.IsNullOrEmpty(scp)) {
                        item.ExtraListClickScript = scp;
                    }
                    else {
                        item.ExtraListClickScript = string.Empty;
                    }
                }
                if (calc.TryGetGlobalVariable("redirectdsl", out v)) {
                    item.RedirectDsl = v.AsString;
                }
                if (calc.TryGetGlobalVariable("redirectargs", out v)) {
                    var dict = v.As<IDictionary>();
                    if (null != dict) {
                        var strDict = new Dictionary<string, string>();
                        var enumer = dict.GetEnumerator();
                        while (enumer.MoveNext()) {
                            var key = enumer.Key as string;
                            var val = enumer.Value as string;
                            if (null != key) {
                                strDict.Add(key, val);
                            }
                        }
                        item.RedirectArgs = strDict;
                    }
                }

                if (results.Count <= 0) {
                    results.Add(item);
                }
                else {
                    ret = results.Count;
                }
            }
            return ret;
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("filter {0} exception:{1}\n{2}", item.AssetPath, ex.Message, ex.StackTrace);
            return BoxedValue.NullObject;
        }
    }
    internal static BoxedValue Process(ItemInfo item, DslCalculator calc, int indexCount, Dictionary<string, ParamInfo> args, SceneDepInfo sceneDeps, Dictionary<string, HashSet<string>> refDict, Dictionary<string, HashSet<string>> refByDict)
    {
        try {
            item.PrepareShowInfo();
            var ret = BoxedValue.NullObject;
            if (null != calc) {
                calc.SetGlobalVariable("assetpath", item.AssetPath);
                calc.SetGlobalVariable("scenepath", item.ScenePath);
                calc.SetGlobalVariable("importer", BoxedValue.FromObject(item.Importer));
                calc.SetGlobalVariable("object", BoxedValue.FromObject(item.Object));
                calc.SetGlobalVariable("info", item.Info);
                calc.SetGlobalVariable("order", item.Order);
                calc.SetGlobalVariable("value", item.Value);
                calc.SetGlobalVariable("selected", item.Selected);
                calc.SetGlobalVariable("scenedeps", BoxedValue.FromObject(sceneDeps));
                calc.SetGlobalVariable("refdict", BoxedValue.FromObject(refDict));
                calc.SetGlobalVariable("refbydict", BoxedValue.FromObject(refByDict));
                calc.SetGlobalVariable("params", BoxedValue.FromObject(args));
                foreach (var pair in args) {
                    var p = pair.Value;
                    calc.SetGlobalVariable(p.Name, p.Value);
                }

                for (int i = 0; i < indexCount; ++i) {
                    ret = calc.Calc(i.ToString());
                }
            }
            return ret;
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("process {0} exception:{1}\n{2}", item.AssetPath, ex.Message, ex.StackTrace);
            return BoxedValue.NullObject;
        }
    }
    internal static BoxedValue Group(GroupInfo item, DslCalculator calc, int indexCount, Dictionary<string, ParamInfo> args, SceneDepInfo sceneDeps, Dictionary<string, HashSet<string>> refDict, Dictionary<string, HashSet<string>> refByDict)
    {
        try {
            item.PrepareShowInfo();
            var ret = BoxedValue.NullObject;
            if (null != calc) {
                calc.SetGlobalVariable("group", item.Group);
                calc.SetGlobalVariable("items", BoxedValue.FromObject(item.Items));
                calc.SetGlobalVariable("sum", item.Sum);
                calc.SetGlobalVariable("max", item.Max);
                calc.SetGlobalVariable("min", item.Min);
                calc.SetGlobalVariable("count", item.Count);
                calc.SetGlobalVariable("avg", item.Avg);
                calc.SetGlobalVariable("assetpath", item.AssetPath);
                calc.SetGlobalVariable("scenepath", item.ScenePath);
                calc.SetGlobalVariable("info", item.Info);
                calc.SetGlobalVariable("order", item.Order);
                calc.SetGlobalVariable("value", item.Value);
                calc.SetGlobalVariable("selected", item.Selected);
                calc.SetGlobalVariable("scenedeps", BoxedValue.FromObject(sceneDeps));
                calc.SetGlobalVariable("refdict", BoxedValue.FromObject(refDict));
                calc.SetGlobalVariable("refbydict", BoxedValue.FromObject(refByDict));
                calc.SetGlobalVariable("params", BoxedValue.FromObject(args));
                foreach (var pair in args) {
                    var p = pair.Value;
                    calc.SetGlobalVariable(p.Name, p.Value);
                }
                calc.SetGlobalVariable("extralist", BoxedValue.NullObject);

                for (int i = 0; i < indexCount; ++i) {
                    ret = calc.Calc(i.ToString());
                }

                BoxedValue v;
                if (calc.TryGetGlobalVariable("assetpath", out v)) {
                    var path = v.AsString;
                    if (!string.IsNullOrEmpty(path))
                        item.AssetPath = path;
                }
                if (calc.TryGetGlobalVariable("scenepath", out v)) {
                    var path = v.AsString;
                    if (!string.IsNullOrEmpty(path))
                        item.ScenePath = path;
                }
                if (calc.TryGetGlobalVariable("info", out v)) {
                    item.Info = v.AsString;
                    if (null == item.Info) {
                        item.Info = string.Empty;
                    }
                }
                if (calc.TryGetGlobalVariable("order", out v)) {
                    if (!v.IsNullObject) {
                        item.Order = v.GetDouble();
                    }
                }
                if (calc.TryGetGlobalVariable("value", out v)) {
                    if (!v.IsNullObject) {
                        item.Value = v.GetDouble();
                    }
                }
                if (calc.TryGetGlobalVariable("selected", out v)) {
                    if (!v.IsNullObject) {
                        item.Selected = v.GetBool();
                    }
                }
                if (calc.TryGetGlobalVariable("extralist", out v)) {
                    var list = v.As<IList>();
                    if (null != list) {
                        var pairList = new List<KeyValuePair<string, BoxedValue>>();
                        foreach (var pair in list) {
                            var keyObj = (KeyValuePair<string, BoxedValue>)pair;
                            pairList.Add(keyObj);
                        }
                        item.ExtraList = pairList;
                    }
                    else {
                        item.ExtraList = null;
                    }
                }
                if (calc.TryGetGlobalVariable("extraobject", out v)) {
                    item.ExtraObject = v;
                }
                if (calc.TryGetGlobalVariable("extralistbuild", out v)) {
                    string scp = v.AsString;
                    if (!string.IsNullOrEmpty(scp)) {
                        item.ExtraListBuildScript = scp;
                    }
                    else {
                        item.ExtraListBuildScript = string.Empty;
                    }
                }
                if (calc.TryGetGlobalVariable("extralistclick", out v)) {
                    string scp = v.AsString;
                    if (!string.IsNullOrEmpty(scp)) {
                        item.ExtraListClickScript = scp;
                    }
                    else {
                        item.ExtraListClickScript = string.Empty;
                    }
                }
                if (calc.TryGetGlobalVariable("redirectdsl", out v)) {
                    item.RedirectDsl = v.AsString;
                }
                if (calc.TryGetGlobalVariable("redirectargs", out v)) {
                    var dict = v.As<IDictionary>();
                    if (null != dict) {
                        var strDict = new Dictionary<string, string>();
                        var enumer = dict.GetEnumerator();
                        while (enumer.MoveNext()) {
                            var key = enumer.Key as string;
                            var val = enumer.Value as string;
                            if (null != key) {
                                strDict.Add(key, val);
                            }
                        }
                        item.RedirectArgs = strDict;
                    }
                }
            }
            return ret;
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("group {0} exception:{1}\n{2}", item.AssetPath, ex.Message, ex.StackTrace);
            return BoxedValue.NullObject;
        }
    }
    internal static BoxedValue GroupProcess(GroupInfo item, DslCalculator calc, int indexCount, Dictionary<string, ParamInfo> args, SceneDepInfo sceneDeps, Dictionary<string, HashSet<string>> refDict, Dictionary<string, HashSet<string>> refByDict)
    {
        try {
            item.PrepareShowInfo();
            var ret = BoxedValue.NullObject;
            if (null != calc) {
                calc.SetGlobalVariable("group", item.Group);
                calc.SetGlobalVariable("items", BoxedValue.FromObject(item.Items));
                calc.SetGlobalVariable("sum", item.Sum);
                calc.SetGlobalVariable("max", item.Max);
                calc.SetGlobalVariable("min", item.Min);
                calc.SetGlobalVariable("count", item.Count);
                calc.SetGlobalVariable("avg", item.Avg);
                calc.SetGlobalVariable("assetpath", item.AssetPath);
                calc.SetGlobalVariable("scenepath", item.ScenePath);
                calc.SetGlobalVariable("info", item.Info);
                calc.SetGlobalVariable("order", item.Order);
                calc.SetGlobalVariable("value", item.Value);
                calc.SetGlobalVariable("selected", item.Selected);
                calc.SetGlobalVariable("scenedeps", BoxedValue.FromObject(sceneDeps));
                calc.SetGlobalVariable("refdict", BoxedValue.FromObject(refDict));
                calc.SetGlobalVariable("refbydict", BoxedValue.FromObject(refByDict));
                calc.SetGlobalVariable("params", BoxedValue.FromObject(args));
                foreach (var pair in args) {
                    var p = pair.Value;
                    calc.SetGlobalVariable(p.Name, p.Value);
                }

                for (int i = 0; i < indexCount; ++i) {
                    ret = calc.Calc(i.ToString());
                }
            }
            return ret;
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("group process {0} exception:{1}\n{2}", item.AssetPath, ex.Message, ex.StackTrace);
            return BoxedValue.NullObject;
        }
    }
    internal static void ResetResourceParamsCalculator()
    {
        s_ResourceParamsCalculator = null;
    }
    internal static bool SetParamsToResource(string proc, ResourceParams resParams, UnityEngine.Object obj)
    {
        bool ret = false;
        var calc = GetResourceParamsCalculator();
        if (null != calc) {
            var r = calc.Calc(proc, BoxedValue.FromObject(resParams), BoxedValue.FromObject(obj));
            if (!r.IsNullObject) {
                ret = r.GetBool();
            }
        }
        return ret;
    }
    internal static bool GetParamsFromResource(string proc, ResourceParams resParams, UnityEngine.Object obj)
    {
        bool ret = false;
        var calc = GetResourceParamsCalculator();
        if (null != calc) {
            var r = calc.Calc(proc, BoxedValue.FromObject(resParams), BoxedValue.FromObject(obj));
            if (!r.IsNullObject) {
                ret = r.GetBool();
            }
        }
        return ret;
    }
    internal static void ResetCommandCalculator()
    {
        s_CommandCalculator = null;
    }
    internal static DslCalculator GetCommandCalculator()
    {
        if (null == s_CommandCalculator) {
            s_CommandCalculator = new DslCalculator();
            InitCalculator(s_CommandCalculator);
        }
        return s_CommandCalculator;
    }
    internal static BoxedValue RunCommandScript(string name, BoxedValue obj, BoxedValue item, Dictionary<string, ParamInfo> args, Dictionary<string, BoxedValue> addVars)
    {
        try {
            var calc = GetCommandCalculator();
            var ret = BoxedValue.NullObject;
            if (null != calc) {
                calc.SetGlobalVariable("params", BoxedValue.FromObject(args));
                foreach (var pair in args) {
                    var p = pair.Value;
                    calc.SetGlobalVariable(p.Name, p.Value);
                }
                if (null != addVars) {
                    foreach (var pair in addVars) {
                        calc.SetGlobalVariable(pair.Key, pair.Value);
                    }
                }

                ret = calc.Calc(name, obj, item);
            }
            return ret;
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("RunCommandScript {0} exception:{1}\n{2}", name, ex.Message, ex.StackTrace);
            return BoxedValue.NullObject;
        }
    }
    internal static bool LoadCommand(string code, Dictionary<string, ParamInfo> args, Dictionary<string, BoxedValue> addVars)
    {
        bool ret = false;
        string procCode = string.Format("script{{ {0}; }};", code);
        var file = new Dsl.DslFile();
        if (file.LoadFromString(procCode, msg => { Debug.LogErrorFormat("{0}", msg); })) {
            var dslInfo = file.DslInfos[0];
            var func = dslInfo as Dsl.FunctionData;
            var stData = dslInfo as Dsl.StatementData;
            if (null == func && null != stData) {
                func = stData.First.AsFunction;
            }
            if (null != func) {
                var calc = GetCommandCalculator();
                calc.SetGlobalVariable("params", BoxedValue.FromObject(args));
                foreach (var pair in args) {
                    var p = pair.Value;
                    calc.SetGlobalVariable(p.Name, p.Value);
                }
                if (null != addVars) {
                    foreach (var pair in addVars) {
                        calc.SetGlobalVariable(pair.Key, pair.Value);
                    }
                }
                calc.LoadDsl("main", new string[] { "$obj", "$item" }, func);
                ret = true;
            }
        }
        return ret;
    }
    internal static BoxedValue EvalCommand(BoxedValue obj, BoxedValue item)
    {
        var calc = GetCommandCalculator();
        var r = calc.Calc("main", obj, item);
        return r;
    }

    internal static bool FindSceneObject(string name, string type, ref string assetPath, ref string scenePath, ref UnityEngine.Object sceneObj)
    {
        bool ret = false;
        for (int i = 0; i < EditorSceneManager.sceneCount; ++i) {
            var scene = EditorSceneManager.GetSceneAt(i);
            var objs = scene.GetRootGameObjects();
            foreach (var obj in objs) {
                ret = FindChildObjectsRecursively(string.Empty, obj, name, type, ref assetPath, ref scenePath, ref sceneObj);
                if (ret)
                    break;
            }
            if (ret)
                break;
        }
        return ret;
    }
    private static bool FindChildObjectsRecursively(string path, GameObject obj, string name, string type, ref string assetPath, ref string scenePath, ref UnityEngine.Object sceneObj)
    {
        if (string.IsNullOrEmpty(path)) {
            path = obj.name;
        }
        else {
            path = path + "/" + obj.name;
        }
        bool ret = false;
        if (obj.name == name) {
            if (type == "GameObject") {
                ret = true;
            }
            else if (type == "ParticleSystem") {
                var comp = obj.GetComponent<ParticleSystem>();
                if (null != comp) {
                    ret = true;
                }
            }
            var prefabObj = PrefabUtility.GetPrefabObject(obj);
            var prefabPath = AssetDatabase.GetAssetPath(prefabObj);
            if (!ret) {
                var objs = AssetDatabase.LoadAllAssetsAtPath(prefabPath);
                foreach (var assetObject in objs) {
                    if (null != assetObject && assetObject.GetType().Name.EndsWith(type)) {
                        ret = true;
                        break;
                    }
                }
            }
            if (ret) {
                assetPath = prefabPath;
                if (string.IsNullOrEmpty(assetPath)) {
                    assetPath = string.Empty;
                }
                scenePath = path;
                sceneObj = obj;
            }
        }
        if (!ret) {
            var trans = obj.transform;
            int ct = trans.childCount;
            for (int i = 0; i < ct; ++i) {
                var t = trans.GetChild(i);
                ret = FindChildObjectsRecursively(path, t.gameObject, name, type, ref assetPath, ref scenePath, ref sceneObj);
                if (ret)
                    break;
            }
        }
        return ret;
    }

    internal static Regex GetRegex(string str)
    {
        Regex regex;
        if (!s_Regexes.TryGetValue(str, out regex)) {
            regex = new Regex(str, RegexOptions.Compiled);
        }
        return regex;
    }
    internal static double CellToNumeric(NPOI.SS.UserModel.ICell cell)
    {
        double r = 0.0;
        if (null != cell) {
            switch (cell.CellType) {
                case NPOI.SS.UserModel.CellType.Boolean:
                    r = cell.BooleanCellValue ? 1.0 : 0.0;
                    break;
                case NPOI.SS.UserModel.CellType.Numeric:
                    r = cell.NumericCellValue;
                    break;
                case NPOI.SS.UserModel.CellType.String:
                    double.TryParse(cell.StringCellValue, out r);
                    break;
                case NPOI.SS.UserModel.CellType.Formula:
                    switch (cell.CachedFormulaResultType) {
                        case NPOI.SS.UserModel.CellType.Boolean:
                            r = cell.BooleanCellValue ? 1.0 : 0.0;
                            break;
                        case NPOI.SS.UserModel.CellType.Numeric:
                            r = cell.NumericCellValue;
                            break;
                        case NPOI.SS.UserModel.CellType.String:
                            double.TryParse(cell.StringCellValue, out r);
                            break;
                        default:
                            r = 0.0;
                            break;
                    }
                    break;
                case NPOI.SS.UserModel.CellType.Blank:
                default:
                    r = 0.0;
                    break;
            }
        }
        return r;
    }
    internal static string CellToString(NPOI.SS.UserModel.ICell cell)
    {
        string r = string.Empty;
        if (null != cell) {
            switch (cell.CellType) {
                case NPOI.SS.UserModel.CellType.Boolean:
                    r = cell.BooleanCellValue.ToString();
                    break;
                case NPOI.SS.UserModel.CellType.Numeric:
                    r = cell.NumericCellValue.ToString();
                    break;
                case NPOI.SS.UserModel.CellType.String:
                    r = cell.StringCellValue;
                    break;
                case NPOI.SS.UserModel.CellType.Formula:
                    switch (cell.CachedFormulaResultType) {
                        case NPOI.SS.UserModel.CellType.Boolean:
                            r = cell.BooleanCellValue.ToString();
                            break;
                        case NPOI.SS.UserModel.CellType.Numeric:
                            r = cell.NumericCellValue.ToString();
                            break;
                        case NPOI.SS.UserModel.CellType.String:
                            r = cell.StringCellValue;
                            break;
                        default:
                            r = string.Empty;
                            break;
                    }
                    break;
                case NPOI.SS.UserModel.CellType.Blank:
                default:
                    r = string.Empty;
                    break;
            }
        }
        return r;
    }
    internal static Transform FindChildRecursive(Transform parent, string boneName)
    {
        if (parent == null)
            return null;

        Transform t = parent.Find(boneName);
        if (null != t) {
            return t;
        }
        else {
            int ct = parent.childCount;
            for (int i = 0; i < ct; ++i) {
                t = FindChildRecursive(parent.GetChild(i), boneName);
                if (null != t) {
                    return t;
                }
            }
        }
        return null;
    }
    internal static bool IsPathMatch(string path, string filter)
    {
        string ext = Path.GetExtension(path);
        if (ext == ".meta" && filter != "*.meta") {
            return false;
        }
        List<string> infos;
        if (!s_PathMatchInfos.TryGetValue(filter, out infos)) {
            string[] filters = filter.Split(new char[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
            infos = new List<string>(filters);
            s_PathMatchInfos.Add(filter, infos);
        }
        string fileName = Path.GetFileName(path);
        bool match = true;
        int startIx = 0;
        for (int i = 0; i < infos.Count; ++i) {
            var info = infos[i];
            var ix = fileName.IndexOf(info, startIx, StringComparison.CurrentCultureIgnoreCase);
            if (ix >= 0) {
                startIx = ix + info.Length;
            }
            else {
                match = false;
                break;
            }
        }
        return match;
    }
    internal static bool IsValidAssetPath(string path)
    {
        if (path.IndexOfAny(s_InvalidPathChars) >= 0 || path.IndexOfAny(s_InvalidFileNameChars) >= 0) {
            return false;
        }
        string absolutePath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
        if (absolutePath.StartsWith(Application.dataPath)) {
            return File.Exists(absolutePath);
        }
        return false;
    }
    internal static bool IsAssetPath(string path)
    {
        string rootPath = Application.dataPath.Replace('\\', '/');
        path = path.Replace('\\', '/');
        if (path.StartsWith(rootPath)) {
            return true;
        }
        else {
            return false;
        }
    }
    internal static string PathToAssetPath(string path)
    {
        return FilePathToRelativePath(path);
    }
    internal static string AssetPathToPath(string assetPath)
    {
        return RelativePathToFilePath(assetPath);
    }
    internal static string FilePathToRelativePath(string path)
    {
        string rootPath = GetRootPath();
        path = path.Replace('\\', '/');
        if (path.StartsWith(rootPath)) {
            path = path.Substring(rootPath.Length);
        }
        return path;
    }
    internal static string RelativePathToFilePath(string path)
    {
        string rootPath = GetRootPath();
        path = path.Replace('\\', '/');
        return rootPath + path;
    }
    internal static string GetRootPath()
    {
        const string c_AssetsDir = "Assets";
        if (string.IsNullOrEmpty(s_RootPath)) {
            s_RootPath = Application.dataPath.Replace('\\', '/');
            if (s_RootPath.EndsWith(c_AssetsDir))
                s_RootPath = s_RootPath.Substring(0, s_RootPath.Length - c_AssetsDir.Length);
        }
        return s_RootPath;
    }
    internal static bool EnableSaveAndReimport
    {
        get { return s_EnableSaveAndReimport; }
        set { s_EnableSaveAndReimport = value; }
    }
    internal static bool ForceSaveAndReimport
    {
        get { return s_ForceSaveAndReimport; }
        set { s_ForceSaveAndReimport = value; }
    }
    internal static bool UseSpecificSettingDB
    {
        get { return s_UseSpecificSettingDB; }
        set { s_UseSpecificSettingDB = value; }
    }
    internal static bool SaveAfterProcess
    {
        get { return s_SaveAfterProcess; }
        set { s_SaveAfterProcess = value; }
    }
    internal static bool SaveResultWithXrefs
    {
        get { return s_SaveResultWithXrefs; }
        set { s_SaveResultWithXrefs = value; }
    }

    internal static void SelectObject(UnityEngine.Object obj)
    {
        Selection.activeObject = obj;
        EditorGUIUtility.PingObject(Selection.activeObject);
        //SceneView.lastActiveSceneView.FrameSelected(true);
        SceneView.FrameLastActiveSceneView();
    }
    internal static void SelectProjectObject(string assetPath)
    {
        if (assetPath.IndexOfAny(new char[] { '/', '\\' }) < 0) {
            var assetName = Path.GetFileNameWithoutExtension(assetPath);
            var guids = AssetDatabase.FindAssets(assetName);
            if (guids.Length >= 1) {
                int ct = 0;
                for (int i = 0; i < guids.Length; ++i) {
                    var temp = AssetDatabase.GUIDToAssetPath(guids[0]);
                    var name = Path.GetFileNameWithoutExtension(temp);
                    if (string.Compare(name, assetName, true) == 0) {
                        ++ct;
                        if (ct == 1) {
                            assetPath = temp;
                        }
                    }
                }
                if (ct > 1) {
                    EditorUtility.DisplayDialog("alert", string.Format("有{0}个同名的资源，仅选中其中一个:{1}", ct, assetPath), "ok");
                }
            }
        }
        var objLoaded = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (objLoaded != null) {
            if (Selection.activeObject != null && !(Selection.activeObject is GameObject)) {
                Resources.UnloadAsset(Selection.activeObject);
                Selection.activeObject = null;
            }
            Selection.activeObject = objLoaded;
            //EditorGUIUtility.PingObject(Selection.activeObject);
        }
    }
    internal static void SelectSceneObject(string scenePath)
    {
        var obj = GameObject.Find(scenePath);
        if (null != obj) {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(Selection.activeObject);
            //SceneView.lastActiveSceneView.FrameSelected(true);
            SceneView.FrameLastActiveSceneView();
        }
    }

    private static GameObject FindRoot(GameObject obj)
    {
        GameObject ret = null;
        var trans = obj.transform;
        while (null != trans && !(trans is RectTransform)) {
            ret = trans.gameObject;
            trans = trans.parent;
        }
        return ret;
    }
    private static UnityEngine.Object LoadAssetByPathAndName(string path, string name)
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in objs) {
            if (obj.name == name)
                return obj;
        }
        return null;
    }
    private static DslCalculator GetResourceParamsCalculator()
    {
        if (null == s_ResourceParamsCalculator && File.Exists(s_ResourceParamsDslFile)) {
            s_ResourceParamsCalculator = new DslCalculator();
            InitCalculator(s_ResourceParamsCalculator);
            s_ResourceParamsCalculator.LoadDsl(s_ResourceParamsDslFile);
        }
        return s_ResourceParamsCalculator;
    }
    private static void AppendLine(StringBuilder sb, string format, params object[] args)
    {
        sb.AppendFormat(format, args);
        sb.AppendLine();
    }
    private static string GetIndent(int indent)
    {
        return c_IndentString.Substring(0, indent);
    }
    private static string IndentScript(string indent, string scp)
    {
        string[] lines = scp.Split(new char[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int ix = 0; ix < lines.Length; ++ix) {
            lines[ix] = string.Format("{0}{1}", indent, lines[ix]);
        }
        return string.Join("\r\n", lines);
    }

    private static bool s_EnableSaveAndReimport = false;
    private static bool s_ForceSaveAndReimport = false;
    private static bool s_UseSpecificSettingDB = true;
    private static bool s_SaveAfterProcess = false;
    private static bool s_SaveResultWithXrefs = true;

    private static Dictionary<string, Regex> s_Regexes = new Dictionary<string, Regex>();
    private static Dictionary<string, List<string>> s_PathMatchInfos = new Dictionary<string, List<string>>();
    private static string s_RootPath = string.Empty;

    private static char[] s_InvalidPathChars = Path.GetInvalidPathChars();
    private static char[] s_InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private const string c_IndentString = "\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t";

    private static DslCalculator s_ResourceParamsCalculator = null;
    private static DslCalculator s_CommandCalculator = null;
    private static string s_ResourceParamsDslFile = "resourceparams.dsl";
}

#region API
namespace ResourceEditApi
{
    internal class MeshInfo
    {
        public int skinnedMeshCount;
        public int meshFilterCount;
        public int vertexCount;
        public int triangleCount;
        public int boneCount;
        public int materialCount;
        public int clipCount;
        public int maxTexWidth;
        public int maxTexHeight;
        public string maxTexName;
        public string maxTexPropName;
        public int maxKeyFrameCount;
        public string maxKeyFrameCurveName = string.Empty;
        public string maxKeyFrameClipName = string.Empty;
        public int updateWhenOffscreenCount = 0;
        public int animatorCount = 0;
        public int alwaysAnimateCount = 0;
        public List<SingleMeshInfo> meshes = new List<SingleMeshInfo>();
        public List<MaterialInfo> materials = new List<MaterialInfo>();
        public List<AnimationClipInfo> clips = new List<AnimationClipInfo>();

        public void AddSingleMesh(bool isParticle, string name, int count, int vc, int tc)
        {
            meshes.Add(new SingleMeshInfo { isParticle = isParticle, meshName = name, meshCount = count, vertexCount = vc, triangleCount = tc, totalVertexCount = count * vc, totalTriangleCount = count * tc });
        }
        public void CollectMaterials(IList<Material> mats)
        {
            foreach (var mat in mats) {
                if (null != mat) {
                    var matInfo = new MaterialInfo();
                    matInfo.name = mat.name;
                    matInfo.shaderName = null == mat.shader ? string.Empty : mat.shader.name;
                    matInfo.maxTexWidth = 0;
                    matInfo.maxTexHeight = 0;
                    matInfo.maxTexName = string.Empty;
                    matInfo.maxTexPropName = string.Empty;
                    foreach (var prop in mat.GetTexturePropertyNames()) {
                        var tex = mat.GetTexture(prop);
                        if (null != tex) {
                            var texInfo = new TextureInfo();
                            texInfo.propName = prop;
                            texInfo.texName = tex.name;
                            texInfo.width = tex.width;
                            texInfo.height = tex.height;
                            matInfo.texs.Add(texInfo);

                            if (texInfo.width * texInfo.height > matInfo.maxTexWidth * matInfo.maxTexHeight) {
                                matInfo.maxTexWidth = texInfo.width;
                                matInfo.maxTexHeight = texInfo.height;
                                matInfo.maxTexName = texInfo.texName;
                                matInfo.maxTexPropName = texInfo.propName;

                                if (texInfo.width * texInfo.height > maxTexWidth * maxTexHeight) {
                                    maxTexWidth = texInfo.width;
                                    maxTexHeight = texInfo.height;
                                    maxTexName = texInfo.texName;
                                    maxTexPropName = texInfo.propName;
                                }
                            }
                        }
                    }

                    materials.Add(matInfo);
                }
            }
        }
        public void CollectClip(AnimationClip clip)
        {
            var clipInfo = new AnimationClipInfo();
            clipInfo.clipName = clip.name;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            int maxKfc = 0;
            string curveName = string.Empty;
            foreach (var binding in bindings) {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                int kfc = curve.keys.Length;
                clipInfo.curves.Add(new KeyFrameCurveInfo { curveName = binding.propertyName, curvePath = binding.path, keyFrameCount = kfc });
                if (maxKfc < kfc) {
                    maxKfc = kfc;
                    curveName = binding.path + "/" + binding.propertyName;
                }
            }
            clipInfo.maxKeyFrameCount = maxKfc;
            clipInfo.maxKeyFrameCurveName = curveName;
            clips.Add(clipInfo);

            if (maxKeyFrameCount < maxKfc) {
                maxKeyFrameCount = maxKfc;
                maxKeyFrameCurveName = curveName;
                maxKeyFrameClipName = clip.name;
            }
        }
    }
    internal class SingleMeshInfo
    {
        public bool isParticle;
        public string meshName;
        public int meshCount;
        public int vertexCount;
        public int triangleCount;
        public int totalVertexCount;
        public int totalTriangleCount;
    }
    internal class TextureInfo
    {
        public string propName;
        public string texName;
        public int width;
        public int height;
    }
    internal class MaterialInfo
    {
        public string name;
        public string shaderName;
        public int maxTexWidth;
        public int maxTexHeight;
        public string maxTexName;
        public string maxTexPropName;
        public List<TextureInfo> texs = new List<TextureInfo>();
    }
    internal class KeyFrameCurveInfo
    {
        public string curveName = string.Empty;
        public string curvePath = string.Empty;
        public int keyFrameCount;
    }
    internal class AnimationClipInfo
    {
        public string clipName = string.Empty;
        public int maxKeyFrameCount;
        public string maxKeyFrameCurveName = string.Empty;
        public List<KeyFrameCurveInfo> curves = new List<KeyFrameCurveInfo>();
    }
    internal class AnimatorControllerInfo
    {
        public int layerCount;
        public int paramCount;
        public int stateCount;
        public int subStateMachineCount;
        public int clipCount;
        public int maxKeyFrameCount;
        public string maxKeyFrameCurveName = string.Empty;
        public string maxKeyFrameClipName = string.Empty;
        public List<AnimationClipInfo> clips = new List<AnimationClipInfo>();

        public void CollectClip(AnimationClip clip)
        {
            var clipInfo = new AnimationClipInfo();
            clipInfo.clipName = clip.name;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            int maxKfc = 0;
            string curveName = string.Empty;
            foreach (var binding in bindings) {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                int kfc = curve.keys.Length;
                clipInfo.curves.Add(new KeyFrameCurveInfo { curveName = binding.propertyName, curvePath = binding.path, keyFrameCount = kfc });
                if (maxKfc < kfc) {
                    maxKfc = kfc;
                    curveName = binding.path + "/" + binding.propertyName;
                }
            }
            clipInfo.maxKeyFrameCount = maxKfc;
            clipInfo.maxKeyFrameCurveName = curveName;
            clips.Add(clipInfo);

            if (maxKeyFrameCount < maxKfc) {
                maxKeyFrameCount = maxKfc;
                maxKeyFrameCurveName = curveName;
                maxKeyFrameClipName = clip.name;
            }
        }
    }
    internal class SetParamsToModelExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var param = operands[0].As<ResourceParams>();
                var importer = operands[1].As<ModelImporter>();

                string val;
                if (param.TryGetValue("compression", out val)) {
                    var v = (ModelImporterAnimationCompression)Enum.Parse(typeof(ModelImporterAnimationCompression), val);
                    if (v != importer.animationCompression) {
                        importer.animationCompression = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("rotationError", out val)) {
                    var v = float.Parse(val);
                    if (Math.Abs(v - importer.animationRotationError) > float.Epsilon) {
                        importer.animationRotationError = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("posistionError", out val)) {
                    var v = float.Parse(val);
                    if (Math.Abs(v - importer.animationPositionError) > float.Epsilon) {
                        importer.animationPositionError = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("scaleError", out val)) {
                    var v = float.Parse(val);
                    if (Math.Abs(v - importer.animationScaleError) > float.Epsilon) {
                        importer.animationScaleError = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("meshCompression", out val)) {
                    var v = (ModelImporterMeshCompression)Enum.Parse(typeof(ModelImporterMeshCompression), val);
                    if (v != importer.meshCompression) {
                        importer.meshCompression = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("optimizeMesh", out val)) {
                    var v = bool.Parse(val);
                    if (v != importer.optimizeMesh) {
                        importer.optimizeMesh = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("isReadable", out val)) {
                    var v = bool.Parse(val);
                    if (v != importer.isReadable) {
                        importer.isReadable = v;
                        r = true;
                    }
                }
            }
            return r;
        }
    }
    internal class GetParamsFromModelExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = true;
            if (operands.Count >= 2) {
                var param = operands[0].As<ResourceParams>();
                var importer = operands[1].As<ModelImporter>();

                param.AddOrUpdate("compression", importer.animationCompression.ToString());
                param.AddOrUpdate("rotationError", importer.animationRotationError.ToString());
                param.AddOrUpdate("posistionError", importer.animationPositionError.ToString());
                param.AddOrUpdate("scaleError", importer.animationScaleError.ToString());
                param.AddOrUpdate("meshCompression", importer.meshCompression.ToString());
                param.AddOrUpdate("optimizeMesh", importer.optimizeMesh.ToString());
                param.AddOrUpdate("isReadable", importer.isReadable.ToString());
            }
            return r;
        }
    }
    internal class SetParamsToTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var param = operands[0].As<ResourceParams>();
                var importer = operands[1].As<TextureImporter>();

                string val;
                if (param.TryGetValue("isReadable", out val)) {
                    var v = bool.Parse(val);
                    if (v != importer.isReadable) {
                        importer.isReadable = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("mipmapEnabled", out val)) {
                    var v = bool.Parse(val);
                    if (v != importer.mipmapEnabled) {
                        importer.mipmapEnabled = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("streamingMipmaps", out val)) {
                    var v = bool.Parse(val);
                    if (v != importer.streamingMipmaps) {
                        importer.streamingMipmaps = v;
                        r = true;
                    }
                }
                if (param.TryGetValue("filterMode", out val))
                {
                    FilterMode v;

                    if (string.IsNullOrEmpty(val) || !Enum.TryParse<FilterMode>(val, out v))
                    {
                        v = FilterMode.Bilinear;
                    }

                    if (v != importer.filterMode)
                    {
                        importer.filterMode = v;
                        r = true;
                    }
                }
                var def = importer.GetDefaultPlatformTextureSettings();
                r = r || SetParamsToSettings(param, "Default.", def);
                importer.SetPlatformTextureSettings(def);
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                r = r || SetParamsToSettings(param, "Standalone.", standalone);
                importer.SetPlatformTextureSettings(standalone);
                var ios = importer.GetPlatformTextureSettings("Android");
                r = r || SetParamsToSettings(param, "Android.", ios);
                importer.SetPlatformTextureSettings(ios);
                var android = importer.GetPlatformTextureSettings("iPhone");
                r = r || SetParamsToSettings(param, "iPhone.", android);
                importer.SetPlatformTextureSettings(android);
            }
            return r;
        }
        private static bool SetParamsToSettings(ResourceParams param, string category, TextureImporterPlatformSettings settings)
        {
            bool r = false;
            string val;
            if (param.TryGetValue(category + "name", out val)) {
                var v = val;
                if (v != settings.name) {
                    settings.name = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "overridden", out val)) {
                var v = bool.Parse(val);
                if (v != settings.overridden) {
                    settings.overridden = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "maxTextureSize", out val)) {
                var v = int.Parse(val);
                if (v != settings.maxTextureSize) {
                    settings.maxTextureSize = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "resizeAlgorithm", out val)) {
                var v = (TextureResizeAlgorithm)Enum.Parse(typeof(TextureResizeAlgorithm), val);
                if (v != settings.resizeAlgorithm) {
                    settings.resizeAlgorithm = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "format", out val)) {
                var v = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), val);
                if (v != settings.format) {
                    settings.format = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "textureCompression", out val)) {
                var v = (TextureImporterCompression)Enum.Parse(typeof(TextureImporterCompression), val);
                if (v != settings.textureCompression) {
                    settings.textureCompression = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "compressionQuality", out val)) {
                var v = int.Parse(val);
                if (v != settings.compressionQuality) {
                    settings.compressionQuality = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "crunchedCompression", out val)) {
                var v = bool.Parse(val);
                if (v != settings.crunchedCompression) {
                    settings.crunchedCompression = v;
                    r = true;
                }
            }
            if (param.TryGetValue(category + "allowsAlphaSplitting", out val)) {
                var v = bool.Parse(val);
                if (v != settings.allowsAlphaSplitting) {
                    settings.allowsAlphaSplitting = v;
                    r = true;
                }
            }
            return r;
        }
    }
    internal class GetParamsFromTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = true;
            if (operands.Count >= 2) {
                var param = operands[0].As<ResourceParams>();
                var importer = operands[1].As<TextureImporter>();

                param.AddOrUpdate("isReadable", importer.isReadable.ToString());
                param.AddOrUpdate("mipmapEnabled", importer.mipmapEnabled.ToString());
                param.AddOrUpdate("streamingMipmaps", importer.streamingMipmaps.ToString());
                param.AddOrUpdate("filterMode", importer.filterMode.ToString());
                var def = importer.GetDefaultPlatformTextureSettings();
                GetParamsFromSettings(param, "Default.", def);
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                GetParamsFromSettings(param, "Standalone.", standalone);
                var ios = importer.GetPlatformTextureSettings("Android");
                GetParamsFromSettings(param, "Android.", ios);
                var android = importer.GetPlatformTextureSettings("iPhone");
                GetParamsFromSettings(param, "iPhone.", android);
            }
            return r;
        }
        private static void GetParamsFromSettings(ResourceParams param, string category, TextureImporterPlatformSettings settings)
        {
            param.AddOrUpdate(category + "name", settings.name);
            param.AddOrUpdate(category + "overridden", settings.overridden.ToString());
            param.AddOrUpdate(category + "maxTextureSize", settings.maxTextureSize.ToString());
            param.AddOrUpdate(category + "resizeAlgorithm", settings.resizeAlgorithm.ToString());
            param.AddOrUpdate(category + "format", settings.format.ToString());
            param.AddOrUpdate(category + "textureCompression", settings.textureCompression.ToString());
            param.AddOrUpdate(category + "compressionQuality", settings.compressionQuality.ToString());
            param.AddOrUpdate(category + "crunchedCompression", settings.crunchedCompression.ToString());
            param.AddOrUpdate(category + "allowsAlphaSplitting", settings.allowsAlphaSplitting.ToString());
        }
    }
    internal class SetParamsToPrefabExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var param = operands[0].As<ResourceParams>();
                var prefab = operands[1].As<UnityEngine.GameObject>();

                string val;
                if (param.TryGetValue("cullingMode", out val)) {
                    var v = (AnimatorCullingMode)Enum.Parse(typeof(AnimatorCullingMode), val);
                    var anis = prefab.GetComponentsInChildren<Animator>();
                    foreach (var ani in anis) {
                        if (ani.cullingMode != v) {
                            ani.cullingMode = v;
                            r = true;
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class GetParamsFromPrefabExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = true;
            if (operands.Count >= 2) {
                var param = operands[0].As<ResourceParams>();
                var prefab = operands[1].As<UnityEngine.GameObject>();

                AnimatorCullingMode v = AnimatorCullingMode.AlwaysAnimate;
                var anis = prefab.GetComponentsInChildren<Animator>();
                foreach (var ani in anis) {
                    if (v != ani.cullingMode) {
                        v = ani.cullingMode;
                        r = true;
                    }
                }

                param.AddOrUpdate("cullingMode", v.ToString());
            }
            return r;
        }
    }
    internal class UpdateModelDbExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var assetPath = operands[0].AsString;
                var importer = operands[1].As<ModelImporter>();
                if (!string.IsNullOrEmpty(assetPath) && null != importer) {
                    r = ModelImporterParamsDB.UpdateDB(assetPath, importer);
                    EditorUtility.SetDirty(ModelImporterParamsDB.DBInstance);
                }
            }
            return r;
        }
    }
    internal class UpdateTextureDbExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var assetPath = operands[0].AsString;
                var importer = operands[1].As<TextureImporter>();
                if (!string.IsNullOrEmpty(assetPath) && null != importer) {
                    r = TextureImporterParamsDB.UpdateDB(assetPath, importer);
                    EditorUtility.SetDirty(TextureImporterParamsDB.DBInstance);
                }
            }
            return r;
        }
    }
    internal class UpdatePrefabDbExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var assetPath = operands[0].AsString;
                var prefab = operands[1].As<UnityEngine.GameObject>();
                if (!string.IsNullOrEmpty(assetPath) && null != prefab) {
                    r = PrefabParamsDB.UpdateDB(assetPath, prefab);
                    EditorUtility.SetDirty(PrefabParamsDB.DBInstance);
                }
            }
            return r;
        }
    }
    internal class CallScriptExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var proc = operands[0].AsString;
                if (null != proc) {
                    var args = ResourceProcessor.Instance.NewCalculatorValueList();
                    for (int i = 1; i < operands.Count; ++i) {
                        args.Add(operands[i]);
                    }
                    r = ResourceProcessor.Instance.CallScript(Calculator, proc, args);
                    ResourceProcessor.Instance.RecycleCalculatorValueList(args);
                }
            }
            return r;
        }
    }
    internal class SetRedirectExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var dsl = operands[0].AsString;
                if (!string.IsNullOrEmpty(dsl)) {
                    var args = new Dictionary<string, string>();
                    for (int i = 1; i < operands.Count - 1; i += 2) {
                        var key = operands[i].AsString;
                        var val = operands[i + 1].AsString;
                        if (!string.IsNullOrEmpty(key)) {
                            args.Add(key, val);
                        }
                    }
                    Calculator.SetGlobalVariable("redirectdsl", BoxedValue.FromObject(dsl));
                    Calculator.SetGlobalVariable("redirectargs", BoxedValue.FromObject(args));
                }
            }
            return r;
        }
    }
    internal class NewItemExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var results = Calculator.GetVariable("results").As<List<ResourceEditUtility.ItemInfo>>();
                if (null != results) {
                    var item = new ResourceEditUtility.ItemInfo();
                    item.PrepareShowInfo();
                    results.Add(item);
                    r = BoxedValue.FromObject(item);
                }
            }
            return r;
        }
    }
    internal class SetExtraObjectExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var item = operands[0].As<ResourceEditUtility.ItemInfo>();
                r = operands[1];
                item.ExtraObject = r;
            }
            return r;
        }
    }
    internal class GetExtraObjectExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var item = operands[0].As<ResourceEditUtility.ItemInfo>();
                r = item.ExtraObject;
            }
            return r;
        }
    }
    internal class CalcExtraObjectFieldCountExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var items = operands[0].As<IList>();
                var index = operands[1].GetInt();
                if (null != items && index >= 0) {
                    HashSet<string> hash = new HashSet<string>();
                    foreach (var item in items) {
                        IList<string> fields;
                        var itemInfo = item as ResourceEditUtility.ItemInfo;
                        var groupInfo = item as ResourceEditUtility.GroupInfo;
                        if (null != itemInfo) {
                            fields = itemInfo.ExtraObject.As<IList<string>>();
                        }
                        else if (null != groupInfo) {
                            fields = groupInfo.ExtraObject.As<IList<string>>();
                        }
                        else {
                            continue;
                        }
                        if (null != fields && index < fields.Count) {
                            var fv = fields[index];
                            if (!hash.Contains(fv))
                                hash.Add(fv);
                        }
                    }
                    r.Set(hash.Count);
                }
            }
            return r;
        }
    }
    internal class NewExtraListExp : AbstractExpression
    {
        protected override BoxedValue DoCalc()
        {
            var r = BoxedValue.NullObject;
            var list = new List<KeyValuePair<string, BoxedValue>>();
            for (int i = 0; i < m_Expressions.Count - 1; i += 2) {
                var key = m_Expressions[i].Calc().AsString;
                var val = m_Expressions[i + 1].Calc();
                if (null != key) {
                    list.Add(new KeyValuePair<string, BoxedValue>(key, val));
                }
            }
            r = BoxedValue.FromObject(list);
            return r;
        }
        protected override bool Load(Dsl.FunctionData funcData)
        {
            if (funcData.IsHighOrder) {
                LoadCall(funcData.LowerOrderFunction);
            }
            else if (funcData.HaveParam()) {
                LoadCall(funcData);
            }
            if (funcData.HaveStatement()) {
                for (int i = 0; i < funcData.GetParamNum(); ++i) {
                    Dsl.FunctionData callData = funcData.GetParam(i) as Dsl.FunctionData;
                    if (null != callData && callData.GetParamNum() == 2) {
                        var expKey = Calculator.Load(callData.GetParam(0));
                        m_Expressions.Add(expKey);
                        var expVal = Calculator.Load(callData.GetParam(1));
                        m_Expressions.Add(expVal);
                    }
                }
            }
            return true;
        }
        private bool LoadCall(Dsl.FunctionData callData)
        {
            for (int i = 0; i < callData.GetParamNum(); ++i) {
                Dsl.FunctionData paramCallData = callData.GetParam(i) as Dsl.FunctionData;
                if (null != paramCallData && paramCallData.GetParamNum() == 2) {
                    var expKey = Calculator.Load(paramCallData.GetParam(0));
                    m_Expressions.Add(expKey);
                    var expVal = Calculator.Load(paramCallData.GetParam(1));
                    m_Expressions.Add(expVal);
                }
            }
            return true;
        }

        private List<IExpression> m_Expressions = new List<IExpression>();
    }
    internal class ExtraListAddExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 3) {
                var list = operands[0].As<List<KeyValuePair<string, BoxedValue>>>();
                string key = operands[1].AsString;
                var val = operands[2];
                if (null != list && null != key) {
                    list.Add(new KeyValuePair<string, BoxedValue>(key, val));
                }
            }
            return r;
        }
    }
    internal class ExtraListClearExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var list = operands[0].As<List<KeyValuePair<string, BoxedValue>>>();
                if (null != list) {
                    list.Clear();
                }
            }
            return r;
        }
    }
    internal class GetReferenceAssetsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var dict = Calculator.GetVariable("refdict").As<Dictionary<string, HashSet<string>>>();
                var path = operands[0].AsString;
                if (null != dict && !string.IsNullOrEmpty(path)) {
                    HashSet<string> refbyset;
                    if (dict.TryGetValue(path, out refbyset)) {
                        r = BoxedValue.FromObject(refbyset.ToArray());
                    }
                    else {
                        r = BoxedValue.FromObject(new List<string>());
                    }
                }
            }
            return r;
        }
    }
    internal class GetReferenceByAssetsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var dict = Calculator.GetVariable("refbydict").As<Dictionary<string, HashSet<string>>>();
                var path = operands[0].AsString;
                if (null != dict && !string.IsNullOrEmpty(path)) {
                    HashSet<string> refbyset;
                    if (dict.TryGetValue(path, out refbyset)) {
                        r = BoxedValue.FromObject(refbyset.ToArray());
                    }
                    else {
                        r = BoxedValue.FromObject(new List<string>());
                    }
                }
            }
            return r;
        }
    }
    internal class CalcRefCountExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var refDict = Calculator.GetVariable("refdict").As<Dictionary<string, HashSet<string>>>();
                var file = operands[0].AsString;
                if (null != file) {
                    HashSet<string> hash;
                    if (refDict.TryGetValue(file, out hash)) {
                        r = hash.Count;
                    }
                    else {
                        r = 0;
                    }
                }
            }
            return r;
        }
    }
    internal class CalcRefByCountExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var refByDict = Calculator.GetVariable("refbydict").As<Dictionary<string, HashSet<string>>>();
                var file = operands[0].AsString;
                if (null != file) {
                    HashSet<string> hash;
                    if (refByDict.TryGetValue(file, out hash)) {
                        r = hash.Count;
                    }
                    else {
                        r = 0;
                    }
                }
            }
            return r;
        }
    }
    internal class FindAssetExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var sceneDeps = Calculator.GetVariable("scenedeps").As<ResourceEditUtility.SceneDepInfo>();
                var asset = operands[0].AsString;
                var type = operands[1].AsString;
                var assetPath = string.Empty;
                var scenePath = string.Empty;
                UnityEngine.Object sceneObj = null;
                if (!string.IsNullOrEmpty(asset) && !string.IsNullOrEmpty(type)) {
                    bool handled = false;
                    string fileName = Path.GetFileNameWithoutExtension(asset);
                    string dirName = Path.GetDirectoryName(asset).Replace('\\', '/');
                    string[] guids;
                    if (string.IsNullOrEmpty(dirName)) {
                        guids = AssetDatabase.FindAssets(fileName);
                    }
                    else {
                        guids = AssetDatabase.FindAssets(fileName, new string[] { dirName });
                    }
                    for (int i = 0; i < guids.Length; ++i) {
                        var temp = AssetDatabase.GUIDToAssetPath(guids[0]);
                        var name = Path.GetFileNameWithoutExtension(temp);
                        if (string.Compare(name, fileName, true) == 0) {
                            assetPath = temp;
                            handled = true;
                            bool result = ResourceEditUtility.FindSceneObject(asset, type, ref assetPath, ref scenePath, ref sceneObj);
                        }
                    }
                    if (!handled) {
                        bool result = ResourceEditUtility.FindSceneObject(asset, type, ref assetPath, ref scenePath, ref sceneObj);
                        if (result) {
                        }
                        else if (type == "Texture2D") {
                            if (sceneDeps.name2paths.TryGetValue(asset + ".png", out assetPath)) {
                            }
                            else if (sceneDeps.name2paths.TryGetValue(asset + ".tga", out assetPath)) {
                            }
                            else if (sceneDeps.name2paths.TryGetValue(asset + ".jpg", out assetPath)) {
                            }
                            else if (sceneDeps.name2paths.TryGetValue(asset + ".exr", out assetPath)) {
                            }
                            else if (sceneDeps.name2paths.TryGetValue(asset + ".hdr", out assetPath)) {
                            }
                        }
                        else if (type == "Mesh") {
                            sceneDeps.name2paths.TryGetValue(asset + ".fbx", out assetPath);
                        }
                        else if (type == "AnimationClip") {
                            if (sceneDeps.name2paths.TryGetValue(asset + ".clip", out assetPath)) {
                            }
                            else if (sceneDeps.name2paths.TryGetValue(asset + ".fbx", out assetPath)) {
                            }
                        }
                        else if (type == "Material") {
                            sceneDeps.name2paths.TryGetValue(asset + ".mat", out assetPath);
                        }
                        else if (type == "Shader") {
                            sceneDeps.name2paths.TryGetValue(asset + ".shader", out assetPath);
                        }
                        else {
                            if (sceneDeps.name2paths.TryGetValue(asset + ".prefab", out assetPath)) {
                            }
                            else if (sceneDeps.name2paths.TryGetValue(asset + ".asset", out assetPath)) {
                            }
                        }
                    }
                }
                r = BoxedValue.FromObject(new object[] { assetPath, scenePath, sceneObj });
            }
            return r;
        }
    }
    internal class SelectFrameExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var frame = operands[0].GetInt();
                var w = EditorWindow.GetWindow<ProfilerWindow>();
                w.Show(true);
                w.Focus();
                w.selectedFrameIndex = frame - 1;
                r = true;
            }
            return r;
        }
    }
    internal class FilterCpuSampleExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var filter = operands[0].AsString;
                var threadIndex = 0;
                if (operands.Count >= 2) {
                    threadIndex = operands[1].GetInt();
                }
                var w = EditorWindow.GetWindow<ProfilerWindow>();
                w.Show(true);
                w.Focus();
                var sel = w.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
                sel.focusedThreadIndex = threadIndex;
                sel.sampleNameSearchFilter = filter;
                r = true;
            }
            return r;
        }
    }
    internal class FilterGpuSampleExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var filter = operands[0].AsString;
                var w = EditorWindow.GetWindow<ProfilerWindow>();
                w.Show(true);
                w.Focus();
                var sel = w.GetFrameTimeViewSampleSelectionController(ProfilerWindow.gpuModuleIdentifier);
                sel.focusedThreadIndex = 1;
                sel.sampleNameSearchFilter = filter;
                r = true;
            }
            return r;
        }
    }
    internal class SelectSampleExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 3) {
                var frame = operands[0].As<ResourceEditUtility.InstrumentInfo>();
                var record = operands[1].As<ResourceEditUtility.InstrumentRecord>();
                var tinfo = operands[2].As<ResourceEditUtility.InstrumentThreadInfo>();
                var w = EditorWindow.GetWindow<ProfilerWindow>();
                w.Show(true);
                w.Focus();
                w.selectedFrameIndex = frame.frame - 1;
                var sel = w.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
                sel.focusedThreadIndex = tinfo.theadIndex;

                using (var hierView = ProfilerDriver.GetHierarchyFrameDataView(frame.frame, tinfo.theadIndex, HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, true)) {
                    if (null != hierView && hierView.valid) {
                        List<int> parentsCacheList = new List<int>();
                        List<int> childrenCacheList = new List<int>();
                        List<int> indices = new List<int>();
                        int rootId = hierView.GetRootItemID();
                        hierView.GetItemDescendantsThatHaveChildren(rootId, parentsCacheList);
                        foreach (int parentId in parentsCacheList) {
                            childrenCacheList.Clear();
                            hierView.GetItemChildren(parentId, childrenCacheList);

                            foreach (var id in childrenCacheList) {
                                string path = hierView.GetItemPath(id);
                                if (path == record.layerPath) {
                                    int markerId = hierView.GetItemMarkerID(id);
                                    List<int> markerIdPath = new List<int>();
                                    hierView.GetItemMarkerIDPath(id, markerIdPath);

                                    ProfilerEditorUtility.SetSelection(sel, frame.frame - 1, tinfo.threadGroup, tinfo.threadName, markerId, markerIdPath, tinfo.threadId);
                                    r = true;
                                    return r;
                                }
                            }
                        }
                    }
                }
                ProfilerEditorUtility.SetSelection(sel, frame.frame - 1, tinfo.threadGroup, tinfo.threadName, record.name);
                r = true;
            }
            return r;
        }
    }
    internal class SelectPropertyPathExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var path = operands[0].AsString;
                ProfilerDriver.selectedPropertyPath = path;
                var w = EditorWindow.GetWindow<ProfilerWindow>();
                w.Show(true);
                w.Focus();
                r = true;
            }
            return r;
        }
    }
    internal class SetMarkerFilterExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var filter = operands[0].AsString;
                ProfilerDriver.SetMarkerFiltering(filter);
                var w = EditorWindow.GetWindow<ProfilerWindow>();
                w.Show(true);
                w.Focus();
                r = true;
            }
            return r;
        }
    }
    internal class SetMaxRefByNumPerObjExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = ShortestPathToRootObjectFinder.s_MaxRefByNumPerObj;
            if (operands.Count >= 1) {
                int num = operands[0].GetInt();
                ShortestPathToRootObjectFinder.s_MaxRefByNumPerObj = num;
                r = num;
            }
            return r;
        }
    }
    internal class FindShortestPathToRootExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                if (obj.IsObject && obj.GetObject() is ObjectData) {
                    var data = (ObjectData)obj.GetObject();
                    r = BoxedValue.FromObject(ResourceProcessor.Instance.FindShortestPathToRoot(data));
                }
                else {
                    try {
                        ulong addr = obj.GetULong();
                        r = BoxedValue.FromObject(ResourceProcessor.Instance.FindShortestPathToRoot(addr));
                    }
                    catch {
                    }
                }
            }
            return r;
        }
    }
    internal class GetRefByObjectDataExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                if (obj.IsObject && obj.GetObject() is ObjectData) {
                    var data = (ObjectData)obj.GetObject();
                    r = BoxedValue.FromObject(ResourceProcessor.Instance.GetRefByObjectData(data));
                }
                else {
                    try {
                        ulong addr = obj.GetULong();
                        r = BoxedValue.FromObject(ResourceProcessor.Instance.GetRefByObjectData(addr));
                    }
                    catch {
                    }
                }
            }
            return r;
        }
    }
    internal class GetRefObjectDataExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                if (obj.IsObject && obj.GetObject() is ObjectData) {
                    var data = (ObjectData)obj.GetObject();
                    r = BoxedValue.FromObject(ResourceProcessor.Instance.GetRefObjectData(data));
                }
                else {
                    try {
                        ulong addr = obj.GetULong();
                        r = BoxedValue.FromObject(ResourceProcessor.Instance.GetRefObjectData(addr));
                    }
                    catch {
                    }
                }
            }
            return r;
        }
    }
    internal class ObjectDataFromAddressExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                ulong addr = obj.GetULong();
                r = BoxedValue.FromObject(ResourceProcessor.Instance.ObjectDataFromAddress(addr));
            }
            return r;
        }
    }
    internal class ObjectDataFromUnifiedObjectIndexExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                long index = obj.GetLong();
                r = BoxedValue.FromObject(ResourceProcessor.Instance.ObjectDataFromUnifiedObjectIndex(index));
            }
            return r;
        }
    }
    internal class ObjectDataFromNativeObjectIndexExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                long index = obj.GetLong();
                r = BoxedValue.FromObject(ResourceProcessor.Instance.ObjectDataFromNativeObjectIndex(index));
            }
            return r;
        }
    }
    internal class ObjectDataFromManagedObjectIndexExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0];
                long index = obj.GetLong();
                r = BoxedValue.FromObject(ResourceProcessor.Instance.ObjectDataFromManagedObjectIndex(index));
            }
            return r;
        }
    }
    internal class LoadIdaproSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (!string.IsNullOrEmpty(file)) {
                    var fileName = Path.GetFileName(file);
                    var symbols = new List<ResourceEditUtility.SymbolInfo>();
                    using (var sr = new StreamReader(file)) {
                        int curCt = 0;
                        int totalCt = 500000;
                        while (!sr.EndOfStream) {
                            var line = sr.ReadLine();
                            var m = s_Address.Match(line);
                            if (m.Success) {
                                var addr = m.Groups[1].Value;
                                var name = m.Groups[2].Value;
                                ulong v;
                                ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v);
                                if (name.IndexOf("loc_") != 0 && name.IndexOf("off_") != 0 && name.IndexOf("dword_") != 0) {
                                    symbols.Add(new ResourceEditUtility.SymbolInfo { Addr = v, Name = name });
                                }
                            }
                            ++curCt;
                            if (curCt > totalCt)
                                totalCt += 10000;
                            if (curCt % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("load symbols from " + fileName, curCt, totalCt)) {
                                break;
                            }
                        }
                    }
                    r = BoxedValue.FromObject(symbols);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static Regex s_Address = new Regex(@"^ [0-9a-fA-F]+:([0-9a-fA-F]+)       (.*)", RegexOptions.Compiled);
    }
    internal class LoadXcodeSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (!string.IsNullOrEmpty(file)) {
                    var fileName = Path.GetFileName(file);
                    var symbols = new List<ResourceEditUtility.SymbolInfo>();
                    var sections = new List<ulong[]>();
                    using (var sr = new StreamReader(file)) {
                        int curCt = 0;
                        int totalCt = 1000000;
                        while (!sr.EndOfStream) {
                            var line = sr.ReadLine();
                            var m = s_Section.Match(line);
                            if (m.Success) {
                                var addr = m.Groups[1].Value;
                                var size = m.Groups[2].Value;
                                ulong v, s;
                                ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v);
                                ulong.TryParse(size, System.Globalization.NumberStyles.AllowHexSpecifier, null, out s);
                                sections.Add(new ulong[] { v, s });
                            }
                            else {
                                m = s_Address.Match(line);
                                if (m.Success) {
                                    var addr = m.Groups[1].Value;
                                    var size = m.Groups[2].Value;
                                    var name = m.Groups[3].Value;
                                    ulong v, s;
                                    ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v);
                                    ulong.TryParse(size, System.Globalization.NumberStyles.AllowHexSpecifier, null, out s);
                                    if (s > 0x10 && name.IndexOf("_g_") != 0 && name.IndexOf("_s_") != 0 && name.IndexOf("literal string: ") != 0) {
                                        var b = FindBaseAddr(sections, v);
                                        symbols.Add(new ResourceEditUtility.SymbolInfo { Addr = v - b, Name = name });
                                    }
                                }
                            }
                            ++curCt;
                            if (curCt > totalCt)
                                totalCt += 10000;
                            if (curCt % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("load symbols from " + fileName, curCt, totalCt)) {
                                break;
                            }
                        }
                    }
                    r = BoxedValue.FromObject(symbols);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static ulong FindBaseAddr(IList<ulong[]> sections, ulong v)
        {
            int lo = 0;
            int hi = sections.Count - 2;
            for (; lo <= hi;) {
                int i = (lo + hi) / 2;
                var st = sections[i][0];
                var ed = st + sections[i][1];
                if (st > v) {
                    hi = i - 1;
                }
                else if (ed <= v) {
                    lo = i + 1;
                }
                else {
                    return st;
                }
            }
            return 0;
        }

        private static Regex s_Section = new Regex(@"^0x([0-9a-fA-F]+)	0x([0-9a-fA-F]+)	__TEXT	", RegexOptions.Compiled);
        private static Regex s_Address = new Regex(@"^0x([0-9a-fA-F]+)	0x([0-9a-fA-F]+)	\[[0-9]+\] (.*)", RegexOptions.Compiled);
    }
    internal class LoadBuglyAndroidSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (!string.IsNullOrEmpty(file)) {
                    var fileName = Path.GetFileName(file);
                    var symbols = new List<ResourceEditUtility.SymbolInfo>();
                    using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                        using (var br = new BinaryReader(fs)) {
                            long fileLength = br.BaseStream.Length;
                            if (fileLength > sizeof(int) * 4) {
                                ResourceEditUtility.StifHeader header = new ResourceEditUtility.StifHeader();
                                header.Tag1 = br.ReadInt32();
                                header.Tag2 = br.ReadInt32();
                                header.Tag3 = br.ReadInt32();
                                header.Count = ResourceEditUtility.StifSymbolInfo.ReverseEndian(br.ReadInt32());

                                int curCt = 0;
                                int totalCt = header.Count;
                                var stifSyms = new List<ResourceEditUtility.StifSymbolInfo>();
                                if (fileLength > sizeof(int) * 4 + sizeof(int) * 3 * totalCt) {
                                    for (int i = 0; i < totalCt; ++i) {
                                        var stifSym = new ResourceEditUtility.StifSymbolInfo();
                                        stifSym.Begin = ResourceEditUtility.StifSymbolInfo.ReverseEndian(br.ReadInt32());
                                        stifSym.End = ResourceEditUtility.StifSymbolInfo.ReverseEndian(br.ReadInt32());
                                        stifSym.NameOffset = ResourceEditUtility.StifSymbolInfo.ReverseEndian(br.ReadInt32());
                                        stifSyms.Add(stifSym);

                                        ++curCt;
                                        if (curCt % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("load symbols from " + fileName, curCt, totalCt)) {
                                            break;
                                        }
                                    }
                                }

                                foreach (var stifSym in stifSyms) {
                                    long pos = br.BaseStream.Seek(stifSym.NameOffset, SeekOrigin.Begin);
                                    for (int i = 0; i < c_max_name_length && pos + i < fileLength; ++i) {
                                        byte b = br.ReadByte();
                                        if (b != 0) {
                                            s_NameBuffer[i] = b;
                                        }
                                        else {
                                            var str = Encoding.ASCII.GetString(s_NameBuffer, 0, i);
                                            var sym = new ResourceEditUtility.SymbolInfo { Addr = (ulong)stifSym.Begin, Name = str };
                                            symbols.Add(sym);
                                            break;
                                        }
                                    }

                                    ++curCt;
                                    if (curCt % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("load symbols name from " + fileName, curCt, totalCt)) {
                                        break;
                                    }
                                }
                            }
                        }
                        fs.Close();
                    }
                    r = BoxedValue.FromObject(symbols);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }

        private const int c_max_name_length = 1024;
        private static byte[] s_NameBuffer = new byte[c_max_name_length];
    }
    internal class LoadBuglyIosSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (!string.IsNullOrEmpty(file)) {
                    var fileName = Path.GetFileName(file);
                    var symbols = new List<ResourceEditUtility.SymbolInfo>();
                    using (var sr = new StreamReader(file)) {
                        int curCt = 0;
                        int totalCt = 10000000;
                        while (!sr.EndOfStream) {
                            var line = sr.ReadLine();
                            var m = s_Address.Match(line);
                            if (m.Success) {
                                var addr = m.Groups[1].Value;
                                var name = m.Groups[2].Value;
                                ulong v;
                                ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v);
                                symbols.Add(new ResourceEditUtility.SymbolInfo { Addr = v, Name = name });
                            }
                            ++curCt;
                            if (curCt > totalCt)
                                totalCt += 10000;
                            if (curCt % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("load symbols from " + fileName, curCt, totalCt)) {
                                break;
                            }
                        }
                    }
                    r = BoxedValue.FromObject(symbols);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static Regex s_Address = new Regex(@"^([0-9a-fA-F]+)\s+[0-9a-fA-F]+\s+(.*)", RegexOptions.Compiled);
    }
    internal class ConvertToRelativeAddrsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 3) {
                var lines = operands[0].As<IList<string>>();
                var key = operands[1].AsString;
                var baseAddr = operands[2].GetULong();
                if (null != lines && null != key && baseAddr > 0) {
                    for (int i = 0; i < lines.Count; ++i) {
                        lines[i] = lines[i].TrimEnd();
                    }
                    int ct = lines.Count;
                    for (int ix = 0; ix < ct; ++ix) {
                        var line = lines[ix];
                        var m = s_Address.Match(line);
                        string addr = null, so = null;
                        if (m.Success) {
                            addr = m.Groups[1].Value;
                            so = m.Groups[2].Value;
                            if (so.Contains(key)) {
                                ulong v;
                                if (ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v)) {
                                    v -= baseAddr;
                                    lines[ix] = line.Replace(addr, v.ToString("x8"));
                                }
                            }
                        }
                        if (ResourceProcessor.Instance.DisplayCancelableProgressBar("convert addr ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(lines);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static Regex s_Address = new Regex(@"^[0-9]+\s+#[0-9]+\s+pc\s+([0-9a-fA-F]+)\s+(\S+)", RegexOptions.Compiled);
    }
    internal class MapBuglyAndroidSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 3) {
                var lines = operands[0].As<IList<string>>();
                var symbols = operands[1].As<IList<ResourceEditUtility.SymbolInfo>>();
                var key = operands[2].AsString;
                var prefix = string.Empty;
                if (operands.Count >= 4) {
                    prefix = operands[3].AsString;
                }
                if (null != lines && null != symbols && null != key) {
                    for (int i = 0; i < lines.Count; ++i) {
                        if (string.IsNullOrEmpty(prefix))
                            lines[i] = lines[i].TrimEnd();
                        else
                            lines[i] = prefix + lines[i].TrimEnd();
                    }
                    int ct = lines.Count;
                    for (int ix = 0; ix < ct; ++ix) {
                        var line = lines[ix];
                        var m = s_Address.Match(line);
                        string addr = null, so = null;
                        bool isMatch = false;
                        if (m.Success) {
                            addr = m.Groups[1].Value;
                            so = m.Groups[2].Value;
                            isMatch = true;
                        }
                        else {
                            m = s_Address2.Match(line);
                            if (m.Success) {
                                so = m.Groups[1].Value;
                                addr = m.Groups[2].Value;
                                isMatch = true;
                            }
                        }
                        if (isMatch && so.Contains(key)) {
                            ulong v;
                            if (ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v)) {
                                int lo = 0;
                                int hi = symbols.Count - 2;
                                for (; lo <= hi;) {
                                    int i = (lo + hi) / 2;
                                    var st = symbols[i].Addr;
                                    var ed = symbols[i + 1].Addr;
                                    var name = symbols[i].Name;
                                    if (st > v) {
                                        hi = i - 1;
                                    }
                                    else if (ed <= v) {
                                        lo = i + 1;
                                    }
                                    else {
                                        lines[ix] = s_Remove.Replace(line, string.Empty) + " " + name;
                                        break;
                                    }
                                }
                            }
                        }
                        if (ResourceProcessor.Instance.DisplayCancelableProgressBar("map symbols ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(lines);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static Regex s_Address = new Regex(@"^[0-9]+\s+#[0-9]+\s+pc\s+([0-9a-fA-F]+)\s+(\S+)", RegexOptions.Compiled);
        private static Regex s_Address2 = new Regex(@"^[0-9]+\s+(\S+)\.([0-9a-fA-F]+).*", RegexOptions.Compiled);
        private static Regex s_Remove = new Regex(@"/[^=]*==", RegexOptions.Compiled);
    }
    internal class MapBuglyIosSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 3) {
                var lines = operands[0].As<IList<string>>();
                var symbols = operands[1].As<IList<ResourceEditUtility.SymbolInfo>>();
                var key = operands[2].AsString;
                var prefix = string.Empty;
                if (operands.Count >= 4) {
                    prefix = operands[3].AsString;
                }
                if (null != lines && null != symbols && null != key) {
                    for (int i = 0; i < lines.Count; ++i) {
                        if (string.IsNullOrEmpty(prefix))
                            lines[i] = lines[i].TrimEnd();
                        else
                            lines[i] = prefix + lines[i].TrimEnd();
                    }
                    int ct = lines.Count;
                    for (int ix = 0; ix < ct; ++ix) {
                        var line = lines[ix];
                        var m = s_Address.Match(line);
                        if (m.Success) {
                            var so = m.Groups[1].Value;
                            var offset = m.Groups[2].Value;
                            if (so.Contains(key)) {
                                ulong v;
                                if (ulong.TryParse(offset, System.Globalization.NumberStyles.Integer, null, out v)) {
                                    int lo = 0;
                                    int hi = symbols.Count - 2;
                                    for (; lo <= hi;) {
                                        int i = (lo + hi) / 2;
                                        var st = symbols[i].Addr;
                                        var ed = symbols[i + 1].Addr;
                                        var name = symbols[i].Name;
                                        if (st > v) {
                                            hi = i - 1;
                                        }
                                        else if (ed <= v) {
                                            lo = i + 1;
                                        }
                                        else {
                                            lines[ix] = line + " " + name;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        if (ResourceProcessor.Instance.DisplayCancelableProgressBar("map symbols ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(lines);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static Regex s_Address = new Regex(@"^[0-9]+\s+(\S+)\s+0x[0-9a-fA-F]+ 0x[0-9a-fA-F]+ \+ ([0-9]+)", RegexOptions.Compiled);
    }
    internal class MapMyhookSymbolsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 4) {
                var lines = operands[0].As<IList<string>>();
                var section_start = operands[1].GetULong();
                var section_end = operands[2].GetULong();
                var symbols = operands[3].As<IList<ResourceEditUtility.SymbolInfo>>();
                var key = string.Empty;
                if (operands.Count >= 5)
                    key = operands[4].AsString;
                if (null != lines && null != symbols && null != key) {
                    for (int i = 0; i < lines.Count; ++i) {
                        lines[i] = lines[i].TrimEnd();
                    }
                    int ct = lines.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    for (int ix = 0; ix < ct; ++ix) {
                        var line = lines[ix];
                        var m = s_Address1.Match(line);
                        if (m.Success) {
                            var addr = m.Groups[1].Value;
                            var raddr = m.Groups[2].Value;
                            var so = m.Groups[3].Value;
                            if (!string.IsNullOrEmpty(key) && so.Contains(key)) {
                                ulong v;
                                if (ulong.TryParse(raddr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v)) {
                                    string name = FindSymbol(v, symbols);
                                    if (!string.IsNullOrEmpty(name)) {
                                        lines[ix] = s_Remove.Replace(line, string.Empty) + " " + name;
                                    }
                                }
                            }
                            else {
                                ulong v;
                                if (ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v) && v >= section_start && v < section_end) {
                                    v -= section_start;
                                    string name = FindSymbol(v, symbols);
                                    if (!string.IsNullOrEmpty(name)) {
                                        lines[ix] = s_Remove.Replace(line, string.Empty) + " " + name;
                                    }
                                }
                            }
                        }
                        else {
                            m = s_Address2.Match(line);
                            if (m.Success) {
                                var addr = m.Groups[1].Value;
                                ulong v;
                                if (ulong.TryParse(addr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out v) && v >= section_start && v < section_end) {
                                    v -= section_start;
                                    string name = FindSymbol(v, symbols);
                                    if (!string.IsNullOrEmpty(name)) {
                                        lines[ix] = s_Remove.Replace(line, string.Empty) + " " + name;
                                    }
                                }
                            }
                        }
                        if (ix % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("map symbols ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(lines);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private static string FindSymbol(ulong v, IList<ResourceEditUtility.SymbolInfo> symbols)
        {
            int lo = 0;
            int hi = symbols.Count - 2;
            for (; lo <= hi;) {
                int i = (lo + hi) / 2;
                var st = symbols[i].Addr;
                var ed = symbols[i + 1].Addr;
                var name = symbols[i].Name;
                if (st > v) {
                    hi = i - 1;
                }
                else if (ed <= v) {
                    lo = i + 1;
                }
                else {
                    return name;
                }
            }
            return string.Empty;
        }

        private static Regex s_Address1 = new Regex(@"#[0-9]+:0x([0-9a-fA-F]+) 0x([0-9a-fA-F]+) (.*)", RegexOptions.Compiled);
        private static Regex s_Address2 = new Regex(@"#[0-9]+:0x([0-9a-fA-F]+)", RegexOptions.Compiled);
        private static Regex s_Remove = new Regex(@"\|[^=]*==", RegexOptions.Compiled);
    }
    internal class GrepExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var lines = operands[0].As<IList<string>>();
                Regex regex = null;
                if (operands.Count >= 2)
                    regex = new Regex(operands[1].AsString, RegexOptions.Compiled);
                var outLines = new List<string>();
                if (null != lines) {
                    int ct = lines.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    for (int i = 0; i < ct; ++i) {
                        string lineStr = lines[i];
                        if (null != regex) {
                            if (regex.IsMatch(lineStr)) {
                                outLines.Add(lineStr);
                            }
                        }
                        else {
                            outLines.Add(lineStr);
                        }
                        if (i % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("grep ...", i, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(outLines);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
    }
    internal class SubstExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 3) {
                var lines = operands[0].As<IList<string>>();
                Regex regex = new Regex(operands[1].AsString, RegexOptions.Compiled);
                string subst = operands[2].AsString;
                int count = -1;
                if (operands.Count >= 4)
                    count = operands[3].GetInt();
                var outLines = new List<string>();
                if (null != lines && null != regex && null != subst) {
                    int ct = lines.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    for (int i = 0; i < ct; ++i) {
                        string lineStr = lines[i];
                        lineStr = regex.Replace(lineStr, subst, count);
                        outLines.Add(lineStr);
                        if (i % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("subst ...", i, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(outLines);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
    }
    internal class SetClipboardExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var str = operands[0].AsString;
                if (null != str) {
                    GUIUtility.systemCopyBuffer = str;
                    r = str;
                }
            }
            return r;
        }
    }
    internal class GetClipboardExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            string r = GUIUtility.systemCopyBuffer;
            return r;
        }
    }
    internal class SelectObjectExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var obj = operands[0].As<UnityEngine.Object>();
                if (null != obj) {
                    ResourceEditUtility.SelectObject(obj);
                    r = true;
                }
            }
            return r;
        }
    }
    internal class SelectProjectObjectExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var path = operands[0].AsString;
                if (!string.IsNullOrEmpty(path)) {
                    ResourceEditUtility.SelectProjectObject(path);
                    r = true;
                }
            }
            return r;
        }
    }
    internal class SelectSceneObjectExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 1) {
                var path = operands[0].AsString;
                if (!string.IsNullOrEmpty(path)) {
                    ResourceEditUtility.SelectSceneObject(path);
                    r = true;
                }
            }
            return r;
        }
    }
    internal class SaveAndReimportExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<AssetImporter>();
                if (null != importer && ResourceEditUtility.EnableSaveAndReimport) {
                    //importer.SaveAndReimport();
                    //ResourceProcessor::Process会统一执行StartAssetEditing/StopAssetEditing
                    AssetDatabase.ImportAsset(importer.assetPath);
                }
            }
            return r;
        }
    }
    internal class SetDirtyExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0].As<UnityEngine.Object>();
                if (null != obj) {
                    EditorUtility.SetDirty(obj);
                }
            }
            return r;
        }
    }
    internal class GetDefaultTextureSettingExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                if (null != importer) {
                    r = BoxedValue.FromObject(importer.GetDefaultPlatformTextureSettings());
                }
            }
            return r;
        }
    }
    internal class GetTextureSettingExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var platform = operands[0].AsString;
                if (null != importer) {
                    r = BoxedValue.FromObject(importer.GetPlatformTextureSettings(platform));
                }
            }
            return r;
        }
    }
    internal class SetTextureSettingExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                if (null != importer && null != setting) {
                    importer.SetPlatformTextureSettings(setting);
                    r = BoxedValue.FromObject(setting);
                }
            }
            return r;
        }
    }
    internal class TextureIsNotAstcExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            int r = 0;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                if (null != importer && null != setting) {
                    bool ret = true;
                    if (setting.overridden) {
                        switch (setting.format) {
                            case TextureImporterFormat.ASTC_10x10:
                            case TextureImporterFormat.ASTC_12x12:
                            case TextureImporterFormat.ASTC_4x4:
                            case TextureImporterFormat.ASTC_5x5:
                            case TextureImporterFormat.ASTC_6x6:
                            case TextureImporterFormat.ASTC_8x8:
                                ret = false;
                                break;
                        }
                        r = ret ? 1 : 0;
                    }
                }
            }
            return r;
        }
    }
    internal class IsAstcTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            int r = 0;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                var sizeNoAlpha = 8;
                var sizeAlpha = 8;
                if (null != importer && null != setting) {
                    bool ret = false;
                    if (importer.textureType == TextureImporterType.NormalMap && importer.maxTextureSize > 512) {
                        sizeNoAlpha = 6;
                        sizeAlpha = 6;
                    }
                    if (importer.alphaSource == TextureImporterAlphaSource.None) {
                        switch (sizeNoAlpha) {
                            case 4:
                                ret = setting.format == TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                ret = setting.format == TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                ret = setting.format == TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                ret = setting.format == TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                ret = setting.format == TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                ret = setting.format == TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                ret = setting.format == TextureImporterFormat.ASTC_8x8;
                                break;
                        }
                    }
                    else {
                        switch (sizeAlpha) {
                            case 4:
                                ret = setting.format == TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                ret = setting.format == TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                ret = setting.format == TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                ret = setting.format == TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                ret = setting.format == TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                ret = setting.format == TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                ret = setting.format == TextureImporterFormat.ASTC_6x6;
                                break;
                        }
                    }
                    r = ret ? 1 : 0;
                }
            }
            return r;
        }
    }
    internal class SetAstcTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                var sizeNoAlpha = 8;
                var sizeAlpha = 8;
                if (null != importer && null != setting) {
                    if (importer.textureShape == TextureImporterShape.TextureCube) {
                        if (setting.maxTextureSize > 128)
                            setting.maxTextureSize = 128;
                    }
                    string fileName = Path.GetFileNameWithoutExtension(importer.assetPath).ToLower();
                    if (importer.textureType == TextureImporterType.NormalMap || fileName.EndsWith("_nm")) {
                        sizeNoAlpha = 6;
                        sizeAlpha = 6;
                    }
                    if (importer.alphaSource == TextureImporterAlphaSource.None) {
                        switch (sizeNoAlpha) {
                            case 4:
                                setting.format = TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                setting.format = TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                setting.format = TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                setting.format = TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                setting.format = TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                setting.format = TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                setting.format = TextureImporterFormat.ASTC_8x8;
                                break;
                        }
                    }
                    else {
                        switch (sizeAlpha) {
                            case 4:
                                setting.format = TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                setting.format = TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                setting.format = TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                setting.format = TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                setting.format = TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                setting.format = TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                setting.format = TextureImporterFormat.ASTC_6x6;
                                break;
                        }
                    }
                }
            }
            return r;
        }

        private static int[] s_AstcSizes = new int[] { 4, 5, 6, 8, 10, 12 };
    }
    internal class IsSceneAstcTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            int r = 0;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                var sizeNoAlpha = 8;
                var sizeAlpha = 8;
                if (null != importer && null != setting) {
                    bool ret = false;
                    if (importer.alphaSource == TextureImporterAlphaSource.None) {
                        switch (sizeNoAlpha) {
                            case 4:
                                ret = setting.format == TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                ret = setting.format == TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                ret = setting.format == TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                ret = setting.format == TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                ret = setting.format == TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                ret = setting.format == TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                ret = setting.format == TextureImporterFormat.ASTC_8x8;
                                break;
                        }
                    }
                    else {
                        switch (sizeAlpha) {
                            case 4:
                                ret = setting.format == TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                ret = setting.format == TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                ret = setting.format == TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                ret = setting.format == TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                ret = setting.format == TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                ret = setting.format == TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                ret = setting.format == TextureImporterFormat.ASTC_6x6;
                                break;
                        }
                    }
                    r = ret ? 1 : 0;
                }
            }
            return r;
        }
    }
    internal class SetSceneAstcTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                var sizeNoAlpha = 8;
                var sizeAlpha = 8;
                if (null != importer && null != setting) {
                    if (importer.textureShape == TextureImporterShape.TextureCube) {
                        if (setting.maxTextureSize > 128)
                            setting.maxTextureSize = 128;
                    }
                    if (importer.alphaSource == TextureImporterAlphaSource.None) {
                        switch (sizeNoAlpha) {
                            case 4:
                                setting.format = TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                setting.format = TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                setting.format = TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                setting.format = TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                setting.format = TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                setting.format = TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                setting.format = TextureImporterFormat.ASTC_8x8;
                                break;
                        }
                    }
                    else {
                        switch (sizeAlpha) {
                            case 4:
                                setting.format = TextureImporterFormat.ASTC_4x4;
                                break;
                            case 5:
                                setting.format = TextureImporterFormat.ASTC_5x5;
                                break;
                            case 6:
                                setting.format = TextureImporterFormat.ASTC_6x6;
                                break;
                            case 8:
                                setting.format = TextureImporterFormat.ASTC_8x8;
                                break;
                            case 10:
                                setting.format = TextureImporterFormat.ASTC_10x10;
                                break;
                            case 12:
                                setting.format = TextureImporterFormat.ASTC_12x12;
                                break;
                            default:
                                setting.format = TextureImporterFormat.ASTC_6x6;
                                break;
                        }
                    }
                }
            }
            return r;
        }

        private static int[] s_AstcSizes = new int[] { 4, 5, 6, 8, 10, 12 };
    }
    internal class IsTextureNoAlphaSourceExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                if (null != importer) {
                    r = importer.alphaSource == TextureImporterAlphaSource.None;
                }
            }
            return r;
        }
    }
    internal class DoesTextureHaveAlphaExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                if (null != importer) {
                    r = importer.DoesSourceTextureHaveAlpha();
                }
            }
            return r;
        }
    }
    internal class CorrectNoneAlphaTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                if (null != importer && !importer.DoesSourceTextureHaveAlpha()) {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    r = true;
                }
            }
            return r;
        }
    }
    internal class SetNoneAlphaTextureExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<TextureImporter>();
                if (null != importer) {
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    r = true;
                }
            }
            return r;
        }
    }
    internal class GetTextureCompressionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                if (null != setting) {
                    switch (setting.textureCompression) {
                        case TextureImporterCompression.Uncompressed:
                            r = "none";
                            break;
                        case TextureImporterCompression.CompressedLQ:
                            r = "lowquality";
                            break;
                        case TextureImporterCompression.Compressed:
                            r = "normal";
                            break;
                        case TextureImporterCompression.CompressedHQ:
                            r = "highquality";
                            break;
                    }
                }
            }
            return r;
        }
    }
    internal class SetTextureCompressionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var setting = operands[0].As<TextureImporterPlatformSettings>();
                var type = operands[1].AsString;
                if (null != setting && null != type) {
                    r = type;
                    if (type == "none")
                        setting.textureCompression = TextureImporterCompression.Uncompressed;
                    else if (type == "lowquality")
                        setting.textureCompression = TextureImporterCompression.CompressedLQ;
                    else if (type == "normal")
                        setting.textureCompression = TextureImporterCompression.Compressed;
                    else if (type == "highquality")
                        setting.textureCompression = TextureImporterCompression.CompressedHQ;
                }
            }
            return r;
        }
    }
    internal class GetTextureStorageMemorySizeExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            long r = 0;
            if (operands.Count >= 1) {
                var tex = operands[0].As<Texture>();
                if (null != tex) {
                    var method = GetStorageMemorySizeLongMethod();
                    if (null != method) {
                        r = (long)method.Invoke(null, new object[] { tex });
                    }
                }
            }
            return r;
        }
        public static MethodInfo GetStorageMemorySizeLongMethod()
        {
            if (s_GetStorageMemorySizeLong == null) {
                s_GetStorageMemorySizeLong = GetTextureUtil().GetMethod("GetStorageMemorySizeLong", BindingFlags.Static | BindingFlags.Public);
            }
            return s_GetStorageMemorySizeLong;
        }
        public static Type GetTextureUtil()
        {
            if (s_TextureUtil == null) {
                s_TextureUtil = Type.GetType("UnityEditor.TextureUtil,UnityEditor");
            }
            return s_TextureUtil;
        }
        private static Type s_TextureUtil = null;
        private static MethodInfo s_GetStorageMemorySizeLong = null;
    }
    internal class GetTextureRuntimeMemorySizeExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            long r = 0;
            if (operands.Count >= 1) {
                var tex = operands[0].As<Texture>();
                if (null != tex) {
                    var method = GetRuntimeMemorySizeLongMethod();
                    if (null != method) {
                        r = (long)method.Invoke(null, new object[] { tex });
                    }
                }
            }
            return r;
        }
        public static MethodInfo GetRuntimeMemorySizeLongMethod()
        {
            if (s_GetStorageMemorySizeLong == null) {
                s_GetStorageMemorySizeLong = GetTextureStorageMemorySizeExp.GetTextureUtil().GetMethod("GetRuntimeMemorySizeLong", BindingFlags.Static | BindingFlags.Public);
            }
            return s_GetStorageMemorySizeLong;
        }
        private static MethodInfo s_GetStorageMemorySizeLong = null;
    }
    internal class GetRuntimeMemorySizeExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            long r = 0;
            if (operands.Count >= 1) {
                var obj = operands[0].As<UnityEngine.Object>();
                if (null != obj) {
                    r = Profiler.GetRuntimeMemorySizeLong(obj);
                }
            }
            return r;
        }
    }
    internal class SetMaxBoundingBoxExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 6) {
                var centerX = operands[0].GetFloat();
                var centerY = operands[1].GetFloat();
                var centerZ = operands[2].GetFloat();
                var sizeX = operands[3].GetFloat();
                var sizeY = operands[4].GetFloat();
                var sizeZ = operands[5].GetFloat();
                GetBoundingBoxExp.s_MaxBoudingBox = new Bounds(new Vector3(centerX, centerY, centerZ), new Vector3(sizeX, sizeY, sizeZ));
                r = true;
            }
            return BoxedValue.FromBool(r);
        }
    }
    internal class ResetBoundingBoxExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            GetBoundingBoxExp.s_NeedReset = true;
            return BoxedValue.FromBool(true);
        }
    }
    internal class MergeBoundingBoxExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            Bounds retBounds = new Bounds();
            if (operands.Count >= 1) {
                var obj = operands[0].As<GameObject>();
                if (null != obj) {
                    r = true;
                    var renderers = obj.GetComponents<Renderer>();
                    foreach (var r0 in renderers) {
                        if (GetBoundingBoxExp.s_MaxBoudingBox.Contains(r0.bounds.center - r0.bounds.extents) && GetBoundingBoxExp.s_MaxBoudingBox.Contains(r0.bounds.center + r0.bounds.extents)) {
                            if (GetBoundingBoxExp.s_NeedReset) {
                                GetBoundingBoxExp.s_BoudingBox = r0.bounds;
                                GetBoundingBoxExp.s_NeedReset = false;
                            }
                            else {
                                GetBoundingBoxExp.s_BoudingBox.Encapsulate(r0.bounds);
                            }
                        }
                        else {
                            r = false;
                            retBounds = r0.bounds;
                            break;
                        }
                    }
                }
            }
            if (r) {
                retBounds = GetBoundingBoxExp.s_BoudingBox;
            }
            return BoxedValue.From(Tuple.Create(BoxedValue.FromBool(r), BoxedValue.FromObject(new[] { retBounds.min.x, retBounds.min.y, retBounds.min.z, retBounds.max.x, retBounds.max.y, retBounds.max.z })));
        }
    }
    internal class GetBoundingBoxExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (s_NeedReset) {
                return BoxedValue.FromObject(new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f });
            }
            else {
                var bounds = s_BoudingBox;
                return BoxedValue.FromObject(new[] { bounds.min.x, bounds.min.y, bounds.min.z, bounds.max.x, bounds.max.y, bounds.max.z });
            }
        }
        public static Bounds s_BoudingBox = new Bounds();
        public static bool s_NeedReset = true;
        public static Bounds s_MaxBoudingBox = new Bounds(Vector3.zero, new Vector3(10000.0f, 10000.0f, 10000.0f));
    }
    internal class AddOrUpdateBoundingBoxExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = !GetBoundingBoxExp.s_NeedReset;
            var strName = "WorldBoundingBox";
            Shader shader = null;
            var colorName = "_BaseColor";
            var color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            if (operands.Count >= 1) {
                strName = operands[0].GetString();
            }
            if (operands.Count >= 2) {
                var opd = operands[1];
                if (opd.IsString) {
                    shader = Shader.Find(opd.GetString());
                }
                else {
                    shader = opd.As<Shader>();
                }
            }
            if (operands.Count >= 4) {
                colorName = operands[2].GetString();
                var t4 = operands[3].GetTuple4();
                color = new Color(t4.Item1.GetFloat(), t4.Item2.GetFloat(), t4.Item3.GetFloat(), t4.Item4.GetFloat());
            }
            var gobj = GameObject.Find(strName);
            if (!gobj) {
                gobj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gobj.name = strName;
            }
            gobj.transform.position = GetBoundingBoxExp.s_BoudingBox.center;
            gobj.transform.localRotation = Quaternion.identity;
            gobj.transform.localScale = GetBoundingBoxExp.s_BoudingBox.size;
            var renderer = gobj.GetComponent<Renderer>();
            Material mat;
            if (shader) {
                mat = new Material(shader);
                renderer.material = mat;
            }
            else {
                mat = renderer.material;
            }
            mat.SetColor(colorName, color);
            for (int ix = 4; ix < operands.Count - 1; ix += 2) {
                var argName = operands[ix].GetString();
                var argVal = operands[ix + 1];
                if (argVal.IsInteger) {
                    mat.SetInteger(argName, argVal.GetInt());
                }
                else {
                    mat.SetFloat(argName, argVal.GetFloat());
                }
            }
            var collider = gobj.GetComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
            return BoxedValue.FromBool(r);
        }
    }
    internal class GetMeshCompressionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                if (null != importer) {
                    switch (importer.meshCompression) {
                        case ModelImporterMeshCompression.Off:
                            r = "off";
                            break;
                        case ModelImporterMeshCompression.Low:
                            r = "low";
                            break;
                        case ModelImporterMeshCompression.Medium:
                            r = "medium";
                            break;
                        case ModelImporterMeshCompression.High:
                            r = "high";
                            break;
                    }
                }
            }
            return r;
        }
    }
    internal class SetMeshCompressionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                var type = operands[0].AsString;
                if (null != importer && null != type) {
                    r = type;
                    if (type == "off")
                        importer.meshCompression = ModelImporterMeshCompression.Off;
                    else if (type == "low")
                        importer.meshCompression = ModelImporterMeshCompression.Low;
                    else if (type == "medium")
                        importer.meshCompression = ModelImporterMeshCompression.Medium;
                    else if (type == "high")
                        importer.meshCompression = ModelImporterMeshCompression.High;
                }
            }
            return r;
        }
    }
    internal class SetMeshImportExternalMaterialsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                if (null != importer) {
                    r = true;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.materialLocation = ModelImporterMaterialLocation.External;
                    importer.materialName = ModelImporterMaterialName.BasedOnTextureName;
                    importer.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
                }
            }
            return r;
        }
    }
    internal class SetMeshImportInPrefabMaterialsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                if (null != importer) {
                    r = true;
                    importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                    importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                }
            }
            return r;
        }
    }
    internal class CloseMeshAnimationIfNoAnimationExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                if (null != importer) {
                    if (importer.importedTakeInfos.Length <= 0 && importer.defaultClipAnimations.Length <= 0 && importer.clipAnimations.Length <= 0) {
                        importer.animationType = ModelImporterAnimationType.None;
                    }
                }
            }
            return r;
        }
    }
    internal class CollectMeshesExp : SimpleExpressionBase
    {
        internal enum ScopeEnum : int
        {
            None = 0,
            NonParticle = 1,
            Particle = 2,
            All = 3,
        }
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count >= 1) {
                var obj0 = operands[0].As<GameObject>();
                bool includeChildren = false;
                if (operands.Count >= 2) {
                    includeChildren = operands[1].GetBool();
                }
                int scope = (int)ScopeEnum.All; //0--none 1--non particle 2--particle 3--all
                if (operands.Count >= 3) {
                    scope = operands[2].GetInt();
                }
                if (null != obj0) {
                    List<GameObject> list = new List<GameObject>();
                    if (includeChildren) {
                        var comps = obj0.GetComponentsInChildren<Renderer>();
                        foreach (var comp in comps) {
                            list.Add(comp.gameObject);
                        }
                    }
                    else {
                        list.Add(obj0);
                    }
                    List<Mesh> results = new List<Mesh>();
                    foreach (var obj in list) {
                        string objName = obj.name;
                        if ((scope & (int)ScopeEnum.NonParticle) > 0) {
                            var filters = obj.GetComponents<MeshFilter>();
                            foreach (var filter in filters) {
                                if (null != filter && null != filter.sharedMesh) {
                                    var mesh = filter.sharedMesh;
                                    results.Add(mesh);
                                }
                            }
                            var renderers = obj.GetComponents<SkinnedMeshRenderer>();
                            foreach (var renderer in renderers) {
                                if (null != renderer && null != renderer.sharedMesh) {
                                    var mesh = renderer.sharedMesh;
                                    results.Add(mesh);
                                }
                            }
                        }
                        if ((scope & (int)ScopeEnum.Particle) > 0) {
                            var pss = obj.GetComponents<ParticleSystem>();
                            foreach (ParticleSystem ps in pss) {
                                if (null != ps) {
                                    ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                                    if (null != renderer && renderer.renderMode == ParticleSystemRenderMode.Mesh) {
                                        if (null != renderer.mesh) {
                                            var mesh = renderer.mesh;
                                            results.Add(mesh);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return BoxedValue.FromObject(results);
                }
            }
            return BoxedValue.FromObject(new List<Mesh>());
        }
    }
    internal class CollectMeshInfoExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0].As<UnityEngine.GameObject>();
                ModelImporter importer = null;
                if (operands.Count >= 2)
                    importer = operands[1].As<ModelImporter>();
                if (null != obj) {
                    var info = new MeshInfo();
                    info.maxTexWidth = 0;
                    info.maxTexHeight = 0;
                    info.maxTexName = string.Empty;
                    info.maxTexPropName = string.Empty;
                    info.maxKeyFrameCount = 0;
                    info.maxKeyFrameCurveName = string.Empty;
                    info.maxKeyFrameClipName = string.Empty;
                    int vc = 0;
                    int tc = 0;
                    int bc = 0;
                    int mc = 0;
                    int offscreenct = 0;
                    var skinnedrenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    info.skinnedMeshCount = skinnedrenderers.Length;
                    foreach (var renderer in skinnedrenderers) {
                        if (null != renderer.sharedMesh) {
                            var mesh = renderer.sharedMesh;
                            vc += mesh.vertexCount;
                            tc += mesh.triangles.Length;
                            info.AddSingleMesh(false, renderer.name + "/" + mesh.name, 1, mesh.vertexCount, mesh.triangles.Length / 3);
                        }
                        bc += renderer.bones.Length;
                        mc += renderer.sharedMaterials.Length;
                        offscreenct += renderer.updateWhenOffscreen ? 1 : 0;

                        info.CollectMaterials(renderer.sharedMaterials);
                    }
                    var filters = obj.GetComponentsInChildren<MeshFilter>();
                    info.meshFilterCount = filters.Length;
                    foreach (var filter in filters) {
                        if (null != filter.sharedMesh) {
                            var mesh = filter.sharedMesh;
                            vc += mesh.vertexCount;
                            tc += mesh.triangles.Length;
                            info.AddSingleMesh(false, filter.name + "/" + mesh.name, 1, mesh.vertexCount, mesh.triangles.Length / 3);
                        }
                    }
                    var meshrenderers = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in meshrenderers) {
                        mc += renderer.sharedMaterials.Length;

                        info.CollectMaterials(renderer.sharedMaterials);
                    }
                    info.vertexCount = vc;
                    info.triangleCount = tc / 3;
                    info.boneCount = bc;
                    info.materialCount = mc;
                    info.updateWhenOffscreenCount = offscreenct;
                    int alwaysct = 0;
                    var animators = obj.GetComponentsInChildren<Animator>();
                    info.animatorCount = animators.Length;
                    foreach (var anim in animators) {
                        alwaysct += anim.cullingMode == AnimatorCullingMode.AlwaysAnimate ? 1 : 0;
                    }
                    info.alwaysAnimateCount = alwaysct;
                    if (null != importer && info.clipCount <= 0) {
                        info.clipCount = importer.clipAnimations.Length;
                        if (info.clipCount <= 0)
                            info.clipCount = importer.defaultClipAnimations.Length;
                        var objs = AssetDatabase.LoadAllAssetsAtPath(importer.assetPath);
                        foreach (var clipObj in objs) {
                            var clip = clipObj as AnimationClip;
                            if (null != clip) {
                                if (importer.clipAnimations.Length > 0) {
                                    bool isDefault = false;
                                    foreach (var ci in importer.defaultClipAnimations) {
                                        if (ci.name == clip.name) {
                                            isDefault = true;
                                            break;
                                        }
                                    }
                                    if (isDefault)
                                        continue;
                                }
                                info.CollectClip(clip);
                            }
                        }
                    }
                    r = BoxedValue.FromObject(info);
                }
            }
            return r;
        }
    }
    internal class CollectAnimatorControllerInfoExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var ctrl = operands[0].As<RuntimeAnimatorController>();
                if (null != ctrl) {
                    var info = new AnimatorControllerInfo();
                    info.maxKeyFrameCount = 0;
                    info.maxKeyFrameCurveName = string.Empty;
                    info.maxKeyFrameClipName = string.Empty;
                    var editorCtrl = ctrl as UnityEditor.Animations.AnimatorController;
                    if (null == editorCtrl) {
                        var overrideCtrl = ctrl as AnimatorOverrideController;
                        if (null != overrideCtrl) {
                            editorCtrl = overrideCtrl.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                        }
                    }
                    if (null != editorCtrl) {
                        info.layerCount = editorCtrl.layers.Length;
                        info.paramCount = editorCtrl.parameters.Length;
                        int sc = 0;
                        int smc = 0;
                        foreach (var layer in editorCtrl.layers) {
                            sc += CalcStateCount(layer.stateMachine);
                            smc += CalcSubStateMachineCount(layer.stateMachine);
                        }
                        info.stateCount = sc;
                        info.subStateMachineCount = smc;
                    }
                    info.clipCount = ctrl.animationClips.Length;
                    foreach (var clip in ctrl.animationClips) {
                        if (null != clip) {
                            info.CollectClip(clip);
                        }
                    }
                    r = BoxedValue.FromObject(info);
                }
            }
            return r;
        }
        private static int CalcStateCount(AnimatorStateMachine sm)
        {
            int ct = 0;
            ct += sm.states.Length;
            foreach (var ssm in sm.stateMachines) {
                ct += CalcStateCount(ssm.stateMachine);
            }
            return ct;
        }
        private static int CalcSubStateMachineCount(AnimatorStateMachine sm)
        {
            int ct = 0;
            ct += sm.stateMachines.Length;
            foreach (var ssm in sm.stateMachines) {
                ct += CalcSubStateMachineCount(ssm.stateMachine);
            }
            return ct;
        }
    }
    internal class CollectPrefabInfoExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var obj = operands[0].As<UnityEngine.GameObject>();
                if (null != obj) {
                    var info = new MeshInfo();
                    info.maxTexWidth = 0;
                    info.maxTexHeight = 0;
                    info.maxTexName = string.Empty;
                    info.maxTexPropName = string.Empty;
                    info.maxKeyFrameCount = 0;
                    info.maxKeyFrameCurveName = string.Empty;
                    info.maxKeyFrameClipName = string.Empty;
                    int vc = 0;
                    int tc = 0;
                    int bc = 0;
                    int mc = 0;
                    int offscreenct = 0;
                    var skinnedrenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    info.skinnedMeshCount = skinnedrenderers.Length;
                    foreach (var renderer in skinnedrenderers) {
                        if (null != renderer.sharedMesh) {
                            var mesh = renderer.sharedMesh;
                            vc += mesh.vertexCount;
                            tc += mesh.triangles.Length;
                            info.AddSingleMesh(false, renderer.name + "/" + mesh.name, 1, mesh.vertexCount, mesh.triangles.Length / 3);
                        }
                        bc += renderer.bones.Length;
                        mc += renderer.sharedMaterials.Length;
                        offscreenct += renderer.updateWhenOffscreen ? 1 : 0;

                        info.CollectMaterials(renderer.sharedMaterials);
                    }
                    var filters = obj.GetComponentsInChildren<MeshFilter>();
                    info.meshFilterCount = filters.Length;
                    foreach (var filter in filters) {
                        if (null != filter.sharedMesh) {
                            var mesh = filter.sharedMesh;
                            vc += mesh.vertexCount;
                            tc += mesh.triangles.Length;
                            info.AddSingleMesh(false, filter.name + "/" + mesh.name, 1, mesh.vertexCount, mesh.triangles.Length / 3);
                        }
                    }
                    var meshrenderers = obj.GetComponentsInChildren<MeshRenderer>();
                    foreach (var renderer in meshrenderers) {
                        mc += renderer.sharedMaterials.Length;

                        info.CollectMaterials(renderer.sharedMaterials);
                    }
                    var pss = obj.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem ps in pss) {
                        if (null != ps) {
                            int multiple = (int)(ps.emission.rateOverTime.constant * ps.main.startLifetime.constant);
                            multiple = Mathf.Clamp(multiple, 1, ps.main.maxParticles);
                            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                            if (null != renderer && renderer.renderMode == ParticleSystemRenderMode.Mesh) {
                                if (null != renderer.mesh) {
                                    var mesh = renderer.mesh;
                                    vc += multiple * mesh.vertexCount;
                                    tc += multiple * mesh.triangles.Length;
                                    info.AddSingleMesh(true, ps.name + "/" + mesh.name, multiple, mesh.vertexCount, mesh.triangles.Length / 3);
                                }
                                mc += renderer.sharedMaterials.Length;

                                info.CollectMaterials(renderer.sharedMaterials);
                            }
                        }
                    }
                    info.vertexCount = vc;
                    info.triangleCount = tc / 3;
                    info.boneCount = bc;
                    info.materialCount = mc;
                    info.updateWhenOffscreenCount = offscreenct;
                    int alwaysct = 0;
                    var animators = obj.GetComponentsInChildren<Animator>();
                    info.animatorCount = animators.Length;
                    foreach (var anim in animators) {
                        alwaysct += anim.cullingMode == AnimatorCullingMode.AlwaysAnimate ? 1 : 0;
                    }
                    info.alwaysAnimateCount = alwaysct;
                    int clipCount = 0;
                    foreach (var anim in animators) {
                        var ctrl = anim.runtimeAnimatorController;
                        if (null != ctrl) {
                            clipCount += ctrl.animationClips.Length;
                            foreach (var clip in ctrl.animationClips) {
                                if (null != clip) {
                                    info.CollectClip(clip);
                                }
                            }
                        }
                    }
                    info.clipCount = clipCount;
                    r = BoxedValue.FromObject(info);
                }
            }
            return r;
        }
    }
    internal class GetAnimationClipInfoExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                string assetPath = Calculator.GetVariable("assetpath").AsString;
                if (!string.IsNullOrEmpty(assetPath)) {
                    var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    if (null != obj) {
                        var clip = obj as AnimationClip;
                        if (null != clip) {
                            var clipInfo = new AnimationClipInfo();
                            clipInfo.clipName = clip.name;
                            var bindings = AnimationUtility.GetCurveBindings(clip);
                            int maxKfc = 0;
                            string curveName = string.Empty;
                            foreach (var binding in bindings) {
                                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                                int kfc = curve.keys.Length;
                                clipInfo.curves.Add(new KeyFrameCurveInfo { curveName = binding.propertyName, curvePath = binding.path, keyFrameCount = kfc });
                                if (maxKfc < kfc) {
                                    maxKfc = kfc;
                                    curveName = binding.path + "/" + binding.propertyName;
                                }
                            }
                            clipInfo.maxKeyFrameCount = maxKfc;
                            clipInfo.maxKeyFrameCurveName = curveName;
                            r = BoxedValue.FromObject(clipInfo);
                        }
                        Resources.UnloadAsset(obj);
                    }
                }
            }
            return r;
        }
    }
    internal class GetAnimationCompressionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                if (null != importer) {
                    switch (importer.animationCompression) {
                        case ModelImporterAnimationCompression.Off:
                            r = "off";
                            break;
                        case ModelImporterAnimationCompression.KeyframeReduction:
                            r = "keyframe";
                            break;
                        case ModelImporterAnimationCompression.KeyframeReductionAndCompression:
                            r = "keyframeandcompression";
                            break;
                        case ModelImporterAnimationCompression.Optimal:
                            r = "optimal";
                            break;
                    }
                }
            }
            return r;
        }
    }
    internal class SetAnimationCompressionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                var type = operands[0].AsString;
                if (null != importer && null != type) {
                    r = type;
                    if (type == "off")
                        importer.animationCompression = ModelImporterAnimationCompression.Off;
                    else if (type == "keyframe")
                        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
                    else if (type == "keyframeandcompression")
                        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReductionAndCompression;
                    else if (type == "optimal")
                        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                }
            }
            return r;
        }
    }
    internal class GetAnimationTypeExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                if (null != importer) {
                    switch (importer.animationType) {
                        case ModelImporterAnimationType.None:
                            r = "none";
                            break;
                        case ModelImporterAnimationType.Legacy:
                            r = "legacy";
                            break;
                        case ModelImporterAnimationType.Generic:
                            r = "generic";
                            break;
                        case ModelImporterAnimationType.Human:
                            r = "humanoid";
                            break;
                    }
                }
            }
            return r;
        }
    }
    internal class SetAnimationTypeExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                var type = operands[0].AsString;
                if (null != importer && null != type) {
                    r = type;
                    if (type == "none")
                        importer.animationType = ModelImporterAnimationType.None;
                    else if (type == "legacy")
                        importer.animationType = ModelImporterAnimationType.Legacy;
                    else if (type == "generic")
                        importer.animationType = ModelImporterAnimationType.Generic;
                    else if (type == "humanoid")
                        importer.animationType = ModelImporterAnimationType.Human;
                }
            }
            return r;
        }
    }
    internal class SetExtraExposedTransformPathsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var importer = Calculator.GetVariable("importer").As<ModelImporter>();
                var obj = operands[0].As<UnityEngine.GameObject>();
                if (null != importer && null != obj) {
                    List<string> paths = new List<string>();
                    for (int i = 1; i < operands.Count; ++i) {
                        var name = operands[i].AsString;
                        if (!string.IsNullOrEmpty(name)) {
                            if (name == "*") {
                                var boneListPath = ResourceEditUtility.AssetPathToPath("Assets/StreamingAssets/BoneList.txt");
                                var lines = File.ReadAllLines(boneListPath);
                                foreach (var line in lines) {
                                    AddBonePath(paths, obj.transform, line.Trim());
                                }
                            }
                            else {
                                AddBonePath(paths, obj.transform, name);
                            }
                        }
                    }
                    importer.extraExposedTransformPaths = paths.ToArray();
                }
            }
            return r;
        }
        private static void AddBonePath(List<string> paths, Transform root, string name)
        {
            var t = ResourceEditUtility.FindChildRecursive(root, name);
            if (null != t) {
                var path = CalcPath(t);
                paths.Add(path);
            }
        }
        private static string CalcPath(Transform t)
        {
            List<string> names = new List<string>();
            while (null != t) {
                names.Insert(0, t.name);
                if (t.parent == t)
                    break;
                t = t.parent;
            }
            return string.Join("/", names.ToArray());
        }
    }
    internal class ClearAnimationScaleCurveExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 0) {
                var path = Calculator.GetVariable("assetpath").AsString;
                if (null != path) {
                    GameObject obj = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                    bool modified = false;
                    List<AnimationClip> animationClipList = new List<AnimationClip>(AnimationUtility.GetAnimationClips(obj));
                    foreach (AnimationClip theAnimation in animationClipList) {
                        foreach (EditorCurveBinding theCurveBinding in AnimationUtility.GetCurveBindings(theAnimation)) {
                            string name = theCurveBinding.propertyName.ToLower();
                            if (name.Contains("scale")) {
                                AnimationUtility.SetEditorCurve(theAnimation, theCurveBinding, null);
                                modified = true;
                            }
                        }
                    }
                    if (modified) {
                        EditorUtility.SetDirty(obj);
                    }
                }
            }
            return r;
        }
    }
    internal class GetAudioSettingExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var importer = Calculator.GetVariable("importer").As<AudioImporter>();
                var platform = operands[0].AsString;
                if (null != importer) {
                    r = BoxedValue.FromObject(importer.GetOverrideSampleSettings(platform));
                }
            }
            return r;
        }
    }
    internal class SetAudioSettingExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var importer = Calculator.GetVariable("importer").As<AudioImporter>();
                var platform = operands[0].AsString;
                var setting = (AudioImporterSampleSettings)operands[1].GetObject();
                if (null != importer) {
                    importer.SetOverrideSampleSettings(platform, setting);
                    r = BoxedValue.FromObject(setting);
                }
            }
            return r;
        }
    }
    internal class SplitAnimationReferenceExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
#if SPECIAL
            if (operands.Count >= 1) {
                var path = Calculator.GetVariable("assetpath").AsString;
                var reference = AssetDatabase.LoadAssetAtPath<AnimationReference>(path);
                if (null != reference && reference.split) {
                    HashSet<string> hashset = new HashSet<string>();
                    foreach (var item in reference.items) {
                        hashset.Add(item.name);
                    }
                    int lastIndex = operands.Count;
                    for (int i = 0; i < operands.Count; ++i) {
                        var list = operands[i].As<IList>();
                        if (null != list) {
                            SplitFile(reference, hashset, list, path, i);
                        }
                        else {
                            var key = operands[i].AsString;
                            if (!string.IsNullOrEmpty(key) && key == "*") {
                                lastIndex = i;
                            }
                        }
                    }
                    SplitFile(reference, hashset, hashset, path, lastIndex);
                }
                AnimationReferenceInspector.GenerateAnimationPathGroup(reference);
            }
#endif
            return r;
        }
#if SPECIAL
        private static void SplitFile(AnimationReference refs, HashSet<string> hashset, IEnumerable coll, string path, int ix)
        {
            var split = UnityEngine.ScriptableObject.CreateInstance<AnimationReference>();
            split.animator = refs.animator;
            foreach (var obj in coll) {
                var name = obj as string;
                if (name.Contains(".*")) {
                    AddMatchedItems(refs, hashset, name, split);
                }
                else {
                    if (coll != hashset) {
                        hashset.Remove(name);
                    }
                    var item = refs.FindByName(name);
                    split.items.Add(item);
                }
            }
            var newPath = GenFilePath(path, ix);
            var filePath = ResourceEditUtility.AssetPathToPath(newPath);
            if (File.Exists(filePath)) {
                AssetDatabase.DeleteAsset(newPath);
            }
            AssetDatabase.CreateAsset(split, newPath);
        }
        private static void AddMatchedItems(AnimationReference refs, HashSet<string> hashset, string regex, AnimationReference split)
        {
            var regexObj = ResourceEditUtility.GetRegex(regex);
            var items = refs.items;
            for (int i = 0; i < items.Count; ++i) {
                var item = items[i];
                if (regexObj.IsMatch(item.name)) {
                    hashset.Remove(item.name);
                    split.items.Add(item);
                }
            }
        }
        private static string GenFilePath(string path, int ix)
        {
            var dir = Path.GetDirectoryName(path);
            var fileName = Path.GetFileNameWithoutExtension(path);
            return Path.Combine(dir, fileName + "_" + ix + "_split" + ".asset").Replace('\\', '/');
        }
#endif
    }
    internal class CalcMeshVertexComponentCountExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count >= 1) {
                var obj0 = operands[0].As<GameObject>();
                bool includeChildren = false;
                if (operands.Count >= 2) {
                    includeChildren = operands[1].GetBool();
                }
                if (null != obj0) {
                    List<GameObject> list = new List<GameObject>();
                    if (includeChildren) {
                        var comps = obj0.GetComponentsInChildren<Renderer>();
                        foreach (var comp in comps) {
                            list.Add(comp.gameObject);
                        }
                    }
                    else {
                        list.Add(obj0);
                    }
                    List<KeyValuePair<string, int>> pairList = new List<KeyValuePair<string, int>>();
                    foreach (var obj in list) {
                        string objName = obj.name;
                        var filters = obj.GetComponents<MeshFilter>();
                        foreach (var filter in filters) {
                            if (null != filter && null != filter.sharedMesh) {
                                var mesh = filter.sharedMesh;
                                var pair = CalcOneMesh(objName, mesh);
                                pairList.Add(pair);
                            }
                        }
                        var renderers = obj.GetComponents<SkinnedMeshRenderer>();
                        foreach (var renderer in renderers) {
                            if (null != renderer && null != renderer.sharedMesh) {
                                var mesh = renderer.sharedMesh;
                                var pair = CalcOneMesh(objName, mesh);
                                pairList.Add(pair);
                            }
                        }
                    }
                    return BoxedValue.FromObject(pairList);
                }
            }
            return BoxedValue.NullObject;
        }
        private KeyValuePair<string, int> CalcOneMesh(string name, Mesh mesh)
        {
            int ct = 0;
            StringBuilder sb = new StringBuilder();
            sb.Append(name);
            sb.Append("/");
            sb.Append(mesh.name);
            sb.Append(":");
            if (null != mesh.uv && mesh.uv.Length > 0) {
                ++ct;
                sb.Append(" uv");
            }
            if (null != mesh.uv2 && mesh.uv2.Length > 0) {
                ++ct;
                sb.Append(" uv2");
            }
            if (null != mesh.uv3 && mesh.uv3.Length > 0) {
                ++ct;
                sb.Append(" uv3");
            }
            if (null != mesh.uv4 && mesh.uv4.Length > 0) {
                ++ct;
                sb.Append(" uv4");
            }
            if (null != mesh.uv5 && mesh.uv5.Length > 0) {
                ++ct;
                sb.Append(" uv5");
            }
            if (null != mesh.uv6 && mesh.uv6.Length > 0) {
                ++ct;
                sb.Append(" uv6");
            }
            if (null != mesh.uv7 && mesh.uv7.Length > 0) {
                ++ct;
                sb.Append(" uv7");
            }
            if (null != mesh.uv8 && mesh.uv8.Length > 0) {
                ++ct;
                sb.Append(" uv8");
            }
            if (null != mesh.colors && mesh.colors.Length > 0) {
                ++ct;
                sb.Append(" colors");
            }
            return new KeyValuePair<string, int>(sb.ToString(), ct);
        }
    }
    internal class CalcMeshTexRatioExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            object ret = new object[] { string.Empty, 0 };
            if (operands.Count >= 1) {
                var obj0 = operands[0].As<GameObject>();
                bool includeChildren = false;
                if (operands.Count >= 2) {
                    includeChildren = operands[1].GetBool();
                }
                if (null != obj0) {
                    List<GameObject> list = new List<GameObject>();
                    if (includeChildren) {
                        var comps = obj0.GetComponentsInChildren<Renderer>();
                        foreach (var comp in comps) {
                            list.Add(comp.gameObject);
                        }
                    }
                    else {
                        list.Add(obj0);
                    }
                    List<string> uvRatios = new List<string>();
                    List<string> uvTiledRatios = new List<string>();
                    float maxVal = 0;
                    float firstRatio1 = 0;
                    float firstRatio2 = 0;
                    foreach (var obj in list) {
                        var areas = MeshAreaHelper.CalculateMeshAreaWorld(obj);
                        var uvs = MeshAreaHelper.CalculateMeshUVAreaObject(obj);
                        var tiledUvs = MeshAreaHelper.CalculateMeshUVAreaTile(obj);
                        var texSizes = MeshAreaHelper.TextureSize(obj);
                        if (areas.Count == uvs.Count) {
                            Vector2 lastSize = Vector2.one;
                            for (int i = 0; i < areas.Count; ++i) {
                                var area = areas[i];
                                if (area < float.Epsilon) {
                                    uvRatios.Add("0");
                                }
                                else {
                                    float r = uvs[i] / areas[i];
                                    if (i < texSizes.Count) {
                                        var v = texSizes[i];
                                        r *= v.x * v.y;
                                        lastSize = v;
                                    }
                                    else {
                                        r *= lastSize.x * lastSize.y;
                                    }
                                    if (maxVal < r) {
                                        maxVal = r;
                                    }
                                    if (firstRatio1 * 1000 < float.Epsilon) {
                                        firstRatio1 = r;
                                    }
                                    uvRatios.Add(r.ToString("f6"));
                                }
                            }
                        }
                        if (areas.Count == tiledUvs.Count) {
                            Vector2 lastSize = Vector2.one;
                            for (int i = 0; i < areas.Count; ++i) {
                                var area = areas[i];
                                if (area < float.Epsilon) {
                                    uvTiledRatios.Add("0");
                                }
                                else {
                                    float r = tiledUvs[i] / areas[i];
                                    if (i < texSizes.Count) {
                                        var v = texSizes[i];
                                        r *= v.x * v.y;
                                        lastSize = v;
                                    }
                                    else {
                                        r *= lastSize.x * lastSize.y;
                                    }
                                    if (maxVal < r) {
                                        maxVal = r;
                                    }
                                    if (firstRatio2 * 1000 < float.Epsilon) {
                                        firstRatio2 = r;
                                    }
                                    uvTiledRatios.Add(r.ToString("f6"));
                                }
                            }
                        }
                    }
                    var info = string.Format("uv/area:({0}) tiled uv/area:({1})", string.Join(", ", uvRatios.ToArray()), string.Join(", ", uvTiledRatios.ToArray()));
                    ret = new object[] { info, maxVal, firstRatio1, firstRatio2 };
                }
            }
            return BoxedValue.FromObject(ret);
        }
    }
    internal class CalcAssetMd5Exp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (null != file) {
                    file = ResourceEditUtility.AssetPathToPath(file);
                    byte[] buffer = File.ReadAllBytes(file);
                    MD5 mD = MD5.Create();
                    byte[] array = mD.ComputeHash(buffer);
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 0; i < array.Length; i++) {
                        stringBuilder.Append(array[i].ToString("x2"));
                    }
                    r = stringBuilder.ToString();
                }
            }
            return r;
        }
    }
    internal class CalcAssetSizeExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (null != file) {
                    file = ResourceEditUtility.AssetPathToPath(file);
                    var fileInfo = new FileInfo(file);
                    r = fileInfo.Length;
                }
            }
            return r;
        }
    }
    internal class DeleteAssetExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                if (null != file) {
                    r = AssetDatabase.DeleteAsset(file);
                }
            }
            return r;
        }
    }
    internal class GetShaderUtilExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = typeof(ShaderUtil);
            return r;
        }
    }
    internal class GetShaderPropertyCountExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var shader = operands[0].As<Shader>();
                if (null != shader) {
                    if (operands.Count >= 2) {
                        int type = operands[1].GetInt();
                        int count = 0;
                        int ct = ShaderUtil.GetPropertyCount(shader);
                        for (int i = 0; i < ct; ++i) {
                            var t = ShaderUtil.GetPropertyType(shader, i);
                            if ((int)t == type) {
                                ++count;
                            }
                        }
                        r = count;
                    }
                    else {
                        r = ShaderUtil.GetPropertyCount(shader);
                    }
                }
            }
            return r;
        }
    }
    internal class GetShaderPropertyNamesExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var shader = operands[0].As<Shader>();
                int type = (int)ShaderUtil.ShaderPropertyType.TexEnv;
                if (operands.Count >= 2) {
                    type = operands[1].GetInt();
                }
                if (null != shader) {
                    List<string> list = new List<string>();
                    int ct = ShaderUtil.GetPropertyCount(shader);
                    for (int i = 0; i < ct; ++i) {
                        var t = ShaderUtil.GetPropertyType(shader, i);
                        if ((int)t == type) {
                            var name = ShaderUtil.GetPropertyName(shader, i);
                            list.Add(name);
                        }
                    }
                    r = BoxedValue.FromObject(list);
                }
            }
            return r;
        }
    }
    internal class RemoveYamlLeafPropertiesExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count >= 2) {
                string path = operands[0].GetString();
                List<string> properties = new List<string>();
                for (int ix = 1; ix < operands.Count; ++ix) {
                    properties.Add(operands[ix].GetString());
                }
                string full_path = ResourceEditUtility.AssetPathToPath(path);
                if (File.Exists(full_path)) {
                    if (full_path.Length >= 260) {
                        full_path = "\\\\?\\" + full_path;
                    }
                    string txt = File.ReadAllText(full_path);
                    if (EditorUtility.IsValidUnityYAML(txt)) {
                        int i1 = -1, i2 = -1, i3 = -1, i4 = -1;
                        i1 = txt.IndexOf("<<<<<<< .mine");
                        if (i1 >= 0) {
                            i2 = txt.IndexOf("||||||| .r", i1);
                            if (i2 >= 0) {
                                i3 = txt.IndexOf("=======", i2);
                                if (i3 >= 0) {
                                    i4 = txt.IndexOf(">>>>>>> .r", i3);
                                    if (i4 >= 0) {
                                        return BoxedValue.FromBool(false);
                                    }
                                }
                            }
                        }
                        var lines = txt.Split(s_chars, StringSplitOptions.RemoveEmptyEntries);
                        List<string> newLines = new List<string>();
                        foreach (string line in lines) {
                            bool match = false;
                            foreach (var p in properties) {
                                if (line.Contains(p)) {
                                    match = true;
                                    break;
                                }
                            }
                            if (!match) {
                                newLines.Add(line);
                            }
                        }
                        txt = string.Join("\n", newLines.ToArray()) + "\n";
                        if (EditorUtility.IsValidUnityYAML(txt)) {
                            File.WriteAllText(full_path, txt);
                            return BoxedValue.FromBool(true);
                        }
                    }
                }
            }
            return BoxedValue.FromBool(false);
        }
        private static char[] s_chars = new[] { '\r', '\n' };
    }
    internal class CheckYamlExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count >= 1) {
                string path = operands[0].GetString();
                int start = 0;
                int count = 8;
                bool partialConflict = false;
                if (operands.Count >= 2) {
                    start = operands[1].GetInt();
                }
                if (operands.Count >= 3) {
                    count = operands[2].GetInt();
                }
                if (operands.Count >= 4) {
                    partialConflict = operands[3].GetBool();
                }
                string full_path = ResourceEditUtility.AssetPathToPath(path);
                if (File.Exists(full_path)) {
                    if (full_path.Length >= 260) {
                        full_path = "\\\\?\\" + full_path;
                    }
                    string txt = File.ReadAllText(full_path);
                    if (IsText(txt, start, count) && EditorUtility.IsValidUnityYAML(txt)) {
                        int i1 = -1, i2 = -1, i3 = -1, i4 = -1;
                        i1 = txt.IndexOf("<<<<<<< .mine");
                        if (i1 >= 0) {
                            i2 = txt.IndexOf("||||||| .r", i1);
                            if (i2 >= 0) {
                                i3 = txt.IndexOf("=======", i2);
                                if (i3 >= 0) {
                                    i4 = txt.IndexOf(">>>>>>> .r", i3);
                                    if (i4 >= 0) {
                                        return BoxedValue.FromBool(false);
                                    }
                                }
                            }
                        }
                        if (i1 >= 0 || i2 >= 0 || i3 >= 0 || i4 >= 0) {
                            if(partialConflict) {
                                return BoxedValue.FromBool(false);
                            }
                            LogSystem.Warn("[maybe] yaml merge conflict: {0}", full_path);
                        }
                        return BoxedValue.FromBool(true);
                    }
                }
            }
            return BoxedValue.FromBool(false);
        }
        private bool IsText(string txt, int start, int count)
        {
            start = start >= 0 && start < txt.Length ? start : 0;
            count = count > 0 && start + count <= txt.Length ? count : txt.Length - start;
            return txt.IndexOf('\0', start, count) < 0;
        }
        private static string s_WhitespaceChars = " \t\r\n";
    }
    internal class IsPathTooLongExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count >= 1) {
                string path = operands[0].GetString();
                string full_path = ResourceEditUtility.AssetPathToPath(path);
                if (full_path.Length >= 260) {
                    return BoxedValue.FromBool(true);
                }
            }
            return BoxedValue.FromBool(false);
        }
    }
    internal class GetShaderVariantsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var shader = operands[0].As<Shader>();
                if (null != shader) {
                    if (null == s_EmptyShaderVariants) {
                        s_EmptyShaderVariants = Resources.Load("EmptyShaderVariants") as ShaderVariantCollection;
                    }
                    if (null != s_EmptyShaderVariants) {
                        var t = typeof(ShaderUtil);
                        var args = new object[] { shader, s_EmptyShaderVariants, null, null };
                        t.InvokeMember("GetShaderVariantEntries", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, args);
                        var types = args[2] as int[];
                        var keywords = args[3] as string[];
                        r = BoxedValue.FromObject(new object[] { types, keywords });
                    }
                }
            }
            return r;
        }
        private static ShaderVariantCollection s_EmptyShaderVariants = null;
    }
    internal class AddShaderToCollectionExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var shader = operands[0].As<Shader>();
                ShaderVariantCollection coll = null;
                if (operands.Count >= 2) {
                    coll = operands[1].As<ShaderVariantCollection>();
                }
                else {
                    if (null == s_DefaultShaderVariants) {
                        s_DefaultShaderVariants = AssetDatabase.LoadAssetAtPath("Assets/ResourceAB/ShaderVariants/ShaderVariants.shadervariants", typeof(UnityEngine.Object)) as ShaderVariantCollection;
                    }
                    coll = s_DefaultShaderVariants;
                }
                if (null != shader && null != coll) {
                    var t = typeof(ShaderUtil);
                    var args = new object[] { shader, coll };
                    r = BoxedValue.FromObject(t.InvokeMember("AddNewShaderToCollection", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, args));
                    EditorUtility.SetDirty(coll);
                }
            }
            return r;
        }

        private static ShaderVariantCollection s_DefaultShaderVariants = null;
    }
    internal class GetAllDslFilesExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count > 0) {
                int sceneId = operands[0].GetInt();
                var hash = BuildHashSet(sceneId);
                return BoxedValue.FromObject(hash.ToList());
            }
            return BoxedValue.NullObject;
        }
        internal static HashSet<string> BuildHashSet(int sceneId)
        {
#if SPECIAL
            var hash = new HashSet<string>();
            var txt1 = TxtTableParser.ReadFile("../../Product/Table/temp/qs_sceneinfo.txt", Encoding.UTF8);
            var txtTable1 = TxtTableParser.Decode(txt1);
            var row = txtTable1.FindRow("Id", sceneId.ToString());
            var lastRow = row;
            while (null != row) {
                var follow = row.GetInt("FollowSceneId");
                lastRow = row;
                if (follow > 0) {
                    row = txtTable1.FindRow("Id", follow.ToString());
                }
                else {
                    row = null;
                }
            }
            if (null != lastRow) {
                int baseSceneId = lastRow.GetInt("Id");

                var txt2 = TxtTableParser.ReadFile("../../Product/Table/temp/qs_scene_dsl.txt", Encoding.UTF8);
                var txtTable2 = TxtTableParser.Decode(txt2);
                var rows = txtTable2.GetAllRows();
                foreach (var trow in rows) {
                    int id = trow.GetInt("Id");
                    if (id == baseSceneId) {
                        string dslFile = trow.GetString("SceneDslFile");
                        if (!hash.Contains(dslFile)) {
                            hash.Add(dslFile);
                        }
                    }
                }
            }
            return hash;
#else
            return new HashSet<string>();
#endif
        }
    }
    internal class BuildAssetStringListExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var list = new List<string>();
            for (int i = 0; i < operands.Count; ++i) {
                var file = operands[i].AsString;
                if (!string.IsNullOrEmpty(file) && File.Exists(file)) {
                    list.Add(file);
                }
            }
            HashSet<string> strSet = new HashSet<string>();
            int curCt = 0;
            int totalCt = list.Count;
            foreach (var file in list) {
                ++curCt;
                if (EditorUtility.DisplayCancelableProgressBar("收集程序代码资源字符串", string.Format("{0}/{1} {2}", curCt, totalCt, file), curCt * 1.0f / totalCt))
                    break;
                var lines = File.ReadAllLines(file);
                foreach (var line in lines) {
                    if (MaybePathString(line)) {
                        if (!strSet.Contains(line))
                            strSet.Add(line);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            ReadDslString(strSet);
            ReadPlayableString(strSet);
            ReadDisplayString(strSet);
            var dict = ReadTableList();
            curCt = 0;
            totalCt = dict.Count;
            foreach (var pair in dict) {
                ++curCt;
                if (EditorUtility.DisplayCancelableProgressBar("收集策划表资源字符串", string.Format("{0}/{1} {2}", curCt, totalCt, pair.Key), curCt * 1.0f / totalCt))
                    break;
                ReadTableString(pair.Key, pair.Value, strSet);
            }
            EditorUtility.ClearProgressBar();
            return BoxedValue.FromObject(strSet.ToArray());
        }
        private static void ReadDslString(HashSet<string> strSet)
        {
            var hash = BuildDslHashSet();
            foreach (var file in hash) {
                var path = "../../Product/DslFile/" + file + ".dsl";
                if (File.Exists(path)) {
                    Dsl.DslFile dslFile = new Dsl.DslFile();
                    if (dslFile.Load(path, msg => { Debug.LogError(msg); })) {
                        foreach (var dslInfo in dslFile.DslInfos) {
                            ReadDslStringFromSyntaxComponent(dslInfo, strSet);
                        }
                    }
                }
                else {
                    Debug.LogWarningFormat("Can't find dsl file {0} !", path);
                }
            }
        }
        private static HashSet<string> BuildDslHashSet()
        {
#if SPECIAL
            var hash = new HashSet<string>();
            var txt1 = TxtTableParser.ReadFile("../../Product/Table/temp/qs_sceneinfo.txt", Encoding.UTF8);
            var txtTable1 = TxtTableParser.Decode(txt1);
            var fileStr1 = txtTable1.GetColValueString("SceneDslFile");

            var txt2 = TxtTableParser.ReadFile("../../Product/Table/temp/qs_scene_dsl.txt", Encoding.UTF8);
            var txtTable2 = TxtTableParser.Decode(txt2);
            var fileStr2 = txtTable2.GetColValueString("SceneDslFile");

            foreach (var ss in fileStr1) {
                var files = Converter.ConvertStringList(ss);
                if (files.Count > 0) {
                    foreach (var file in files) {
                        if (!hash.Contains(file)) {
                            hash.Add(file);
                        }
                    }
                }
            }
            foreach (var file in fileStr2) {
                if (!hash.Contains(file)) {
                    hash.Add(file);
                }
            }
            return hash;
#else
            return new HashSet<string>();
#endif
        }
        private static void ReadDslStringFromStatementData(Dsl.StatementData val, HashSet<string> strSet)
        {
            foreach (var funcData in val.Functions) {
                ReadDslStringFromFunctionData(funcData.AsFunction, strSet);
            }
        }
        private static void ReadDslStringFromFunctionData(Dsl.FunctionData val, HashSet<string> strSet)
        {
            Dsl.FunctionData cd = null;
            if (val.IsHighOrder)
                cd = val.LowerOrderFunction;
            else if (val.HaveParam())
                cd = val;
            if(null != cd)
                ReadDslStringFromCallData(cd, strSet);
            if (val.HaveStatement()) {
                foreach (var p in val.Params) {
                    ReadDslStringFromSyntaxComponent(p, strSet);
                }
            }
        }
        private static void ReadDslStringFromCallData(Dsl.FunctionData val, HashSet<string> strSet)
        {
            if (val.IsHighOrder) {
                ReadDslStringFromCallData(val.LowerOrderFunction, strSet);
            }
            foreach (var p in val.Params) {
                ReadDslStringFromSyntaxComponent(p, strSet);
            }
        }
        private static void ReadDslStringFromValueData(Dsl.ValueData val, HashSet<string> strSet)
        {
            if (val.GetIdType() == Dsl.ValueData.STRING_TOKEN) {
                var id = val.GetId();
                if (!strSet.Contains(id)) {
                    strSet.Add(id);
                }
            }
        }
        private static void ReadDslStringFromSyntaxComponent(Dsl.ISyntaxComponent comp, HashSet<string> strSet)
        {
            var stData = comp as Dsl.StatementData;
            if (null != stData) {
                ReadDslStringFromStatementData(stData, strSet);
            }
            else {
                var funcData = comp as Dsl.FunctionData;
                if (null != funcData) {
                    ReadDslStringFromFunctionData(funcData, strSet);
                }
                else {
                    var callData = comp as Dsl.FunctionData;
                    if (null != callData) {
                        ReadDslStringFromCallData(callData, strSet);
                    }
                    else {
                        var valueData = comp as Dsl.ValueData;
                        if (null != valueData) {
                            ReadDslStringFromValueData(valueData, strSet);
                        }
                        else {
                            //no other type, unreachable.
                        }
                    }
                }
            }
        }
        private static void ReadPlayableString(HashSet<string> strSet)
        {
#if SPECIAL
            var npc = "../../Product/Table/temp/qs_npc_represent.txt";
            var player = "../../Product/Table/temp/qs_avatar_part.txt";
            var pet = "../../Product/Table/temp/qs_pet_npc.txt";
            var horse = "../../Product/Table/temp/qs_vehicle_info.txt";
            var item = "../../Product/Table/temp/qs_ItemList.txt";

            ReadStringFromTable(strSet, npc, Encoding.UTF8, "AnimBase", "AnimShow");
            ReadStringFromTable(strSet, player, Encoding.UTF8, "anim_base", "anim_show");
            ReadStringFromTable(strSet, pet, Encoding.UTF8, "AnimBase", "AnimShow");
            ReadStringFromTable(strSet, horse, Encoding.UTF8, "AnimBase");
            ReadStringFromTable(strSet, item, Encoding.UTF8, "AnimBase");
#endif
        }
        private static void ReadStringFromTable(HashSet<string> strSet, string file, Encoding encoding, params string[] cols)
        {
#if SPECIAL
            var txt = TxtTableParser.ReadFile(file, encoding);
            var table = TxtTableParser.Decode(txt);
            foreach (var col in cols) {
                var strs = table.GetColValueString(col);
                foreach (var str in strs) {
                    if (!string.IsNullOrEmpty(str) && !strSet.Contains(str)) {
                        strSet.Add(str);
                    }
                }
            }
#endif
        }
        private static void ReadDisplayString(HashSet<string> strSet)
        {
#if SPECIAL
            SkillDisplayer.Displayer.LoadAllConfigs();
            var dicts = SkillDisplayer.Displayer.GetAllConfigs();
            int curCt = 0;
            int totalCt = dicts.Count;
            foreach (var pair in dicts) {
                ++curCt;
                if (EditorUtility.DisplayCancelableProgressBar("收集表现组资源字符串", string.Format("{0}/{1} {2}", curCt, totalCt, pair.Key), curCt * 1.0f / totalCt))
                    break;
                var displayer = pair.Value;
                if (null != displayer) {
                    var list = displayer.GetDependParticlePathes();
                    foreach (var str in list) {
                        if (!strSet.Contains(str))
                            strSet.Add(str);
                    }
                    list = displayer.GetDependSoundEvents();
                    foreach (var str in list) {
                        if (!strSet.Contains(str))
                            strSet.Add(str);
                    }
                }
                else {
                    LogSystem.Error("displayer {0} is null !", pair.Key);
                }
            }
            EditorUtility.ClearProgressBar();
#endif
        }
        private static void ReadTimelineString(HashSet<string> strSet)
        {
#if SPECIAL
            var files = Directory.GetFiles("Assets/ResourceAB/LogicScenes", "*.prefab", SearchOption.AllDirectories);
            foreach (var file in files) {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(file);
                if (null != obj) {
                    var director = obj.GetComponentInChildren<UnityEngine.Playables.PlayableDirector>();
                    if (null != director) {
                        var timelineAsset = director.playableAsset as UnityEngine.Timeline.TimelineAsset;
                        if (null != timelineAsset) {
                            foreach (var track in timelineAsset.GetRootTracks()) {
                                var t = track as UnityEngine.Timeline.PlayableTrack;
                                if (null != t) {
                                    foreach (var clip in t.GetClips()) {
                                        var clipAsset = clip.asset as UnityMessagePlayableAsset;
                                        if (null != clipAsset && (clipAsset.MessageId == "PlaySoundWithFmod" ||
                                            clipAsset.MessageId == "PlayTimelineVoice" ||
                                            clipAsset.MessageId == "PlayMusicWithFmod")) {
                                            if (null == clipAsset.Args && clipAsset.Args.Length >= 1) {
                                                var evt = clipAsset.Args[0].StrValue;
                                                if (!strSet.Contains(evt))
                                                    strSet.Add(evt);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endif
        }
        private static void ReadTableString(string file, string encode, HashSet<string> strSet)
        {
            var lines = File.ReadAllLines("../../Product/Table/temp/" + file, Encoding.GetEncoding(encode));
            var types = lines[0].Split('\t');
            for (int i = 2; i < lines.Length; ++i) {
                var line = lines[i];
                if (line.StartsWith("//"))
                    continue;
                var fields = line.Split('\t');
                for (int j = 0; j < fields.Length && j < types.Length; ++j) {
                    var type = types[j];
                    if (type == "string") {
                        var field = fields[j];
                        if (MaybePathString(field)) {
                            if (!strSet.Contains(field))
                                strSet.Add(field);
                        }
                    }
                    else if (type == "string[]") {
                        var field = fields[j];
                        var vals = field.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var val in vals) {
                            if (MaybePathString(val)) {
                                if (!strSet.Contains(val))
                                    strSet.Add(val);
                            }
                        }
                    }
                }
            }
        }
        private static Dictionary<string, string> ReadTableList()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            var lines = File.ReadAllLines("../../Product/Table/Config.txt");
            for (int i = 1; i < lines.Length; ++i) {
                var line = lines[i];
                var fields = line.Split('\t');
                string file = fields[0];
                string type = fields[1];
                string encode = fields[2];
                dict.Add(file, encode);
            }
            return dict;
        }
        private static bool MaybePathString(string str)
        {
            if (!string.IsNullOrEmpty(str)) {
                foreach (var c in str) {
                    if (char.IsWhiteSpace(c) || char.IsControl(c))
                        return false;
                    if (!char.IsPunctuation(c) && !char.IsDigit(c) && !(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z'))
                        return false;
                }
                return true;
            }
            return false;
        }
    }
    internal class CreateRefAssetExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            if (operands.Count > 1) {
                var assetPath = operands[0].AsString;
                var list = operands[1].As<IList>();
                if (!string.IsNullOrEmpty(assetPath) && null != list) {
                    AssetDatabase.DeleteAsset(assetPath);
                    var asset = ScriptableObject.CreateInstance<ReferencedByTableOrCode>();
                    AssetDatabase.CreateAsset(asset, assetPath);
                    var dict = CollectAllAssets();
                    var objList = new List<UnityEngine.Object>();
                    int curCt = 0;
                    int totalCt = list.Count;
                    foreach (var strObj in list) {
                        var str = strObj as string;
                        if (!string.IsNullOrEmpty(str)) {
                            ++curCt;
                            List<string> files;
                            if (dict.TryGetValue(str, out files)) {
                                int ix = 0;
                                int ct = files.Count;
                                foreach (var file in files) {
                                    ++ix;
                                    if (EditorUtility.DisplayCancelableProgressBar("构造资源依赖", string.Format("{0}/{1} of {2}/{3} {4}", ix, ct, curCt, totalCt, str), curCt * 1.0f / totalCt))
                                        goto L_End;
                                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file);
                                    if (null != obj) {
                                        objList.Add(obj);
                                    }
                                }
                            }
                        }
                    }
                L_End:
                    EditorUtility.ClearProgressBar();
                    asset.Objects = objList.ToArray();
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    return true;
                }
            }
            return false;
        }
        private static Dictionary<string, List<string>> CollectAllAssets()
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
            List<string> files = new List<string>();
            var files1 = Directory.GetFiles(c_ResourceAB, "*", SearchOption.AllDirectories);
            var files2 = Directory.GetFiles(c_Resources, "*", SearchOption.AllDirectories);
            var files3 = Directory.GetFiles(c_GUI, "*", SearchOption.AllDirectories);
            var files4 = Directory.GetFiles(c_SceneRes, "*", SearchOption.AllDirectories);
            files.AddRange(files1);
            files.AddRange(files2);
            files.AddRange(files3);
            files.AddRange(files4);
            int curCt = 0;
            int totalCt = files.Count;
            foreach (var file in files) {
                ++curCt;
                if (curCt % 100 == 0 && EditorUtility.DisplayCancelableProgressBar("收集所有资源文件", string.Format("{0}/{1} {2}", curCt, totalCt, file), curCt * 1.0f / totalCt))
                    break;
                var ext = Path.GetExtension(file);
                if (ext == "meta" || ext == "cs")
                    continue;
                var assetFile = file.Replace('\\', '/');
                var name = Path.GetFileNameWithoutExtension(assetFile);
                List<string> list;
                if (!dict.TryGetValue(name, out list)) {
                    list = new List<string>();
                    dict.Add(name, list);
                }
                list.Add(assetFile);

                TryAddRelativePath(dict, assetFile, c_ResourceAB);
                TryAddRelativePath(dict, assetFile, c_Resources);
                TryAddRelativePath(dict, assetFile, c_GUI);
                TryAddRelativePath(dict, assetFile, c_SceneRes);
            }
            EditorUtility.ClearProgressBar();
            return dict;
        }
        private static void TryAddRelativePath(Dictionary<string, List<string>> dict, string assetFile, string prefix)
        {
            if (assetFile.StartsWith(prefix)) {
                var rpath = assetFile.Substring(prefix.Length);
                List<string> list;
                if (!dict.TryGetValue(rpath, out list)) {
                    list = new List<string>();
                    dict.Add(rpath, list);
                }
                list.Add(assetFile);

                var filename = Path.GetFileName(rpath);
                if (!dict.TryGetValue(filename, out list)) {
                    list = new List<string>();
                    dict.Add(filename, list);
                }
                list.Add(assetFile);

                var dir = Path.GetDirectoryName(rpath);
                var filenameNoExt = Path.GetFileNameWithoutExtension(rpath);
                rpath = Path.Combine(dir, filenameNoExt).Replace('\\', '/');

                if (!dict.TryGetValue(rpath, out list)) {
                    list = new List<string>();
                    dict.Add(rpath, list);
                }
                list.Add(assetFile);

                if (!dict.TryGetValue(filenameNoExt, out list)) {
                    list = new List<string>();
                    dict.Add(filenameNoExt, list);
                }
                list.Add(assetFile);
            }
        }

        private const string c_ResourceAB = "Assets/ResourceAB/";
        private const string c_Resources = "Assets/Resources/";
        private const string c_GUI = "Assets/GUI/";
        private const string c_SceneRes = "Assets/SceneRes/";
    }
    internal class FindRowIndexExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            int r = -1;
            if (operands.Count >= 3) {
                var sheet = operands[0].As<NPOI.SS.UserModel.ISheet>();
                var dict = new Dictionary<int, string>();
                for (int ix = 1; ix < operands.Count - 1; ix += 2) {
                    var index = operands[ix].GetInt();
                    var val = operands[ix + 1].ToString();
                    dict.Add(index, val);
                }
                if (null != sheet) {
                    for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; ++i) {
                        var row = sheet.GetRow(i);
                        bool find = true;
                        foreach (var pair in dict) {
                            var ix = pair.Key;
                            var val = pair.Value;
                            var cell = row.GetCell(ix);
                            if (val != ResourceEditUtility.CellToString(cell).Trim()) {
                                find = false;
                                break;
                            }
                        }
                        if (find) {
                            r = i;
                            break;
                        }
                    }
                }
                else {
                    var tb = operands[0].As<ResourceEditUtility.DataTable>();
                    if (null != tb) {
                        for (int i = 0; i < tb.RowCount; ++i) {
                            var trow = tb.GetRow(i);
                            bool find = true;
                            foreach (var pair in dict) {
                                var ix = pair.Key;
                                var val = pair.Value;
                                if (val != trow.GetCell(ix).Trim()) {
                                    find = false;
                                    break;
                                }
                            }
                            if (find) {
                                r = i;
                                break;
                            }
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class FindRowIndexesExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var list = new List<int>();
            if (operands.Count >= 3) {
                var sheet = operands[0].As<NPOI.SS.UserModel.ISheet>();
                var dict = new Dictionary<int, Regex>();
                for (int ix = 1; ix < operands.Count - 1; ix += 2) {
                    var index = operands[ix].GetInt();
                    var val = ResourceEditUtility.GetRegex(operands[ix + 1].ToString());
                    dict.Add(index, val);
                }
                if (null != sheet) {
                    for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; ++i) {
                        var row = sheet.GetRow(i);
                        bool find = true;
                        foreach (var pair in dict) {
                            var ix = pair.Key;
                            var val = pair.Value;
                            var cell = row.GetCell(ix);
                            if (val.IsMatch(ResourceEditUtility.CellToString(cell).Trim())) {
                                find = false;
                                break;
                            }
                        }
                        if (find) {
                            list.Add(i);
                        }
                    }
                }
                else {
                    var tb = operands[0].As<ResourceEditUtility.DataTable>();
                    if (null != tb) {
                        for (int i = 0; i < tb.RowCount; ++i) {
                            var trow = tb.GetRow(i);
                            bool find = true;
                            foreach (var pair in dict) {
                                var ix = pair.Key;
                                var val = pair.Value;
                                if (val.IsMatch(trow.GetCell(ix).Trim())) {
                                    find = false;
                                    break;
                                }
                            }
                            if (find) {
                                list.Add(i);
                            }
                        }
                    }
                }
            }
            return BoxedValue.FromObject(list);
        }
    }
    internal class FindCellIndexExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            int r = -1;
            if (operands.Count >= 2) {
                var row = operands[0].As<NPOI.SS.UserModel.IRow>();
                var name = operands[1].AsString;
                if (!string.IsNullOrEmpty(name)) {
                    if (null != row) {
                        for (int i = row.FirstCellNum; i <= row.LastCellNum; ++i) {
                            var cell = row.GetCell(i);
                            if (null != cell && ResourceEditUtility.CellToString(cell).Trim() == name) {
                                r = i;
                                break;
                            }
                        }
                    }
                    else {
                        var trow = operands[0].As<ResourceEditUtility.DataRow>();
                        if (null != trow) {
                            for (int i = 0; i < trow.CellCount; ++i) {
                                var val = trow.GetCell(i);
                                if (val == name) {
                                    r = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class FindCellIndexesExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            IList<int> r = null;
            if (operands.Count >= 2) {
                var row = operands[0].As<NPOI.SS.UserModel.IRow>();
                var nameObjs = operands[1].As<IList>();
                var markChars = string.Empty;
                List<NPOI.SS.UserModel.IRow> markRows = null;
                if (operands.Count >= 4) {
                    markChars = operands[2].AsString;
                    markRows = new List<NPOI.SS.UserModel.IRow>();
                    for (int ix = 3; ix < operands.Count; ++ix) {
                        var markRow = operands[ix].As<NPOI.SS.UserModel.IRow>();
                        if (null != markRow) {
                            markRows.Add(markRow);
                        }
                    }
                }
                List<string> names = null;
                if (null != nameObjs) {
                    names = new List<string>();
                    foreach (var nameObj in nameObjs) {
                        names.Add(nameObj.ToString());
                    }
                }
                if (null != names && names.Count > 0) {
                    if (null != row) {
                        r = new List<int>();
                        int curIx = 0;
                        for (int i = row.FirstCellNum; i <= row.LastCellNum; ++i) {
                            var cell = row.GetCell(i);
                            if (null != cell && ResourceEditUtility.CellToString(cell).Trim() == names[curIx] && IsValidColumn(i, markChars, markRows)) {
                                r.Add(i);
                                ++curIx;
                                if (curIx >= names.Count)
                                    break;
                            }
                        }
                    }
                    else {
                        var trow = operands[0].As<ResourceEditUtility.DataRow>();
                        if (null != trow) {
                            r = new List<int>();
                            int curIx = 0;
                            for (int i = 0; i < trow.CellCount; ++i) {
                                var val = trow.GetCell(i);
                                if (val == names[curIx]) {
                                    r.Add(i);
                                    ++curIx;
                                    if (curIx >= names.Count)
                                        break;
                                }
                            }
                        }
                    }
                }
                else {
                    //选择所有有效的列
                    if (null != row) {
                        r = new List<int>();
                        for (int i = row.FirstCellNum; i <= row.LastCellNum; ++i) {
                            var cell = row.GetCell(i);
                            if (null != cell && IsValidColumn(i, markChars, markRows)) {
                                r.Add(i);
                            }
                        }
                    }
                    else {
                        var trow = operands[0].As<ResourceEditUtility.DataRow>();
                        if (null != trow) {
                            r = new List<int>();
                            for (int i = 0; i < trow.CellCount; ++i) {
                                var val = trow.GetCell(i);
                                r.Add(i);
                            }
                        }
                    }
                }
            }
            if (null == r) {
                r = new List<int>();
            }
            return BoxedValue.FromObject(r);
        }
        private static bool IsValidColumn(int ix, string markChars, IList<NPOI.SS.UserModel.IRow> markRows)
        {
            bool r = false;
            if (null != markRows) {
                for (int i = 0; i < markRows.Count; ++i) {
                    var row = markRows[i];
                    if (i < markChars.Length) {
                        var cell = row.GetCell(ix);
                        var v = ResourceEditUtility.CellToString(cell).Trim();
                        if (v.Length == 1 && v[0] == markChars[i]) {
                            r = true;
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class GetCellValueExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var row = operands[0].As<NPOI.SS.UserModel.IRow>();
                var ix = CastTo<int>(operands[1]);
                if (ix >= 0) {
                    if (null != row) {
                        var cell = row.GetCell(ix);
                        if (null != cell) {
                            switch (cell.CellType) {
                                case NPOI.SS.UserModel.CellType.Boolean:
                                    r = cell.BooleanCellValue;
                                    break;
                                case NPOI.SS.UserModel.CellType.Numeric:
                                    r = cell.NumericCellValue;
                                    break;
                                case NPOI.SS.UserModel.CellType.String:
                                    r = cell.StringCellValue;
                                    break;
                                case NPOI.SS.UserModel.CellType.Formula:
                                    switch (cell.CachedFormulaResultType) {
                                        case NPOI.SS.UserModel.CellType.Boolean:
                                            r = cell.BooleanCellValue;
                                            break;
                                        case NPOI.SS.UserModel.CellType.Numeric:
                                            r = cell.NumericCellValue;
                                            break;
                                        case NPOI.SS.UserModel.CellType.String:
                                            r = cell.StringCellValue;
                                            break;
                                        default:
                                            r = BoxedValue.NullObject;
                                            break;
                                    }
                                    break;
                                case NPOI.SS.UserModel.CellType.Blank:
                                    r = string.Empty;
                                    break;
                                default:
                                    r = BoxedValue.NullObject;
                                    break;
                            }
                        }
                    }
                    else {
                        var trow = operands[0].As<ResourceEditUtility.DataRow>();
                        if (null != trow) {
                            r = trow.GetCell(ix);
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class GetCellStringExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            string r = string.Empty;
            if (operands.Count >= 2) {
                var row = operands[0].As<NPOI.SS.UserModel.IRow>();
                var ix = CastTo<int>(operands[1]);
                if (ix >= 0) {
                    if (null != row) {
                        var cell = row.GetCell(ix);
                        r = ResourceEditUtility.CellToString(cell);
                    }
                    else {
                        var trow = operands[0].As<ResourceEditUtility.DataRow>();
                        if (null != trow) {
                            r = trow.GetCell(ix);
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class GetCellNumericExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            double r = 0.0;
            if (operands.Count >= 2) {
                var row = operands[0].As<NPOI.SS.UserModel.IRow>();
                var ix = CastTo<int>(operands[1]);
                if (ix >= 0) {
                    if (null != row) {
                        var cell = row.GetCell(ix);
                        r = ResourceEditUtility.CellToNumeric(cell);
                    }
                    else {
                        var trow = operands[0].As<ResourceEditUtility.DataRow>();
                        if (null != trow) {
                            var v = trow.GetCell(ix);
                            double.TryParse(v, out r);
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class RowToLineExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            string r = string.Empty;
            if (operands.Count >= 1) {
                var row = operands[0].As<NPOI.SS.UserModel.IRow>();
                int skipCols = 0;
                List<int> colIndexes = null;
                if (operands.Count >= 2) {
                    skipCols = operands[1].GetInt();
                }
                if (operands.Count >= 3) {
                    var colObjs = operands[2].As<IList>();
                    if (null != colObjs) {
                        colIndexes = new List<int>();
                        foreach (var colObj in colObjs) {
                            colIndexes.Add(CastTo<int>(colObj));
                        }
                    }
                }
                if (null != row) {
                    string[] cols = null;
                    if (null == colIndexes) {
                        if (skipCols < row.LastCellNum - row.FirstCellNum + 1) {
                            cols = new string[row.LastCellNum - row.FirstCellNum + 1 - skipCols];
                            for (int ix = row.FirstCellNum + skipCols; ix <= row.LastCellNum; ++ix) {
                                var cell = row.GetCell(ix);
                                cols[ix - row.FirstCellNum - skipCols] = ResourceEditUtility.CellToString(cell);
                            }
                            r = string.Join(",", cols);
                        }
                    }
                    else {
                        cols = new string[colIndexes.Count];
                        int i = 0;
                        foreach (var ix in colIndexes) {
                            var cell = row.GetCell(ix);
                            cols[i++] = ResourceEditUtility.CellToString(cell);
                        }
                        r = string.Join(",", cols);
                    }
                }
                else {
                    var trow = operands[0].As<ResourceEditUtility.DataRow>();
                    if (null != trow) {
                        r = trow.GetLine(skipCols, colIndexes);
                    }
                }
            }
            return r;
        }
    }
    internal class TableToHashtableExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 3) {
                var sheet = operands[0].As<NPOI.SS.UserModel.ISheet>();
                int skipRows = operands[1].GetInt();
                List<int> colIndexes = new List<int>();
                var colObjs = operands[2].As<IList>();
                if (null != colObjs) {
                    foreach (var colObj in colObjs) {
                        colIndexes.Add(CastTo<int>(colObj));
                    }
                }
                if (colIndexes.Count > 0) {
                    if (null != sheet) {
                        var dict = new Dictionary<string, object>();
                        for (int i = sheet.FirstRowNum + skipRows; i <= sheet.LastRowNum; ++i) {
                            var temp = dict;
                            var row = sheet.GetRow(i);
                            foreach (var ix in colIndexes) {
                                var cell = row.GetCell(ix);
                                var key = ResourceEditUtility.CellToString(cell);
                                object tempObj;
                                Dictionary<string, object> temp2 = null;
                                if (!temp.TryGetValue(key, out tempObj)) {
                                    temp2 = new Dictionary<string, object>();
                                    temp.Add(key, temp2);
                                }
                                else {
                                    temp2 = tempObj as Dictionary<string, object>;
                                }
                                temp = temp2;
                            }
                            temp.Add(i.ToString(), row);
                        }
                        r = BoxedValue.FromObject(dict);
                    }
                    else {
                        var table = operands[0].As<ResourceEditUtility.DataTable>();
                        if (null != table) {
                            var dict = new Dictionary<string, object>();
                            for (int i = skipRows; i < table.RowCount; ++i) {
                                var temp = dict;
                                var row = table.GetRow(i);
                                foreach (var ix in colIndexes) {
                                    var key = row.GetCell(ix);
                                    object tempObj;
                                    Dictionary<string, object> temp2 = null;
                                    if (!temp.TryGetValue(key, out tempObj)) {
                                        temp2 = new Dictionary<string, object>();
                                        temp.Add(key, temp2);
                                    }
                                    else {
                                        temp2 = tempObj as Dictionary<string, object>;
                                    }
                                    temp = temp2;
                                }
                                temp.Add(i.ToString(), row);
                            }
                            r = BoxedValue.FromObject(dict);
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class FindRowFromHashtableExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var hash = operands[0].As<Dictionary<string, object>>();
                List<string> keys = new List<string>();
                var keyObjs = operands[1].As<IList>();
                if (null != keyObjs) {
                    foreach (var keyObj in keyObjs) {
                        var str = keyObj as string;
                        keys.Add(str);
                    }
                }
                if (null != hash && keys.Count > 0) {
                    var temp = hash;
                    foreach (var key in keys) {
                        object tempObj;
                        if (temp.TryGetValue(key, out tempObj)) {
                            temp = tempObj as Dictionary<string, object>;
                        }
                        else {
                            temp = null;
                            break;
                        }
                    }
                    if (null != temp) {
                        foreach (var pair in temp) {
                            r = BoxedValue.FromObject(pair.Value);
                            break;
                        }
                    }
                    else {
                        r = BoxedValue.NullObject;
                    }
                }
            }
            return r;
        }
    }
    internal class LoadManagedHeapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                var list = new List<ResourceEditUtility.SectionInfo>();
                var lines = File.ReadAllLines(file);
                foreach (var line in lines) {
                    var mapsInfo = new ResourceEditUtility.SectionInfo();
                    var fields = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields[0] == "Managed") {
                        ulong size = ulong.Parse(fields[1]);
                        ulong start = ulong.Parse(fields[2]);
                        mapsInfo.size = size;
                        mapsInfo.vm_start = start;
                        mapsInfo.vm_end = start + size;
                        list.Add(mapsInfo);
                    }
                }
                list.Sort((a, b) => {
                    if (a.vm_start < b.vm_start)
                        return -1;
                    else if (a.vm_start > b.vm_start)
                        return 1;
                    else if (a.vm_end < b.vm_end)
                        return -1;
                    else if (a.vm_end > b.vm_end)
                        return 1;
                    else
                        return 0;
                });
                r = BoxedValue.FromObject(list);
            }
            return r;
        }
    }
    internal class FindManagedHeapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list = operands[0].As<List<ResourceEditUtility.SectionInfo>>();
                var addr = operands[1].GetULong();
                if (null != list && addr > 0) {
                    var low = 0;
                    var high = list.Count - 1;
                    while (low <= high) {
                        var cur = (low + high) / 2;
                        var vs = list[cur].vm_start;
                        var ve = list[cur].vm_end;
                        if (addr < vs)
                            high = cur - 1;
                        else if (addr >= ve)
                            low = cur + 1;
                        else {
                            r = BoxedValue.FromObject(list[cur]);
                            break;
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class MatchManagedHeapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list1 = operands[0].As<List<ResourceEditUtility.SectionInfo>>();
                var list2 = operands[1].As<List<ResourceEditUtility.SectionInfo>>();
                if (null != list1 && null != list2) {
                    int ct = list1.Count + list2.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    int left = -1;
                    int right = -1;
                    ResourceEditUtility.SectionInfo leftInfo = null;
                    ResourceEditUtility.SectionInfo rightInfo = null;
                    MoveNext(list1, ref left, ref leftInfo);
                    MoveNext(list2, ref right, ref rightInfo);
                    var list = new List<ResourceEditUtility.SectionInfo[]>();
                    while (null != leftInfo || null != rightInfo) {
                        if (null == leftInfo) {
                            list.Add(new ResourceEditUtility.SectionInfo[] { null, rightInfo });
                            MoveNext(list2, ref right, ref rightInfo);
                        }
                        else if (null == rightInfo) {
                            list.Add(new ResourceEditUtility.SectionInfo[] { leftInfo, null });
                            MoveNext(list1, ref left, ref leftInfo);
                        }
                        else {
                            if (leftInfo.vm_start == rightInfo.vm_start && leftInfo.vm_end == rightInfo.vm_end) {//完全重叠
                                list.Add(new ResourceEditUtility.SectionInfo[] { leftInfo, rightInfo });
                                MoveNext(list1, ref left, ref leftInfo);
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                            else if (leftInfo.vm_end <= rightInfo.vm_start) {//左上、右下无重叠
                                list.Add(new ResourceEditUtility.SectionInfo[] { leftInfo, null });
                                MoveNext(list1, ref left, ref leftInfo);
                            }
                            else if (leftInfo.vm_start >= rightInfo.vm_end) {//左下、右上无重叠
                                list.Add(new ResourceEditUtility.SectionInfo[] { null, rightInfo });
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                            else {//交叉
                                list.Add(new ResourceEditUtility.SectionInfo[] { leftInfo, null });
                                MoveNext(list1, ref left, ref leftInfo);
                                list.Add(new ResourceEditUtility.SectionInfo[] { null, rightInfo });
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                        }
                        int ix = left + right;
                        if (ix % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("match managed heaps ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(list);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private void MoveNext(List<ResourceEditUtility.SectionInfo> list, ref int index, ref ResourceEditUtility.SectionInfo info)
        {
            if (index < list.Count - 1) {
                ++index;
                info = list[index];
            }
            else {
                info = null;
            }
        }
    }
    internal class CalcMatchedManagedHeapsDiffExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list = operands[0].As<List<ResourceEditUtility.SectionInfo[]>>();
                int index = operands[1].GetInt();
                if (null != list && index >= 0) {
                    int ct = list.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    var results = new List<ResourceEditUtility.SectionInfo>();
                    for (int i = 0; i < ct; ++i) {
                        var infos = list[i];
                        if (index < infos.Length) {
                            var info = infos[index];
                            if (null != info) {
                                bool match = true;
                                for (int ix = 0; ix < infos.Length; ++ix) {
                                    if (ix != index && null != infos[ix]) {
                                        match = false;
                                        break;
                                    }
                                }
                                if (match) {
                                    results.Add(info);
                                }
                            }
                        }
                        if (i % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("calc matched managed heaps diff ...", i, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(results);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private void MoveNext(List<ResourceEditUtility.SectionInfo> list, ref int index, ref ResourceEditUtility.SectionInfo info)
        {
            if (index < list.Count - 1) {
                ++index;
                info = list[index];
            }
            else {
                info = null;
            }
        }
    }
    internal class LoadMapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                var list = new List<ResourceEditUtility.MapsInfo>();
                var lines = File.ReadAllLines(file);
                foreach (var line in lines) {
                    var mapsInfo = new ResourceEditUtility.MapsInfo();
                    var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var se = fields[0].Split('-');
                    ulong start = ulong.Parse(se[0], System.Globalization.NumberStyles.AllowHexSpecifier);
                    ulong end = ulong.Parse(se[1], System.Globalization.NumberStyles.AllowHexSpecifier);
                    mapsInfo.vm_start = start;
                    mapsInfo.vm_end = end;
                    for (int i = 0; i < fields.Length; ++i) {
                        switch (i) {
                            case 1:
                                mapsInfo.flags = fields[i];
                                break;
                            case 2:
                                mapsInfo.offset = fields[i];
                                break;
                            case 3:
                                mapsInfo.file1 = fields[i];
                                break;
                            case 4:
                                mapsInfo.file2 = fields[i];
                                break;
                            case 5:
                                mapsInfo.module = fields[i];
                                break;
                        }
                    }
                    mapsInfo.size += mapsInfo.vm_end - mapsInfo.vm_start;
                    list.Add(mapsInfo);
                }
                list.Sort((a, b) => {
                    if (a.vm_start < b.vm_start)
                        return -1;
                    else if (a.vm_start > b.vm_start)
                        return 1;
                    else if (a.vm_end < b.vm_end)
                        return -1;
                    else if (a.vm_end > b.vm_end)
                        return 1;
                    else
                        return 0;
                });
                r = BoxedValue.FromObject(list);
            }
            return r;
        }
    }
    internal class FindMapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list = operands[0].As<List<ResourceEditUtility.MapsInfo>>();
                var addr = operands[1].GetULong();
                if (null != list && addr > 0) {
                    var low = 0;
                    var high = list.Count - 1;
                    while (low <= high) {
                        var cur = (low + high) / 2;
                        var vs = list[cur].vm_start;
                        var ve = list[cur].vm_end;
                        if (addr < vs)
                            high = cur - 1;
                        else if (addr >= ve)
                            low = cur + 1;
                        else {
                            r = BoxedValue.FromObject(list[cur]);
                            break;
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class MatchMapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list1 = operands[0].As<List<ResourceEditUtility.MapsInfo>>();
                var list2 = operands[1].As<List<ResourceEditUtility.MapsInfo>>();
                if (null != list1 && null != list2) {
                    int ct = list1.Count + list2.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    int left = -1;
                    int right = -1;
                    ResourceEditUtility.MapsInfo leftInfo = null;
                    ResourceEditUtility.MapsInfo rightInfo = null;
                    MoveNext(list1, ref left, ref leftInfo);
                    MoveNext(list2, ref right, ref rightInfo);
                    var list = new List<ResourceEditUtility.MapsInfo[]>();
                    while (null != leftInfo || null != rightInfo) {
                        if (null == leftInfo) {
                            list.Add(new ResourceEditUtility.MapsInfo[] { null, rightInfo });
                            MoveNext(list2, ref right, ref rightInfo);
                        }
                        else if (null == rightInfo) {
                            list.Add(new ResourceEditUtility.MapsInfo[] { leftInfo, null });
                            MoveNext(list1, ref left, ref leftInfo);
                        }
                        else {
                            if (leftInfo.vm_start == rightInfo.vm_start && leftInfo.vm_end == rightInfo.vm_end) {//完全重叠
                                list.Add(new ResourceEditUtility.MapsInfo[] { leftInfo, rightInfo });
                                MoveNext(list1, ref left, ref leftInfo);
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                            else if (leftInfo.vm_end <= rightInfo.vm_start) {//左上、右下无重叠
                                list.Add(new ResourceEditUtility.MapsInfo[] { leftInfo, null });
                                MoveNext(list1, ref left, ref leftInfo);
                            }
                            else if (leftInfo.vm_start >= rightInfo.vm_end) {//左下、右上无重叠
                                list.Add(new ResourceEditUtility.MapsInfo[] { null, rightInfo });
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                            else {//交叉
                                list.Add(new ResourceEditUtility.MapsInfo[] { leftInfo, null });
                                MoveNext(list1, ref left, ref leftInfo);
                                list.Add(new ResourceEditUtility.MapsInfo[] { null, rightInfo });
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                        }
                        int ix = left + right;
                        if (ix % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("match maps ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(list);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private void MoveNext(List<ResourceEditUtility.MapsInfo> list, ref int index, ref ResourceEditUtility.MapsInfo info)
        {
            if (index < list.Count - 1) {
                ++index;
                info = list[index];
            }
            else {
                info = null;
            }
        }
    }
    internal class CalcMatchedMapsDiffExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list = operands[0].As<List<ResourceEditUtility.MapsInfo[]>>();
                int index = operands[1].GetInt();
                if (null != list && index >= 0) {
                    int ct = list.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    var results = new List<ResourceEditUtility.MapsInfo>();
                    for (int i = 0; i < ct; ++i) {
                        var infos = list[i];
                        if (index < infos.Length) {
                            var info = infos[index];
                            if (null != info) {
                                bool match = true;
                                for (int ix = 0; ix < infos.Length; ++ix) {
                                    if (ix != index && null != infos[ix]) {
                                        match = false;
                                        break;
                                    }
                                }
                                if (match) {
                                    results.Add(info);
                                }
                            }
                        }
                        if (i % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("calc matched maps diff ...", i, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(results);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private void MoveNext(List<ResourceEditUtility.MapsInfo> list, ref int index, ref ResourceEditUtility.MapsInfo info)
        {
            if (index < list.Count - 1) {
                ++index;
                info = list[index];
            }
            else {
                info = null;
            }
        }
    }
    internal class LoadSmapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                var list = new List<ResourceEditUtility.SmapsInfo>();
                ResourceEditUtility.SmapsInfo curInfo = null;
                var lines = File.ReadAllLines(file);
                foreach (var line in lines) {
                    var mapsInfo = new ResourceEditUtility.SmapsInfo();
                    var fields = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields[0].IndexOf('-') > 0) {
                        var se = fields[0].Split('-');
                        ulong start = ulong.Parse(se[0], System.Globalization.NumberStyles.AllowHexSpecifier);
                        ulong end = ulong.Parse(se[1], System.Globalization.NumberStyles.AllowHexSpecifier);
                        mapsInfo.vm_start = start;
                        mapsInfo.vm_end = end;
                        for (int i = 0; i < fields.Length; ++i) {
                            switch (i) {
                                case 1:
                                    mapsInfo.flags = fields[i];
                                    break;
                                case 2:
                                    mapsInfo.offset = fields[i];
                                    break;
                                case 3:
                                    mapsInfo.file1 = fields[i];
                                    break;
                                case 4:
                                    mapsInfo.file2 = fields[i];
                                    break;
                                case 5:
                                    mapsInfo.module = fields[i];
                                    break;
                            }
                        }
                        mapsInfo.size += mapsInfo.vm_end - mapsInfo.vm_start;

                        curInfo = mapsInfo;
                    }
                    else {
                        var key = fields[0];
                        var val = fields[1];
                        if (key == "Size:") {
                            curInfo.sizeKB = ulong.Parse(val);
                        }
                        else if (key == "Rss:") {
                            curInfo.rss = ulong.Parse(val);
                        }
                        else if (key == "Pss:") {
                            curInfo.pss = ulong.Parse(val);
                        }
                        else if (key == "Shared_Clean:") {
                            curInfo.shared_clean = ulong.Parse(val);
                        }
                        else if (key == "Shared_Dirty:") {
                            curInfo.shared_dirty = ulong.Parse(val);
                        }
                        else if (key == "Private_Clean:") {
                            curInfo.private_clean = ulong.Parse(val);
                        }
                        else if (key == "Private_Dirty:") {
                            curInfo.private_dirty = ulong.Parse(val);
                        }
                        else if (key == "Referenced:") {
                            curInfo.referenced = ulong.Parse(val);
                        }
                        else if (key == "Anonymous:") {
                            curInfo.anonymous = ulong.Parse(val);
                        }
                        else if (key == "Swap:") {
                            curInfo.swap = ulong.Parse(val);
                        }
                        else if (key == "SwapPss:") {
                            curInfo.swappss = ulong.Parse(val);
                        }
                    }
                    list.Add(mapsInfo);
                }
                list.Sort((a, b) => {
                    if (a.vm_start < b.vm_start)
                        return -1;
                    else if (a.vm_start > b.vm_start)
                        return 1;
                    else if (a.vm_end < b.vm_end)
                        return -1;
                    else if (a.vm_end > b.vm_end)
                        return 1;
                    else
                        return 0;
                });
                r = BoxedValue.FromObject(list);
            }
            return r;
        }
    }
    internal class FindSmapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list = operands[0].As<List<ResourceEditUtility.SmapsInfo>>();
                var addr = operands[1].GetULong();
                if (null != list && addr > 0) {
                    var low = 0;
                    var high = list.Count - 1;
                    while (low <= high) {
                        var cur = (low + high) / 2;
                        var vs = list[cur].vm_start;
                        var ve = list[cur].vm_end;
                        if (addr < vs)
                            high = cur - 1;
                        else if (addr >= ve)
                            low = cur + 1;
                        else {
                            r = BoxedValue.FromObject(list[cur]);
                            break;
                        }
                    }
                }
            }
            return r;
        }
    }
    internal class MatchSmapsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list1 = operands[0].As<List<ResourceEditUtility.SmapsInfo>>();
                var list2 = operands[1].As<List<ResourceEditUtility.SmapsInfo>>();
                if (null != list1 && null != list2) {
                    int ct = list1.Count + list2.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    int left = -1;
                    int right = -1;
                    ResourceEditUtility.SmapsInfo leftInfo = null;
                    ResourceEditUtility.SmapsInfo rightInfo = null;
                    MoveNext(list1, ref left, ref leftInfo);
                    MoveNext(list2, ref right, ref rightInfo);
                    var list = new List<ResourceEditUtility.SmapsInfo[]>();
                    while (null != leftInfo || null != rightInfo) {
                        if (null == leftInfo) {
                            list.Add(new ResourceEditUtility.SmapsInfo[] { null, rightInfo });
                            MoveNext(list2, ref right, ref rightInfo);
                        }
                        else if (null == rightInfo) {
                            list.Add(new ResourceEditUtility.SmapsInfo[] { leftInfo, null });
                            MoveNext(list1, ref left, ref leftInfo);
                        }
                        else {
                            if (leftInfo.vm_start == rightInfo.vm_start && leftInfo.vm_end == rightInfo.vm_end) {//完全重叠
                                list.Add(new ResourceEditUtility.SmapsInfo[] { leftInfo, rightInfo });
                                MoveNext(list1, ref left, ref leftInfo);
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                            else if (leftInfo.vm_end <= rightInfo.vm_start) {//左上、右下无重叠
                                list.Add(new ResourceEditUtility.SmapsInfo[] { leftInfo, null });
                                MoveNext(list1, ref left, ref leftInfo);
                            }
                            else if (leftInfo.vm_start >= rightInfo.vm_end) {//左下、右上无重叠
                                list.Add(new ResourceEditUtility.SmapsInfo[] { null, rightInfo });
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                            else {//交叉
                                list.Add(new ResourceEditUtility.SmapsInfo[] { leftInfo, null });
                                MoveNext(list1, ref left, ref leftInfo);
                                list.Add(new ResourceEditUtility.SmapsInfo[] { null, rightInfo });
                                MoveNext(list2, ref right, ref rightInfo);
                            }
                        }
                        int ix = left + right;
                        if (ix % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("match maps ...", ix, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(list);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private void MoveNext(List<ResourceEditUtility.SmapsInfo> list, ref int index, ref ResourceEditUtility.SmapsInfo info)
        {
            if (index < list.Count - 1) {
                ++index;
                info = list[index];
            }
            else {
                info = null;
            }
        }
    }
    internal class CalcMatchedSmapsDiffExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 2) {
                var list = operands[0].As<List<ResourceEditUtility.SmapsInfo[]>>();
                int index = operands[1].GetInt();
                if (null != list && index >= 0) {
                    int ct = list.Count;
                    int delta = 1;
                    if (ct > 100000)
                        delta = 1000;
                    else if (ct > 10000)
                        delta = 100;
                    else if (ct > 1000)
                        delta = 10;
                    else
                        delta = 1;
                    var results = new List<ResourceEditUtility.SmapsInfo>();
                    for (int i = 0; i < ct; ++i) {
                        var infos = list[i];
                        if (index < infos.Length) {
                            var info = infos[index];
                            if (null != info) {
                                bool match = true;
                                for (int ix = 0; ix < infos.Length; ++ix) {
                                    if (ix != index && null != infos[ix]) {
                                        match = false;
                                        break;
                                    }
                                }
                                if (match) {
                                    results.Add(info);
                                }
                            }
                        }
                        if (i % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("calc matched smaps diff ...", i, ct)) {
                            break;
                        }
                    }
                    r = BoxedValue.FromObject(results);
                }
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
        private void MoveNext(List<ResourceEditUtility.MapsInfo> list, ref int index, ref ResourceEditUtility.MapsInfo info)
        {
            if (index < list.Count - 1) {
                ++index;
                info = list[index];
            }
            else {
                info = null;
            }
        }
    }
    internal class LoadAddrsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var file = operands[0].AsString;
                bool isHex = true;
                Regex regex = null;
                if (operands.Count >= 2) {
                    isHex = operands[1].GetBool();
                }
                if (operands.Count >= 3) {
                    var str = operands[2].AsString;
                    if (!string.IsNullOrEmpty(str)) {
                        regex = new Regex(str, RegexOptions.Compiled);
                    }
                }
                var list = new List<ulong>();
                var lines = File.ReadAllLines(file);
                int ct = lines.Length;
                int delta = 1;
                if (ct > 100000)
                    delta = 1000;
                else if (ct > 10000)
                    delta = 100;
                else if (ct > 1000)
                    delta = 10;
                else
                    delta = 1;
                for (int ix = 0; ix < ct; ++ix) {
                    var addrStr = lines[ix];
                    if (null != regex) {
                        var m = regex.Match(addrStr);
                        if (m.Success) {
                            addrStr = m.Groups[0].Value;
                        }
                    }
                    ulong addr;
                    if (ulong.TryParse(addrStr, System.Globalization.NumberStyles.AllowHexSpecifier, null, out addr)) {
                        list.Add(addr);
                    }
                    if (ix % delta == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("load addrs ...", ix, ct)) {
                        break;
                    }
                }
                list.Sort();
                r = BoxedValue.FromObject(list);
            }
            EditorUtility.ClearProgressBar();
            return r;
        }
    }
    internal class EscapeUrlExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var txt = operands[0].AsString;
                var space = string.Empty;
                if (operands.Count >= 2) {
                    var str = operands[1].AsString;
                    if (!string.IsNullOrEmpty(str)) {
                        space = str;
                    }
                }
                if (string.IsNullOrEmpty(txt)) {
                    if (!string.IsNullOrEmpty(space))
                        txt = txt.Replace(" ", space);
                    r = txt;
                }
                else {
                    if (!string.IsNullOrEmpty(space))
                        txt = txt.Replace(space, " ");
                    r = UnityEngine.Networking.UnityWebRequest.EscapeURL(txt);
                }
            }
            return r;
        }
    }
    internal class UnEscapeUrlExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var txt = operands[0].AsString;
                var space = string.Empty;
                if (operands.Count >= 2) {
                    var str = operands[1].AsString;
                    if (!string.IsNullOrEmpty(str)) {
                        space = str;
                    }
                }
                if (string.IsNullOrEmpty(txt)) {
                    if (!string.IsNullOrEmpty(space))
                        txt = txt.Replace(space, " ");
                    r = txt;
                }
                else {
                    txt = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(txt);
                    if (!string.IsNullOrEmpty(space))
                        txt = txt.Replace(space, " ");
                    r = txt;
                }
            }
            return r;
        }
    }
    internal class ParseUrlArgsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var txt = operands[0].AsString;
                var space = string.Empty;
                if (operands.Count >= 2) {
                    var str = operands[1].AsString;
                    if (!string.IsNullOrEmpty(str)) {
                        space = str;
                    }
                }
                var kvSep = s_KeyValueSeperator;
                if (operands.Count >= 3) {
                    var str = operands[2].AsString;
                    if (!string.IsNullOrEmpty(str)) {
                        kvSep = str;
                    }
                    else if (str == string.Empty) {
                        kvSep = string.Empty;
                    }
                    else {
                        kvSep = s_KeyValueSeperator;
                    }
                }
                var sep = s_BuglySeperators;
                if (operands.Count >= 4) {
                    var list = new List<string>();
                    for (int i = 3; i < operands.Count; ++i) {
                        var str = operands[i].AsString;
                        if (!string.IsNullOrEmpty(str)) {
                            list.Add(str);
                        }
                    }
                    sep = list.ToArray();
                }
                var newTxt = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(txt);
                if (!string.IsNullOrEmpty(space))
                    newTxt = newTxt.Replace(space, " ");
                var fields = newTxt.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (!string.IsNullOrEmpty(kvSep)) {
                    var hash = new Dictionary<string, string>();
                    foreach (var field in fields) {
                        string[] kv = field.Split(new string[] { kvSep }, StringSplitOptions.None);
                        if (kv.Length == 1) {
                            var key = kv[0].Trim();
                            if (!string.IsNullOrEmpty(key) && !hash.ContainsKey(key)) {
                                hash.Add(key, string.Empty);
                            }
                        }
                        else {
                            var key = kv[0].Trim();
                            var val = kv[1].Trim();
                            if (!string.IsNullOrEmpty(key) && !hash.ContainsKey(key)) {
                                hash.Add(key, val);
                            }
                        }
                    }
                    r = BoxedValue.FromObject(hash);
                }
                else {
                    r = BoxedValue.FromObject(fields);
                }
            }
            return r;
        }
        private static string s_KeyValueSeperator = "=";
        private static string[] s_BuglySeperators = new string[] { ";" };
    }
    internal class ParseBuglyInfoExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            var r = BoxedValue.NullObject;
            if (operands.Count >= 1) {
                var txt = operands[0].AsString;
                var space = string.Empty;
                if (operands.Count >= 2) {
                    var str = operands[1].AsString;
                    if (!string.IsNullOrEmpty(str)) {
                        space = str;
                    }
                }
                var sep = s_BuglySeperators;
                if (operands.Count >= 3) {
                    var list = new List<string>();
                    for (int i = 2; i < operands.Count; ++i) {
                        var str = operands[i].AsString;
                        if (!string.IsNullOrEmpty(str)) {
                            list.Add(str);
                        }
                    }
                    sep = list.ToArray();
                }
                var newTxt = UnityEngine.Networking.UnityWebRequest.UnEscapeURL(txt);
                if (!string.IsNullOrEmpty(space))
                    newTxt = newTxt.Replace(space, " ");
                var fields = newTxt.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                r = BoxedValue.FromObject(fields);
            }
            return r;
        }
        private static string[] s_BuglySeperators = new string[] { "\n" };
    }
    internal class IntHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<int>>();
                var vobj = operands[1];
                if (null != hash && !vobj.IsNullObject) {
                    var v = vobj.GetInt();
                    r = hash.Count == 0 || hash.Contains(v);
                }
            }
            return r;
        }
    }
    internal class UintHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<uint>>();
                var vobj = operands[1];
                if (null != hash && !vobj.IsNullObject) {
                    var v = vobj.GetUInt();
                    r = hash.Count == 0 || hash.Contains(v);
                }
            }
            return r;
        }
    }
    internal class LongHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<long>>();
                var vobj = operands[1];
                if (null != hash && !vobj.IsNullObject) {
                    var v = vobj.GetLong();
                    r = hash.Count == 0 || hash.Contains(v);
                }
            }
            return r;
        }
    }
    internal class UlongHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<ulong>>();
                var vobj = operands[1];
                if (null != hash && !vobj.IsNullObject) {
                    var v = vobj.GetULong();
                    r = hash.Count == 0 || hash.Contains(v);
                }
            }
            return r;
        }
    }
    internal class FloatHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<float>>();
                var vobj = operands[1];
                if (null != hash && !vobj.IsNullObject) {
                    var v = vobj.GetFloat();
                    r = hash.Count == 0 || hash.Contains(v);
                }
            }
            return r;
        }
    }
    internal class DoubleHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<double>>();
                var vobj = operands[1];
                if (null != hash && !vobj.IsNullObject) {
                    var v = vobj.GetDouble();
                    r = hash.Count == 0 || hash.Contains(v);
                }
            }
            return r;
        }
    }
    internal class StringHashContainsExp : SimpleExpressionBase
    {
        protected override BoxedValue OnCalc(IList<BoxedValue> operands)
        {
            bool r = false;
            if (operands.Count >= 2) {
                var hash = operands[0].As<HashSet<string>>();
                var vstr = operands[1].AsString;
                if (null != hash && null != vstr) {
                    r = hash.Count == 0 || hash.Contains(vstr);
                }
            }
            return r;
        }
    }
}
#endregion

#region MeshArea
internal static class MeshAreaHelper
{
    internal static float Area(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 dirA = b - a;
        Vector3 dirB = c - a;
        return Vector3.Cross(dirA, dirB).magnitude / 2;
    }
    internal static float Area(Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 dirA = b - a;
        Vector2 dirB = c - a;
        return Vector3.Cross(new Vector3(dirA.x, dirA.y, 0), new Vector3(dirB.x, dirB.y, 0)).magnitude / 2;
    }

    internal static List<Vector2> TextureSize(GameObject go)
    {
        List<Vector2> textureSize = new List<Vector2>();

        Renderer r = go.GetComponent<Renderer>();
        if (r != null) {
            Material[] materials = r.sharedMaterials;
            if (materials != null) {
                for (int i = 0; i < materials.Length; i++) {
                    var m = materials[i];
                    if (null != m) {
                        var tex = m.mainTexture;
                        if (null != tex) {
                            textureSize.Add(new Vector2(tex.width, tex.height));
                        }
                        else {
                            textureSize.Add(Vector2.one);
                        }
                    }
                    else {
                        textureSize.Add(Vector2.one);
                    }
                }
            }
        }
        return textureSize;
    }
    internal static List<Vector2> GetLightMapSize(int index)
    {
        List<Vector2> lightMapSize = new List<Vector2>(3);
        lightMapSize[0] = Vector2.one;
        lightMapSize[1] = Vector2.one;
        lightMapSize[2] = Vector2.one;

        LightmapData[] datas = LightmapSettings.lightmaps;
        if (datas != null && datas[index] != null) {
            if (datas[index].lightmapColor != null) {
                var tex = datas[index].lightmapColor;
                lightMapSize[0] = new Vector2(tex.width, tex.height);
            }
            if (datas[index].lightmapDir != null) {
                var tex = datas[index].lightmapDir;
                lightMapSize[1] = new Vector2(tex.width, tex.height);
            }
            if (datas[index].shadowMask != null) {
                var tex = datas[index].shadowMask;
                lightMapSize[2] = new Vector2(tex.width, tex.height);
            }
        }

        return lightMapSize;
    }
    internal static List<float> CalculateMeshAreaWorld(GameObject go)
    {
        List<float> subMeshArea = new List<float>();
        Mesh mesh = GetMesh(go);
        if (mesh != null) {
            Vector3[] vertData = mesh.vertices;
            int submeshCount = mesh.subMeshCount;
            for (int i = 0; i < submeshCount; i++) {
                float subMeshArea_i = 0;
                int[] subIndex = mesh.GetIndices(i);
                for (int j = 0; j < subIndex.Length / 3; j++) {
                    int indexA = subIndex[j * 3 + 0];
                    int indexB = subIndex[j * 3 + 1];
                    int indexC = subIndex[j * 3 + 2];

                    Vector3 vertexA = go.transform.TransformPoint(vertData[indexA]);
                    Vector3 vertexB = go.transform.TransformPoint(vertData[indexB]);
                    Vector3 vertexC = go.transform.TransformPoint(vertData[indexC]);

                    subMeshArea_i += Area(vertexA, vertexB, vertexC);
                }
                subMeshArea.Add(subMeshArea_i);
            }
        }
        return subMeshArea;
    }
    internal static List<float> CalculateMeshUVAreaObject(GameObject go)
    {
        List<float> subMeshArea = new List<float>();
        Mesh mesh = GetMesh(go);
        if (mesh != null) {
            Vector2[] uvData = mesh.uv;
            int submeshCount = mesh.subMeshCount;
            for (int i = 0; i < submeshCount; i++) {
                float subMeshArea_i = 0;
                int[] subIndex = mesh.GetIndices(i);
                for (int j = 0; j < subIndex.Length / 3; j++) {
                    int indexA = subIndex[j * 3 + 0];
                    int indexB = subIndex[j * 3 + 1];
                    int indexC = subIndex[j * 3 + 2];

                    Vector2 vertexA = uvData[indexA];
                    Vector2 vertexB = uvData[indexB];
                    Vector2 vertexC = uvData[indexC];

                    subMeshArea_i += Area(vertexA, vertexB, vertexC);
                }
                subMeshArea.Add(subMeshArea_i);
            }
        }
        return subMeshArea;
    }
    internal static List<float> CalculateMeshUVAreaTile(GameObject go)
    {
        List<float> subMeshArea = new List<float>();
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return subMeshArea;
        Material[] materials = renderer.sharedMaterials;
        if (materials == null) return subMeshArea;
        Mesh mesh = GetMesh(go);
        if (mesh != null) {
            Vector2[] uvData = mesh.uv;
            int submeshCount = mesh.subMeshCount;
            Material m = null;
            for (int i = 0; i < submeshCount; i++) {
                if (i < materials.Length)
                    m = materials[i];
                float subMeshArea_i = 0;
                if (null != m) {
                    int[] subIndex = mesh.GetIndices(i);
                    for (int j = 0; j < subIndex.Length / 3; j++) {
                        int indexA = subIndex[j * 3 + 0];
                        int indexB = subIndex[j * 3 + 1];
                        int indexC = subIndex[j * 3 + 2];

                        Vector2 vertexA = new Vector2(uvData[indexA].x * m.mainTextureScale.x, uvData[indexA].y * m.mainTextureScale.y);
                        Vector2 vertexB = new Vector2(uvData[indexB].x * m.mainTextureScale.x, uvData[indexB].y * m.mainTextureScale.y); //uvData[indexB];
                        Vector2 vertexC = new Vector2(uvData[indexC].x * m.mainTextureScale.x, uvData[indexC].y * m.mainTextureScale.y); //uvData[indexC];

                        subMeshArea_i += Area(vertexA, vertexB, vertexC);
                    }
                }
                subMeshArea.Add(subMeshArea_i);
            }
        }
        return subMeshArea;
    }
    internal static List<float> CalculateMeshLightmapUVArea(GameObject go)
    {
        List<float> subMeshArea = new List<float>();
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) { return subMeshArea; }
        Vector4 lightMapScaleOffset = renderer.lightmapScaleOffset;
        Mesh mesh = GetMesh(go);
        if (mesh != null) {
            Vector2[] uvData = mesh.uv2;
            int submeshCount = mesh.subMeshCount;
            for (int i = 0; i < submeshCount; i++) {
                float subMeshArea_i = 0;
                int[] subIndex = mesh.GetIndices(i);
                for (int j = 0; j < subIndex.Length / 3; j++) {
                    int indexA = subIndex[j * 3 + 0];
                    int indexB = subIndex[j * 3 + 1];
                    int indexC = subIndex[j * 3 + 2];

                    Vector2 vertexA = new Vector2(lightMapScaleOffset.x * uvData[indexA].x, lightMapScaleOffset.y * uvData[indexA].y);
                    Vector2 vertexB = new Vector2(lightMapScaleOffset.x * uvData[indexB].x, lightMapScaleOffset.y * uvData[indexB].y);
                    Vector2 vertexC = new Vector2(lightMapScaleOffset.x * uvData[indexC].x, lightMapScaleOffset.y * uvData[indexC].y);

                    subMeshArea_i += Area(vertexA, vertexB, vertexC);
                }
                subMeshArea.Add(subMeshArea_i);
            }
        }
        return subMeshArea;
    }

    private static Mesh GetMesh(GameObject go)
    {
        var mf = go.GetComponent<MeshFilter>();
        if (mf != null) {
            return mf.sharedMesh;
        }
        else {
            var skin = go.GetComponent<SkinnedMeshRenderer>();
            if (null != skin) {
                return skin.sharedMesh;
            }
            else {
                return null;
            }
        }
    }
}
#endregion

#region MemoryProfiler
class ShortestPathToRootObjectFinder
{
    public ShortestPathToRootObjectFinder(CachedSnapshot snapshot)
    {
        _snapshot = snapshot;
        _refbydict = new Dictionary<long, HashSet<CachedSnapshot.SourceIndex>>((int)(snapshot.SortedManagedObjects.Count + snapshot.SortedNativeObjects.Count));

        long ct = snapshot.CrawledData.Connections.Count;
        for (long i = 0; i < ct; ++i) {
            var c = snapshot.CrawledData.Connections[i];
            var to = c.IndexTo;
            var from = c.IndexFrom;
            if (to.Id == CachedSnapshot.SourceIndex.SourceId.ManagedObject) {
                long objIndex = snapshot.ManagedObjectIndexToUnifiedObjectIndex(to.Index);
                TryAddRefBy(objIndex, new CachedSnapshot.SourceIndex((CachedSnapshot.SourceIndex.SourceId)0xff, i));
            }
            else if (to.Id == CachedSnapshot.SourceIndex.SourceId.NativeObject) {
                long objIndex = snapshot.NativeObjectIndexToUnifiedObjectIndex(to.Index);
                TryAddRefBy(objIndex, new CachedSnapshot.SourceIndex((CachedSnapshot.SourceIndex.SourceId)0xff, i));
            }
            if (i % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("build reference using dictionary", i, ct)) {
                break;
            }
        }
        //从cache里检索一遍
        ct = snapshot.Connections.Count;
        int ix = 0;
        foreach (var pair in snapshot.Connections.ReferencedBy) {
            var to = pair.Key;
            foreach (var from in pair.Value) {
                if (to.Id == CachedSnapshot.SourceIndex.SourceId.ManagedObject) {
                    long objIndex = snapshot.ManagedObjectIndexToUnifiedObjectIndex(to.Index);
                    TryAddRefBy(objIndex, from);
                }
                else if (to.Id == CachedSnapshot.SourceIndex.SourceId.NativeObject) {
                    long objIndex = snapshot.NativeObjectIndexToUnifiedObjectIndex(to.Index);
                    TryAddRefBy(objIndex, from);
                }
                if (ix % 1000 == 0 && ResourceProcessor.Instance.DisplayCancelableProgressBar("build reference using dictionary", ix, ct)) {
                    goto L_Exit;
                }
                ++ix;
            }
        }
    L_Exit:
        EditorUtility.ClearProgressBar();
    }

    public ObjectData[] FindFor(ObjectData data)
    {
        var seen = new HashSet<ObjectData>();
        var queue = new Queue<List<ObjectData>>();
        queue.Enqueue(new List<ObjectData> { data });

        ObjectData[] ret = null;
        while (queue.Any()) {
            var pop = queue.Dequeue();
            var obj = pop.Last();
            var subObj = obj.displayObject;

            string reason;
            if (IsRoot(obj, out reason)) {
                ret = pop.ToArray();
                break;
            }

            if (_refbydict.TryGetValue(subObj.GetUnifiedObjectIndex(_snapshot), out var hash)) {
                HashSet<ObjectData> refBys = new HashSet<ObjectData>(hash.Count);
                foreach (var sindex in hash) {
                    if ((int)sindex.Id == 0xff) {
                        var c = _snapshot.CrawledData.Connections[sindex.Index];
                        ObjectData objData = ObjectConnection.GetManagedReferenceSource(_snapshot, c);
                        refBys.Add(objData);
                    }
                    else {
                        refBys.Add(ObjectData.FromSourceLink(_snapshot, sindex));
                    }
                }
                foreach (var next in refBys) {
                    if (seen.Contains(next))
                        continue;
                    seen.Add(next);
                    var dupe = new List<ObjectData>(pop) { next };
                    queue.Enqueue(dupe);
                }
            }
            if (ResourceProcessor.Instance.DisplayCancelableProgressBar("find shortest path to root", queue.Count, 10000)) {
                ret = pop.ToArray();
                break;
            }
        }
        EditorUtility.ClearProgressBar();
        return ret;
    }
    public HashSet<ObjectData> GetRefByObjs(ObjectData data)
    {
        long index = data.GetUnifiedObjectIndex(_snapshot);
        _refbydict.TryGetValue(index, out var hash);
        HashSet<ObjectData> objs = new HashSet<ObjectData>(hash.Count);
        foreach (var sindex in hash) {
            if ((int)sindex.Id == 0xff) {
                var c = _snapshot.CrawledData.Connections[sindex.Index];
                ObjectData objData = ObjectConnection.GetManagedReferenceSource(_snapshot, c);
                objs.Add(objData);
            }
            else {
                objs.Add(ObjectData.FromSourceLink(_snapshot, sindex));
            }
        }
        return objs;
    }
    public ObjectData[] GetRefObjs(ObjectData data)
    {
        List<ObjectData> objs = new List<ObjectData>();
        ObjectConnection.GetAllReferencedObjects(_snapshot, data.GetSourceLink(_snapshot), ref objs);
        return objs.ToArray();
    }
    public bool IsRoot(ObjectData data, out string reason)
    {
        reason = null;
        if (data.IsValid) {
            bool isStatic = false;
            if (data.IsField()) {
                if (data.fieldIndex >= 0 && data.fieldIndex < _snapshot.FieldDescriptions.Count) {
                    isStatic = _snapshot.FieldDescriptions.IsStatic[data.fieldIndex] != 0;
                }
            }
            if (isStatic || data.dataType == ObjectDataType.Unknown || data.dataType == ObjectDataType.Type) {
                reason = "Static fields are global variables. Anything they reference will not be unloaded.";
                return true;
            }
            if (data.isManaged)
                return false;

            var classID = _snapshot.NativeObjects.NativeTypeArrayIndex[data.nativeObjectIndex];
            var flags = (int)_snapshot.NativeObjects.Flags[data.nativeObjectIndex];
            var hideFlags = _snapshot.NativeObjects.HideFlags[data.nativeObjectIndex];

            if ((flags & (int)Unity.MemoryProfilerExtension.Editor.Format.ObjectFlags.IsPersistent) != 0)
                return false;
            if ((flags & (int)Unity.MemoryProfilerExtension.Editor.Format.ObjectFlags.IsManager) != 0) {
                reason = "this is an internal unity'manager' style object, which is a global object that will never be unloaded";
                return true;
            }
            if ((flags & (int)Unity.MemoryProfilerExtension.Editor.Format.ObjectFlags.IsDontDestroyOnLoad) != 0) {
                reason = "DontDestroyOnLoad() was called on this object, so it will never be unloaded";
                return true;
            }

            if ((hideFlags & HideFlags.DontUnloadUnusedAsset) != 0) {
                reason = "the DontUnloadUnusedAsset hideflag is set on this object. Unity's builtin resources set this flag. Users can also set the flag themselves";
                return true;
            }

            if (IsComponent(classID)) {
                reason = "this is a component, living on a gameobject, that is either part of the loaded scene, or was generated by script. It will be unloaded on next scene load.";
                return true;
            }
            if (IsGameObject(classID)) {
                reason = "this is a gameobject, that is either part of the loaded scene, or was generated by script. It will be unloaded on next scene load if nobody is referencing it";
                return true;
            }
            if (IsAssetBundle(classID)) {
                reason = "this object is an assetbundle, which is never unloaded automatically, but only through an explicit .Unload() call.";
                return true;
            }
        }
        reason = "This object might be a root, but the memory profiler UI does not yet understand why";
        return false;
    }

    private bool IsGameObject(int classID)
    {
        return _snapshot.NativeTypes.TypeName[classID] == "GameObject";
    }

    private bool IsAssetBundle(int classID)
    {
        return _snapshot.NativeTypes.TypeName[classID] == "AssetBundle";
    }

    private bool IsComponent(int classID)
    {
        var typeName = _snapshot.NativeTypes.TypeName[classID];
        var nativeBaseTypeArrayIndex = _snapshot.NativeTypes.NativeBaseTypeArrayIndex[classID];

        if (typeName == "Component")
            return true;

        var baseClassID = nativeBaseTypeArrayIndex;

        return baseClassID != -1 && IsComponent(baseClassID);
    }

    private void TryAddRefBy(long objIndex, CachedSnapshot.SourceIndex index)
    {
        if (objIndex >= 0) {
            HashSet<CachedSnapshot.SourceIndex> hash;
            if (!_refbydict.TryGetValue(objIndex, out hash)) {
                hash = new HashSet<CachedSnapshot.SourceIndex>(s_InitRefByNumPerObj);
                _refbydict.Add(objIndex, hash);
            }
            if (hash.Count < s_MaxRefByNumPerObj) {
                hash.Add(index);
            }
        }
    }

    private readonly CachedSnapshot _snapshot;
    private Dictionary<long, HashSet<CachedSnapshot.SourceIndex>> _refbydict;

    internal static int s_InitRefByNumPerObj = 8;
    internal static int s_MaxRefByNumPerObj = 256;
}
#endregion