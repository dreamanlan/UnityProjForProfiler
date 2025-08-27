using UnityEngine;
//using UnityEngine.UI;
using UnityEditor;
//using UnityEditor.UI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.MemoryProfiler;
using UnityEditorInternal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.IO;
using System.Linq;
#if UNITY_2018_3_OR_NEWER
using UnityEngine.Profiling;
using UnityEditor.Profiling;
#endif
using Unity.MemoryProfilerExtension.Editor;
using Unity.Profiling;
using StoryScript;
using StoryScript.DslExpression;
using Unity.Profiling.Editor;
using Unity.Collections.LowLevel.Unsafe;

internal sealed class ResourceEditWindow : EditorWindow
{
    [MenuItem("Dsl资源工具/编辑器命令", false, 199)]
    internal static void InitWindowAndShowCommand()
    {
        ResourceEditWindow window = (ResourceEditWindow) EditorWindow.GetWindow(typeof(ResourceEditWindow));
        window.Init();
        window.Show();
        EditorUtility.ClearProgressBar();
        window.ShowCommand();
    }
    [MenuItem("Dsl资源工具/资源处理", false, 200)]
    internal static void InitWindow()
    {
        ResourceEditWindow window = (ResourceEditWindow)EditorWindow.GetWindow(typeof(ResourceEditWindow));
        window.Init();
        window.Show();
        EditorUtility.ClearProgressBar();
    }

    internal void QueueProcessBegin()
    {
        m_BatchActions.Enqueue(w => w.Focus());
        m_BatchActions.Enqueue(w => w.ClearProcessedAssets());
    }
    internal void QueueProcess(string dslPath, string resPath, int index, int count)
    {
        m_BatchActions.Enqueue(w => BatchAction1(dslPath, resPath));
        m_BatchActions.Enqueue(w => BatchAction2(index, count));
        m_BatchActions.Enqueue(w => BatchAction3(index, count));
    }
    internal void QueueProcessEnd()
    {
        m_BatchActions.Enqueue(w => SpecificBatchAction());
        m_BatchActions.Enqueue(w => w.Focus());
    }
    internal void QueueProcess(string dslPath, IDictionary<string, string> args)
    {
        m_BatchActions.Enqueue(w => RedirectAction(dslPath, args));
    }

    private void Init()
    {
        s_CurrentWindow = this;
    }
    private void ShowCommand()
    {
        DeferAction(obj => { ResourceCommandWindow.InitWindow(obj, string.Empty, BoxedValue.NullObject, BoxedValue.NullObject); });
    }

    private void OnGUI()
    {
        bool oldRichText = GUI.skin.button.richText;
        GUI.skin.button.richText = true;

        EditorGUILayout.BeginHorizontal();
        ResourceEditUtility.EnableSaveAndReimport = EditorGUILayout.Toggle("允许SaveAndReimport", ResourceEditUtility.EnableSaveAndReimport);
        ResourceEditUtility.UseSpecificSettingDB = EditorGUILayout.Toggle("跳过特殊设置DB数据里的资源", ResourceEditUtility.UseSpecificSettingDB);
        ResourceEditUtility.ForceSaveAndReimport = EditorGUILayout.Toggle("强制SaveAndReimport", ResourceEditUtility.ForceSaveAndReimport);
        ResourceEditUtility.SaveAfterProcess = EditorGUILayout.Toggle("处理后保存", ResourceEditUtility.SaveAfterProcess);
        ResourceEditUtility.SaveResultWithXrefs = EditorGUILayout.Toggle("结果保存包含引用数据", ResourceEditUtility.SaveResultWithXrefs);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("清理缓存", EditorStyles.toolbarButton, GUILayout.Width(60))) {
            DeferAction(obj => { ResourceProcessor.Instance.ClearCaches(); });
        }
        EditorGUILayout.LabelField("资源依赖:", EditorStyles.toolbarTextField, GUILayout.Width(60));
        if (GUILayout.Button("分析", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.AnalyseAssets(); });
        }
        if (GUILayout.Button("保存", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.SaveDependencies(); });
        }
        if (GUILayout.Button("加载", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.LoadDependencies(); });
        }
        EditorGUILayout.LabelField("内存:", EditorStyles.toolbarTextField, GUILayout.Width(40));
        if (GUILayout.Button("加载/刷新", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.LoadMemoryInfo(); });
        }
        if (GUILayout.Button("批量转换", EditorStyles.toolbarButton)) {
            DeferAction(obj => { BatchLoadWindow.InitWindow(); });
        }
        EditorGUILayout.LabelField("耗时:", EditorStyles.toolbarTextField, GUILayout.Width(40));
        if (GUILayout.Button("清空", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.ClearInstrumentInfo(); });
        }
        if (GUILayout.Button("记录", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.RecordInstrument(); });
        }
        if (GUILayout.Button("保存", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.SaveInstrumentInfo(); });
        }
        if (GUILayout.Button("加载", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.LoadInstrumentInfo(); });
        }
        if (GUILayout.Button("uTraceCsv", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.LoadUTraceCsv(); });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新列表", EditorStyles.toolbarButton, GUILayout.Width(60))) {
            m_Menus = null;
            m_Files = null;
            m_SelectedIndex = 0;
            DeferAction(obj => { ResourceProcessor.Instance.ClearDsl(); obj.CopyCollectResult(); });
        }

        if (null == m_Menus || m_Menus.Length <= 0) {
            SortedDictionary<string, string> dslFiles = new SortedDictionary<string, string>();
            dslFiles.Add("0.Empty", string.Empty);
            var files = Directory.GetFiles("./FindDsl", "*.dsl", SearchOption.AllDirectories);
            foreach (var file in files) {
                string name, desc;
                if (ResourceProcessor.ReadMenuAndDescription(file, out name, out desc)) {
                    try {
                        dslFiles.Add(string.Format("1.Find/{0}", name), file);
                    }
                    catch (Exception ex) {
                        Debug.LogFormat("Add 'Find' menu {0} desc {1} exception:{2}", name, desc, ex.Message);
                    }
                }
            }
            files = Directory.GetFiles("./ProcessDsl", "*.dsl", SearchOption.AllDirectories);
            foreach (var file in files) {
                string name, desc;
                if (ResourceProcessor.ReadMenuAndDescription(file, out name, out desc)) {
                    try {
                        dslFiles.Add(string.Format("2.Process/{0}", name), file);
                    }
                    catch (Exception ex) {
                        Debug.LogFormat("Add 'Process' menu {0} desc {1} exception:{2}", name, desc, ex.Message);
                    }
                }
            }
            m_Menus = dslFiles.Keys.ToArray();
            m_Files = dslFiles.Values.ToArray();
        }
        int newIndex = EditorGUILayout.Popup(m_SelectedIndex, m_Menus, EditorStyles.toolbarPopup, GUILayout.Width(320));
        if (newIndex != m_SelectedIndex) {
            m_SelectedIndex = newIndex;
            string file = m_Files[m_SelectedIndex];
            if (!string.IsNullOrEmpty(file)) {
                DeferAction(obj => { ResourceProcessor.Instance.SelectDsl(file); obj.CopyConfig(); obj.CopyCollectResult(); });
            }
            else {
                DeferAction(obj => { ResourceProcessor.Instance.ClearDsl(); obj.CopyCollectResult(); });
            }
        }

        if (GUILayout.Button("收集资源", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.Collect(); obj.CopyCollectResult(); });
        }
        if (GUILayout.Button("处理选中资源", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.Process(); });
        }
        if (GUILayout.Button("编辑器同步选中", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.SelectAssetsOrObjects(); });
        }
        if (GUILayout.Button("创建场景", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.GenerateScene(); });
        }
        GUILayout.Space(20);
        if (GUILayout.Button("保存", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.SaveResult(obj.m_ItemList, obj.m_GroupList); });
        }
        if (GUILayout.Button("加载", EditorStyles.toolbarButton)) {
            DeferAction(obj => { ResourceProcessor.Instance.LoadResult(); obj.CopyCollectResult(); });
        }
        if (GUILayout.Button("拷贝", EditorStyles.toolbarButton)) {
            DeferAction(obj => { obj.CopyToClipboard(); });
        }
        if (GUILayout.Button("命令", EditorStyles.toolbarButton)) {
            ShowCommand();
        }
        GUILayout.Space(20);
        if (GUILayout.Button("批处理", EditorStyles.toolbarButton)) {
            DeferAction(obj => { BatchResourceProcessWindow.InitWindow(obj); });
        }
        EditorGUILayout.EndHorizontal();

        var paramNames = ResourceProcessor.Instance.ParamNames;
        var paramInfos = ResourceProcessor.Instance.Params;
        if (paramNames.Count > 0) {
            foreach (var name in paramNames) {
                ResourceEditUtility.ParamInfo info;
                if (paramInfos.TryGetValue(name, out info)) {
                    EditorGUILayout.BeginHorizontal();
                    if (info.Type == typeof(UnityEngine.UIElements.Label)) {
                        EditorGUILayout.LabelField(new GUIContent(info.Caption, info.Tooltip));
                    }
                    else {
                        EditorGUILayout.LabelField(new GUIContent(info.Caption, info.Tooltip), GUILayout.Width(160));
                    }
                    string oldVal = info.StringValue;
                    string newVal = oldVal;
                    if (!string.IsNullOrEmpty(info.Script)) {
                        double curTime = EditorApplication.timeSinceStartup;
                        if (info.NextRunScriptTime <= curTime) {
                            var r = ResourceProcessor.Instance.CallScript(null, info.Script, BoxedValue.FromObject(info));
                            if (!r.IsNullObject) {
                                info.NextRunScriptTime = curTime + r.GetDouble();
                            }
                        }
                    }
                    if (info.OptionStyle == "asset_bundle_name") {
                        if (null != ResourceProcessor.Instance.AssetBundleInfo && info.OptionNames.Count != ResourceProcessor.Instance.AssetBundleInfo.assetBuildItems.Length) {
                            info.PopupOptionNames = null;
                            info.OptionNames.Clear();
                            info.Options.Clear();
                            foreach (var bundleInfo in ResourceProcessor.Instance.AssetBundleInfo.assetBuildItems) {
                                info.OptionNames.Add(bundleInfo.assetBundleName);
                                info.Options.Add(bundleInfo.assetBundleName, bundleInfo.assetBundleName);
                            }
                        }
                        DoPopup(info, oldVal, ref newVal);
                    }
                    else if (info.OptionStyle == "excel_sheets") {
                        DoPopup(info, oldVal, ref newVal);
                    }
                    else if (info.OptionStyle == "managed_memory_group") {
                        if (info.OptionNames.Count != ResourceProcessor.Instance.ClassifiedManagedMemoryInfos.Count) {
                            info.PopupOptionNames = null;
                            info.OptionNames.Clear();
                            info.Options.Clear();
                            foreach (var pair in ResourceProcessor.Instance.ClassifiedManagedMemoryInfos) {
                                info.OptionNames.Add(pair.Key);
                                info.Options.Add(pair.Key, pair.Key);
                            }
                        }
                        DoPopup(info, oldVal, ref newVal);
                    }
                    else if (info.OptionStyle == "native_memory_group") {
                        if (info.OptionNames.Count != ResourceProcessor.Instance.ClassifiedNativeMemoryInfos.Count) {
                            info.PopupOptionNames = null;
                            info.OptionNames.Clear();
                            info.Options.Clear();
                            foreach (var pair in ResourceProcessor.Instance.ClassifiedNativeMemoryInfos) {
                                info.OptionNames.Add(pair.Key);
                                info.Options.Add(pair.Key, pair.Key);
                            }
                        }
                        DoPopup(info, oldVal, ref newVal);
                    }
                    else if (info.Options.Count > 0) {
                        if (info.OptionStyle == "toggle") {
                            DoToggle(info, oldVal, ref newVal);
                        }
                        else if (info.OptionStyle == "multiple") {
                            DoMultiple(info, oldVal, ref newVal);
                        }
                        else {
                            DoPopup(info, oldVal, ref newVal);
                        }
                    }
                    else if (info.Type == typeof(bool)) {
                        bool v = EditorGUILayout.Toggle((bool)info.Value);
                        newVal = v ? "true" : "false";
                    }
                    else if (info.Type == typeof(int)) {
                        if (!info.MinValue.IsNullObject && !info.MaxValue.IsNullObject) {
                            int min = (int)info.MinValue;
                            int max = (int)info.MaxValue;
                            int v = EditorGUILayout.IntSlider((int)info.Value, min, max, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                        else {
                            int v = EditorGUILayout.IntField((int)info.Value, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                    }
                    else if (info.Type == typeof(uint)) {
                        if (!info.MinValue.IsNullObject && !info.MaxValue.IsNullObject) {
                            int min = (int)info.MinValue;
                            int max = (int)info.MaxValue;
                            int v = EditorGUILayout.IntSlider((int)info.Value, min, max, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                        else {
                            uint v = (uint)EditorGUILayout.IntField((int)(uint)info.Value, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                    }
                    else if (info.Type == typeof(long)) {
                        if (!info.MinValue.IsNullObject && !info.MaxValue.IsNullObject) {
                            int min = (int)info.MinValue;
                            int max = (int)info.MaxValue;
                            int v = EditorGUILayout.IntSlider((int)(long)info.Value, min, max, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                        else {
                            long v = EditorGUILayout.LongField((long)info.Value, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                    }
                    else if (info.Type == typeof(ulong)) {
                        if (!info.MinValue.IsNullObject && !info.MaxValue.IsNullObject) {
                            int min = (int)info.MinValue;
                            int max = (int)info.MaxValue;
                            int v = EditorGUILayout.IntSlider((int)(long)info.Value, min, max, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                        else {
                            ulong v = (ulong)EditorGUILayout.LongField((long)(ulong)info.Value, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                    }
                    else if (info.Type == typeof(float)) {
                        if (!info.MinValue.IsNullObject && !info.MaxValue.IsNullObject) {
                            float min = (float)info.MinValue;
                            float max = (float)info.MaxValue;
                            float v = EditorGUILayout.Slider((float)info.Value, min, max, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                        else {
                            float v = EditorGUILayout.FloatField((float)info.Value, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                    }
                    else if (info.Type == typeof(double)) {
                        if (!info.MinValue.IsNullObject && !info.MaxValue.IsNullObject) {
                            float min = (float)info.MinValue;
                            float max = (float)info.MaxValue;
                            float v = EditorGUILayout.Slider((float)(double)info.Value, min, max, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                        else {
                            double v = EditorGUILayout.DoubleField((double)info.Value, GUILayout.MaxWidth(1024));
                            newVal = v.ToString();
                        }
                    }
                    else if (info.Type == typeof(UnityEngine.UIElements.Label)) {
                        //label don't need a input control.
                    }
                    else if (info.Type == typeof(UnityEngine.GUIContent)) {
                        if (GUILayout.Button(new GUIContent(string.Format("Return [{0}]", oldVal), oldVal))) {
                            var redirectDsl = oldVal;
                            var redirectArgs = info.Options;
                            QueueProcess(redirectDsl, redirectArgs);
                        }
                    }
                    else if (!string.IsNullOrEmpty(info.FileExts)) {
                        newVal = EditorGUILayout.TextField(oldVal, GUILayout.MaxWidth(1024));
                        if (GUILayout.Button("选择")) {
                            newVal = EditorUtility.OpenFilePanel("选择文件", info.FileInitDir, info.FileExts);
                        }
                    }
                    else {
                        newVal = EditorGUILayout.TextField(oldVal, GUILayout.MaxWidth(1024));
                    }
                    EditorGUILayout.EndHorizontal();
                    if (name == "excel") {
                        ResourceEditUtility.ParamInfo sheetNameInfo;
                        if (paramInfos.TryGetValue("sheetname", out sheetNameInfo)) {
                            if (newVal != oldVal || sheetNameInfo.OptionNames.Count == 0) {
                                sheetNameInfo.PopupOptionNames = null;
                                sheetNameInfo.OptionNames.Clear();
                                sheetNameInfo.Options.Clear();
                                var file = newVal;
                                var path = file;
                                if (!File.Exists(path)) {
                                    path = Path.Combine("../../Product/Excel", file);
                                }
                                if (File.Exists(path)) {
                                    var ext = Path.GetExtension(file);
                                    NPOI.SS.UserModel.IWorkbook book = null;
                                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                                        if (ext == ".xls") {
                                            book = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
                                        }
                                        else {
                                            book = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
                                        }
                                        for (int i = 0; i < book.NumberOfSheets; ++i) {
                                            var sheetName = book.GetSheetName(i);
                                            sheetNameInfo.OptionNames.Add(sheetName);
                                            sheetNameInfo.Options.Add(sheetName, sheetName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (newVal != oldVal) {
                        m_EditedParams.Add(name, newVal);
                    }
                }
            }
            if (m_EditedParams.Count > 0) {
                foreach (var pair in m_EditedParams) {
                    ResourceEditUtility.ParamInfo val;
                    if (paramInfos.TryGetValue(pair.Key, out val)) {
                        val.NextRunScriptTime = 0;
                        val.StringValue = pair.Value;
                        if (val.Type == typeof(int)) {
                            int v = int.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                int min = (int)val.MinValue;
                                int max = (int)val.MaxValue;
                                if (v < min) v = min;
                                if (v > max) v = max;
                            }
                            val.Value = v;
                        }
                        if (val.Type == typeof(uint)) {
                            uint v = uint.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                uint min = (uint)(int)val.MinValue;
                                uint max = (uint)(int)val.MaxValue;
                                if (v < min) v = min;
                                if (v > max) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(long)) {
                            long v = long.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                int min = (int)val.MinValue;
                                int max = (int)val.MaxValue;
                                if (v < min) v = min;
                                if (v > max) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(ulong)) {
                            ulong v = ulong.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                ulong min = (ulong)(int)val.MinValue;
                                ulong max = (ulong)(int)val.MaxValue;
                                if (v < min) v = min;
                                if (v > max) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(float)) {
                            float v = float.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                float min = (float)val.MinValue;
                                float max = (float)val.MaxValue;
                                if (v < min) v = min;
                                if (v > max) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(double)) {
                            double v = double.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                float min = (float)val.MinValue;
                                float max = (float)val.MaxValue;
                                if (v < min) v = min;
                                if (v > max) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(string)) {
                            string v = pair.Value;
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                string min = (string)val.MinValue;
                                string max = (string)val.MaxValue;
                                if (v.CompareTo(min) < 0) v = min;
                                if (v.CompareTo(min) > 0) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(bool)) {
                            bool v = bool.Parse(pair.Value);
                            if (!val.MinValue.IsNullObject && !val.MaxValue.IsNullObject) {
                                bool min = (bool)val.MinValue;
                                bool max = (bool)val.MaxValue;
                                if (v.CompareTo(min) < 0) v = min;
                                if (v.CompareTo(min) > 0) v = max;
                            }
                            val.Value = v;
                        }
                        else if (val.Type == typeof(List<int>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<int>();
                            foreach (var str in v) {
                                int iv;
                                int.TryParse(str, out iv);
                                list.Add(iv);
                            }
                            val.Value = BoxedValue.FromObject(list);
                        }
                        else if (val.Type == typeof(List<uint>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<uint>();
                            foreach (var str in v) {
                                uint iv;
                                uint.TryParse(str, out iv);
                                list.Add(iv);
                            }
                            val.Value = BoxedValue.FromObject(list);
                        }
                        else if (val.Type == typeof(List<long>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<long>();
                            foreach (var str in v) {
                                long iv;
                                long.TryParse(str, out iv);
                                list.Add(iv);
                            }
                            val.Value = BoxedValue.FromObject(list);
                        }
                        else if (val.Type == typeof(List<ulong>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<ulong>();
                            foreach (var str in v) {
                                ulong iv;
                                ulong.TryParse(str, out iv);
                                list.Add(iv);
                            }
                            val.Value = BoxedValue.FromObject(list);
                        }
                        else if (val.Type == typeof(List<float>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<float>();
                            foreach (var str in v) {
                                float fv;
                                float.TryParse(str, out fv);
                                list.Add(fv);
                            }
                            val.Value = BoxedValue.FromObject(list);
                        }
                        else if (val.Type == typeof(List<double>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var list = new List<double>();
                            foreach (var str in v) {
                                double fv;
                                double.TryParse(str, out fv);
                                list.Add(fv);
                            }
                            val.Value = BoxedValue.FromObject(list);
                        }
                        else if (val.Type == typeof(List<string>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_StringListSeps, StringSplitOptions.RemoveEmptyEntries);
                            val.Value = BoxedValue.FromObject(v);
                        }
                        else if (val.Type == typeof(HashSet<int>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<int>();
                            foreach (var str in v) {
                                int iv;
                                int.TryParse(str, out iv);
                                if (!hash.Contains(iv)) {
                                    hash.Add(iv);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(HashSet<uint>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<uint>();
                            foreach (var str in v) {
                                uint iv;
                                uint.TryParse(str, out iv);
                                if (!hash.Contains(iv)) {
                                    hash.Add(iv);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(HashSet<long>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<long>();
                            foreach (var str in v) {
                                long iv;
                                long.TryParse(str, out iv);
                                if (!hash.Contains(iv)) {
                                    hash.Add(iv);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(HashSet<ulong>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<ulong>();
                            foreach (var str in v) {
                                ulong iv;
                                ulong.TryParse(str, out iv);
                                if (!hash.Contains(iv)) {
                                    hash.Add(iv);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(HashSet<float>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<float>();
                            foreach (var str in v) {
                                float fv;
                                float.TryParse(str, out fv);
                                if (!hash.Contains(fv)) {
                                    hash.Add(fv);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(HashSet<double>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<double>();
                            foreach (var str in v) {
                                double fv;
                                double.TryParse(str, out fv);
                                if (!hash.Contains(fv)) {
                                    hash.Add(fv);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(HashSet<string>)) {
                            var v = pair.Value.Split(ResourceProcessor.s_StringListSeps, StringSplitOptions.RemoveEmptyEntries);
                            var hash = new HashSet<string>();
                            foreach (var str in v) {
                                if (!hash.Contains(str)) {
                                    hash.Add(str);
                                }
                            }
                            val.Value = BoxedValue.FromObject(hash);
                        }
                        else if (val.Type == typeof(ResourceEditUtility.DataTable)) {
                            val.Value = pair.Value;
                        }
                        else if (val.Type == typeof(NPOI.SS.UserModel.IWorkbook)) {
                            val.Value = pair.Value;
                        }
                        else if (val.Type == typeof(object)) {
                            val.Value = pair.Value;
                        }
                    }
                }
                m_EditedParams.Clear();
            }
        }
        var text = ResourceProcessor.Instance.Text;
        if (m_ItemList.Count <= 0 && !ResourceProcessor.Instance.CanRefresh) {
            if (!string.IsNullOrEmpty(text)) {
                m_PanelPos = EditorGUILayout.BeginScrollView(m_PanelPos, true, true);
                EditorGUILayout.TextArea(text);
                EditorGUILayout.EndScrollView();
            }
        }
        else {
            if (!string.IsNullOrEmpty(text)) {
                EditorGUILayout.TextArea(text, GUILayout.MaxHeight(70));
            }
            if (m_IsReady) {
                if (m_ItemList.Count <= 0) {
                    if (GUILayout.Button("Refresh")) {
                        DeferAction(obj => { ResourceProcessor.Instance.Refresh(); obj.CopyCollectResult(); });
                    }
                }
                else {
                    ResourceEditUtility.ParamInfo val;
                    if (paramInfos.TryGetValue("pathwidth", out val)) {
                        m_PathWidth = val.Value.GetFloat();
                    }
                    if (m_UnfilteredGroupCount <= 0) {
                        ListItem();
                    }
                    else {
                        ListGroupedItem();
                    }
                }
            }
        }
        GUI.skin.button.richText = oldRichText;

        ExecuteDeferredActions();
        ExecuteBatchActions();
    }
    private void DoToggle(ResourceEditUtility.ParamInfo info, string oldVal, ref string newVal)
    {
        bool changed = false;
        foreach (var key in info.OptionNames) {
            string val;
            if (info.Options.TryGetValue(key, out val)) {
                if (changed) {
                    EditorGUILayout.Toggle(key, false);
                }
                else {
                    bool toggle = val == oldVal;
                    if (EditorGUILayout.Toggle(key, toggle)) {
                        if (!toggle) {
                            changed = true;
                            newVal = val;
                        }
                    }
                }
            }
        }
    }
    private void DoMultiple(ResourceEditUtility.ParamInfo info, string oldVal, ref string newVal)
    {
        if (null == info.MultipleOldValues) {
            info.MultipleOldValues = new List<string>(oldVal.Split('|'));
            info.MultipleNewValues = new List<string>();
        }
        else {
            info.MultipleNewValues.Clear();
        }
        bool changed = false;
        foreach (var key in info.OptionNames) {
            string val;
            if (info.Options.TryGetValue(key, out val)) {
                bool toggle = info.MultipleOldValues.IndexOf(val) >= 0;
                if (EditorGUILayout.Toggle(key, toggle)) {
                    info.MultipleNewValues.Add(val);
                    if (!toggle)
                        changed = true;
                }
                else if (toggle) {
                    changed = true;
                }
            }
        }
        if (changed) {
            newVal = string.Join("|", info.MultipleNewValues.ToArray());
            info.MultipleOldValues.Clear();
            info.MultipleOldValues.AddRange(info.MultipleNewValues);
        }
    }
    private void DoPopup(ResourceEditUtility.ParamInfo info, string oldVal, ref string newVal)
    {
        int ix = 0;
        if (null == info.PopupOptionNames || info.PopupOptionNames.Length != info.OptionNames.Count) {
            info.PopupOptionNames = info.OptionNames.ToArray();
        }
        if (info.OptionNames.Count > 0) {
            foreach (var key in info.OptionNames) {
                string val;
                if (info.Options.TryGetValue(key, out val)) {
                    if (val == oldVal) {
                        break;
                    }
                }
                ++ix;
            }
            if (ix >= info.Options.Count)
                ix = 0;
            int newIx = ix;
            newIx = EditorGUILayout.Popup(ix, info.PopupOptionNames);
            if (ix == 0 || newIx != ix) {
                newVal = info.Options[info.PopupOptionNames[newIx]];
            }
        }
    }

    private void ExecuteDeferredActions()
    {
        if (m_InActions)
            return;
        try {
            m_InActions = true;
            while (m_Actions.Count > 0) {
                var action = m_Actions.Dequeue();
                if (null != action) {
                    action(this);
                }
            }
        }
        finally {
            m_InActions = false;
        }
    }
    private void DeferAction(Action<ResourceEditWindow> action)
    {
        m_Actions.Enqueue(action);
    }
    private void DelayExcuteUntil(System.Action<ResourceEditWindow> action, System.Func<bool> condition)
    {
        DeferAction(w => DelayExcuteUntilRecursively(action, w, condition, 0));
    }
    private void DelayExcuteUntil(System.Action<ResourceEditWindow> action, System.Func<bool> condition, int waitFrames)
    {
        DeferAction(w => DelayExcuteUntilRecursively(action, w, condition, waitFrames));
    }
    private void DelayExcuteUntilRecursively(System.Action<ResourceEditWindow> action, ResourceEditWindow w, System.Func<bool> condition, int waitFrames)
    {
        if (waitFrames > 0) {
            DeferAction(w => DelayExcuteUntilRecursively(action, w, condition, waitFrames - 1));
        }
        else if (condition()) {
            action(w);
        }
        else {
            DeferAction(w => DelayExcuteUntilRecursively(action, w, condition, 0));
        }
    }
    private void ExecuteBatchActions()
    {
        if (m_InBatchActions)
            return;
        try {
            m_InBatchActions = true;
            if (m_BatchActions.Count > 0) {
                var action = m_BatchActions.Dequeue();
                if (null != action) {
                    action(this);
                }
            }
        }
        finally {
            m_InBatchActions = false;
        }
    }

    private void ClearProcessedAssets()
    {
        ResourceProcessor.Instance.ClearProcessedAssets();
    }
    private void BatchAction1(string dslPath, string resPath)
    {
        ResourceProcessor.Instance.SelectDsl(dslPath);
        ResourceProcessor.Instance.CollectPath = resPath;
    }
    private void BatchAction2(int index, int count)
    {
        ResourceProcessor.Instance.OverridedProgressTitle = string.Format("{0}/{1}", index + 1, count);
        ResourceProcessor.Instance.Refresh(true);
        CopyCollectResult();
    }
    private void BatchAction3(int index, int count)
    {
        ResourceProcessor.Instance.OverridedProgressTitle = string.Format("{0}/{1}", index + 1, count);
        ResourceProcessor.Instance.SelectAll();
        ResourceProcessor.Instance.Process(true);
    }
    private void SpecificBatchAction()
    {
        if (ResourceEditUtility.UseSpecificSettingDB) {
            TextureImporterParamsDB.UpdateAllTextures();
            ModelImporterParamsDB.UpdateAllModels();
            PrefabParamsDB.UpdateAllPrefabs();
        }
    }
    private void RedirectAction(string dslPath, IDictionary<string, string> args)
    {
        ResourceProcessor.Instance.SelectDsl(dslPath);
        ResourceProcessor.Instance.CollectPath = string.Empty;
        var paramNames = ResourceProcessor.Instance.ParamNames;
        var paramInfos = ResourceProcessor.Instance.Params;
        foreach (var pair in args) {
            var name = pair.Key;
            var val = pair.Value;
            ResourceEditUtility.ParamInfo info;
            if (paramInfos.TryGetValue(name, out info)) {
                info.StringValue = val;
                if (info.Type == typeof(bool)) {
                    info.Value = bool.Parse(val);
                }
                else if (info.Type == typeof(int)) {
                    info.Value = int.Parse(val);
                }
                else if (info.Type == typeof(float)) {
                    info.Value = float.Parse(val);
                }
                else {
                    info.Value = val;
                }
            }
        }
        ResourceProcessor.Instance.Refresh(true);
        CopyCollectResult();
    }

    private void CopyConfig()
    {
        m_ItemCommand = ResourceProcessor.Instance.DefaultItemCommand;
        m_GroupCommand = ResourceProcessor.Instance.DefaultGroupCommand;
    }
    private void CopyCollectResult()
    {
        m_IsReady = false;
        try {
            m_UnfilteredGroupCount = ResourceProcessor.Instance.UnfilteredGroupCount;
            m_ItemList.Clear();
            m_ItemList.AddRange(ResourceProcessor.Instance.ItemList);
            m_GroupList.Clear();
            m_GroupList.AddRange(ResourceProcessor.Instance.GroupList);
            m_TotalItemValue = ResourceProcessor.Instance.TotalItemValue;
            m_NonZeroItemCount = ResourceProcessor.Instance.NonZeroItemCount;
        }
        finally {
            m_IsReady = true;
        }
    }
    private void CopyToClipboard()
    {
        var sb = new StringBuilder();
        if (m_GroupList.Count > 0) {
            sb.AppendLine("asset_path\tscene_path\tinfo\torder\tvalue\tref\trefby\textra");
            int curCount = 0;
            int totalCount = m_GroupList.Count;
            foreach (var item in m_GroupList) {
                if (ResourceEditUtility.SaveResultWithXrefs && !string.IsNullOrEmpty(item.AssetPath)) {
                    HashSet<string> refs;
                    HashSet<string> refbys;
                    ResourceProcessor.Instance.ReferenceAssets.TryGetValue(item.AssetPath, out refs);
                    ResourceProcessor.Instance.ReferenceByAssets.TryGetValue(item.AssetPath, out refbys);
                    int refCt = 0;
                    if (null != refs)
                        refCt = refs.Count;
                    int refByCt = 0;
                    if (null != refbys)
                        refByCt = refbys.Count;
                    int extraCt = 0;
                    if (null != item.ExtraList)
                        extraCt = item.ExtraList.Count;
                    int ct = Mathf.Max(refCt, refByCt, extraCt);
                    IEnumerator<string> refEnumer = null;
                    if (null != refs)
                        refEnumer = refs.GetEnumerator();
                    IEnumerator<string> refByEnumer = null;
                    if (null != refbys)
                        refByEnumer = refbys.GetEnumerator();
                    IEnumerator<KeyValuePair<string, BoxedValue>> extraEnumer = null;
                    if (null != item.ExtraList)
                        extraEnumer = item.ExtraList.GetEnumerator();
                    if (ct == 0) {
                        sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                        sb.AppendLine();
                    }
                    else {
                        for (int i = 0; i < ct; ++i) {
                            string refAsset = string.Empty;
                            string refByAsset = string.Empty;
                            string extra = string.Empty;
                            if (i < refCt && refEnumer.MoveNext()) {
                                refAsset = refEnumer.Current;
                            }
                            if (i < refByCt && refByEnumer.MoveNext()) {
                                refByAsset = refByEnumer.Current;
                            }
                            if (i < extraCt && extraEnumer.MoveNext()) {
                                extra = extraEnumer.Current.Key;
                            }
                            sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value, refAsset, refByAsset, extra);
                            sb.AppendLine();
                        }
                    }
                }
                else {
                    sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                    sb.AppendLine();
                }
                ++curCount;
                if (ResourceProcessor.Instance.DisplayCancelableProgressBar("拷贝进度", curCount, totalCount)) {
                    break;
                }
            }
        }
        else {
            sb.AppendLine("asset_path\tscene_path\tinfo\torder\tvalue\tref\trefby\textra");
            int curCount = 0;
            int totalCount = m_ItemList.Count;
            foreach (var item in m_ItemList) {
                if (ResourceEditUtility.SaveResultWithXrefs && !string.IsNullOrEmpty(item.AssetPath)) {
                    HashSet<string> refs;
                    HashSet<string> refbys;
                    ResourceProcessor.Instance.ReferenceAssets.TryGetValue(item.AssetPath, out refs);
                    ResourceProcessor.Instance.ReferenceByAssets.TryGetValue(item.AssetPath, out refbys);
                    int refCt = 0;
                    if (null != refs)
                        refCt = refs.Count;
                    int refByCt = 0;
                    if (null != refbys)
                        refByCt = refbys.Count;
                    int extraCt = 0;
                    if (null != item.ExtraList)
                        extraCt = item.ExtraList.Count;
                    int ct = Mathf.Max(refCt, refByCt, extraCt);
                    IEnumerator<string> refEnumer = null;
                    if (null != refs)
                        refEnumer = refs.GetEnumerator();
                    IEnumerator<string> refByEnumer = null;
                    if (null != refbys)
                        refByEnumer = refbys.GetEnumerator();
                    IEnumerator<KeyValuePair<string, BoxedValue>> extraEnumer = null;
                    if (null != item.ExtraList)
                        extraEnumer = item.ExtraList.GetEnumerator();
                    if (ct == 0) {
                        sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                        sb.AppendLine();
                    }
                    else {
                        for (int i = 0; i < ct; ++i) {
                            string refAsset = string.Empty;
                            string refByAsset = string.Empty;
                            string extra = string.Empty;
                            if (i < refCt && refEnumer.MoveNext()) {
                                refAsset = refEnumer.Current;
                            }
                            if (i < refByCt && refByEnumer.MoveNext()) {
                                refByAsset = refByEnumer.Current;
                            }
                            if (i < extraCt && extraEnumer.MoveNext()) {
                                extra = extraEnumer.Current.Key;
                            }
                            sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value, refAsset, refByAsset, extra);
                            sb.AppendLine();
                        }
                    }
                }
                else {
                    sb.AppendFormat("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                    sb.AppendLine();
                }
                ++curCount;
                if (ResourceProcessor.Instance.DisplayCancelableProgressBar("拷贝进度", curCount, totalCount)) {
                    break;
                }
            }
        }
        EditorUtility.ClearProgressBar();

        GUIUtility.systemCopyBuffer = sb.ToString();
    }

    private void Sort(bool asc)
    {
        m_ItemList.Sort((a, b) => {
            int v;
            if (a.Order < b.Order)
                v = -1;
            else if (a.Order > b.Order)
                v = 1;
            else {
                if (!string.IsNullOrEmpty(a.AssetPath) && !string.IsNullOrEmpty(b.AssetPath)) {
                    v = string.CompareOrdinal(a.AssetPath, b.AssetPath);
                }
                else if (!string.IsNullOrEmpty(a.ScenePath) && !string.IsNullOrEmpty(b.ScenePath)) {
                    v = string.CompareOrdinal(a.ScenePath, b.ScenePath);
                }
                else if (!string.IsNullOrEmpty(a.Info) && !string.IsNullOrEmpty(b.Info)) {
                    v = string.CompareOrdinal(a.Info, b.Info);
                }
                else {
                    v = 0;
                }
            }
            if (!asc)
                v = -v;
            return v;
        });
    }
    private void GroupSort(bool asc)
    {
        m_GroupList.Sort((a, b) => {
            int v;
            if (a.Order < b.Order)
                v = -1;
            else if (a.Order > b.Order)
                v = 1;
            else {
                if (!string.IsNullOrEmpty(a.AssetPath) && !string.IsNullOrEmpty(b.AssetPath)) {
                    v = string.CompareOrdinal(a.AssetPath, b.AssetPath);
                }
                else if (!string.IsNullOrEmpty(a.ScenePath) && !string.IsNullOrEmpty(b.ScenePath)) {
                    v = string.CompareOrdinal(a.ScenePath, b.ScenePath);
                }
                else if (!string.IsNullOrEmpty(a.Info) && !string.IsNullOrEmpty(b.Info)) {
                    v = string.CompareOrdinal(a.Info, b.Info);
                }
                else {
                    v = 0;
                }
            }
            if (!asc)
                v = -v;
            return v;
        });
    }
    private void ReGroup()
    {
        ResourceProcessor.Instance.ReGroup(m_ItemCommand, m_GroupCommand);
        CopyCollectResult();
    }
    private void ListItem()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(40))) {
            foreach (var item in m_ItemList) {
                item.Selected = true;
            }
        }
        if (GUILayout.Button("全不选", GUILayout.Width(60))) {
            foreach (var item in m_ItemList) {
                item.Selected = false;
            }
        }
        GUILayout.Label(string.Format("Total count ({0})", m_ItemList.Count), GUILayout.Width(160));
        GUILayout.Label(string.Format("Go to page ({0})", m_ItemList.Count / c_ItemsPerPage + 1), GUILayout.Width(140));
        string strPage = EditorGUILayout.TextField(m_Page.ToString(), GUILayout.Width(60));
        int.TryParse(strPage, out m_Page);
        if (GUILayout.Button("Prev", GUILayout.Width(80))) {
            m_Page--;
        }
        if (GUILayout.Button("Next", GUILayout.Width(80))) {
            m_Page++;
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) {
            DeferAction(obj => { ResourceProcessor.Instance.Refresh(); obj.CopyCollectResult(); });
        }
        if (GUILayout.Button("升序", GUILayout.Width(60))) {
            Sort(true);
        }
        if (GUILayout.Button("降序", GUILayout.Width(60))) {
            Sort(false);
        }
        GUILayout.Label(string.Format("Sum/Avg ({0:f3}/{1:f3}) NZ Avg {2:f3} NZ Count {3}", m_TotalItemValue, m_TotalItemValue / m_ItemList.Count, m_TotalItemValue / m_NonZeroItemCount, m_NonZeroItemCount));
        GUILayout.Label("ItemCmd");
        m_ItemCommand = EditorGUILayout.TextField(m_ItemCommand, EditorStyles.toolbarTextField, GUILayout.MinWidth(80), GUILayout.MaxWidth(this.position.width - 180));
        GUILayout.Label("GroupCmd");
        m_GroupCommand = EditorGUILayout.TextField(m_GroupCommand, EditorStyles.toolbarTextField, GUILayout.MinWidth(80), GUILayout.MaxWidth(this.position.width - 120));
        if (GUILayout.Button("ReGroup", EditorStyles.toolbarButton, GUILayout.Width(60))) {
            DeferAction(w => ReGroup());
        }
        EditorGUILayout.EndHorizontal();
        m_Page = Mathf.Max(1, Mathf.Min(m_ItemList.Count / c_ItemsPerPage + 1, m_Page));
        bool showReferences = false;
        float rightWidth = 0;
        if (null != m_SelectedItem && null == m_SelectedItem.ExtraList && !string.IsNullOrEmpty(m_SelectedItem.ExtraListBuildScript)) {
            m_SelectedItem.ExtraList = ResourceProcessor.Instance.CallScript(null, m_SelectedItem.ExtraListBuildScript, BoxedValue.FromObject(m_SelectedItem)).As<IList<KeyValuePair<string, BoxedValue>>>();
        }
        if (!string.IsNullOrEmpty(m_SelectedAssetPath) && (ResourceProcessor.Instance.ReferenceAssets.Count > 0 || ResourceProcessor.Instance.ReferenceByAssets.Count > 0) ||
            null != m_SelectedItem && null != m_SelectedItem.ExtraList) {
            showReferences = true;
            rightWidth = m_RightWidth;
        }
        float windowWidth = position.width;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        m_PanelPos = EditorGUILayout.BeginScrollView(m_PanelPos, true, true, GUILayout.Width(windowWidth - rightWidth));
        int index = 0;
        int totalShown = 0;
        foreach (var item in m_ItemList) {
            ++index;
            if (index <= (m_Page - 1) * c_ItemsPerPage)
                continue;
            ++totalShown;
            if (totalShown > c_ItemsPerPage)
                break;
            EditorGUILayout.BeginHorizontal();
            item.Selected = GUILayout.Toggle(item.Selected, index + ".", GUILayout.Width(60));
            bool assetNotEmpty = !string.IsNullOrEmpty(item.AssetPath);
            bool sceneNotEmpty = !string.IsNullOrEmpty(item.ScenePath);
            string buttonName = string.Empty;
            if (assetNotEmpty && sceneNotEmpty)
                buttonName = string.Format("{0},{1}", item.AssetPath, item.ScenePath);
            else if (assetNotEmpty)
                buttonName = item.AssetPath;
            else if (sceneNotEmpty)
                buttonName = item.ScenePath;
            if (!string.IsNullOrEmpty(item.RedirectDsl) && GUILayout.Button(new GUIContent("Go", item.RedirectDsl), GUILayout.MinWidth(30), GUILayout.MinWidth(30))) {
                QueueProcess(item.RedirectDsl, item.RedirectArgs);
            }
            if (!string.IsNullOrEmpty(buttonName)) {
                var minWidth = GUILayout.MinWidth(80);
                var maxWidth = GUILayout.MaxWidth(m_PathWidth);
                if (ResourceProcessor.Instance.SearchSource == "sceneobjects" || ResourceProcessor.Instance.SearchSource == "scenecomponents") {
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(buttonName, buttonName), minWidth, maxWidth)) {
                        DeferAction(obj => {
                            if (null != item.Object)
                                ResourceEditUtility.SelectObject(item.Object);
                            else
                                ResourceEditUtility.SelectSceneObject(item.ScenePath);
                            if (!string.IsNullOrEmpty(item.AssetPath)) {
                                ResourceEditUtility.SelectProjectObject(item.AssetPath);
                                obj.m_SelectedAssetPath = item.AssetPath;
                            }
                            else {
                                obj.m_SelectedAssetPath = string.Empty;
                            }
                            obj.m_SelectedItem = item;
                        });
                    }
                    GUI.skin.button.alignment = oldAlignment;
                }
                else {
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(item.AssetPath)) {
                        icon = AssetDatabase.GetCachedIcon(item.AssetPath);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(buttonName, icon, buttonName), minWidth, maxWidth)) {
                        string assetFileName = Path.GetFileNameWithoutExtension(item.AssetPath);
                        if (null != item.Object || AssetDatabase.FindAssets(assetFileName).Length > 0) {
                            DeferAction(obj => {
                                if (null != item.Object)
                                    ResourceEditUtility.SelectObject(item.Object);
                                else
                                    ResourceEditUtility.SelectProjectObject(item.AssetPath);
                                obj.m_SelectedAssetPath = item.AssetPath;
                                obj.m_SelectedItem = item;
                            });
                        }
                        else {
                            m_SelectedAssetPath = item.AssetPath;
                            m_SelectedItem = item;
                            ResourceCommandWindow.InitWindow(this, item.Info, item.ExtraObject, BoxedValue.FromObject(item));
                        }
                    }
                    GUI.skin.button.alignment = oldAlignment;
                }
            }
            else {
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(0));
            }
            EditorGUILayout.TextArea(item.Info, GUILayout.MaxHeight(72), GUILayout.MinWidth(80), GUILayout.MaxWidth(windowWidth - rightWidth));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        if (showReferences) {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(m_RightWidth - 200);
            m_RightWidth = EditorGUILayout.Slider(m_RightWidth, windowWidth - 200, 200, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();
            m_PanelPosRight = EditorGUILayout.BeginScrollView(m_PanelPosRight, true, true, GUILayout.Width(rightWidth));
            HashSet<string> refSet;
            if (ResourceProcessor.Instance.ReferenceAssets.TryGetValue(m_SelectedAssetPath, out refSet)) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("References:");
                EditorGUILayout.EndHorizontal();
                foreach (string assetPath in refSet) {
                    EditorGUILayout.BeginHorizontal();
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(assetPath)) {
                        icon = AssetDatabase.GetCachedIcon(assetPath);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(assetPath, icon, assetPath), GUILayout.MinWidth(80), GUILayout.MaxWidth(rightWidth))) {
                        DeferAction(obj => {
                            ResourceEditUtility.SelectProjectObject(assetPath);
                        });
                    }
                    GUI.skin.button.alignment = oldAlignment;
                    EditorGUILayout.EndHorizontal();
                }
            }
            HashSet<string> refBySet;
            if (ResourceProcessor.Instance.ReferenceByAssets.TryGetValue(m_SelectedAssetPath, out refBySet)) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("References by:");
                EditorGUILayout.EndHorizontal();
                foreach (string assetPath in refBySet) {
                    EditorGUILayout.BeginHorizontal();
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(assetPath)) {
                        icon = AssetDatabase.GetCachedIcon(assetPath);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(assetPath, icon, assetPath), GUILayout.MinWidth(80), GUILayout.MaxWidth(rightWidth))) {
                        DeferAction(obj => {
                            ResourceEditUtility.SelectProjectObject(assetPath);
                        });
                    }
                    GUI.skin.button.alignment = oldAlignment;
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (null != m_SelectedItem.ExtraList) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Extra List:");
                EditorGUILayout.EndHorizontal();
                foreach (var pair in m_SelectedItem.ExtraList) {
                    var info = pair.Key;
                    EditorGUILayout.BeginHorizontal();
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(info)) {
                        icon = AssetDatabase.GetCachedIcon(info);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(info, icon, info), GUILayout.MinWidth(80), GUILayout.MaxWidth(rightWidth))) {
                        if (!string.IsNullOrEmpty(m_SelectedItem.ExtraListClickScript)) {
                            ResourceProcessor.Instance.CallScript(null, m_SelectedItem.ExtraListClickScript, BoxedValue.FromObject(pair), BoxedValue.FromObject(m_SelectedItem));
                        }
                        else if (pair.Value.IsObject && pair.Value.ObjectVal is ObjectData) {
                            var data = (ObjectData)pair.Value.ObjectVal;
                            ResourceProcessor.Instance.OpenLink(data);
                        }
                        else {
                            ResourceCommandWindow.InitWindow(this, info, BoxedValue.FromObject(pair), BoxedValue.FromObject(m_SelectedItem));
                        }
                    }
                    GUI.skin.button.alignment = oldAlignment;
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }
    private void ListGroupedItem()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("全选", GUILayout.Width(40))) {
            foreach (var item in m_GroupList) {
                item.Selected = true;
            }
        }
        if (GUILayout.Button("全不选", GUILayout.Width(60))) {
            foreach (var item in m_GroupList) {
                item.Selected = false;
            }
        }
        GUILayout.Label(string.Format("Total count ({0})", m_GroupList.Count), GUILayout.Width(160));
        GUILayout.Label(string.Format("Go to page ({0})", m_GroupList.Count / c_ItemsPerPage + 1), GUILayout.Width(140));
        string strPage = EditorGUILayout.TextField(m_Page.ToString(), GUILayout.Width(60));
        int.TryParse(strPage, out m_Page);
        if (GUILayout.Button("Prev", GUILayout.Width(80))) {
            m_Page--;
        }
        if (GUILayout.Button("Next", GUILayout.Width(80))) {
            m_Page++;
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(60))) {
            DeferAction(obj => { ResourceProcessor.Instance.Refresh(); obj.CopyCollectResult(); });
        }
        if (GUILayout.Button("升序", GUILayout.Width(60))) {
            GroupSort(true);
        }
        if (GUILayout.Button("降序", GUILayout.Width(60))) {
            GroupSort(false);
        }
        GUILayout.Label(string.Format("Sum/Avg ({0:f3}/{1:f3}) NZ Avg {2:f3} NZ Count {3}", m_TotalItemValue, m_TotalItemValue / m_GroupList.Count, m_TotalItemValue / m_NonZeroItemCount, m_NonZeroItemCount));
        GUILayout.Label("ItemCmd");
        m_ItemCommand = EditorGUILayout.TextField(m_ItemCommand, EditorStyles.toolbarTextField, GUILayout.MinWidth(80), GUILayout.MaxWidth(this.position.width - 180));
        GUILayout.Label("GroupCmd");
        m_GroupCommand = EditorGUILayout.TextField(m_GroupCommand, EditorStyles.toolbarTextField, GUILayout.MinWidth(80), GUILayout.MaxWidth(this.position.width - 120));
        if (GUILayout.Button("ReGroup", EditorStyles.toolbarButton, GUILayout.Width(60))) {
            DeferAction(w => ReGroup());
        }
        EditorGUILayout.EndHorizontal();
        m_Page = Mathf.Max(1, Mathf.Min(m_GroupList.Count / c_ItemsPerPage + 1, m_Page));
        bool showReferences = false;
        float rightWidth = 0;
        if (null != m_SelectedGroup && null == m_SelectedGroup.ExtraList && !string.IsNullOrEmpty(m_SelectedGroup.ExtraListBuildScript)) {
            m_SelectedGroup.ExtraList = ResourceProcessor.Instance.CallScript(null, m_SelectedGroup.ExtraListBuildScript, BoxedValue.FromObject(m_SelectedGroup)).As<IList<KeyValuePair<string, BoxedValue>>>();
        }
        if (!string.IsNullOrEmpty(m_SelectedAssetPath) && (ResourceProcessor.Instance.ReferenceAssets.Count > 0 || ResourceProcessor.Instance.ReferenceByAssets.Count > 0) ||
            null != m_SelectedGroup && null != m_SelectedGroup.ExtraList) {
            showReferences = true;
            rightWidth = m_RightWidth;
        }
        float windowWidth = position.width;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical();
        m_PanelPos = EditorGUILayout.BeginScrollView(m_PanelPos, true, true, GUILayout.Width(windowWidth - rightWidth));
        int index = 0;
        int totalShown = 0;
        foreach (var item in m_GroupList) {
            ++index;
            if (index <= (m_Page - 1) * c_ItemsPerPage)
                continue;
            ++totalShown;
            if (totalShown > c_ItemsPerPage)
                break;
            EditorGUILayout.BeginHorizontal();
            item.Selected = GUILayout.Toggle(item.Selected, index + ".", GUILayout.Width(60));
            bool assetNotEmpty = !string.IsNullOrEmpty(item.AssetPath);
            bool sceneNotEmpty = !string.IsNullOrEmpty(item.ScenePath);
            string buttonName = string.Empty;
            if (assetNotEmpty && sceneNotEmpty)
                buttonName = string.Format("{0},{1}", item.AssetPath, item.ScenePath);
            else if (assetNotEmpty)
                buttonName = item.AssetPath;
            else if (sceneNotEmpty)
                buttonName = item.ScenePath;
            if (!string.IsNullOrEmpty(item.RedirectDsl) && GUILayout.Button(new GUIContent("Go", item.RedirectDsl), GUILayout.MinWidth(30), GUILayout.MinWidth(30))) {
                QueueProcess(item.RedirectDsl, item.RedirectArgs);
            }
            if (!string.IsNullOrEmpty(buttonName)) {
                var minWidth = GUILayout.MinWidth(80);
                var maxWidth = GUILayout.MaxWidth(m_PathWidth);
                if (ResourceProcessor.Instance.SearchSource == "sceneobjects" || ResourceProcessor.Instance.SearchSource == "scenecomponents") {
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(buttonName, buttonName), minWidth, maxWidth)) {
                        DeferAction(obj => {
                            ResourceEditUtility.SelectSceneObject(item.ScenePath);
                            if (!string.IsNullOrEmpty(item.AssetPath)) {
                                ResourceEditUtility.SelectProjectObject(item.AssetPath);
                                obj.m_SelectedAssetPath = item.AssetPath;
                            }
                            else {
                                obj.m_SelectedAssetPath = string.Empty;
                            }
                            obj.m_SelectedGroup = item;
                        });
                    }
                    GUI.skin.button.alignment = oldAlignment;
                }
                else {
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(item.AssetPath)) {
                        icon = AssetDatabase.GetCachedIcon(item.AssetPath);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(buttonName, icon, buttonName), minWidth, maxWidth)) {
                        string assetFileName = Path.GetFileNameWithoutExtension(item.AssetPath);
                        if (AssetDatabase.FindAssets(assetFileName).Length > 0) {
                            DeferAction(obj => {
                                ResourceEditUtility.SelectProjectObject(item.AssetPath);
                                obj.m_SelectedAssetPath = item.AssetPath;
                                obj.m_SelectedGroup = item;
                            });
                        }
                        else {
                            m_SelectedAssetPath = item.AssetPath;
                            m_SelectedGroup = item;
                            ResourceCommandWindow.InitWindow(this, item.Info, item.ExtraObject, BoxedValue.FromObject(item));
                        }
                    }
                    GUI.skin.button.alignment = oldAlignment;
                }
            }
            else {
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(0));
            }
            EditorGUILayout.TextArea(item.Info, GUILayout.MaxHeight(72), GUILayout.MinWidth(80), GUILayout.MaxWidth(windowWidth - rightWidth));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        if (showReferences) {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(m_RightWidth - 200);
            m_RightWidth = EditorGUILayout.Slider(m_RightWidth, windowWidth - 200, 200, GUILayout.Width(160));
            EditorGUILayout.EndHorizontal();
            m_PanelPosRight = EditorGUILayout.BeginScrollView(m_PanelPosRight, true, true, GUILayout.Width(rightWidth));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("References:");
            EditorGUILayout.EndHorizontal();
            HashSet<string> refSet;
            if (ResourceProcessor.Instance.ReferenceAssets.TryGetValue(m_SelectedAssetPath, out refSet)) {
                foreach (string assetPath in refSet) {
                    EditorGUILayout.BeginHorizontal();
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(assetPath)) {
                        icon = AssetDatabase.GetCachedIcon(assetPath);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(assetPath, icon, assetPath), GUILayout.MinWidth(80), GUILayout.MaxWidth(rightWidth))) {
                        DeferAction(obj => {
                            ResourceEditUtility.SelectProjectObject(assetPath);
                        });
                    }
                    GUI.skin.button.alignment = oldAlignment;
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("References by:");
            EditorGUILayout.EndHorizontal();
            HashSet<string> refBySet;
            if (ResourceProcessor.Instance.ReferenceByAssets.TryGetValue(m_SelectedAssetPath, out refBySet)) {
                foreach (string assetPath in refBySet) {
                    EditorGUILayout.BeginHorizontal();
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(assetPath)) {
                        icon = AssetDatabase.GetCachedIcon(assetPath);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(assetPath, icon, assetPath), GUILayout.MinWidth(80), GUILayout.MaxWidth(rightWidth))) {
                        DeferAction(obj => {
                            ResourceEditUtility.SelectProjectObject(assetPath);
                        });
                    }
                    GUI.skin.button.alignment = oldAlignment;
                    EditorGUILayout.EndHorizontal();
                }
            }
            if (null != m_SelectedGroup.ExtraList) {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Extra List:");
                EditorGUILayout.EndHorizontal();
                foreach (var pair in m_SelectedGroup.ExtraList) {
                    var info = pair.Key;
                    EditorGUILayout.BeginHorizontal();
                    Texture icon = null;
                    if (ResourceEditUtility.IsValidAssetPath(info)) {
                        icon = AssetDatabase.GetCachedIcon(info);
                    }
                    var oldAlignment = GUI.skin.button.alignment;
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button(new GUIContent(info, icon, info), GUILayout.MinWidth(80), GUILayout.MaxWidth(rightWidth))) {
                        if (!string.IsNullOrEmpty(m_SelectedGroup.ExtraListClickScript)) {
                            ResourceProcessor.Instance.CallScript(null, m_SelectedGroup.ExtraListClickScript, BoxedValue.FromObject(pair), BoxedValue.FromObject(m_SelectedGroup));
                        }
                        else if (pair.Value.IsObject && pair.Value.GetObject() is ObjectData) {
                            var data = (ObjectData)pair.Value.GetObject();
                            ResourceProcessor.Instance.OpenLink(data);
                        }
                        else {
                            ResourceCommandWindow.InitWindow(this, info, BoxedValue.FromObject(pair), BoxedValue.FromObject(m_SelectedGroup));
                        }
                    }
                    GUI.skin.button.alignment = oldAlignment;
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndHorizontal();
    }

    private string[] m_Menus = null;
    private string[] m_Files = null;
    private int m_SelectedIndex = 0;

    private string m_ItemCommand = string.Empty;
    private string m_GroupCommand = string.Empty;

    private List<ResourceEditUtility.ItemInfo> m_ItemList = new List<ResourceEditUtility.ItemInfo>();
    private List<ResourceEditUtility.GroupInfo> m_GroupList = new List<ResourceEditUtility.GroupInfo>();
    private int m_UnfilteredGroupCount = 0;
    private double m_TotalItemValue = 0;
    private int m_NonZeroItemCount = 0;

    private Vector2 m_PanelPos = Vector2.zero;
    private Vector2 m_PanelPosRight = Vector2.zero;
    private float m_PathWidth = 240;
    private float m_RightWidth = 240;
    private Dictionary<string, string> m_EditedParams = new Dictionary<string, string>();
    private int m_Page = 1;
    private string m_SelectedAssetPath = string.Empty;
    private ResourceEditUtility.ItemInfo m_SelectedItem = null;
    private ResourceEditUtility.GroupInfo m_SelectedGroup = null;

    private bool m_IsReady = false;
    private bool m_InActions = false;
    private bool m_InBatchActions = false;
    private Queue<Action<ResourceEditWindow>> m_Actions = new Queue<Action<ResourceEditWindow>>();
    private Queue<Action<ResourceEditWindow>> m_BatchActions = new Queue<Action<ResourceEditWindow>>();

    private static ResourceEditWindow s_CurrentWindow = null;

    private const int c_ItemsPerPage = 50;
}

internal sealed class ResourceProcessor
{
    internal string OverridedProgressTitle
    {
        get { return m_OverridedProgressTitle; }
        set { m_OverridedProgressTitle = value; }
    }
    internal string DslMenu
    {
        get { return m_DslMenu; }
    }
    internal string DslDescription
    {
        get { return m_DslDescription; }
    }
    internal string DefaultItemCommand
    {
        get { return m_DefaultItemCommand; }
    }
    internal string DefaultGroupCommand
    {
        get { return m_DefaultGroupCommand; }
    }
    internal string SearchSource
    {
        get { return m_SearchSource; }
    }
    internal List<string> ParamNames
    {
        get { return m_ParamNames; }
    }
    internal Dictionary<string, ResourceEditUtility.ParamInfo> Params
    {
        get { return m_Params; }
    }
    internal bool CanRefresh
    {
        get { return m_CanRefresh; }
    }
    internal string Text
    {
        get { return m_Text; }
    }
    internal string CollectPath
    {
        get { return m_CollectPath; }
        set { m_CollectPath = value; }
    }
    internal List<ResourceEditUtility.ItemInfo> ItemList
    {
        get { return m_ItemList; }
    }
    internal List<ResourceEditUtility.GroupInfo> GroupList
    {
        get { return m_GroupList; }
    }
    internal int UnfilteredGroupCount
    {
        get { return m_UnfilteredGroupCount; }
    }
    internal double TotalItemValue
    {
        get { return m_TotalItemValue; }
    }
    internal int NonZeroItemCount
    {
        get { return m_NonZeroItemCount; }
    }
    internal AssetInformation AssetBundleInfo
    {
        get {
            if (null == m_AssetBundleInfo) {
                var assetPath = "Assets/Publish/resources.asset";
                var filePath = AssetPathToPath(assetPath);
                if (File.Exists(filePath)) {
                    m_AssetBundleInfo = AssetDatabase.LoadAssetAtPath<AssetInformation>(assetPath);
                }
            }
            return m_AssetBundleInfo;
        }
    }
    internal IDictionary<string, HashSet<string>> ReferenceAssets
    {
        get { return m_ReferenceAssets; }
    }
    internal IDictionary<string, HashSet<string>> ReferenceByAssets
    {
        get { return m_ReferenceByAssets; }
    }
    internal IDictionary<string, ResourceEditUtility.MemoryGroupInfo> ClassifiedNativeMemoryInfos
    {
        get {
            return m_ClassifiedNativeMemoryInfos;
        }
    }
    internal IDictionary<string, ResourceEditUtility.MemoryGroupInfo> ClassifiedManagedMemoryInfos
    {
        get {
            return m_ClassifiedManagedMemoryInfos;
        }
    }
    internal IDictionary<int, ResourceEditUtility.InstrumentInfo> InstrumentInfos
    {
        get {
            return m_InstrumentInfos;
        }
    }
    internal void ClearProcessedAssets()
    {
        m_ProcessedAssets.Clear();
    }
    internal void SelectAssetsOrObjects()
    {
        var list = new List<UnityEngine.Object>();
        foreach (var item in m_ItemList) {
            if (item.Selected) {
                if (m_SearchSource == "sceneobjects" || m_SearchSource == "scenecomponents") {
                    if (null != item.Object)
                        list.Add(item.Object);
                }
                else {
                    if (null != item.Object) {
                        list.Add(item.Object);
                    }
                    else {
                        var obj = AssetDatabase.LoadMainAssetAtPath(item.AssetPath);
                        if (null != obj) {
                            list.Add(obj);
                        }
                    }
                }
            }
        }
        Selection.objects = list.ToArray();
        if (!(m_SearchSource == "sceneobjects" || m_SearchSource == "scenecomponents")) {
            Type.GetType("UnityEditor.ProjectBrowser,UnityEditor").InvokeMember("ShowSelectedObjectsInLastInteractedProjectBrowser", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, null, null);
        }
    }
    internal void GenerateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        int size = 10;
        int ct = 32;
        int ix = 1;
        int iy = 1;
        foreach (var item in m_ItemList) {
            if (item.Selected) {
                var obj = AssetDatabase.LoadMainAssetAtPath(item.AssetPath) as GameObject;
                if (null != obj) {
                    var newObj = PrefabUtility.InstantiatePrefab(obj, scene) as GameObject;
                    if (null != newObj) {
                        newObj.transform.position = new Vector3(ix * size, 0, iy * size);
                        ++ix;
                        if (ix >= ct) {
                            ix = 1;
                            ++iy;
                        }
                    }
                }
            }
        }
    }
    internal void AnalyseAssets()
    {
        m_ReferenceAssets.Clear();
        m_ReferenceByAssets.Clear();
        m_UnusedAssets.Clear();
        var guids = AssetDatabase.FindAssets(string.Empty);
        var allFiles = new HashSet<string>();
        var depFiles = new HashSet<string>();
        for (int i = 0; i < guids.Length; ++i) {
            string file = AssetDatabase.GUIDToAssetPath(guids[i]);
            string fpath = AssetPathToPath(file);
            if (!File.Exists(fpath) || IsIgnoreDir(file))
                continue;
            if (!allFiles.Contains(file)) {
                allFiles.Add(file);

                var deps = AssetDatabase.GetDependencies(file);
                HashSet<string> refSet;
                if (!m_ReferenceAssets.TryGetValue(file, out refSet)) {
                    refSet = new HashSet<string>();
                    m_ReferenceAssets.Add(file, refSet);
                }
                foreach (var dep in deps) {
                    if (dep == file)
                        continue;
                    if (!depFiles.Contains(dep))
                        depFiles.Add(dep);
                    if (!refSet.Contains(dep)) {
                        refSet.Add(dep);
                    }
                    HashSet<string> refBySet;
                    if (!m_ReferenceByAssets.TryGetValue(dep, out refBySet)) {
                        refBySet = new HashSet<string>();
                        m_ReferenceByAssets.Add(dep, refBySet);
                    }
                    if (!refBySet.Contains(file)) {
                        refBySet.Add(file);
                    }
                }
            }
            int ct = i + 1;
            if (DisplayCancelableProgressBar("依赖分析进度", depFiles.Count, ct, guids.Length, false)) {
                m_ReferenceAssets.Clear();
                m_ReferenceByAssets.Clear();
                m_UnusedAssets.Clear();
                EditorUtility.ClearProgressBar();
                return;
            }
        }
        foreach (string file in allFiles) {
            if (!depFiles.Contains(file)) {
                m_UnusedAssets.Add(file);
            }
        }
        EditorUtility.ClearProgressBar();
    }
    internal void SaveDependencies()
    {
        string fullpath = EditorPrefs.GetString(c_pref_key_save_dependencies);
        bool noPath = string.IsNullOrEmpty(fullpath);
        string dir = noPath ? Application.dataPath : Path.GetDirectoryName(fullpath);
        string name = noPath ? "dependencies" : Path.GetFileName(fullpath);
        string path = EditorUtility.SaveFilePanel("请指定要保存依赖信息的文件", dir, name, "txt");
        if (!string.IsNullOrEmpty(path)) {
            EditorPrefs.SetString(c_pref_key_save_dependencies, path);

            if (File.Exists(path)) {
                File.Delete(path);
            }
            using (StreamWriter sw = new StreamWriter(path)) {
                sw.WriteLine("asset1\tasset2");
                int curCount = 0;
                int totalCount = 0;
                foreach (var pair in m_ReferenceAssets) {
                    totalCount += pair.Value.Count;
                }
                totalCount += m_UnusedAssets.Count;
                foreach (var pair in m_ReferenceAssets) {
                    var asset1 = pair.Key;
                    foreach (var asset2 in pair.Value) {
                        sw.WriteLine("{0}\t{1}", asset1, asset2);
                        ++curCount;
                        if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                            goto L_EndSaveDep;
                        }
                    }
                }
                foreach (var asset in m_UnusedAssets) {
                    sw.WriteLine("unused_asset_tag\t{0}", asset);
                    ++curCount;
                    if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                        goto L_EndSaveDep;
                    }
                }
            L_EndSaveDep:
                sw.Close();
                EditorUtility.ClearProgressBar();
            }
        }
    }
    internal void LoadDependencies()
    {
        string file = EditorPrefs.GetString(c_pref_key_load_dependencies);
        string path = EditorUtility.OpenFilePanel("请指定要加载依赖信息的文件", string.IsNullOrEmpty(file) ? string.Empty : Path.GetDirectoryName(file), "txt");
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            EditorPrefs.SetString(c_pref_key_load_dependencies, path);

            int i = 0;
            try {
                var fi = new FileInfo(path);
                var len = fi.Length;
                long curCount = 0;
                long totalCount = len;

                m_ReferenceAssets.Clear();
                m_ReferenceByAssets.Clear();
                m_UnusedAssets.Clear();
                using (var reader = fi.OpenText()) {
                    int ix = 0;
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        curCount = reader.BaseStream.Position;
                        ++i;
                        if (i <= 1) {
                            continue;
                        }

                        var fields = line.Split('\t');
                        var one = fields[0];
                        var two = fields[1];

                        if (one == "unused_asset_tag") {
                            m_UnusedAssets.Add(two);
                        }
                        else {
                            HashSet<string> refSet;
                            if (!m_ReferenceAssets.TryGetValue(one, out refSet)) {
                                refSet = new HashSet<string>();
                                m_ReferenceAssets.Add(one, refSet);
                            }
                            if (!refSet.Contains(two))
                                refSet.Add(two);
                            HashSet<string> refBySet;
                            if (!m_ReferenceByAssets.TryGetValue(two, out refBySet)) {
                                refBySet = new HashSet<string>();
                                m_ReferenceByAssets.Add(two, refBySet);
                            }
                            if (!refBySet.Contains(one))
                                refBySet.Add(one);
                        }

                        if (DisplayCancelableProgressBar("加载进度", curCount, totalCount)) {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                EditorUtility.DisplayDialog("异常", string.Format("line {0} exception {1}\n{2}", i, ex.Message, ex.StackTrace), "ok");
            }
            EditorUtility.ClearProgressBar();
        }
    }
    internal void AnalyseSnapshot()
    {
        m_ClassifiedNativeMemoryInfos.Clear();
        m_ClassifiedManagedMemoryInfos.Clear();
        long curCount = 0;
        long totalCount = s_CachedSnapshot.SortedNativeObjects.Count;
        for (int i = 0; i < totalCount; ++i) {
            var size = s_CachedSnapshot.SortedNativeObjects.Size(i);
            var addr = s_CachedSnapshot.SortedNativeObjects.Address(i);
            var name = s_CachedSnapshot.SortedNativeObjects.Name(i);
            var refCount = s_CachedSnapshot.SortedNativeObjects.Refcount(i);
            var instanceId = s_CachedSnapshot.SortedNativeObjects.InstanceId(i);
            var nativeTypeIndex = s_CachedSnapshot.SortedNativeObjects.NativeTypeArrayIndex(i);
            string typeName = string.Empty;
            if (nativeTypeIndex >= 0 && nativeTypeIndex < s_CachedSnapshot.NativeTypes.Count) {
                typeName = s_CachedSnapshot.NativeTypes.TypeName[nativeTypeIndex];
            }
            int managedObjectIndex = s_CachedSnapshot.SortedNativeObjects.ManagedObjectIndex(i);

            var memory = new ResourceEditUtility.MemoryInfo();
            memory.instanceId = (ulong)instanceId;
            memory.name = name;
            memory.className = typeName;
            memory.size = (long)size;
            memory.refCount = refCount;
            memory.address = addr;
            memory.isManaged = false;
            memory.sortedObjectIndex = i;
            long index;
            if (s_CachedSnapshot.NativeObjects.InstanceId2Index.TryGetValue(instanceId, out index)) {
                memory.objectData = ObjectData.FromNativeObjectIndex(s_CachedSnapshot, index);
            }

            ResourceEditUtility.MemoryGroupInfo groupInfo = null;
            if (!m_ClassifiedNativeMemoryInfos.TryGetValue(memory.className, out groupInfo)) {
                groupInfo = new ResourceEditUtility.MemoryGroupInfo();
                groupInfo.group = memory.className;
                m_ClassifiedNativeMemoryInfos.Add(memory.className, groupInfo);
            }
            ++groupInfo.count;
            groupInfo.size += memory.size;
            groupInfo.memories.Add(memory);

            ++curCount;
            if (curCount % 100 == 0 && DisplayCancelableProgressBar("内存native信息分类进度", curCount, totalCount, false)) {
                m_ClassifiedNativeMemoryInfos.Clear();
                EditorUtility.ClearProgressBar();
                return;
            }
        }
        curCount = 0;
        totalCount = s_CachedSnapshot.SortedManagedObjects.Count;
        for (int i = 0; i < totalCount; ++i) {
            var size = s_CachedSnapshot.SortedManagedObjects.Size(i);
            var addr = s_CachedSnapshot.SortedManagedObjects.Address(i);
            ManagedObjectInfo objInfo;
            int refCount = 0;
            string typeName = string.Empty;
            long index;
            if (s_CachedSnapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(addr, out index)) {
                objInfo = s_CachedSnapshot.CrawledData.ManagedObjects[index];
                refCount = objInfo.RefCount;
                if (objInfo.ITypeDescription >= 0 && objInfo.ITypeDescription < s_CachedSnapshot.TypeDescriptions.Count) {
                    typeName = s_CachedSnapshot.TypeDescriptions.TypeDescriptionName[objInfo.ITypeDescription];
                }
            }
            else {
                objInfo = new ManagedObjectInfo();
            }

            var memory = new ResourceEditUtility.MemoryInfo();
            memory.instanceId = addr;
            memory.className = typeName;
            memory.size = (long)size;
            memory.address = addr;
            memory.refCount = refCount;
            memory.isManaged = true;
            memory.sortedObjectIndex = i;
            memory.objectData = ObjectData.FromManagedObjectInfo(s_CachedSnapshot, objInfo);
            memory.name = memory.objectData.GetValueAsString(s_CachedSnapshot);
            if (string.IsNullOrEmpty(memory.name)) {
                memory.name = typeName;
            }

            ResourceEditUtility.MemoryGroupInfo groupInfo = null;
            if (!m_ClassifiedManagedMemoryInfos.TryGetValue(memory.className, out groupInfo)) {
                groupInfo = new ResourceEditUtility.MemoryGroupInfo();
                groupInfo.group = memory.className;
                m_ClassifiedManagedMemoryInfos.Add(memory.className, groupInfo);
            }
            ++groupInfo.count;
            groupInfo.size += memory.size;
            groupInfo.memories.Add(memory);

            ++curCount;
            if (curCount % 1000 == 0 && DisplayCancelableProgressBar("内存managed信息分类进度", curCount, totalCount, false)) {
                m_ClassifiedManagedMemoryInfos.Clear();
                EditorUtility.ClearProgressBar();
                return;
            }
        }
        EditorUtility.ClearProgressBar();
    }
    internal IList<KeyValuePair<string, BoxedValue>> FindShortestPathToRoot(ulong addr)
    {
        var data = ObjectDataFromAddress(addr);
        return FindShortestPathToRoot(data);
    }
    internal IList<KeyValuePair<string, BoxedValue>> FindShortestPathToRoot(ObjectData obj)
    {
        var list = new List<KeyValuePair<string, BoxedValue>>();
        if (null != s_ShortestPathToRootFinder) {
            var refbys = s_ShortestPathToRootFinder.FindFor(obj);
            if (null != refbys) {
                list.Add(new KeyValuePair<string, BoxedValue>("=ShortestPathToRoot=", BoxedValue.NullObject));
                foreach (var data in refbys) {
                    if (data.IsField()) {
                        var parent = data.Parent.Obj;
                        string name = string.Empty;
                        if (parent.managedTypeIndex >= 0 && parent.managedTypeIndex < s_CachedSnapshot.TypeDescriptions.Count) {
                            name = s_CachedSnapshot.TypeDescriptions.TypeDescriptionName[parent.managedTypeIndex];
                        }
                        list.Add(new KeyValuePair<string, BoxedValue>(string.Format("{0}.{1}", name, data.GetFieldName(s_CachedSnapshot)), BoxedValue.FromObject(data.displayObject)));
                    }
                    else if (data.IsArrayItem()) {
                        var parent = data.Parent.Obj;
                        var arrInfo = parent.GetArrayInfo(s_CachedSnapshot);
                        if (null != arrInfo) {
                            string type = string.Empty;
                            if (arrInfo.ElementTypeDescription >= 0 && arrInfo.ElementTypeDescription < s_CachedSnapshot.TypeDescriptions.Count) {
                                type = s_CachedSnapshot.TypeDescriptions.TypeDescriptionName[arrInfo.ElementTypeDescription];
                            }
                            string rank = arrInfo.ArrayRankToString();
                            var indexStr = arrInfo.IndexToRankedString(data.arrayIndex);
                            list.Add(new KeyValuePair<string, BoxedValue>(string.Format("{0}(rank:{1})[{2}]", type, rank, indexStr), BoxedValue.FromObject(data.displayObject)));
                        }
                        else {
                            string name = string.Empty;
                            if (data.managedTypeIndex >= 0 && data.managedTypeIndex < s_CachedSnapshot.TypeDescriptions.Count) {
                                name = s_CachedSnapshot.TypeDescriptions.TypeDescriptionName[data.managedTypeIndex];
                            }
                            list.Add(new KeyValuePair<string, BoxedValue>(string.Format("{0}[{1}]", name, parent.arrayIndex), BoxedValue.FromObject(data.displayObject)));
                        }
                    }
                    else if (data.isManaged) {
                        string name = string.Empty;
                        if (data.managedTypeIndex >= 0 && data.managedTypeIndex < s_CachedSnapshot.TypeDescriptions.Count) {
                            name = s_CachedSnapshot.TypeDescriptions.TypeDescriptionName[data.managedTypeIndex];
                        }
                        list.Add(new KeyValuePair<string, BoxedValue>(name, BoxedValue.FromObject(data.displayObject)));
                    }
                    else if (data.isNativeObject) {
                        string name = string.Empty;
                        string type = string.Empty;
                        if (data.nativeObjectIndex >= 0 && data.nativeObjectIndex < s_CachedSnapshot.NativeObjects.Count) {
                            name = s_CachedSnapshot.NativeObjects.ObjectName[data.nativeObjectIndex];
                            int typeIndex = s_CachedSnapshot.NativeObjects.NativeTypeArrayIndex[data.nativeObjectIndex];
                            if (typeIndex >= 0 && typeIndex < s_CachedSnapshot.NativeTypes.Count) {
                                type = s_CachedSnapshot.NativeTypes.TypeName[typeIndex];
                            }
                        }
                        list.Add(new KeyValuePair<string, BoxedValue>(name + "(" + type + ")", BoxedValue.FromObject(data.displayObject)));
                    }
                    else {
                        list.Add(new KeyValuePair<string, BoxedValue>(data.ToString(), BoxedValue.FromObject(data.displayObject)));
                    }
                }
                string reason;
                s_ShortestPathToRootFinder.IsRoot(refbys.Last(), out reason);
                list.Add(new KeyValuePair<string, BoxedValue>("This is a root because:" + reason, BoxedValue.NullObject));
            }
            else {
                list.Add(new KeyValuePair<string, BoxedValue>("No root is keeping this object alive.It will be collected next UnloadUnusedAssets() or scene load", BoxedValue.NullObject));
            }
        }
        list.Add(new KeyValuePair<string, BoxedValue>(string.Empty, BoxedValue.NullObject));
        list.Add(new KeyValuePair<string, BoxedValue>("[goto self]", BoxedValue.FromObject(obj.displayObject)));
        return list;
    }
    internal HashSet<ObjectData> GetRefByObjectData(ulong addr)
    {
        var data = ObjectDataFromAddress(addr);
        return GetRefByObjectData(data);
    }
    internal HashSet<ObjectData> GetRefByObjectData(ObjectData obj)
    {
        if (null != s_ShortestPathToRootFinder) {
            return s_ShortestPathToRootFinder.GetRefByObjs(obj);
        }
        else {
            return s_EmptyObjectDataHash;
        }
    }
    internal IList<ObjectData> GetRefObjectData(ulong addr)
    {
        var data = ObjectDataFromAddress(addr);
        return GetRefObjectData(data);
    }
    internal IList<ObjectData> GetRefObjectData(ObjectData obj)
    {
        if (null != s_ShortestPathToRootFinder) {
            return s_ShortestPathToRootFinder.GetRefObjs(obj);
        }
        else {
            return s_EmptyObjectDataList;
        }
    }
    internal ObjectData ObjectDataFromAddress(ulong addr)
    {
        if (null == s_CachedSnapshot)
            return ObjectData.Invalid;
        s_CachedSnapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(addr, out var index);
        var data = ObjectData.FromManagedObjectIndex(s_CachedSnapshot, index);
        if (!data.IsValid) {
            long low = 0;
            long high = s_CachedSnapshot.SortedNativeObjects.Count - 1;
            while (low <= high) {
                var ix = (low + high) / 2;
                var lowAddr = s_CachedSnapshot.SortedNativeObjects.Address(low);
                var highAddr = s_CachedSnapshot.SortedNativeObjects.Address(high);
                var vaddr = s_CachedSnapshot.SortedNativeObjects.Address(ix);
                if (vaddr < addr) {
                    low = ix + 1;
                }
                else if (vaddr > addr) {
                    high = ix - 1;
                }
                else {
                    var instanceId = s_CachedSnapshot.SortedNativeObjects.InstanceId(ix);
                    long nativeObjectIndex;
                    if (s_CachedSnapshot.NativeObjects.InstanceId2Index.TryGetValue(instanceId, out nativeObjectIndex)) {
                        data = ObjectData.FromNativeObjectIndex(s_CachedSnapshot, nativeObjectIndex);
                    }
                    break;
                }
            }
        }
        return data;
    }
    internal ObjectData ObjectDataFromUnifiedObjectIndex(long index)
    {
        if (null == s_CachedSnapshot)
            return ObjectData.Invalid;
        return ObjectData.FromUnifiedObjectIndex(s_CachedSnapshot, index);
    }
    internal ObjectData ObjectDataFromNativeObjectIndex(long index)
    {
        if (null == s_CachedSnapshot)
            return ObjectData.Invalid;
        return ObjectData.FromNativeObjectIndex(s_CachedSnapshot, index);
    }
    internal ObjectData ObjectDataFromManagedObjectIndex(long index)
    {
        if (null == s_CachedSnapshot)
            return ObjectData.Invalid;
        return ObjectData.FromManagedObjectIndex(s_CachedSnapshot, index);
    }
    internal void OpenLink(ObjectData data)
    {
        MemoryProfilerWindow.Select(data);
    }
    internal void LoadMemoryInfo()
    {
        s_CachedSnapshot = null;
        m_ClassifiedNativeMemoryInfos.Clear();
        m_ClassifiedManagedMemoryInfos.Clear();
        Unity.MemoryProfilerExtension.Editor.MemoryProfilerWindow.ShowWindow();
        s_CachedSnapshot = Unity.MemoryProfilerExtension.Editor.MemoryProfilerWindow.GetBaseCachedSnapshot();
        if (null != s_CachedSnapshot) {
            s_ShortestPathToRootFinder = new ShortestPathToRootObjectFinder(s_CachedSnapshot);
            AnalyseSnapshot();
        }
    }
    internal void ClearInstrumentInfo()
    {
        m_InstrumentInfos.Clear();
        m_uTraceFrames.Clear();
    }
    internal void RecordInstrument()
    {
        m_InstrumentInfos.Clear();
        int firstIndex = ProfilerDriver.firstFrameIndex;
        int lastIndex = ProfilerDriver.lastFrameIndex;

        var iter = new ProfilerFrameDataIterator();
        int midFrame = (firstIndex + lastIndex) / 2;
        int threadCount = iter.GetThreadCount(midFrame);
        var threads = new SortedDictionary<int, ResourceEditUtility.InstrumentThreadInfo>();
        for (int ix = 0; ix < threadCount; ++ix) {
            var rawView = ProfilerDriver.GetRawFrameDataView(midFrame, ix);
            if (null != rawView) {
                if (!threads.TryGetValue(rawView.threadIndex, out var tinfo)) {
                    tinfo = new ResourceEditUtility.InstrumentThreadInfo();
                    threads.Add(rawView.threadIndex, tinfo);
                }

                tinfo.theadIndex = rawView.threadIndex;
                tinfo.threadName = rawView.threadName;
                tinfo.threadGroup = rawView.threadGroupName;
                tinfo.threadId = rawView.threadId;
            }
        }

        if (lastIndex >= firstIndex && lastIndex >= 0) {
            float[] batches = new float[lastIndex - firstIndex + 1];
            float[] triangles = new float[lastIndex - firstIndex + 1];
            var labels = ProfilerDriver.GetGraphStatisticsPropertiesForArea(ProfilerArea.Rendering);
            foreach (string l in labels) {
                //var id = ProfilerDriver.GetStatisticsIdentifierForArea(ProfilerArea.Rendering, l);
                var lowerLabel = l.ToLower();
                if (lowerLabel.StartsWith("batches")) {
                    float maxVal;
                    ProfilerDriver.GetCounterValuesBatch(ProfilerArea.Rendering, l, firstIndex, 1.0f, batches, out maxVal);
                }
                else if (lowerLabel.StartsWith("triangles")) {
                    float maxVal;
                    ProfilerDriver.GetCounterValuesBatch(ProfilerArea.Rendering, l, firstIndex, 1.0f, triangles, out maxVal);
                }
            }

            HierarchyFrameDataView.ViewModes viewMode = HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName;
            int sortColumn = HierarchyFrameDataView.columnTotalTime;
            bool sortAscending = false;
            List<int> parentsCacheList = new List<int>();
            List<int> childrenCacheList = new List<int>();

            int recordCount = 0;
            for (int index = firstIndex; index <= lastIndex; ++index) {
                int ix = index - firstIndex;
                float triangle = 0;
                if (ix >= 0 && ix < triangles.Length) {
                    triangle = triangles[ix];
                }
                float batch = 0;
                if (ix >= 0 && ix < batches.Length) {
                    batch = batches[ix];
                }
                if (RecordInstrumentFrame(index, viewMode, sortColumn, sortAscending, triangle, batch, parentsCacheList, childrenCacheList, threads)) {
                    ++recordCount;
                }
                if (DisplayCancelableProgressBar("记录进度", recordCount, lastIndex - firstIndex + 1)) {
                    break;
                }
            }
            EditorUtility.ClearProgressBar();

            if (lastIndex >= 1) {
                --lastIndex;

                int ix = lastIndex - firstIndex;
                float triangle = 0;
                if (ix >= 0 && ix < triangles.Length) {
                    triangle = triangles[ix];
                }
                float batch = 0;
                if (ix >= 0 && ix < batches.Length) {
                    batch = batches[ix];
                }

                /*
                ProfilerProperty prop = new ProfilerProperty();
                prop.SetRoot(lastIndex, HierarchyFrameDataView.columnName, (int)ProfilerViewType.Hierarchy);
                prop.onlyShowGPUSamples = false;

                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("depth:{0}\tfps:{1}\tcpu time:{2}\tgpu time:{3} \ttriangles:{4} \tbatches:{5}", prop.depth, prop.frameFPS, prop.frameTime, prop.frameGpuTime, triangle, batch);
                sb.AppendLine();
                while (prop.Next(true)) {
                    sb.AppendFormat("{0}:{1}->{2}\t{3}\t{4}\t{5}\t{6}", prop.depth, prop.propertyName, prop.propertyPath, prop.GetColumn(HierarchyFrameDataView.columnCalls), prop.GetColumn(HierarchyFrameDataView.columnGcMemory), prop.GetColumn(HierarchyFrameDataView.columnSelfPercent), prop.GetColumn(HierarchyFrameDataView.columnSelfTime));
                    sb.AppendLine();
                }
                m_Text = sb.ToString();
                */

                //0--cpu 1--rendering
                StringBuilder sb = new StringBuilder();
                foreach (var pair in threads) {
                    var thread = pair.Value;
                    sb.AppendFormat("thead index:{0} id:{1} name:{2} group:{3}", thread.theadIndex, thread.threadId, thread.threadName, thread.threadGroup);
                    sb.AppendLine();
                }

                sb.AppendFormat("triangles:{0} batchs:{1}", triangle, batch);
                sb.AppendLine();

                m_Text = sb.ToString();
            }
        }
    }
    internal void SaveInstrumentInfo()
    {
        if (m_InstrumentInfos.Count > 0) {
            string fullpath = EditorPrefs.GetString(c_pref_key_save_instrument);
            bool noPath = string.IsNullOrEmpty(fullpath);
            string dir = noPath ? Application.dataPath : Path.GetDirectoryName(fullpath);
            string name = noPath ? "instrument" : Path.GetFileName(fullpath);
            string path = EditorUtility.SaveFilePanel("请指定要保存耗时信息的文件", dir, name, "txt");
            if (!string.IsNullOrEmpty(path)) {
                EditorPrefs.SetString(c_pref_key_save_instrument, path);

                if (File.Exists(path)) {
                    File.Delete(path);
                }
                using (StreamWriter sw = new StreamWriter(path)) {
                    sw.WriteLine("frame\tthread_index\tsample_count\tdepth\tname\tpath_or_group\tfps_or_calls\tgc\ttotal_time_or_cpu_time\ttotal_percent_or_gpu_time\tself_time_or_triangle\tself_percent_or_batch\tmarker_or_thread_id");
                    bool first = true;
                    int curCount = 0;
                    int totalCount = m_InstrumentInfos.Count;
                    foreach (var pair in m_InstrumentInfos) {
                        totalCount += pair.Value.records.Count;
                    }
                    foreach (var pair in m_InstrumentInfos) {
                        var info = pair.Value;
                        if (first) {
                            foreach (var tp in info.threads) {
                                var tinfo = tp.Value;
                                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                                    0, tinfo.theadIndex, 0, 0, tinfo.threadName, tinfo.threadGroup,
                                    0, 0, 0, 0, 0, 0,
                                    tinfo.threadId);
                            }
                            first = false;
                        }
                        sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                            info.frame, 0, 0, 0, "[frame]", "_",
                            info.fps, info.totalGcMemory, info.totalCpuTime, info.totalGpuTime, info.triangle, info.batch,
                            0);
                        ++curCount;
                        if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                            goto L_EndSaveIns;
                        }
                        foreach (var record in info.records) {
                            sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}",
                                record.frame, record.threadIndex, record.sampleCount, record.depth, record.name, record.layerPath,
                                record.calls, record.gcMemory, record.totalTime, record.totalPercent, record.selfTime, record.selfPercent,
                                record.markerId);
                            ++curCount;
                            if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                                goto L_EndSaveIns;
                            }
                        }
                    }
                L_EndSaveIns:
                    sw.Close();
                    EditorUtility.ClearProgressBar();
                }
            }
        }
        else {
            EditorUtility.DisplayDialog("错误", "没有记录耗时信息，请先记录！", "ok");
        }
    }
    internal void LoadInstrumentInfo()
    {
        string file = EditorPrefs.GetString(c_pref_key_load_instrument);
        string path = EditorUtility.OpenFilePanel("请指定要加载耗时信息的文件", string.IsNullOrEmpty(file) ? string.Empty : Path.GetDirectoryName(file), "txt");
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            EditorPrefs.SetString(c_pref_key_load_instrument, path);

            int i = 0;
            try {
                var fi = new FileInfo(path);
                var len = fi.Length;
                long curCount = 0;
                long totalCount = len;

                m_InstrumentInfos.Clear();
                var threads = new SortedDictionary<int, ResourceEditUtility.InstrumentThreadInfo>();
                using (var reader = fi.OpenText()) {
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        curCount = reader.BaseStream.Position;
                        ++i;
                        if (i <= 1) {
                            continue;
                        }

                        var fields = line.Split('\t');
                        var frame = int.Parse(fields[0]);
                        var threadIndex = int.Parse(fields[1]);
                        var sampleCount = int.Parse(fields[2]);
                        var depth = int.Parse(fields[3]);
                        var name = fields[4];
                        var pathOrGroup = fields[5];
                        var callsOrFps = float.Parse(fields[6]);
                        var gc = float.Parse(fields[7]);
                        var totalTimeOrCpuTime = float.Parse(fields[8]);
                        var totalPercentOrGpuTime = float.Parse(fields[9]);
                        var selfTimeOrTriangle = float.Parse(fields[10]);
                        var selfPercentOrBatch = float.Parse(fields[11]);
                        var markerOrThreadId = int.Parse(fields[12]);

                        if (threadIndex == 0 && sampleCount == 0 && depth == 0 && markerOrThreadId == 0 && name == "[frame]") {
                            var info = new ResourceEditUtility.InstrumentInfo();
                            info.frame = frame;
                            info.fps = callsOrFps;
                            info.totalGcMemory = gc;
                            info.totalCpuTime = totalTimeOrCpuTime;
                            info.totalGpuTime = totalPercentOrGpuTime;
                            info.triangle = selfTimeOrTriangle;
                            info.batch = selfPercentOrBatch;

                            info.threads = threads;

                            m_InstrumentInfos[frame] = info;
                        }
                        else if (m_InstrumentInfos.TryGetValue(frame, out var info)) {
                            var record = new ResourceEditUtility.InstrumentRecord();
                            record.frame = frame;
                            record.threadIndex = threadIndex;
                            record.sampleCount = sampleCount;
                            record.depth = depth;
                            record.name = name;
                            record.layerPath = pathOrGroup;
                            record.gcMemory = gc;
                            record.calls = callsOrFps;
                            record.totalTime = totalTimeOrCpuTime;
                            record.totalPercent = totalPercentOrGpuTime;
                            record.selfTime = selfTimeOrTriangle;
                            record.selfPercent = selfPercentOrBatch;
                            record.markerId = markerOrThreadId;
                            info.records.Add(record);
                        }
                        else if (m_InstrumentInfos.Count == 0) {
                            var m = new ResourceEditUtility.InstrumentThreadInfo();
                            m.theadIndex = threadIndex;
                            m.threadId = (ulong)markerOrThreadId;
                            m.threadName = name;
                            m.threadGroup = pathOrGroup;
                            threads.Add(m.theadIndex, m);
                        }

                        if (DisplayCancelableProgressBar("加载进度", curCount, totalCount)) {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                EditorUtility.DisplayDialog("异常", string.Format("line {0} exception {1}\n{2}", i, ex.Message, ex.StackTrace), "ok");
            }
            EditorUtility.ClearProgressBar();
        }
    }
    internal void LoadUTraceCsv()
    {
        string file = EditorPrefs.GetString(c_pref_key_load_utracecsv);
        string path = EditorUtility.OpenFilePanel("请指定要加载utrace信息的CSV文件", string.IsNullOrEmpty(file) ? string.Empty : Path.GetDirectoryName(file), "csv");
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            EditorPrefs.SetString(c_pref_key_load_utracecsv, path);

            int ix = 0;
            try {
                m_uTraceFrames.Clear();
                var fi = new FileInfo(path);
                var len = fi.Length;
                long curCount = 0;
                long totalCount = len;

                var threads = new SortedDictionary<int, ResourceEditUtility.uTraceThreadInfo>();
                using (var reader = fi.OpenText()) {
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        curCount = reader.BaseStream.Position;
                        ++ix;
                        if (ix <= 1) {
                            continue;
                        }

                        var fields = line.Split(';');
                        if (fields.Length < 8) {
                            continue;
                        }
                        var frame = int.Parse(fields[0]);
                        var timelineIndex = int.Parse(fields[1]);
                        var threadId = int.Parse(fields[2]);
                        var depth = int.Parse(fields[3]);
                        var startTime = double.Parse(fields[4]);
                        var endTime = double.Parse(fields[5]);
                        var time = double.Parse(fields[6]);
                        var name = fields[7];

                        if (timelineIndex == 0 && threadId == 0 && depth == 0 && name == "[frame]") {
                            var info = new ResourceEditUtility.uTraceFrame { frame = frame, startTime = startTime, endTime = endTime, time = time };
                            info.threads = threads;
                            m_uTraceFrames[frame] = info;
                        }
                        else if (m_uTraceFrames.TryGetValue(frame, out var info)) {
                            var timeline = new ResourceEditUtility.uTraceTimeline { frame = frame, timelineIndex = timelineIndex, threadId = threadId, depth = depth, startTime = startTime, endTime = endTime, time = time, name = name };
                            info.records.Add(timeline);
                        }
                        else if (m_uTraceFrames.Count == 0) {
                            if (!threads.TryGetValue(threadId, out var tinfo)) {
                                tinfo = new ResourceEditUtility.uTraceThreadInfo { timelineIndex = timelineIndex, theadId = threadId };
                                threads.Add(threadId, tinfo);
                            }
                            if (depth == 1) {
                                tinfo.threadGroup = name;
                            }
                            else if (depth == 2) {
                                tinfo.threadName = name;
                            }
                            continue;
                        }
                        if (DisplayCancelableProgressBar("加载进度", curCount, totalCount)) {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                EditorUtility.DisplayDialog("异常", string.Format("line {0} exception {1}\n{2}", ix, ex.Message, ex.StackTrace), "ok");
            }
            EditorUtility.ClearProgressBar();
        }
    }
    internal void SaveResult(List<ResourceEditUtility.ItemInfo> itemList, List<ResourceEditUtility.GroupInfo> groupList)
    {
        if (null == itemList && null == groupList) {
            itemList = m_ItemList;
            groupList = m_GroupList;
        }
        if (groupList.Count > 0 || itemList.Count > 0) {
            string fullpath = EditorPrefs.GetString(c_pref_key_save_result);
            bool noPath = string.IsNullOrEmpty(fullpath);
            string dir = noPath ? Application.dataPath : Path.GetDirectoryName(fullpath);
            string name = noPath ? "result" : Path.GetFileName(fullpath);
            string path = EditorUtility.SaveFilePanel("请指定要保存分析结果的文件", dir, name, "txt");
            if (!string.IsNullOrEmpty(path)) {
                EditorPrefs.SetString(c_pref_key_save_result, path);

                if (File.Exists(path)) {
                    File.Delete(path);
                }
                using (StreamWriter sw = new StreamWriter(path)) {
                    if (groupList.Count > 0) {
                        sw.WriteLine("asset_path\tscene_path\tinfo\torder\tvalue\tref\trefby\textra");
                        int curCount = 0;
                        int totalCount = groupList.Count;
                        foreach (var item in groupList) {
                            if (ResourceEditUtility.SaveResultWithXrefs && !string.IsNullOrEmpty(item.AssetPath)) {
                                HashSet<string> refs;
                                HashSet<string> refbys;
                                ResourceProcessor.Instance.ReferenceAssets.TryGetValue(item.AssetPath, out refs);
                                ResourceProcessor.Instance.ReferenceByAssets.TryGetValue(item.AssetPath, out refbys);
                                int refCt = 0;
                                if (null != refs)
                                    refCt = refs.Count;
                                int refByCt = 0;
                                if (null != refbys)
                                    refByCt = refbys.Count;
                                int extraCt = 0;
                                if (null != item.ExtraList)
                                    extraCt = item.ExtraList.Count;
                                int ct = Mathf.Max(refCt, refByCt, extraCt);
                                IEnumerator<string> refEnumer = null;
                                if (null != refs)
                                    refEnumer = refs.GetEnumerator();
                                IEnumerator<string> refByEnumer = null;
                                if (null != refbys)
                                    refByEnumer = refbys.GetEnumerator();
                                IEnumerator<KeyValuePair<string, BoxedValue>> extraEnumer = null;
                                if (null != item.ExtraList)
                                    extraEnumer = item.ExtraList.GetEnumerator();
                                if (ct == 0) {
                                    sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                                }
                                else {
                                    for (int i = 0; i < ct; ++i) {
                                        string refAsset = string.Empty;
                                        string refByAsset = string.Empty;
                                        string extra = string.Empty;
                                        if (i < refCt && refEnumer.MoveNext()) {
                                            refAsset = refEnumer.Current;
                                        }
                                        if (i < refByCt && refByEnumer.MoveNext()) {
                                            refByAsset = refByEnumer.Current;
                                        }
                                        if (i < extraCt && extraEnumer.MoveNext()) {
                                            extra = extraEnumer.Current.Key;
                                        }
                                        sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value, refAsset, refByAsset, extra);
                                    }
                                }
                            }
                            else {
                                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                            }
                            ++curCount;
                            if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                                break;
                            }
                        }
                        sw.Close();
                    }
                    else {
                        sw.WriteLine("asset_path\tscene_path\tinfo\torder\tvalue\tref\trefby\textra");
                        int curCount = 0;
                        int totalCount = itemList.Count;
                        foreach (var item in itemList) {
                            if (ResourceEditUtility.SaveResultWithXrefs && !string.IsNullOrEmpty(item.AssetPath)) {
                                HashSet<string> refs;
                                HashSet<string> refbys;
                                ResourceProcessor.Instance.ReferenceAssets.TryGetValue(item.AssetPath, out refs);
                                ResourceProcessor.Instance.ReferenceByAssets.TryGetValue(item.AssetPath, out refbys);
                                int refCt = 0;
                                if (null != refs)
                                    refCt = refs.Count;
                                int refByCt = 0;
                                if (null != refbys)
                                    refByCt = refbys.Count;
                                int extraCt = 0;
                                if (null != item.ExtraList)
                                    extraCt = item.ExtraList.Count;
                                int ct = Mathf.Max(refCt, refByCt, extraCt);
                                IEnumerator<string> refEnumer = null;
                                if (null != refs)
                                    refEnumer = refs.GetEnumerator();
                                IEnumerator<string> refByEnumer = null;
                                if (null != refbys)
                                    refByEnumer = refbys.GetEnumerator();
                                IEnumerator<KeyValuePair<string, BoxedValue>> extraEnumer = null;
                                if (null != item.ExtraList)
                                    extraEnumer = item.ExtraList.GetEnumerator();
                                if (ct == 0) {
                                    sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                                }
                                else {
                                    for (int i = 0; i < ct; ++i) {
                                        string refAsset = string.Empty;
                                        string refByAsset = string.Empty;
                                        string extra = string.Empty;
                                        if (i < refCt && refEnumer.MoveNext()) {
                                            refAsset = refEnumer.Current;
                                        }
                                        if (i < refByCt && refByEnumer.MoveNext()) {
                                            refByAsset = refByEnumer.Current;
                                        }
                                        if (i < extraCt && extraEnumer.MoveNext()) {
                                            extra = extraEnumer.Current.Key;
                                        }
                                        sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value, refAsset, refByAsset, extra);
                                    }
                                }
                            }
                            else {
                                sw.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t\t\t", item.AssetPath, item.ScenePath, item.Info, item.Order, item.Value);
                            }
                            ++curCount;
                            if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                                break;
                            }
                        }
                        sw.Close();
                    }
                    EditorUtility.ClearProgressBar();
                }
                string infoPath = Path.Combine(Path.GetDirectoryName(path), "n_" + Path.GetFileNameWithoutExtension(path) + ".csv");
                using (StreamWriter sw = new StreamWriter(infoPath)) {
                    if (groupList.Count > 0) {
                        int curCount = 0;
                        int totalCount = groupList.Count;
                        foreach (var item in groupList) {
                            sw.WriteLine(item.Info);
                            ++curCount;
                            if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                                break;
                            }
                        }
                        sw.Close();
                    }
                    else {
                        int curCount = 0;
                        int totalCount = itemList.Count;
                        foreach (var item in itemList) {
                            sw.WriteLine(item.Info);
                            ++curCount;
                            if (DisplayCancelableProgressBar("保存进度", curCount, totalCount)) {
                                break;
                            }
                        }
                        sw.Close();
                    }
                    EditorUtility.ClearProgressBar();
                }
            }
        }
        else {
            EditorUtility.DisplayDialog("错误", "没有分析结果信息，请先采集分析结果！", "ok");
        }
    }
    internal void LoadResult()
    {
        string file = EditorPrefs.GetString(c_pref_key_load_result);
        string path = EditorUtility.OpenFilePanel("请指定要加载分析结果的文件", string.IsNullOrEmpty(file) ? string.Empty : Path.GetDirectoryName(file), "txt");
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            EditorPrefs.SetString(c_pref_key_load_result, path);

            int i = 0;
            try {
                var fi = new FileInfo(path);
                var len = fi.Length;
                long curCount = 0;
                long totalCount = len;

                m_ItemList.Clear();
                ResourceEditUtility.ItemInfo lastItem = null;
                HashSet<string> refs = new HashSet<string>();
                HashSet<string> refbys = new HashSet<string>();
                List<KeyValuePair<string, BoxedValue>> extraList = new List<KeyValuePair<string, BoxedValue>>();
                using (var reader = fi.OpenText()) {
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        curCount = reader.BaseStream.Position;
                        ++i;
                        if (i <= 1) {
                            continue;
                        }

                        var fields = line.Split('\t');
                        var assetPath = fields[0];
                        var scenePath = fields[1];
                        var info = fields[2];
                        var order = double.Parse(fields[3]);
                        var value = double.Parse(fields[4]);
                        string refAsset = string.Empty;
                        string refByAsset = string.Empty;
                        string extraKeyVal = string.Empty;
                        if (fields.Length >= 8) {
                            refAsset = fields[5];
                            refByAsset = fields[6];
                            extraKeyVal = fields[7];
                            if (!string.IsNullOrEmpty(refAsset))
                                refs.Add(refAsset);
                            if (!string.IsNullOrEmpty(refByAsset))
                                refbys.Add(refByAsset);
                            if (!string.IsNullOrEmpty(extraKeyVal))
                                extraList.Add(new KeyValuePair<string, BoxedValue>(extraKeyVal, extraKeyVal));
                        }

                        if (null == lastItem || !lastItem.IsEqual(assetPath, scenePath, info, order, value)) {
                            var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Info = info, Order = order, Value = value };
                            if (refs.Count > 0 && !ResourceProcessor.Instance.ReferenceAssets.ContainsKey(assetPath)) {
                                ResourceProcessor.Instance.ReferenceAssets.Add(assetPath, refs);
                            }
                            if (refbys.Count > 0 && !ResourceProcessor.Instance.ReferenceByAssets.ContainsKey(assetPath)) {
                                ResourceProcessor.Instance.ReferenceByAssets.Add(assetPath, refbys);
                            }
                            if (extraList.Count > 0) {
                                item.ExtraList = extraList;
                            }

                            refs = new HashSet<string>();
                            refbys = new HashSet<string>();
                            extraList = new List<KeyValuePair<string, BoxedValue>>();

                            m_ItemList.Add(item);
                            lastItem = item;
                        }

                        if (DisplayCancelableProgressBar("加载进度", curCount, totalCount)) {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex) {
                EditorUtility.DisplayDialog("异常", string.Format("line {0} exception {1}\n{2}", i, ex.Message, ex.StackTrace), "ok");
            }
            EditorUtility.ClearProgressBar();
        }
    }

    internal void ClearDsl()
    {
        CacheParams();

        m_CanRefresh = false;
        m_DslFile = null;
        m_SearchSource = string.Empty;
        m_AssetProcessors.Clear();
        m_TypeOrExtList.Clear();
        m_TypeList.Clear();
        m_ParamNames.Clear();
        m_Params.Clear();
        m_Text = null;
        m_NextFilterIndex = 0;
        m_NextGroupIndex = 0;
        m_NextProcessIndex = 0;
        m_FilterCalculator = null;
        m_GroupCalculator = null;
        m_ProcessCalculator = null;

        m_ItemList.Clear();
        m_GroupList.Clear();
        ResourceEditUtility.ResetCommandCalculator();
    }
    internal void SelectDsl(string path)
    {
        ClearDsl();
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            Dsl.DslFile file = new Dsl.DslFile();
            if (file.Load(path, (string msg) => { Debug.LogError(msg); })) {
                m_DslPath = path;
                m_DslFile = file;
                m_NextFilterIndex = 0;
                m_NextGroupIndex = 0;
                m_NextProcessIndex = 0;
                m_AssetProcessors.Clear();
                m_ScriptCalculator = new DslCalculator();
                m_FilterCalculator = new DslCalculator();
                m_GroupCalculator = new DslCalculator();
                m_ProcessCalculator = new DslCalculator();
                ResourceEditUtility.InitCalculator(m_ScriptCalculator);
                ResourceEditUtility.InitCalculator(m_FilterCalculator);
                ResourceEditUtility.InitCalculator(m_GroupCalculator);
                ResourceEditUtility.InitCalculator(m_ProcessCalculator);

                bool haveError = false;
                foreach (var syntaxComponent in file.DslInfos) {
                    bool check = false;
                    var func = syntaxComponent as Dsl.FunctionData;
                    var info = syntaxComponent as Dsl.StatementData;
                    if (null == func && null != info) {
                        func = info.First.AsFunction;
                    }
                    int num = null != info ? info.GetFunctionNum() : 1;
                    if (num == 1) {
                        var id = func.GetId();
                        if (id == "script") {
                            check = true;
                            m_ScriptCalculator.LoadDsl(func);
                        }
                    }
                    else if (num == 2) {
                        string firstId = info.First.GetId();
                        string secondId = info.Second.GetId();
                        if (firstId == "script" && secondId == "args") {
                            check = true;
                            m_ScriptCalculator.LoadDsl(info);
                        }
                        else if (firstId == "input" && (secondId == "filter" || secondId == "process" || secondId == "assetprocessor")) {
                            check = true;

                            if (secondId == "filter") {
                                m_FilterCalculator.LoadDsl(m_NextFilterIndex.ToString(), info.Second.AsFunction);
                                ++m_NextFilterIndex;
                            }
                            else if (secondId == "process") {
                                m_ProcessCalculator.LoadDsl(m_NextProcessIndex.ToString(), info.Second.AsFunction);
                                ++m_NextProcessIndex;
                            }
                            else {
                                ParseAssetProcessor(info.Second.AsFunction);
                            }
                        }
                    }
                    else if (num == 3) {
                        string firstId = info.First.GetId();
                        string secondId = info.Second.GetId();
                        string thirdId = info.Last.GetId();
                        if (firstId == "input" && secondId == "filter" && (thirdId == "group" || thirdId == "process" || thirdId == "assetprocessor")) {
                            check = true;

                            m_FilterCalculator.LoadDsl(m_NextFilterIndex.ToString(), info.Second.AsFunction);
                            ++m_NextFilterIndex;
                            if (thirdId == "group") {
                                m_GroupCalculator.LoadDsl(m_NextGroupIndex.ToString(), info.Last.AsFunction);
                                ++m_NextGroupIndex;
                            }
                            else if (thirdId == "process") {
                                m_ProcessCalculator.LoadDsl(m_NextProcessIndex.ToString(), info.Last.AsFunction);
                                ++m_NextProcessIndex;
                            }
                            else {
                                ParseAssetProcessor(info.Last.AsFunction);
                            }
                        }
                    }
                    else if (num == 4) {
                        string firstId = info.First.GetId();
                        string secondId = info.Second.GetId();
                        string thirdId = info.Functions[2].GetId();
                        string fourthId = info.Last.GetId();

                        if (firstId == "input" && secondId == "filter" && thirdId == "group" && (fourthId == "process" || fourthId == "assetprocessor")) {
                            check = true;

                            m_FilterCalculator.LoadDsl(m_NextFilterIndex.ToString(), info.Second.AsFunction);
                            ++m_NextFilterIndex;
                            m_GroupCalculator.LoadDsl(m_NextGroupIndex.ToString(), info.Functions[2].AsFunction);
                            ++m_NextGroupIndex;
                            if (fourthId == "process") {
                                m_ProcessCalculator.LoadDsl(m_NextProcessIndex.ToString(), info.Last.AsFunction);
                                ++m_NextProcessIndex;
                            }
                            else {
                                ParseAssetProcessor(info.Last.AsFunction);
                            }
                        }
                    }
                    if (!check) {
                        EditorUtility.DisplayDialog("错误", string.Format("error script:{0}, must be input(exts){{...}}filter{{...}}; or input(exts){{...}}process{{...}}; or input(exts){{...}}filter{{...}}process{{...}}; or input(exts){{...}}filter{{...}}group{{...}}; or input(exts){{...}}filter{{...}}group{{...}}process{{...}}; or script(func){{...}};", info.GetLine()), "ok");
                        haveError = true;
                    }
                }
                if (!haveError) {
                    m_SearchSource = string.Empty;
                    m_TypeOrExtList.Clear();
                    m_TypeList.Clear();
                    m_ParamNames.Clear();
                    m_Params.Clear();

                    foreach (var syntaxComponent in m_DslFile.DslInfos) {
                        var func = syntaxComponent as Dsl.FunctionData;
                        var info = syntaxComponent as Dsl.StatementData;
                        if (null == func && null != info) {
                            func = info.First.AsFunction;
                        }
                        int num = null != info ? info.GetFunctionNum() : 1;
                        if (num == 1) {
                            var id = func.GetId();
                            if (id == "script") {
                                continue;
                            }
                        }
                        else if (num == 2) {
                            string firstId = info.First.GetId();
                            string secondId = info.Second.GetId();
                            if (firstId == "script" && secondId == "args") {
                                continue;
                            }
                        }
                        var first = info.First.AsFunction;
                        var input = first;
                        if (first.IsHighOrder)
                            input = first.LowerOrderFunction;
                        foreach (var param in input.Params) {
                            string ext = param.GetId();
                            m_TypeOrExtList.Add(ext);
                        }
                        foreach (var comp in first.Params) {
                            var funcData = comp as Dsl.FunctionData;
                            if (null != funcData) {
                                if (funcData.HaveStatement()) {
                                    ParseFunctionData(funcData);
                                }
                                else if (funcData.HaveParam()) {
                                    ParseCallData(funcData);
                                }
                            }
                        }
                    }
                    m_Text = File.ReadAllText(path);
                    RestoreParams();
                }
                else {
                    m_DslFile = null;
                    m_SearchSource = string.Empty;
                    m_TypeOrExtList.Clear();
                    m_TypeList.Clear();
                    m_ParamNames.Clear();
                    m_Params.Clear();
                    m_Text = null;
                    m_NextFilterIndex = 0;
                    m_NextGroupIndex = 0;
                    m_NextProcessIndex = 0;
                    m_FilterCalculator = null;
                    m_GroupCalculator = null;
                    m_ProcessCalculator = null;
                }
            }
        }
    }
    internal string ParseCallData(Dsl.FunctionData callData)
    {
        string id = callData.GetId();
        string key = callData.GetParamId(0);
        string val = callData.GetParamId(1);
        int num = callData.GetParamNum();
        string caption = num > 2 ? callData.GetParamId(2) : (id == "label" ? val : key);
        string tooltip = num > 3 ? callData.GetParamId(3) : caption;
        if (id == "int") {
            //int(name, val);
            int v = int.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(int), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "uint") {
            //uint(name, val);
            uint v = uint.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(uint), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "long") {
            //long(name, val);
            long v = long.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(long), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "ulong") {
            //ulong(name, val);
            ulong v = ulong.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(ulong), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "float") {
            //float(name, val);
            float v = float.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(float), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "double") {
            //double(name, val);
            double v = double.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(double), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "string") {
            //string(name, val);
            string v = val;
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(string), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "intlist") {
            //intlist(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<int>();
            foreach (var str in v) {
                int iv;
                int.TryParse(str, out iv);
                list.Add(iv);
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<int>), Value = BoxedValue.FromObject(list), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "uintlist") {
            //uintlist(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<uint>();
            foreach (var str in v) {
                uint iv;
                uint.TryParse(str, out iv);
                list.Add(iv);
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<uint>), Value = BoxedValue.FromObject(list), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "longlist") {
            //longlist(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<long>();
            foreach (var str in v) {
                long iv;
                long.TryParse(str, out iv);
                list.Add(iv);
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<long>), Value = BoxedValue.FromObject(list), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "ulonglist") {
            //ulonglist(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<ulong>();
            foreach (var str in v) {
                ulong iv;
                ulong.TryParse(str, out iv);
                list.Add(iv);
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<ulong>), Value = BoxedValue.FromObject(list), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "floatlist") {
            //floatlist(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<float>();
            foreach (var str in v) {
                float fv;
                float.TryParse(str, out fv);
                list.Add(fv);
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<float>), Value = BoxedValue.FromObject(list), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "doublelist") {
            //doublelist(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<double>();
            foreach (var str in v) {
                double fv;
                double.TryParse(str, out fv);
                list.Add(fv);
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<double>), Value = BoxedValue.FromObject(list), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "stringlist") {
            //stringlist(name, val);
            var v = val.Split(s_StringListSeps, StringSplitOptions.RemoveEmptyEntries);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(List<string>), Value = BoxedValue.FromObject(v), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "inthash") {
            //inthash(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<int>();
            foreach (var str in v) {
                int iv;
                int.TryParse(str, out iv);
                if (!hash.Contains(iv)) {
                    hash.Add(iv);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<int>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "uinthash") {
            //uinthash(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<uint>();
            foreach (var str in v) {
                uint iv;
                uint.TryParse(str, out iv);
                if (!hash.Contains(iv)) {
                    hash.Add(iv);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<uint>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "longhash") {
            //longhash(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<long>();
            foreach (var str in v) {
                long iv;
                long.TryParse(str, out iv);
                if (!hash.Contains(iv)) {
                    hash.Add(iv);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<long>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "ulonghash") {
            //ulonghash(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<ulong>();
            foreach (var str in v) {
                ulong iv;
                ulong.TryParse(str, out iv);
                if (!hash.Contains(iv)) {
                    hash.Add(iv);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<ulong>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "floathash") {
            //floathash(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<float>();
            foreach (var str in v) {
                float fv;
                float.TryParse(str, out fv);
                if (!hash.Contains(fv)) {
                    hash.Add(fv);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<float>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "doublehash") {
            //doublehash(name, val);
            var v = val.Split(s_NumberListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<double>();
            foreach (var str in v) {
                double fv;
                double.TryParse(str, out fv);
                if (!hash.Contains(fv)) {
                    hash.Add(fv);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<double>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "stringhash") {
            //stringhash(name, val);
            var v = val.Split(s_StringListSeps, StringSplitOptions.RemoveEmptyEntries);
            var hash = new HashSet<string>();
            foreach (var str in v) {
                if (!hash.Contains(str)) {
                    hash.Add(str);
                }
            }
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(HashSet<string>), Value = BoxedValue.FromObject(hash), StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "bool") {
            //bool(name, val);
            bool v = bool.Parse(val);
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(bool), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "label") {
            //label(name, val);
            string v = val;
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(UnityEngine.UIElements.Label), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "button") {
            //button(name, val);
            string v = val;
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(UnityEngine.GUIContent), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "table") {
            //table(name, val);
            string v = val;
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(ResourceEditUtility.DataTable), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "excel") {
            //excel(name, val);
            string v = val;
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(NPOI.SS.UserModel.IWorkbook), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "script") {
            //script(name, val);
            string v = val;
            m_Params[key] = new ResourceEditUtility.ParamInfo { Name = key, Type = typeof(object), Value = v, StringValue = val, Caption = caption, Tooltip = tooltip };
            m_ParamNames.Add(key);
        }
        else if (id == "feature") {
            if (key == "menu") {
                m_DslMenu = val;
            }
            else if (key == "description") {
                m_DslDescription = val;
            }
            else if (key == "itemcommand") {
                m_DefaultItemCommand = val;
            }
            else if (key == "groupcommand") {
                m_DefaultGroupCommand = val;
            }
            else if (key == "source") {
                //feature("source", "script" or "list" or "excel" or "table" or "project" or "sceneobjects" or "scenecomponents" or "sceneassets" or "allassets" or "runtimeobjects" or "unusedassets" or "assetbundle");
                m_SearchSource = val;
            }
        }
        return key;
    }
    internal void ParseFunctionData(Dsl.FunctionData funcData)
    {
        var callData = funcData;
        if (funcData.IsHighOrder)
            callData = funcData.LowerOrderFunction;
        string name = ParseCallData(callData);
        ResourceEditUtility.ParamInfo info;
        if (m_Params.TryGetValue(name, out info)) {
            foreach (var comp in funcData.Params) {
                var id = comp.GetId();
                var cd = comp as Dsl.FunctionData;
                if (null != cd) {
                    if (id == "script") {
                        info.Script = cd.GetParamId(0);
                    }
                    else if (id == "file") {
                        int num = cd.GetParamNum();
                        if (num > 0) {
                            var p1 = cd.GetParamId(0);
                            info.FileExts = p1;
                        }
                        if (num > 1) {
                            var p2 = cd.GetParamId(0);
                            info.FileInitDir = p2;
                        }
                    }
                    else if (id == "encoding") {
                        info.Encoding = cd.GetParamId(0);
                    }
                    else if (id == "range") {
                        int num = cd.GetParamNum();
                        var p1 = cd.GetParam(0);
                        var p2 = cd.GetParam(1);
                        var pvd1 = p1 as Dsl.ValueData;
                        var pvd2 = p2 as Dsl.ValueData;
                        if (null != pvd1 && null != pvd2) {
                            //range(min,max);
                            string min = pvd1.GetId();
                            string max = pvd2.GetId();
                            if (info.Type == typeof(int) || info.Type == typeof(uint) || info.Type == typeof(long) || info.Type == typeof(ulong)) {
                                info.MinValue = int.Parse(min);
                                info.MaxValue = int.Parse(max);
                            }
                            else if (info.Type == typeof(float) || info.Type == typeof(double)) {
                                info.MinValue = float.Parse(min);
                                info.MaxValue = float.Parse(max);
                            }
                            else if (info.Type == typeof(string)) {
                                info.MinValue = min;
                                info.MaxValue = max;
                            }
                            else if (info.Type == typeof(bool)) {
                                info.MinValue = bool.Parse(min);
                                info.MaxValue = bool.Parse(max);
                            }
                        }
                    }
                    else if (id == "popup" || id == "toggle" || id == "multiple" || id == "param") {
                        if (string.IsNullOrEmpty(info.OptionStyle)) {
                            info.OptionStyle = id;
                        }
                        else if (info.OptionStyle != id) {
                            EditorUtility.DisplayDialog("错误", string.Format("param's option must use same style, {0} will use {1} style (dont use {2}) !", info.Name, info.OptionStyle, id), "ok");
                        }

                        int num = cd.GetParamNum();
                        var p1 = cd.GetParam(0);
                        var p2 = cd.GetParam(1);
                        var pvd1 = p1 as Dsl.ValueData;
                        var pvd2 = p2 as Dsl.ValueData;
                        if (null != pvd1 && null != pvd2) {
                            //xxx(key,val);
                            string key = pvd1.GetId();
                            string val = pvd2.GetId();
                            info.Options[key] = val;
                            info.OptionNames.Add(key);
                        }
                        else {
                            var pcd1 = p1 as Dsl.FunctionData;
                            var pcd2 = p2 as Dsl.FunctionData;
                            if (1 == num && null != pcd1) {
                                if (pcd1.GetParamClass() == (int)Dsl.ParamClassEnum.PARAM_CLASS_OPERATOR && pcd1.GetId() == "..") {
                                    //xxx(min..max);
                                    int min = int.Parse(pcd1.GetParamId(0));
                                    int max = int.Parse(pcd1.GetParamId(1));
                                    for (int v = min; v <= max; ++v) {
                                        string kStr = v.ToString("D3");
                                        string vStr = v.ToString();
                                        info.Options[kStr] = vStr;
                                        info.OptionNames.Add(kStr);
                                    }
                                }
                                else if (pcd1.GetParamClass() == (int)Dsl.ParamClassEnum.PARAM_CLASS_BRACKET && !pcd1.HaveId()) {
                                    //xxx([v1,v2,v3,v4,v5]);
                                    for (int i = 0; i < pcd1.GetParamNum(); ++i) {
                                        var vStr = pcd1.GetParamId(i);
                                        info.Options[vStr] = vStr;
                                        info.OptionNames.Add(vStr);
                                    }
                                }
                            }
                            else if (2 == num) {
                                if (null != pcd1 && null != pcd2) {
                                    if (pcd1.GetParamClass() == (int)Dsl.ParamClassEnum.PARAM_CLASS_BRACKET && !pcd1.HaveId() && pcd2.GetParamClass() == (int)Dsl.ParamClassEnum.PARAM_CLASS_BRACKET && !pcd2.HaveId()) {
                                        //xxx([k1,k2,k3,k4,k5],[v1,v2,v3,v4,v5]);
                                        int num1 = pcd1.GetParamNum();
                                        int num2 = pcd2.GetParamNum();
                                        int n = num1 <= num2 ? num1 : num2;
                                        for (int i = 0; i < n; ++i) {
                                            var kStr = pcd1.GetParamId(i);
                                            var vStr = pcd2.GetParamId(i);
                                            info.Options[kStr] = vStr;
                                            info.OptionNames.Add(kStr);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    var vd = comp as Dsl.ValueData;
                    if (null != vd) {
                        //用于支持标签属性
                        info.OptionStyle = vd.GetId();
                    }
                }
            }
        }
    }
    internal void ParseAssetProcessor(Dsl.FunctionData funcData)
    {
        foreach (var comp in funcData.Params) {
            string processor = comp.GetId();
            if (!string.IsNullOrEmpty(processor)) {
                m_AssetProcessors.Add(processor);
            }
        }
    }
    internal List<BoxedValue> NewCalculatorValueList()
    {
        return m_ScriptCalculator.NewCalculatorValueList();
    }
    internal void RecycleCalculatorValueList(List<BoxedValue> list)
    {
        m_ScriptCalculator.RecycleCalculatorValueList(list);
    }
    public BoxedValue CallScript(DslCalculator calc, string name)
    {
        var args = NewCalculatorValueList();
        var r = CallScript(calc, name, args);
        RecycleCalculatorValueList(args);
        return r;
    }
    public BoxedValue CallScript(DslCalculator calc, string name, BoxedValue arg1)
    {
        var args = NewCalculatorValueList();
        args.Add(arg1);
        var r = CallScript(calc, name, args);
        RecycleCalculatorValueList(args);
        return r;
    }
    public BoxedValue CallScript(DslCalculator calc, string name, BoxedValue arg1, BoxedValue arg2)
    {
        var args = NewCalculatorValueList();
        args.Add(arg1);
        args.Add(arg2);
        var r = CallScript(calc, name, args);
        RecycleCalculatorValueList(args);
        return r;
    }
    public BoxedValue CallScript(DslCalculator calc, string name, BoxedValue arg1, BoxedValue arg2, BoxedValue arg3)
    {
        var args = NewCalculatorValueList();
        args.Add(arg1);
        args.Add(arg2);
        args.Add(arg3);
        var r = CallScript(calc, name, args);
        RecycleCalculatorValueList(args);
        return r;
    }
    internal BoxedValue CallScript(DslCalculator calc, string name, List<BoxedValue> args)
    {
        if (null != m_ScriptCalculator) {
            if (null != calc) {
                foreach (var key in calc.GlobalVariableNames) {
                    m_ScriptCalculator.SetGlobalVariable(key, calc.GetGlobalVariable(key));
                }
            }
            else {
                m_ScriptCalculator.SetGlobalVariable("params", BoxedValue.FromObject(m_Params));
                foreach (var pair in m_Params) {
                    m_ScriptCalculator.SetGlobalVariable(pair.Key, pair.Value.Value);
                }
            }
            var ret = m_ScriptCalculator.Calc(name, args);
            if (null != calc) {
                foreach (var key in m_ScriptCalculator.GlobalVariableNames) {
                    calc.SetGlobalVariable(key, m_ScriptCalculator.GetGlobalVariable(key));
                }
            }
            return ret;
        }
        else {
            return BoxedValue.NullObject;
        }
    }
    internal void Collect()
    {
        m_CollectPath = string.Empty;
        Refresh();
    }
    internal void Refresh()
    {
        m_CanRefresh = true;
        m_OverridedProgressTitle = string.Empty;
        Refresh(false);
    }
    internal void Refresh(bool isBatch)
    {
        foreach (var pair in m_Params) {
            var paramInfo = pair.Value;
            if (paramInfo.Type == typeof(ResourceEditUtility.DataTable) && paramInfo.Value.IsString) {
                var file = paramInfo.Value.AsString;
                var ext = Path.GetExtension(file);
                var table = new ResourceEditUtility.DataTable();
                table.Load(file, Encoding.GetEncoding(paramInfo.Encoding), ext == ".csv" ? ',' : '\t');
                paramInfo.Value = BoxedValue.FromObject(table);
            }
            else if (paramInfo.Type == typeof(NPOI.SS.UserModel.IWorkbook) && paramInfo.Value.IsString) {
                var file = paramInfo.Value.AsString;
                var ext = Path.GetExtension(file);
                NPOI.SS.UserModel.IWorkbook book = null;
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (ext == ".xls") {
                        book = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
                    }
                    else {
                        book = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
                    }
                }
                paramInfo.Value = BoxedValue.FromObject(book);
            }
            else if (paramInfo.Type == typeof(object)) {
                var funcName = paramInfo.StringValue;
                paramInfo.Value = CallScript(null, funcName, BoxedValue.FromObject(paramInfo));
            }
        }
        if (m_SearchSource == "script") {
            m_ItemList.Clear();
            SearchScriptResult();
            EditorUtility.ClearProgressBar();
        }
        else if (m_SearchSource == "list") {
            m_ItemList.Clear();
            if (m_TypeOrExtList.Count > 0) {
                SearchList();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "sceneobjects") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = CountSceneObjects();
            if (m_TotalSearchCount > 0) {
                SearchSceneObjects();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "scenecomponents") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = CountSceneObjects();
            if (m_TotalSearchCount > 0) {
                SearchSceneComponents();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "sceneassets") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            CountSceneAssets();
            if (m_TotalSearchCount > 0) {
                SearchSceneAssets();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "allassets") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            CountAllAssets();
            if (m_TotalSearchCount > 0) {
                SearchAllAssets();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "runtimeobjects") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            CountRuntimeObjects();
            if (m_TotalSearchCount > 0) {
                SearchRuntimeObjects();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "excel") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            CountExcelRecords();
            if (m_TotalSearchCount > 0) {
                SearchExcelRecords();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "table") {
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            CountTableRecords();
            if (m_TotalSearchCount > 0) {
                SearchTableRecords();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "unusedassets") {
            if (m_ReferenceAssets.Count <= 0 && m_ReferenceByAssets.Count <= 0 && m_UnusedAssets.Count <= 0) {
                if (!isBatch)
                    EditorUtility.DisplayDialog("错误", "未找到资源依赖信息，请先执行资源依赖‘分析’或‘加载’！", "ok");
                return;
            }
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            CountUnusedAssets();
            if (m_TotalSearchCount > 0) {
                SearchUnusedAssets();
                EditorUtility.ClearProgressBar();
            }
        }
        else if (m_SearchSource == "assetbundle") {
            SearchAssetBundle();
            EditorUtility.ClearProgressBar();
        }
        else if (m_SearchSource == "snapshot") {
            SearchSnapshot(isBatch);
            EditorUtility.ClearProgressBar();
        }
        else if (m_SearchSource == "instruments") {
            if (m_InstrumentInfos.Count <= 0) {
                if (!isBatch)
                    EditorUtility.DisplayDialog("错误", "未找到耗时信息，请先执行耗时‘记录’或‘加载’！", "ok");
                return;
            }
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            SearchInstruments();
            EditorUtility.ClearProgressBar();
        }
        else if (m_SearchSource == "utrace") {
            if (m_uTraceFrames.Count <= 0) {
                if (!isBatch)
                    EditorUtility.DisplayDialog("错误", "未找到utrace信息，请先执行‘uTraceCsv’！", "ok");
                return;
            }
            m_ItemList.Clear();
            m_CurSearchCount = 0;
            m_TotalSearchCount = 0;
            SearchUTrace();
            EditorUtility.ClearProgressBar();
        }
        else {
            if (string.IsNullOrEmpty(m_CollectPath)) {
                string fullpath = EditorPrefs.GetString(c_pref_key_open_asset_folder);
                bool noPath = string.IsNullOrEmpty(fullpath);
                string dir = noPath ? Application.dataPath : Path.GetDirectoryName(fullpath);
                string name = noPath ? string.Empty : Path.GetFileName(fullpath);
                string path = EditorUtility.OpenFolderPanel("请选择要收集资源的根目录", dir, name);
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) {
                    if (IsAssetPath(path)) {
                        EditorPrefs.SetString(c_pref_key_open_asset_folder, path);
                        m_CollectPath = path;
                    }
                    else {
                        if (!isBatch)
                            EditorUtility.DisplayDialog("错误", "必须选择本unity工程的资源路径！", "确定");
                    }
                }
            }
            if (!string.IsNullOrEmpty(m_CollectPath)) {
                m_ItemList.Clear();
                m_CurSearchCount = 0;
                m_TotalSearchCount = 0;
                CountFiles(m_CollectPath);
                if (m_TotalSearchCount > 0) {
                    SearchFiles(m_CollectPath);
                    EditorUtility.ClearProgressBar();
                }
            }
        }
        CalcGroupValue(string.Empty);
        CalcTotalValue();
    }
    internal void ReGroup(string itemCommand, string groupCommand)
    {
        if (!string.IsNullOrEmpty(itemCommand) && ResourceEditUtility.LoadCommand(itemCommand, ResourceProcessor.Instance.Params, new Dictionary<string, BoxedValue> { { "@context", BoxedValue.FromObject(m_ItemList) } })) {
            foreach (var item in m_ItemList) {
                ResourceEditUtility.EvalCommand(item.ExtraObject, BoxedValue.FromObject(item));
            }
        }
        CalcGroupValue(groupCommand);
        CalcTotalValue();
    }
    internal void CalcGroupValue(string groupCommand)
    {
        m_IsItemListGroupStyle = true;
        ResourceEditUtility.ParamInfo paramInfo;
        if (m_Params.TryGetValue("style", out paramInfo)) {
            string str = paramInfo.Value.AsString;
            if (str == "itemlist") {
                m_IsItemListGroupStyle = true;
            }
            else if (str == "grouplist") {
                m_IsItemListGroupStyle = false;
            }
        }
        var groups = new Dictionary<string, ResourceEditUtility.GroupInfo>();
        foreach (var item in m_ItemList) {
            string group = item.Group;
            if (null != group) {
                ResourceEditUtility.GroupInfo groupInfo;
                if (groups.TryGetValue(group, out groupInfo)) {
                    groupInfo.Items.Add(item);
                    groupInfo.Sum += item.Value;
                    if (groupInfo.Max < item.Value)
                        groupInfo.Max = item.Value;
                    if (groupInfo.Min > item.Value)
                        groupInfo.Min = item.Value;
                }
                else {
                    groupInfo = new ResourceEditUtility.GroupInfo();
                    groupInfo.Group = group;
                    groupInfo.Items.Add(item);
                    groupInfo.Sum = item.Value;
                    groupInfo.Max = item.Value;
                    groupInfo.Min = item.Value;
                    groupInfo.Selected = false;
                    groups.Add(group, groupInfo);
                }
            }
            else {
                groups.Clear();
                break;
            }
        }
        m_UnfilteredGroupCount = groups.Count;
        bool hasGroupCommand = false;
        if (!string.IsNullOrEmpty(groupCommand) && ResourceEditUtility.LoadCommand(groupCommand, ResourceProcessor.Instance.Params, new Dictionary<string, BoxedValue> { { "@context", BoxedValue.FromObject(groups) } })) {
            hasGroupCommand = true;
        }
        m_GroupList.Clear();
        int curCt = 0;
        int totalCt = m_UnfilteredGroupCount;
        foreach (var pair in groups) {
            ++curCt;
            var group = pair.Value;
            if (m_IsItemListGroupStyle) {
                group.PrepareShowInfo();
                foreach (var item in group.Items) {
                    var itemGroup = new ResourceEditUtility.GroupInfo();
                    itemGroup.Items = group.Items;
                    itemGroup.CopyFrom(group);

                    itemGroup.AssetPath = item.AssetPath;
                    itemGroup.ScenePath = item.ScenePath;
                    itemGroup.Info = item.Info;

                    itemGroup.Order = item.Order;
                    itemGroup.Value = item.Value;
                    itemGroup.Selected = false;
                    BoxedValue ret;
                    if (hasGroupCommand) {
                        ret = ResourceEditUtility.EvalCommand(item.ExtraObject, BoxedValue.FromObject(itemGroup));
                    }
                    else {
                        ret = ResourceEditUtility.Group(itemGroup, m_GroupCalculator, m_NextGroupIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                    }
                    if (!hasGroupCommand && m_NextGroupIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                        m_GroupList.Add(itemGroup);
                    }
                }
            }
            else {
                group.PrepareShowInfo();
                BoxedValue ret;
                if (hasGroupCommand) {
                    ret = ResourceEditUtility.EvalCommand(group.ExtraObject, BoxedValue.FromObject(group));
                }
                else {
                    ret = ResourceEditUtility.Group(group, m_GroupCalculator, m_NextGroupIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                }
                if (!hasGroupCommand && m_NextGroupIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                    m_GroupList.Add(group);
                }
            }
            if (DisplayCancelableProgressBar("分组进度", m_GroupList.Count, curCt, totalCt)) {
                break;
            }
        }
        EditorUtility.ClearProgressBar();
    }
    internal void CalcTotalValue()
    {
        m_NonZeroItemCount = 0;
        m_TotalItemValue = 0;
        if (m_UnfilteredGroupCount <= 0) {
            foreach (var item in m_ItemList) {
                m_TotalItemValue += item.Value;
                if (Math.Abs(item.Value) > double.Epsilon) {
                    ++m_NonZeroItemCount;
                }
            }
        }
        else {
            foreach (var item in m_GroupList) {
                m_TotalItemValue += item.Value;
                if (Math.Abs(item.Value) > double.Epsilon) {
                    ++m_NonZeroItemCount;
                }
            }
        }
    }
    internal void SelectAll()
    {
        if (m_UnfilteredGroupCount <= 0) {
            foreach (var item in m_ItemList) {
                item.Selected = true;
            }
        }
        else {
            foreach (var item in m_GroupList) {
                item.Selected = true;
            }
        }
    }
    internal int Process()
    {
        m_OverridedProgressTitle = string.Empty;
        return Process(false);
    }
    internal int Process(bool isBatch)
    {
        int ct = 0;
        if (null == m_DslFile) {
            if (!isBatch)
                EditorUtility.DisplayDialog("错误", "请先选择dsl !", "ok");
            return -1;
        }
        if ((null == m_ProcessCalculator || m_NextProcessIndex <= 0) && m_AssetProcessors.Count <= 0) {
            if (!isBatch)
                EditorUtility.DisplayDialog("错误", "当前dsl没有配置process或assetprocessor !", "ok");
            return -1;
        }
        if (!isBatch) {
            m_ProcessedAssets.Clear();
        }
        try {
            AssetDatabase.StartAssetEditing();

            int totalSelectedCount = 0;
            int index = 0;
            if (m_AssetProcessors.Count <= 0) {
                if (m_UnfilteredGroupCount <= 0) {
                    foreach (var item in m_ItemList) {
                        if (item.Selected) {
                            ++totalSelectedCount;
                        }
                    }
                    foreach (var item in m_ItemList) {
                        if (item.Selected) {
                            if (!m_ProcessedAssets.Contains(item.AssetPath)) {
                                if (!string.IsNullOrEmpty(item.AssetPath))
                                    m_ProcessedAssets.Add(item.AssetPath);
                                if (!ResourceEditUtility.UseSpecificSettingDB || !TextureImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !ModelImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !PrefabParamsDB.DBInstance.Data.Contains(item.AssetPath)) {
                                    var o = ResourceEditUtility.Process(item, m_ProcessCalculator, m_NextProcessIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                                    if (!o.IsNullObject && o.GetBool()) {
                                        ++ct;
                                    }
                                }
                            }
                            ++index;
                            if (DisplayCancelableProgressBar("处理进度", index, totalSelectedCount)) {
                                break;
                            }
                        }
                    }
                }
                else {
                    foreach (var item in m_GroupList) {
                        if (item.Selected) {
                            ++totalSelectedCount;
                        }
                    }
                    foreach (var item in m_GroupList) {
                        if (item.Selected) {
                            if (!m_ProcessedAssets.Contains(item.AssetPath)) {
                                if (!string.IsNullOrEmpty(item.AssetPath))
                                    m_ProcessedAssets.Add(item.AssetPath);
                                if (!ResourceEditUtility.UseSpecificSettingDB || !TextureImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !ModelImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !PrefabParamsDB.DBInstance.Data.Contains(item.AssetPath)) {
                                    var o = ResourceEditUtility.GroupProcess(item, m_ProcessCalculator, m_NextProcessIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                                    if (!o && o.GetBool()) {
                                        ++ct;
                                    }
                                }
                            }
                            ++index;
                            if (DisplayCancelableProgressBar("处理进度", index, totalSelectedCount)) {
                                break;
                            }
                        }
                    }
                }
            }
            else {
                var enumNames = Enum.GetNames(typeof(AssetProcessorEnum));
                var enumValues = Enum.GetValues(typeof(AssetProcessorEnum));
                var handlers = new List<AssetProcessorDB.AssetImporterProcesserHanlder>();
                foreach (string processor in m_AssetProcessors) {
                    int ix = Array.IndexOf<string>(enumNames, processor);
                    if (ix >= 0) {
                        var e = (AssetProcessorEnum)enumValues.GetValue(ix);
                        var handler = AssetProcessorDB.GetHandler(e);
                        if (null != handler) {
                            handlers.Add(handler);
                        }
                    }
                }
                if (m_UnfilteredGroupCount <= 0) {
                    foreach (var item in m_ItemList) {
                        if (item.Selected) {
                            ++totalSelectedCount;
                        }
                    }
                    foreach (var item in m_ItemList) {
                        if (item.Selected) {
                            if (!m_ProcessedAssets.Contains(item.AssetPath)) {
                                if (!string.IsNullOrEmpty(item.AssetPath))
                                    m_ProcessedAssets.Add(item.AssetPath);
                                if (!ResourceEditUtility.UseSpecificSettingDB || !TextureImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !ModelImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !PrefabParamsDB.DBInstance.Data.Contains(item.AssetPath)) {
                                    if (null == item.Importer) {
                                        item.Importer = AssetImporter.GetAtPath(item.AssetPath);
                                    }
                                    if (null == item.Importer && null == item.Object || Path.GetExtension(item.AssetPath) == ".prefab") {
                                        item.Importer = null;
                                        item.Object = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetPath);
                                    }
                                    if (null != item.Importer) {
                                        bool handled = false;
                                        foreach (var handler in handlers) {
                                            handled = handler(item.Importer, false, handled);
                                        }

                                        if (handled) {
                                            ++ct;
                                            Debug.Log("handle success:" + item.AssetPath);
                                        }
                                    }
                                    else if (null != item.Object) {
                                        bool handled = false;
                                        foreach (var handler in handlers) {
                                            handled = handler(item.Object, false, handled);
                                        }

                                        if (handled) {
                                            ++ct;
                                            Debug.Log("handle success:" + item.AssetPath);
                                        }
                                    }
                                }
                            }
                            ++index;
                            if (DisplayCancelableProgressBar("处理进度", index, totalSelectedCount)) {
                                break;
                            }
                        }
                    }
                }
                else {
                    foreach (var item in m_GroupList) {
                        if (item.Selected) {
                            ++totalSelectedCount;
                        }
                    }
                    foreach (var item in m_GroupList) {
                        if (item.Selected) {
                            if (!m_ProcessedAssets.Contains(item.AssetPath)) {
                                if (!string.IsNullOrEmpty(item.AssetPath))
                                    m_ProcessedAssets.Add(item.AssetPath);
                                if (!ResourceEditUtility.UseSpecificSettingDB || !TextureImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !ModelImporterParamsDB.DBInstance.Data.Contains(item.AssetPath) && !PrefabParamsDB.DBInstance.Data.Contains(item.AssetPath)) {
                                    var importer = AssetImporter.GetAtPath(item.AssetPath);
                                    UnityEngine.Object obj = null;
                                    if (null == importer) {
                                        obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.AssetPath);
                                    }
                                    if (null != importer) {
                                        bool handled = false;
                                        foreach (var handler in handlers) {
                                            handled = handler(importer, false, handled);
                                        }

                                        if (handled) {
                                            ++ct;
                                            Debug.Log("handle success:" + item.AssetPath);
                                        }
                                    }
                                    else if (null != obj) {
                                        bool handled = false;
                                        foreach (var handler in handlers) {
                                            handled = handler(obj, false, handled);
                                        }

                                        if (handled) {
                                            ++ct;
                                            Debug.Log("handle success:" + item.AssetPath);
                                        }
                                    }
                                }
                            }
                            ++index;
                            if (DisplayCancelableProgressBar("处理进度", index, totalSelectedCount)) {
                                break;
                            }
                        }
                    }
                }
            }
        }
        finally {
            AssetDatabase.StopAssetEditing();

            if (ResourceEditUtility.SaveAfterProcess) {
                if (m_SearchSource == "sceneobjects" || m_SearchSource == "scenecomponents") {
                    EditorSceneManager.SaveOpenScenes();
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.Default);
            }

            EditorUtility.UnloadUnusedAssetsImmediate(true);
            EditorUtility.ClearProgressBar();
            if (!isBatch)
                EditorUtility.DisplayDialog("提示", "处理完成", "ok");
        }
        return ct;
    }
    internal void ClearCaches()
    {
        m_DslParamCaches.Clear();
    }

    private void CacheParams()
    {
        if (!string.IsNullOrEmpty(m_DslPath) && null != m_Params) {
            Dictionary<string, ResourceEditUtility.ParamInfo> paramInfos;
            if (!m_DslParamCaches.TryGetValue(m_DslPath, out paramInfos)) {
                paramInfos = new Dictionary<string, ResourceEditUtility.ParamInfo>();
                m_DslParamCaches.Add(m_DslPath, paramInfos);
            }
            else {
                paramInfos.Clear();
            }
            foreach (var pair in m_Params) {
                paramInfos.Add(pair.Key, pair.Value);
            }
        }
    }
    private void RestoreParams()
    {
        if (!string.IsNullOrEmpty(m_DslPath) && null != m_Params) {
            Dictionary<string, ResourceEditUtility.ParamInfo> paramInfos;
            if (m_DslParamCaches.TryGetValue(m_DslPath, out paramInfos)) {
                foreach (var pair in m_Params) {
                    var key = pair.Key;
                    var val = pair.Value;
                    ResourceEditUtility.ParamInfo info;
                    if (paramInfos.TryGetValue(key, out info) && val.Type == info.Type) {
                        val.StringValue = info.StringValue;
                        val.Value = info.Value;
                        val.Caption = info.Caption;
                        val.Tooltip = info.Tooltip;
                    }
                }
            }
        }
    }

    private void SearchFiles(string dir)
    {
        string dirName = Path.GetFileName(dir).ToLower();
        if (s_IgnoredDirs.Contains(dirName))
            return;
        foreach (string ext in m_TypeOrExtList) {
            SearchFilesRecursively(dir, ext);
        }
    }
    private bool SearchFilesRecursively(string dir, string ext)
    {
        bool canceled = false;
        string[] files = Directory.GetFiles(dir);
        if (ext != "*.*") {
            files = files.Where((string file) => {
                return ResourceEditUtility.IsPathMatch(file, ext);
            }).ToArray();
        }
        foreach (string file in files) {
            ++m_CurSearchCount;
            string assetPath = PathToAssetPath(file);
            var importer = AssetImporter.GetAtPath(assetPath);
            var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = string.Empty, Importer = importer, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
            var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                m_ItemList.AddRange(m_Results);
            }
            canceled = DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount);
            if (canceled)
                return canceled;
        }
        string[] dirs = Directory.GetDirectories(dir);
        foreach (string subDir in dirs) {
            string dirName = Path.GetFileName(subDir).ToLower();
            if (s_IgnoredDirs.Contains(dirName))
                continue;
            canceled = SearchFilesRecursively(subDir, ext);
            if (canceled)
                return canceled;
        }
        return canceled;
    }
    private void CountFiles(string dir)
    {
        string dirName = Path.GetFileName(dir).ToLower();
        if (s_IgnoredDirs.Contains(dirName))
            return;
        foreach (string ext in m_TypeOrExtList) {
            CountFilesRecursively(dir, ext);
        }
    }
    private void CountFilesRecursively(string dir, string ext)
    {
        string[] files = Directory.GetFiles(dir);
        if (ext != "*.*") {
            files = files.Where((string file) => {
                return ResourceEditUtility.IsPathMatch(file, ext);
            }).ToArray();
        }
        m_TotalSearchCount += files.Length;

        string[] dirs = Directory.GetDirectories(dir);
        foreach (string subDir in dirs) {
            string dirName = Path.GetFileName(subDir).ToLower();
            if (s_IgnoredDirs.Contains(dirName))
                continue;
            CountFilesRecursively(subDir, ext);
        }
    }

    private void SearchUnusedAssets()
    {
        if (m_UnusedAssets.Count <= 0)
            return;

        foreach (string ext in m_TypeOrExtList) {
            IList<string> files = m_UnusedAssets;
            if (ext != "*.*") {
                files = m_UnusedAssets.Where((string file) => {
                    return ResourceEditUtility.IsPathMatch(file, ext);
                }).ToArray();
            }

            foreach (string file in files) {
                ++m_CurSearchCount;
                string assetPath = PathToAssetPath(file);
                var importer = AssetImporter.GetAtPath(assetPath);
                var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = string.Empty, Importer = importer, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                    m_ItemList.AddRange(m_Results);
                }
                if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                    return;
                }
            }
        }
    }
    private void CountUnusedAssets()
    {
        foreach (string ext in m_TypeOrExtList) {
            IList<string> files = m_UnusedAssets;
            if (ext != "*.*") {
                files = m_UnusedAssets.Where((string file) => {
                    return ResourceEditUtility.IsPathMatch(file, ext);
                }).ToArray();
            }
            m_TotalSearchCount += files.Count;
        }
    }
    private void SearchAllAssets()
    {
        string filter = string.Join(" ", m_TypeOrExtList.ToArray());
        var guids = AssetDatabase.FindAssets(filter);
        var allFiles = new HashSet<string>();
        for (int i = 0; i < guids.Length; ++i) {
            string file = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!allFiles.Contains(file)) {
                allFiles.Add(file);
            }
        }

        var files = allFiles.ToArray();
        foreach (string file in files) {
            ++m_CurSearchCount;
            string assetPath = PathToAssetPath(file);
            var importer = AssetImporter.GetAtPath(assetPath);
            var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = string.Empty, Importer = importer, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
            var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                m_ItemList.AddRange(m_Results);
            }
            if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                break;
            }
        }
    }
    private void CountAllAssets()
    {
        string filter = string.Join(" ", m_TypeOrExtList.ToArray());
        var guids = AssetDatabase.FindAssets(filter);
        m_TotalSearchCount = guids.Length;
    }

    private void SearchAssetBundle()
    {
        if (null != AssetBundleInfo) {
            string assetBundle = string.Empty;
            ResourceEditUtility.ParamInfo assetBundleParamInfo;
            if (m_Params.TryGetValue("assetbundle", out assetBundleParamInfo)) {
                assetBundle = assetBundleParamInfo.Value.AsString;
            }

            var assetBundleInfo = m_AssetBundleInfo.GetAssetBuildInfoByAssetBundleName(assetBundle);
            if (null != assetBundleInfo) {
                m_ItemList.Clear();
                m_CurSearchCount = 0;
                m_TotalSearchCount = assetBundleInfo.assetNames.Length;
                foreach (string asset in assetBundleInfo.assetNames) {
                    ++m_CurSearchCount;
                    string assetPath = asset;
                    string scenePath = string.Empty;
                    UnityEngine.Object assetObj = null;
                    AssetImporter importer = null;
                    if (!string.IsNullOrEmpty(assetPath)) {
                        importer = AssetImporter.GetAtPath(assetPath);
                        if (null == assetObj)
                            assetObj = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    }
                    var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Importer = importer, Object = assetObj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                    var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "all_asset_bundle_info", BoxedValue.FromObject(m_AssetBundleInfo) }, { "asset_bundle_info", BoxedValue.FromObject(assetBundleInfo) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                    if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                        m_ItemList.AddRange(m_Results);
                    }
                    if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                        return;
                    }
                }
            }
            else {
                m_ItemList.Clear();
                m_CurSearchCount = 0;
                m_TotalSearchCount = m_AssetBundleInfo.assetBuildItems.Length;
                foreach (var abInfo in m_AssetBundleInfo.assetBuildItems) {
                    ++m_CurSearchCount;
                    string assetPath = abInfo.assetBundleName;
                    string scenePath = string.Empty;
                    UnityEngine.Object assetObj = null;
                    AssetImporter importer = null;
                    var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Importer = importer, Object = assetObj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                    var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "all_asset_bundle_info", BoxedValue.FromObject(m_AssetBundleInfo) }, { "asset_bundle_info", BoxedValue.FromObject(abInfo) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                    if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                        m_ItemList.AddRange(m_Results);
                    }
                    if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                        return;
                    }
                }
            }
        }
    }

    private void SearchSceneAssets()
    {
        var dontDestroyOnLoadScene = GetDontDestroyOnLoadScene();
        for (int i = 0; i <= EditorSceneManager.sceneCount; ++i) {
            var scene = dontDestroyOnLoadScene;
            if (i < EditorSceneManager.sceneCount) {
                scene = EditorSceneManager.GetSceneAt(i);
            }
            else if (!Application.isPlaying) {
                return;
            }
            var assets = GetSceneDependencies(scene);

            foreach (string ext in m_TypeOrExtList) {
                var files = assets;
                if (ext != "*.*") {
                    files = assets.Where((string file) => {
                        return ResourceEditUtility.IsPathMatch(file, ext);
                    }).ToArray();
                }

                foreach (string file in files) {
                    ++m_CurSearchCount;
                    string assetPath = PathToAssetPath(file);
                    var importer = AssetImporter.GetAtPath(assetPath);
                    var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = string.Empty, Importer = importer, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                    var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                    if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                        m_ItemList.AddRange(m_Results);
                    }
                    if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                        return;
                    }
                }
            }
        }
    }
    private void CountSceneAssets()
    {
        var dontDestroyOnLoadScene = GetDontDestroyOnLoadScene();
        for (int i = 0; i <= EditorSceneManager.sceneCount; ++i) {
            var scene = dontDestroyOnLoadScene;
            if (i < EditorSceneManager.sceneCount) {
                scene = EditorSceneManager.GetSceneAt(i);
            }
            else if (!Application.isPlaying) {
                return;
            }
            var assets = GetSceneDependencies(scene);

            foreach (string ext in m_TypeOrExtList) {
                var files = assets;
                if (ext != "*.*") {
                    files = assets.Where((string file) => {
                        return ResourceEditUtility.IsPathMatch(file, ext);
                    }).ToArray();
                }
                m_TotalSearchCount += files.Count();
            }
        }
    }

    private void SearchSceneComponents()
    {
        var dontDestroyOnLoadScene = GetDontDestroyOnLoadScene();
        for (int i = 0; i <= EditorSceneManager.sceneCount; ++i) {
            var scene = dontDestroyOnLoadScene;
            if (i < EditorSceneManager.sceneCount) {
                scene = EditorSceneManager.GetSceneAt(i);
            }
            else if (!Application.isPlaying) {
                return;
            }
            var objs = scene.GetRootGameObjects();
            foreach (var obj in objs) {
                if (SearchChildComponentsRecursively(string.Empty, obj))
                    return;
            }
        }
    }
    private bool SearchChildComponentsRecursively(string path, GameObject obj)
    {
        bool canceled = false;
        if (string.IsNullOrEmpty(path)) {
            path = obj.name;
        }
        else {
            path = path + "/" + obj.name;
        }
        ++m_CurSearchCount;
        if (IsMatchedObject(obj)) {
            var comps = obj.GetComponents<Component>();
            foreach (var comp in comps) {
                if (null != comp) {
                    var key = comp.GetType().Name;
                    string assetPath = AssetDatabase.GetAssetPath(comp);
                    AssetImporter importer = null;
                    if (string.IsNullOrEmpty(assetPath)) {
                        assetPath = string.Empty;
                    }
                    else {
                        importer = AssetImporter.GetAtPath(assetPath);
                    }
                    var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = path, Importer = importer, Object = comp, Info = key, Order = m_ItemList.Count, Group = key, Selected = false };
                    var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                    if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                        m_ItemList.AddRange(m_Results);
                    }
                }
            }
        }

        canceled = DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount);
        if (!canceled) {
            var trans = obj.transform;
            int ct = trans.childCount;
            for (int i = 0; i < ct; ++i) {
                var t = trans.GetChild(i);
                canceled = SearchChildComponentsRecursively(path, t.gameObject);
                if (canceled)
                    return canceled;
            }
        }
        return canceled;
    }
    private void SearchSceneObjects()
    {
        RefreshSceneDeps();
        var dontDestroyOnLoadScene = GetDontDestroyOnLoadScene();
        for (int i = 0; i <= EditorSceneManager.sceneCount; ++i) {
            var scene = dontDestroyOnLoadScene;
            if (i < EditorSceneManager.sceneCount) {
                scene = EditorSceneManager.GetSceneAt(i);
            }
            else if (!Application.isPlaying) {
                return;
            }
            var objs = scene.GetRootGameObjects();
            foreach (var obj in objs) {
                if (SearchChildObjectsRecursively(string.Empty, obj))
                    return;
            }
        }
    }
    private bool SearchChildObjectsRecursively(string path, GameObject obj)
    {
        bool canceled = false;
        if (string.IsNullOrEmpty(path)) {
            path = obj.name;
        }
        else {
            path = path + "/" + obj.name;
        }
        ++m_CurSearchCount;
        if (IsMatchedObject(obj)) {
            string assetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
            AssetImporter importer = null;
            if (string.IsNullOrEmpty(assetPath)) {
                assetPath = string.Empty;
            }
            else {
                importer = AssetImporter.GetAtPath(assetPath);
            }
            var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = path, Importer = importer, Object = obj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
            var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                m_ItemList.AddRange(m_Results);
            }
        }
        canceled = DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount);
        if (!canceled) {
            var trans = obj.transform;
            int ct = trans.childCount;
            for (int i = 0; i < ct; ++i) {
                var t = trans.GetChild(i);
                canceled = SearchChildObjectsRecursively(path, t.gameObject);
                if (canceled)
                    return canceled;
            }
        }
        return canceled;
    }
    private bool IsMatchedObject(GameObject obj)
    {
        if (m_TypeList.Count <= 0 && m_TypeOrExtList.Count > 0) {
            foreach (string type in m_TypeOrExtList) {
                Type t = Type.GetType("UnityEngine." + type + ", UnityEngine");
                if (null == t) {
                    t = Type.GetType("UnityEngine.UI." + type + ", UnityEngine.UI");
                }
                if (null == t) {
                    t = Type.GetType(type + ", Assembly-CSharp");
                }
                if (null != t) {
                    m_TypeList.Add(t);
                }
            }
        }
        if (m_TypeList.Count > 0) {
            foreach (Type type in m_TypeList) {
                var com = obj.GetComponent(type);
                if (null != com)
                    return true;
            }
            return false;
        }
        else {
            return true;
        }
    }

    private int CountSceneObjects()
    {
        int totalCount = 0;
        var dontDestroyOnLoadScene = GetDontDestroyOnLoadScene();
        for (int i = 0; i <= EditorSceneManager.sceneCount; ++i) {
            var scene = dontDestroyOnLoadScene;
            if (i < EditorSceneManager.sceneCount) {
                scene = EditorSceneManager.GetSceneAt(i);
            }
            else if (!Application.isPlaying) {
                break;
            }
            var objs = scene.GetRootGameObjects();
            totalCount += objs.Length;

            foreach (var obj in objs) {
                totalCount += CountChildObjectsRecursively(obj);
            }
        }
        return totalCount;
    }
    private int CountChildObjectsRecursively(GameObject obj)
    {
        int totalCount = 0;
        var trans = obj.transform;
        int ct = trans.childCount;
        totalCount += ct;
        for (int i = 0; i < ct; ++i) {
            var t = trans.GetChild(i);
            totalCount += CountChildObjectsRecursively(t.gameObject);
        }
        return totalCount;
    }

    private void CountRuntimeObjects()
    {
        if (m_TypeList.Count <= 0 && m_TypeOrExtList.Count > 0) {
            foreach (string type in m_TypeOrExtList) {
                Type t = Type.GetType("UnityEngine." + type + ", UnityEngine");
                if (null == t) {
                    t = Type.GetType("UnityEngine.UI." + type + ", UnityEngine.UI");
                }
                if (null == t) {
                    t = Type.GetType(type + ", Assembly-CSharp");
                }
                if (null != t) {
                    m_TypeList.Add(t);
                }
            }
        }
        foreach (var t in m_TypeList) {
            var objs = Resources.FindObjectsOfTypeAll(t);
            m_TotalSearchCount += objs.Length;
        }
    }
    private void SearchRuntimeObjects()
    {
        if (m_TypeList.Count <= 0 && m_TypeOrExtList.Count > 0) {
            foreach (string type in m_TypeOrExtList) {
                Type t = Type.GetType("UnityEngine." + type + ", UnityEngine");
                if (null == t) {
                    t = Type.GetType("UnityEngine.UI." + type + ", UnityEngine.UI");
                }
                if (null == t) {
                    t = Type.GetType(type + ", Assembly-CSharp");
                }
                if (null != t) {
                    m_TypeList.Add(t);
                }
            }
        }
        foreach (var t in m_TypeList) {
            var objs = Resources.FindObjectsOfTypeAll(t);
            foreach (var obj in objs) {
                ++m_CurSearchCount;
                string assetPath = GetAssetPath(obj);
                AssetImporter importer = null;
                if (string.IsNullOrEmpty(assetPath)) {
                    assetPath = string.Empty;
                }
                else {
                    importer = AssetImporter.GetAtPath(assetPath);
                }
                var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = string.Empty, Importer = importer, Object = obj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                    m_ItemList.AddRange(m_Results);
                }
                if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                    return;
                }
            }
        }
    }

    private void CountExcelRecords()
    {
        m_TotalSearchCount = 0;
        ResourceEditUtility.ParamInfo info;
        if (m_Params.TryGetValue("excel", out info)) {
            string sheetName = string.Empty;
            ResourceEditUtility.ParamInfo sheetNameInfo;
            if (m_Params.TryGetValue("sheetname", out sheetNameInfo) && !sheetNameInfo.Value.IsNullObject) {
                sheetName = sheetNameInfo.Value.ToString();
            }
            if (string.IsNullOrEmpty(sheetName)) {
                sheetName = m_TypeOrExtList.Count > 0 ? m_TypeOrExtList[0] : string.Empty;
            }
            int skipRowNum = 5;
            ResourceEditUtility.ParamInfo skipInfo;
            if (m_Params.TryGetValue("skiprows", out skipInfo) && !skipInfo.Value.IsNullObject) {
                int.TryParse(skipInfo.Value.ToString(), out skipRowNum);
            }
            var file = info.Value.AsString;
            var path = file;
            if (!File.Exists(path)) {
                path = Path.Combine("../../Product/Excel", file);
            }
            if (File.Exists(path)) {
                var ext = Path.GetExtension(file);
                NPOI.SS.UserModel.IWorkbook book = null;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    if (ext == ".xls") {
                        book = new NPOI.HSSF.UserModel.HSSFWorkbook(stream);
                    }
                    else {
                        book = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
                    }
                    int ct = 0;
                    var sheet = book.GetSheet(sheetName);
                    if (null != sheet) {
                        ct += sheet.LastRowNum - sheet.FirstRowNum - skipRowNum;
                    }
                    m_TotalSearchCount = ct;
                    m_WorkBook = book;
                }
            }
        }
    }
    private void SearchExcelRecords()
    {
        if (null != m_WorkBook) {
            string sheetName = string.Empty;
            ResourceEditUtility.ParamInfo sheetNameInfo;
            if (m_Params.TryGetValue("sheetname", out sheetNameInfo) && !sheetNameInfo.Value.IsNullObject) {
                sheetName = sheetNameInfo.Value.ToString();
            }
            if (string.IsNullOrEmpty(sheetName)) {
                sheetName = m_TypeOrExtList.Count > 0 ? m_TypeOrExtList[0] : string.Empty;
            }
            int skipRowNum = 5;
            ResourceEditUtility.ParamInfo skipInfo;
            if (m_Params.TryGetValue("skiprows", out skipInfo) && !skipInfo.Value.IsNullObject) {
                int.TryParse(skipInfo.Value.ToString(), out skipRowNum);
            }
            var book = m_WorkBook;
            if (null != book) {
                var sheet = book.GetSheet(sheetName);
                if (null != sheet) {
                    for (int i = sheet.FirstRowNum + skipRowNum; i <= sheet.LastRowNum; ++i) {
                        ++m_CurSearchCount;
                        var row = sheet.GetRow(i);
                        if (null != row) {
                            var item = new ResourceEditUtility.ItemInfo { AssetPath = string.Empty, ScenePath = string.Empty, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                            var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "book", BoxedValue.FromObject(book) }, { "sheet", BoxedValue.FromObject(sheet) }, { "row", BoxedValue.FromObject(row) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                                m_ItemList.AddRange(m_Results);
                            }
                            if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                                goto L_EndExcel;
                            }
                        }
                    }
                }
            }
        }
    L_EndExcel:
        EditorUtility.ClearProgressBar();
    }
    private void CountTableRecords()
    {
        m_TotalSearchCount = 0;
        ResourceEditUtility.ParamInfo info;
        if (m_Params.TryGetValue("table", out info)) {
            string encoding = "utf-8";
            int skipRowNum = 1;
            ResourceEditUtility.ParamInfo encodingInfo;
            if (m_Params.TryGetValue("encoding", out encodingInfo) && !encodingInfo.Value.IsNullObject) {
                encoding = encodingInfo.Value.ToString();
            }
            ResourceEditUtility.ParamInfo skipInfo;
            if (m_Params.TryGetValue("skiprows", out skipInfo) && !skipInfo.Value.IsNullObject) {
                int.TryParse(skipInfo.Value.ToString(), out skipRowNum);
            }
            var file = info.Value.AsString;
            var path = file;
            if (!File.Exists(path)) {
                path = Path.Combine("../../Server/resources/Table", file);
            }
            if (File.Exists(path)) {
                var ext = Path.GetExtension(file);
                var table = new ResourceEditUtility.DataTable();
                table.Load(path, Encoding.GetEncoding(encoding), ext == ".csv" ? ',' : '\t');
                m_TotalSearchCount = table.RowCount - skipRowNum;
                m_DataTable = table;
            }
        }
    }
    private void SearchTableRecords()
    {
        if (null != m_DataTable) {
            int skipRowNum = 5;
            ResourceEditUtility.ParamInfo skipInfo;
            if (m_Params.TryGetValue("skiprows", out skipInfo) && !skipInfo.Value.IsNullObject) {
                int.TryParse(skipInfo.Value.ToString(), out skipRowNum);
            }
            var table = m_DataTable;
            if (null != table) {
                for (int i = skipRowNum; i < table.RowCount; ++i) {
                    ++m_CurSearchCount;
                    var row = table[i];
                    if (null != row) {
                        var item = new ResourceEditUtility.ItemInfo { AssetPath = string.Empty, ScenePath = string.Empty, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                        var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "sheet", BoxedValue.FromObject(table) }, { "row", BoxedValue.FromObject(row) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                        if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                            m_ItemList.AddRange(m_Results);
                        }
                        if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                            goto L_EndTable;
                        }
                    }
                }
            }
        }
    L_EndTable:
        EditorUtility.ClearProgressBar();
    }
    private void SearchScriptResult()
    {
        m_TotalSearchCount = 0;
        ResourceEditUtility.ParamInfo info;
        if (m_Params.TryGetValue("script", out info)) {
            var funcName = info.Value.AsString;
            if (!string.IsNullOrEmpty(funcName)) {
                var list = CallScript(null, funcName, BoxedValue.FromObject(info)).As<IList>();
                if (null != list) {
                    foreach (var pathObj in list) {
                        var path = pathObj as string;
                        if (!string.IsNullOrEmpty(path)) {
                            var obj = GameObject.Find(path);
                            string assetPath = string.Empty;
                            string scenePath = string.Empty;
                            AssetImporter importer = null;
                            if (null != obj) {
                                scenePath = path;
                                assetPath = GetAssetPath(obj);
                                if (string.IsNullOrEmpty(assetPath)) {
                                    assetPath = string.Empty;
                                }
                                else {
                                    importer = AssetImporter.GetAtPath(assetPath);
                                }
                            }
                            else {
                                assetPath = path;
                                importer = AssetImporter.GetAtPath(assetPath);
                            }
                            var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Importer = importer, Object = obj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
                            var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
                            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                                m_ItemList.AddRange(m_Results);
                            }
                        }
                    }
                }
            }
        }
    }
    private void SearchList()
    {
        foreach (var path in m_TypeOrExtList) {
            var obj = GameObject.Find(path);
            string assetPath = string.Empty;
            string scenePath = string.Empty;
            AssetImporter importer = null;
            if (null != obj) {
                scenePath = path;
                assetPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(obj));
                if (string.IsNullOrEmpty(assetPath)) {
                    assetPath = string.Empty;
                }
                else {
                    importer = AssetImporter.GetAtPath(assetPath);
                }
            }
            else {
                assetPath = path;
                importer = AssetImporter.GetAtPath(assetPath);
            }
            var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Importer = importer, Object = obj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
            var ret = ResourceEditUtility.Filter(item, null, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                m_ItemList.AddRange(m_Results);
            }
        }
    }

    private void SearchSnapshot(bool isBatch)
    {
        string category = string.Empty;
        ResourceEditUtility.ParamInfo categoryParamInfo;
        if (m_Params.TryGetValue("category", out categoryParamInfo)) {
            category = categoryParamInfo.Value.AsString;
        }
        string _class = string.Empty;
        ResourceEditUtility.ParamInfo classParamInfo;
        if (m_Params.TryGetValue("class", out classParamInfo)) {
            _class = classParamInfo.Value.AsString;
        }
        if (null == s_CachedSnapshot || null == s_ShortestPathToRootFinder) {
            s_CachedSnapshot = Unity.MemoryProfilerExtension.Editor.MemoryProfilerWindow.GetBaseCachedSnapshot();
            if (null != s_CachedSnapshot) {
                s_ShortestPathToRootFinder = new ShortestPathToRootObjectFinder(s_CachedSnapshot);
            }
        }
        if ((category == "mgroup" || category == "managed") && m_ClassifiedManagedMemoryInfos.Count <= 0 ||
            (category == "ngroup" || category == "native") && m_ClassifiedNativeMemoryInfos.Count <= 0 ||
            m_ClassifiedManagedMemoryInfos.Count <= 0 && m_ClassifiedNativeMemoryInfos.Count <= 0) {
            if (null != s_CachedSnapshot) {
                AnalyseSnapshot();
            }
            else if (!isBatch) {
                EditorUtility.DisplayDialog("错误", "未找到内存对象信息，请先执行内存‘捕获’或‘加载’！", "ok");
                return;
            }
        }
        m_ItemList.Clear();
        m_CurSearchCount = 0;
        m_TotalSearchCount = 0;
        RefreshSceneDeps();
        if (category == "mgroup") {
            var infos = m_ClassifiedManagedMemoryInfos;
            int curCount = 0;
            int totalCount = 0;
            totalCount = infos.Count;
            foreach (var pair in infos) {
                DoFilterGroupMemoryInfo(pair.Value, infos);
                ++curCount;
                if (curCount % 1000 == 0 && DisplayCancelableProgressBar("采集进度", m_ItemList.Count, curCount, totalCount)) {
                    goto L_EndSnapshot;
                }
            }
        }
        else if (category == "ngroup") {
            var infos = m_ClassifiedNativeMemoryInfos;
            int curCount = 0;
            int totalCount = 0;
            totalCount = infos.Count;
            foreach (var pair in infos) {
                DoFilterGroupMemoryInfo(pair.Value, infos);
                ++curCount;
                if (curCount % 100 == 0 && DisplayCancelableProgressBar("采集进度", m_ItemList.Count, curCount, totalCount)) {
                    goto L_EndSnapshot;
                }
            }
        }
        else {
            IDictionary<string, ResourceEditUtility.MemoryGroupInfo> infos = null;
            int delta = 1000;
            if (category == "managed") {
                infos = m_ClassifiedManagedMemoryInfos;
                delta = 1000;
            }
            else if (category == "native") {
                infos = m_ClassifiedNativeMemoryInfos;
                delta = 100;
            }
            else
                infos = null;
            int curCount = 0;
            int totalCount = 0;
            bool handled = false;
            if (null != infos && !string.IsNullOrEmpty(_class)) {
                ResourceEditUtility.MemoryGroupInfo groupInfo;
                if (infos.TryGetValue(_class, out groupInfo)) {
                    totalCount = groupInfo.memories.Count;
                    foreach (var memory in groupInfo.memories) {
                        DoFilterMemoryInfo(memory, groupInfo, infos);
                        ++curCount;
                        if (curCount % delta == 0 && DisplayCancelableProgressBar("采集进度", m_ItemList.Count, curCount, totalCount)) {
                            goto L_EndSnapshot;
                        }
                    }
                    handled = true;
                }
            }
            if (!handled) {
                if (null != infos) {
                    totalCount = 0;
                    foreach (var pair in infos) {
                        totalCount += pair.Value.memories.Count;
                    }
                    foreach (var pair in infos) {
                        foreach (var memory in pair.Value.memories) {
                            DoFilterMemoryInfo(memory, pair.Value, infos);
                            ++curCount;
                            if (curCount % 1000 == 0 && DisplayCancelableProgressBar("采集进度", m_ItemList.Count, curCount, totalCount)) {
                                goto L_EndSnapshot;
                            }
                        }
                    }
                }
                else {
                    totalCount = 0;
                    infos = m_ClassifiedManagedMemoryInfos;
                    foreach (var pair in infos) {
                        totalCount += pair.Value.memories.Count;
                    }
                    infos = m_ClassifiedNativeMemoryInfos;
                    foreach (var pair in infos) {
                        totalCount += pair.Value.memories.Count;
                    }
                    infos = m_ClassifiedManagedMemoryInfos;
                    foreach (var pair in infos) {
                        foreach (var memory in pair.Value.memories) {
                            DoFilterMemoryInfo(memory, pair.Value, infos);
                            ++curCount;
                            if (curCount % 1000 == 0 && DisplayCancelableProgressBar("采集进度", m_ItemList.Count, curCount, totalCount)) {
                                goto L_EndSnapshot;
                            }
                        }
                    }
                    infos = m_ClassifiedNativeMemoryInfos;
                    foreach (var pair in infos) {
                        foreach (var memory in pair.Value.memories) {
                            DoFilterMemoryInfo(memory, pair.Value, infos);
                            ++curCount;
                            if (curCount % 1000 == 0 && DisplayCancelableProgressBar("采集进度", m_ItemList.Count, curCount, totalCount)) {
                                goto L_EndSnapshot;
                            }
                        }
                    }
                }
            }
        }
    L_EndSnapshot:
        EditorUtility.ClearProgressBar();
    }
    private void DoFilterMemoryInfo(ResourceEditUtility.MemoryInfo memory, ResourceEditUtility.MemoryGroupInfo groupInfo, IDictionary<string, ResourceEditUtility.MemoryGroupInfo> infos)
    {
        string assetPath = string.Empty;
        string scenePath = string.Empty;
        UnityEngine.Object assetObj = null;
        AssetImporter importer = null;
        if (null == assetObj && !string.IsNullOrEmpty(scenePath)) {
            assetObj = GameObject.Find(scenePath);
        }
        if (!string.IsNullOrEmpty(assetPath)) {
            importer = AssetImporter.GetAtPath(assetPath);
            if (null == assetObj)
                assetObj = AssetDatabase.LoadMainAssetAtPath(assetPath);
        }
        var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Importer = importer, Object = assetObj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
        var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "memory", BoxedValue.FromObject(memory) }, { "group_info", BoxedValue.FromObject(groupInfo) }, { "all_groups", BoxedValue.FromObject(infos) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
        if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
            m_ItemList.AddRange(m_Results);
        }
    }
    private void DoFilterGroupMemoryInfo(ResourceEditUtility.MemoryGroupInfo groupInfo, IDictionary<string, ResourceEditUtility.MemoryGroupInfo> infos)
    {
        string assetPath = string.Empty;
        string scenePath = string.Empty;
        UnityEngine.Object assetObj = null;
        AssetImporter importer = null;
        if (null == assetObj && !string.IsNullOrEmpty(scenePath)) {
            assetObj = GameObject.Find(scenePath);
        }
        if (!string.IsNullOrEmpty(assetPath)) {
            importer = AssetImporter.GetAtPath(assetPath);
            if (null == assetObj)
                assetObj = AssetDatabase.LoadMainAssetAtPath(assetPath);
        }
        var item = new ResourceEditUtility.ItemInfo { AssetPath = assetPath, ScenePath = scenePath, Importer = importer, Object = assetObj, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
        var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "group_info", BoxedValue.FromObject(groupInfo) }, { "all_groups", BoxedValue.FromObject(infos) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
        if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
            m_ItemList.AddRange(m_Results);
        }
    }
    private void SearchInstruments()
    {
        if (m_InstrumentInfos.Count <= 0)
            return;

        m_TotalSearchCount = m_InstrumentInfos.Count;
        foreach (var pair in m_InstrumentInfos) {
            var info = pair.Value;
            ++m_CurSearchCount;
            var item = new ResourceEditUtility.ItemInfo { AssetPath = string.Empty, ScenePath = string.Empty, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
            var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "instrument", BoxedValue.FromObject(info) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                m_ItemList.AddRange(m_Results);
            }
            if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                break;
            }
        }
    }
    private void SearchUTrace()
    {
        if (m_uTraceFrames.Count <= 0)
            return;

        m_TotalSearchCount = m_uTraceFrames.Count;
        foreach (var pair in m_uTraceFrames) {
            var info = pair.Value;
            ++m_CurSearchCount;
            var item = new ResourceEditUtility.ItemInfo { AssetPath = string.Empty, ScenePath = string.Empty, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
            var ret = ResourceEditUtility.Filter(item, new Dictionary<string, BoxedValue> { { "utraceframe", BoxedValue.FromObject(info) } }, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
            if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
                m_ItemList.AddRange(m_Results);
            }
            if (DisplayCancelableProgressBar("采集进度", m_ItemList.Count, m_CurSearchCount, m_TotalSearchCount)) {
                break;
            }
        }
    }

    private bool RecordInstrumentFrame(int frame, HierarchyFrameDataView.ViewModes viewMode, int sortColumn, bool sortAscending, float triangle, float batch, List<int> parentsCacheList, List<int> childrenCacheList, SortedDictionary<int, ResourceEditUtility.InstrumentThreadInfo> threads)
    {
        var info = new ResourceEditUtility.InstrumentInfo();
        info.frame = frame + 1;
        info.triangle = triangle;
        info.batch = batch;

        info.threads = threads;

        foreach (var pair in threads) {
            var tindex = pair.Key;
            var tinfo = pair.Value;
            //0--cpu 1--rendering
            //Neither the item id nor the raw index is stable, so we record the marker id, which is not unique but stable, and we find the item by its path later.
            using (var hierView = ProfilerDriver.GetHierarchyFrameDataView(frame, tindex, viewMode, sortColumn, sortAscending)) {
                if (null != hierView && hierView.valid) {
                    try {
                        if (tindex == 0) {
                            float cpu = hierView.frameTimeMs;
                            float gpu = hierView.frameGpuTimeMs;

                            info.fps = hierView.frameFps;
                            info.totalCpuTime = cpu;
                            info.totalGpuTime = gpu;
                            info.totalGcMemory = hierView.GetItemColumnDataAsFloat(hierView.GetRootItemID(), HierarchyFrameDataView.columnGcMemory) / 1024.0f;
                        }

                        int rootId = hierView.GetRootItemID();
                        parentsCacheList.Clear();
                        hierView.GetItemDescendantsThatHaveChildren(rootId, parentsCacheList);
                        foreach (int parentId in parentsCacheList) {
                            childrenCacheList.Clear();
                            hierView.GetItemChildren(parentId, childrenCacheList);

                            foreach (var id in childrenCacheList) {
                                var data = new ResourceEditUtility.InstrumentRecord();
                                data.frame = frame + 1;
                                data.threadIndex = tindex;
                                data.sampleCount = hierView.GetItemMergedSamplesCount(id);
                                data.depth = hierView.GetItemDepth(id);
                                data.name = hierView.GetItemName(id);
                                data.layerPath = hierView.GetItemPath(id);
                                data.calls = hierView.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnCalls);
                                data.gcMemory = hierView.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnGcMemory) / 1024.0f;
                                data.totalTime = hierView.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalTime);
                                data.totalPercent = hierView.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnTotalPercent);
                                data.selfTime = hierView.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfTime);
                                data.selfPercent = hierView.GetItemColumnDataAsFloat(id, HierarchyFrameDataView.columnSelfPercent);
                                data.markerId = hierView.GetItemMarkerID(id);

                                info.records.Add(data);
                            }
                        }
                    }
                    catch (Exception e) {
                        Debug.LogErrorFormat("frame:{0} thread index:{1}[{2}]({3}) exception:{4} stack:{5}", frame, tindex, tinfo.threadId, tinfo.threadName, e.Message, e.StackTrace);
                    }
                }
            }
        }

        var item = new ResourceEditUtility.ItemInfo { AssetPath = string.Empty, ScenePath = string.Empty, Info = string.Empty, Order = m_ItemList.Count, Selected = false };
        var addVars = new Dictionary<string, BoxedValue> { { "instrument", BoxedValue.FromObject(info) } };
        var ret = ResourceEditUtility.Filter(item, addVars, m_Results, m_FilterCalculator, m_NextFilterIndex, m_Params, m_SceneDeps, m_ReferenceAssets, m_ReferenceByAssets);
        if (m_NextFilterIndex <= 0 || !ret.IsNullObject && ret.GetInt() > 0) {
            m_InstrumentInfos[info.frame] = info;
            return true;
        }
        return false;
    }
    private float InstrumentString2Float(string val)
    {
        try {
            if (string.IsNullOrWhiteSpace(val) || val == "N/A") {
                return 0;
            }
            else if (val == "inf") {
                return float.MaxValue;
            }
            int ix = val.IndexOf('%');
            if (ix > 0) {
                return float.Parse(val.Substring(0, ix).Trim());
            }
            ix = val.IndexOf(" MB");
            if (ix > 0) {
                return float.Parse(val.Substring(0, ix).Trim()) * 1024.0f;
            }
            ix = val.IndexOf(" KB");
            if (ix > 0) {
                return float.Parse(val.Substring(0, ix).Trim());
            }
            ix = val.IndexOf(" B");
            if (ix > 0) {
                return float.Parse(val.Substring(0, ix).Trim()) / 1024.0f;
            }
            return float.Parse(val.Trim());
        }
        catch (Exception ex) {
            Debug.LogErrorFormat("InstrumentString2Float {0} throw exception:{1}\n{2}", val, ex.Message, ex.StackTrace);
            return 0;
        }
    }

    internal void DisplayProgressBar(string title, long resultCount, long curCount, long totalCount)
    {
        DisplayProgressBar(title, resultCount, curCount, totalCount, true);
    }
    internal void DisplayProgressBar(string title, long resultCount, long curCount, long totalCount, bool batch)
    {
        if (!string.IsNullOrEmpty(m_OverridedProgressTitle))
            title = m_OverridedProgressTitle;
        if (batch && totalCount > 1000) {
            if (curCount % 10 == 0) {
                EditorUtility.DisplayProgressBar(title, string.Format("{0} in {1}/{2}", resultCount, curCount, totalCount), curCount * 1.0f / totalCount);
            }
        }
        else {
            EditorUtility.DisplayProgressBar(title, string.Format("{0} in {1}/{2}", resultCount, curCount, totalCount), curCount * 1.0f / totalCount);
        }
    }

    internal void DisplayProgressBar(string title, long curCount, long totalCount)
    {
        DisplayProgressBar(title, curCount, totalCount, true);
    }
    internal void DisplayProgressBar(string title, long curCount, long totalCount, bool batch)
    {
        if (!string.IsNullOrEmpty(m_OverridedProgressTitle))
            title = m_OverridedProgressTitle;
        if (batch && totalCount > 1000) {
            if (curCount % 10 == 0) {
                EditorUtility.DisplayProgressBar(title, string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
            }
        }
        else {
            EditorUtility.DisplayProgressBar(title, string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
        }
    }

    internal bool DisplayCancelableProgressBar(string title, long resultCount, long curCount, long totalCount)
    {
        return DisplayCancelableProgressBar(title, resultCount, curCount, totalCount, true);
    }
    internal bool DisplayCancelableProgressBar(string title, long resultCount, long curCount, long totalCount, bool batch)
    {
        if (!string.IsNullOrEmpty(m_OverridedProgressTitle))
            title = m_OverridedProgressTitle;
        if (batch && totalCount > 1000) {
            if (curCount % 10 == 0) {
                return EditorUtility.DisplayCancelableProgressBar(title, string.Format("{0} in {1}/{2}", resultCount, curCount, totalCount), curCount * 1.0f / totalCount);
            }
        }
        else {
            return EditorUtility.DisplayCancelableProgressBar(title, string.Format("{0} in {1}/{2}", resultCount, curCount, totalCount), curCount * 1.0f / totalCount);
        }
        return false;
    }

    internal bool DisplayCancelableProgressBar(string title, long curCount, long totalCount)
    {
        return DisplayCancelableProgressBar(title, curCount, totalCount, true);
    }
    internal bool DisplayCancelableProgressBar(string title, long curCount, long totalCount, bool batch)
    {
        if (!string.IsNullOrEmpty(m_OverridedProgressTitle))
            title = m_OverridedProgressTitle;
        if (batch && totalCount > 1000) {
            if (curCount % 10 == 0) {
                return EditorUtility.DisplayCancelableProgressBar(title, string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
            }
        }
        else {
            return EditorUtility.DisplayCancelableProgressBar(title, string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
        }
        return false;
    }

    private void RefreshSceneDeps()
    {
        m_SceneDeps.Clear();
        for (int i = 0; i < EditorSceneManager.sceneCount; ++i) {
            var scene = EditorSceneManager.GetSceneAt(i);
            var assets = GetDependencies(scene.path);
            foreach (var asset in assets) {
                string name = Path.GetFileName(asset);
                if (!m_SceneDeps.deps.Contains(asset)) {
                    m_SceneDeps.deps.Add(asset);
                }
                if (!m_SceneDeps.name2paths.ContainsKey(name)) {
                    m_SceneDeps.name2paths.Add(name, asset);
                }
            }
        }
    }
    private IEnumerable<string> GetSceneDependencies(UnityEngine.SceneManagement.Scene scene)
    {
        if (Application.isPlaying) {
            var rootObjs = scene.GetRootGameObjects();
            var assets = new HashSet<string>();
            var dependencies = EditorUtility.CollectDependencies(rootObjs);
            foreach (var dep in dependencies) {
                string path = AssetDatabase.GetAssetPath(dep);
                if (!string.IsNullOrEmpty(path)) {
                    assets.Add(path);
                }
            }
            var deps = GetDependencies(scene.path);
            foreach (var dep in deps) {
                assets.Add(dep);
            }
            foreach (var lightmap in LightmapSettings.lightmaps) {
                if (null != lightmap.lightmapColor) {
                    var asset = GetAssetPath(lightmap.lightmapColor);
                    if (!string.IsNullOrEmpty(asset)) {
                        assets.Add(asset);
                    }
                }
                if (null != lightmap.lightmapDir) {
                    var asset = GetAssetPath(lightmap.lightmapDir);
                    if (!string.IsNullOrEmpty(asset)) {
                        assets.Add(asset);
                    }
                }
                if (null != lightmap.shadowMask) {
                    var asset = GetAssetPath(lightmap.shadowMask);
                    if (!string.IsNullOrEmpty(asset)) {
                        assets.Add(asset);
                    }
                }
            }
            foreach (var obj in rootObjs) {
                CollectSceneAssetsRecursively(obj, assets);
            }
            return assets;
        }
        else {
            return GetDependencies(scene.path);
        }
    }
    private void CollectSceneAssetsRecursively(GameObject obj, HashSet<string> assets)
    {
        string asset = GetAssetPath(obj);
        if (!string.IsNullOrEmpty(asset)) {
            var tmps = GetDependencies(asset);
            foreach (var tmp in tmps) {
                assets.Add(tmp);
            }
            assets.Add(asset);
        }

        var trans = obj.transform;
        int ct = trans.childCount;
        for (int i = 0; i < ct; ++i) {
            var t = trans.GetChild(i);
            CollectSceneAssetsRecursively(t.gameObject, assets);
        }
    }
    private string GetAssetPath(UnityEngine.Object obj)
    {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(assetPath)) {
            var sourceObj = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            if (null != sourceObj) {
                assetPath = AssetDatabase.GetAssetPath(sourceObj);
            }
        }
        if (string.IsNullOrEmpty(assetPath)) {
            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(obj);
            if (null != prefabRoot) {
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
            }
        }
        if (string.IsNullOrEmpty(assetPath)) {
            var prefabObj = PrefabUtility.GetPrefabInstanceHandle(obj);
            if (null != prefabObj) {
                assetPath = AssetDatabase.GetAssetPath(prefabObj);
            }
        }
        return assetPath;
    }
    private IEnumerable<string> GetDependencies(string path)
    {
        HashSet<string> list;
        if (m_ReferenceAssets.TryGetValue(path, out list)) {
            return list;
        }
        else {
            return AssetDatabase.GetDependencies(path);
        }
    }

    private bool IsAssetPath(string path)
    {
        return ResourceEditUtility.IsAssetPath(path);
    }
    private string PathToAssetPath(string path)
    {
        return ResourceEditUtility.PathToAssetPath(path);
    }
    private string AssetPathToPath(string assetPath)
    {
        return ResourceEditUtility.AssetPathToPath(assetPath);
    }
    private bool IsIgnoreDir(string dir)
    {
        string path = dir.ToLower();
        foreach (string key in s_IgnoreDirKeys) {
            if (path.Contains(key)) {
                return true;
            }
        }
        return false;
    }

    private string m_OverridedProgressTitle = string.Empty;

    private string m_DslMenu = string.Empty;
    private string m_DslDescription = string.Empty;
    private string m_DefaultItemCommand = string.Empty;
    private string m_DefaultGroupCommand = string.Empty;
    private string m_SearchSource = string.Empty;
    private List<string> m_AssetProcessors = new List<string>();
    private List<string> m_TypeOrExtList = new List<string>();
    private List<Type> m_TypeList = new List<Type>();
    private List<string> m_ParamNames = new List<string>();
    private Dictionary<string, ResourceEditUtility.ParamInfo> m_Params = new Dictionary<string, ResourceEditUtility.ParamInfo>();

    private string m_DslPath = null;
    private Dsl.DslFile m_DslFile = null;
    private DslCalculator m_ScriptCalculator = null;
    private DslCalculator m_FilterCalculator = null;
    private DslCalculator m_GroupCalculator = null;
    private DslCalculator m_ProcessCalculator = null;
    private int m_NextFilterIndex = 0;
    private int m_NextGroupIndex = 0;
    private int m_NextProcessIndex = 0;

    private bool m_CanRefresh = false;
    private string m_Text = string.Empty;
    private string m_CollectPath = string.Empty;
    private int m_CurSearchCount = 0;
    private int m_TotalSearchCount = 0;
    private List<ResourceEditUtility.ItemInfo> m_Results = new List<ResourceEditUtility.ItemInfo>();
    private HashSet<string> m_ProcessedAssets = new HashSet<string>();

    private Dictionary<string, Dictionary<string, ResourceEditUtility.ParamInfo>> m_DslParamCaches = new Dictionary<string, Dictionary<string, ResourceEditUtility.ParamInfo>>();
    private NPOI.SS.UserModel.IWorkbook m_WorkBook = null;
    private ResourceEditUtility.DataTable m_DataTable = null;

    private bool m_IsItemListGroupStyle = false;
    private List<ResourceEditUtility.ItemInfo> m_ItemList = new List<ResourceEditUtility.ItemInfo>();
    private List<ResourceEditUtility.GroupInfo> m_GroupList = new List<ResourceEditUtility.GroupInfo>();
    private int m_UnfilteredGroupCount = 0;
    private double m_TotalItemValue = 0;
    private int m_NonZeroItemCount = 0;

    private AssetInformation m_AssetBundleInfo = null;
    private ResourceEditUtility.SceneDepInfo m_SceneDeps = new ResourceEditUtility.SceneDepInfo();
    private Dictionary<string, HashSet<string>> m_ReferenceAssets = new Dictionary<string, HashSet<string>>();
    private Dictionary<string, HashSet<string>> m_ReferenceByAssets = new Dictionary<string, HashSet<string>>();
    private List<string> m_UnusedAssets = new List<string>();
    private SortedDictionary<string, ResourceEditUtility.MemoryGroupInfo> m_ClassifiedNativeMemoryInfos = new SortedDictionary<string, ResourceEditUtility.MemoryGroupInfo>();
    private SortedDictionary<string, ResourceEditUtility.MemoryGroupInfo> m_ClassifiedManagedMemoryInfos = new SortedDictionary<string, ResourceEditUtility.MemoryGroupInfo>();

    private SortedList<int, ResourceEditUtility.InstrumentInfo> m_InstrumentInfos = new SortedList<int, ResourceEditUtility.InstrumentInfo>();
    private SortedList<int, ResourceEditUtility.uTraceFrame> m_uTraceFrames = new SortedList<int, ResourceEditUtility.uTraceFrame>();

    internal static UnityEngine.SceneManagement.Scene GetDontDestroyOnLoadScene()
    {
        if (null == s_GetDontDestroyOnLoadSceneMethod) {
            var t = typeof(UnityEditor.SceneManagement.EditorSceneManager);
            s_GetDontDestroyOnLoadSceneMethod = t.GetMethod("GetDontDestroyOnLoadScene", BindingFlags.NonPublic | BindingFlags.Static);
        }
        return (UnityEngine.SceneManagement.Scene)s_GetDontDestroyOnLoadSceneMethod.Invoke(null, null);
    }

    internal static bool ReadMenuAndDescription(string path, out string menu, out string desc)
    {
        bool ret = false;
        bool readSource = false;
        bool readMenu = false;
        bool readDesc = false;
        string source = string.Empty;
        menu = string.Empty;
        desc = string.Empty;
        if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
            Dsl.DslFile file = new Dsl.DslFile();
            if (file.Load(path, (string msg) => { Debug.LogError(msg); })) {
                if (file.DslInfos.Count > 0) {
                    var info = file.DslInfos[0];
                    var func = info as Dsl.FunctionData;
                    var stData = info as Dsl.StatementData;
                    if (null == func && null != stData) {
                        func = stData.First.AsFunction;
                    }
                    if (null != func && func.GetId() == "input") {
                        foreach (var comp in func.Params) {
                            var callData = comp as Dsl.FunctionData;
                            if (null != callData && callData.GetId() == "feature") {
                                string key = callData.GetParamId(0);
                                string val = callData.GetParamId(1);
                                if (key == "source") {
                                    source = val;
                                    readSource = true;
                                }
                                else if (key == "menu") {
                                    menu = val;
                                    readMenu = true;
                                }
                                else if (key == "description") {
                                    desc = val;
                                    readDesc = true;
                                }
                                if (readSource && readMenu && readDesc) {
                                    break;
                                }
                            }
                        }
                    }
                }
                else {
                    Debug.LogErrorFormat("'{0}' no any DSL info !", path);
                }
            }
            if (string.IsNullOrEmpty(source)) {
                source = "project";
            }
            if (string.IsNullOrEmpty(menu)) {
                menu = string.Format("{0}/{1}", source, Path.GetFileNameWithoutExtension(path));
            }
            if (string.IsNullOrEmpty(desc)) {
                desc = path;
            }
            ret = true;
        }
        return ret;
    }
    internal static void ReadFiltersAndAssetProcessors(string path, List<string> filters, List<string> processors)
    {
        filters.Clear();
        processors.Clear();
        if (!string.IsNullOrEmpty(path)) {
            if (!File.Exists(path)) {
                path = ResourceEditUtility.RelativePathToFilePath(path);
            }
            if (File.Exists(path)) {
                Dsl.DslFile file = new Dsl.DslFile();
                if (file.Load(path, (string msg) => { Debug.LogError(msg); })) {
                    var info = file.DslInfos[0];
                    Dsl.FunctionData first = null;
                    Dsl.FunctionData last = null;
                    var stData = info as Dsl.StatementData;
                    if (null != stData) {
                        first = stData.First.AsFunction;
                        last = stData.Last.AsFunction;
                    }
                    if (null != stData && first.GetId() == "input" && last.GetId() == "assetprocessor") {
                        var cd = first;
                        if (first.IsHighOrder)
                            cd = first.LowerOrderFunction;
                        foreach (var param in cd.Params) {
                            filters.Add(param.GetId());
                        }
                        foreach (var comp in last.Params) {
                            var processor = comp.GetId();
                            processors.Add(processor);
                        }
                    }
                }
            }
        }
    }

    internal static readonly char[] s_NumberListSeps = new[] { ',', ';', '|' };
    internal static readonly char[] s_StringListSeps = new[] { ';', '|' };

    private static MethodInfo s_GetDontDestroyOnLoadSceneMethod = null;

    private static CachedSnapshot s_CachedSnapshot = null;
    private static ShortestPathToRootObjectFinder s_ShortestPathToRootFinder = null;
    private static readonly HashSet<ObjectData> s_EmptyObjectDataHash = new HashSet<ObjectData>();
    private static readonly List<ObjectData> s_EmptyObjectDataList = new List<ObjectData>();
    private static readonly HashSet<string> s_IgnoredDirs = new HashSet<string> { "plugins", "streamingassets" };
    private static readonly List<string> s_IgnoreDirKeys = new List<string> { "assets/plugins/", "assets/streamingassets/", "/editor default resources/", "assets/thirdparty/" };

    private const string c_pref_key_open_asset_folder = "ResourceProcessor_OpenAssetFolder";
    private const string c_pref_key_load_dependencies = "ResourceProcessor_LoadDependencies";
    private const string c_pref_key_save_dependencies = "ResourceProcessor_SaveDependencies";
    private const string c_pref_key_load_instrument = "ResourceProcessor_LoadInstrument";
    private const string c_pref_key_save_instrument = "ResourceProcessor_SaveInstrument";
    private const string c_pref_key_load_utracecsv = "ResourceProcessor_LoadUTraceCsv";
    private const string c_pref_key_load_result = "ResourceProcessor_LoadResult";
    private const string c_pref_key_save_result = "ResourceProcessor_SaveResult";

    internal static ResourceProcessor Instance
    {
        get { return s_Instance; }
    }
    private static ResourceProcessor s_Instance = new ResourceProcessor();
}