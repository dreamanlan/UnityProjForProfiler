using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
//using UnityEditor.UI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEditor.Profiling.Memory.Experimental;
using Unity.MemoryProfilerExtension.Editor;
using Unity.MemoryProfilerExtension.Editor.UI;
using Unity.MemoryProfilerExtension.Editor.EnumerationUtilities;
using Unity.Profiling;
using StoryScript;

public class BatchResourceProcessWindow : EditorWindow
{
    internal static void InitWindow(ResourceEditWindow resEdit)
    {
        BatchResourceProcessWindow window = (BatchResourceProcessWindow)EditorWindow.GetWindow(typeof(BatchResourceProcessWindow));
        window.Init(resEdit);
        window.Show();
    }

    private void Init(ResourceEditWindow resEdit)
    {
        m_ResourceEditWindow = resEdit;
        m_IsReady = true;
    }

    private void OnGUI()
    {
        bool handle = false;
        int deleteIndex = -1;
        EditorGUILayout.BeginHorizontal();
        ResourceEditUtility.EnableSaveAndReimport = EditorGUILayout.Toggle("允许SaveAndReimport", ResourceEditUtility.EnableSaveAndReimport);
        ResourceEditUtility.UseSpecificSettingDB = EditorGUILayout.Toggle("使用特殊设置DB数据", ResourceEditUtility.UseSpecificSettingDB);
        ResourceEditUtility.ForceSaveAndReimport = EditorGUILayout.Toggle("强制SaveAndReimport", ResourceEditUtility.ForceSaveAndReimport);
        EditorGUILayout.EndHorizontal();
        var rt = EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载", EditorStyles.toolbarButton)) {
            DeferAction(w => Load());
        }
        if (GUILayout.Button("导入", EditorStyles.toolbarButton)) {
            DeferAction(w => Import());
        }
        if (GUILayout.Button("添加", EditorStyles.toolbarButton)) {
            m_IsReady = false;
            if (m_List.Count > 0) {
                var last = m_List[m_List.Count - 1];
                m_List.Add(new ResourceEditUtility.BatchProcessInfo { DslPath = last.DslPath, ResPath = last.ResPath });
            }
            else {
                m_List.Add(new ResourceEditUtility.BatchProcessInfo());
            }
            m_IsReady = true;
        }
        EditorGUILayout.EndHorizontal();
        if (m_IsReady) {
            m_Pos = EditorGUILayout.BeginScrollView(m_Pos);
            for (int i = 0; i < m_List.Count; ++i) {
                EditorGUILayout.BeginHorizontal();
                var info = m_List[i];
                EditorGUILayout.LabelField("资源:", GUILayout.Width(40));
                EditorGUILayout.LabelField(info.ResPath);
                if (GUILayout.Button("选择", GUILayout.Width(40))) {
                    m_IsReady = false;
                    string res = EditorUtility.OpenFolderPanel("选择资源路径", string.Empty, string.Empty);
                    if (!string.IsNullOrEmpty(res)) {
                        if (IsAssetPath(res)) {
                            info.ResPath = FilePathToRelativePath(res);
                        }
                        else {
                            EditorUtility.DisplayDialog("错误", "必须选择本unity工程的资源路径！", "确定");
                        }
                    }
                    m_IsReady = true;
                }
                EditorGUILayout.LabelField("脚本:", GUILayout.Width(40));
                EditorGUILayout.LabelField(info.DslPath);
                if (GUILayout.Button("选择", EditorStyles.toolbarButton, GUILayout.Width(40))) {
                    m_IsReady = false;
                    string res = EditorUtility.OpenFilePanel("选择dsl处理脚本", string.Empty, "dsl");
                    if (!string.IsNullOrEmpty(res)) {
                        info.DslPath = FilePathToRelativePath(res);
                    }
                    m_IsReady = true;
                }
                if (GUILayout.Button("删除", EditorStyles.toolbarButton, GUILayout.Width(40))) {
                    deleteIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存", EditorStyles.toolbarButton)) {
            DeferAction(w => Save());
        }
        if (GUILayout.Button("导出", EditorStyles.toolbarButton)) {
            DeferAction(w => Export());
        }
        if (GUILayout.Button("处理", EditorStyles.toolbarButton)) {
            handle = true;
        }
        EditorGUILayout.EndHorizontal();
        if (deleteIndex >= 0) {
            m_List.RemoveAt(deleteIndex);
        }
        if (handle) {
            int ct = m_List.Count;
            int ix = 0;
            m_ResourceEditWindow.QueueProcessBegin();
            foreach (var info in m_List) {
                if (!string.IsNullOrEmpty(info.DslPath)) {
                    m_ResourceEditWindow.QueueProcess(RelativePathToFilePath(info.DslPath), RelativePathToFilePath(info.ResPath), ix, ct);
                }
                ++ix;
            }
            m_ResourceEditWindow.QueueProcessEnd();
            DeferAction(w => w.Close());
        }

        ExecuteDeferredActions();
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
    private void DeferAction(Action<BatchResourceProcessWindow> action)
    {
        m_Actions.Enqueue(action);
    }

    private void Load()
    {
        m_IsReady = false;
        try {
            string path = EditorUtility.OpenFilePanel("请指定要加载的批量处理文件", string.Empty, "txt");
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
                int i = 0;
                try {
                    var txt = File.ReadAllText(path);
                    var lines = txt.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    m_List.Clear();
                    int curCount = 1;
                    int totalCount = lines.Length;
                    for (i = 1; i < lines.Length; ++i) {
                        var fields = lines[i].Split('\t');
                        var resPath = fields[0];
                        var dslPath = fields[1];

                        var item = new ResourceEditUtility.BatchProcessInfo { ResPath = resPath, DslPath = dslPath };
                        m_List.Add(item);

                        ++curCount;
                        EditorUtility.DisplayProgressBar("加载进度", string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
                    }
                }
                catch (Exception ex) {
                    EditorUtility.DisplayDialog("异常", string.Format("line {0} exception {1}\n{2}", i, ex.Message, ex.StackTrace), "ok");
                }
                EditorUtility.ClearProgressBar();
            }
        }
        finally {
            m_IsReady = true;
        }
    }

    private void Save()
    {
        m_IsReady = false;
        try {
            if (m_List.Count > 0) {
                string path = EditorUtility.SaveFilePanel("请指定要保存批量处理的文件", string.Empty, "batch", "txt");
                if (!string.IsNullOrEmpty(path)) {
                    if (File.Exists(path)) {
                        File.Delete(path);
                    }
                    using (StreamWriter sw = new StreamWriter(path)) {
                        sw.WriteLine("res_path\tdsl_path");
                        int curCount = 0;
                        int totalCount = m_List.Count;
                        foreach (var item in m_List) {
                            sw.WriteLine("{0}\t{1}", item.ResPath, item.DslPath);
                            ++curCount;
                            EditorUtility.DisplayProgressBar("保存进度", string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
                        }
                        sw.Close();
                        EditorUtility.ClearProgressBar();
                    }
                }
            }
            else {
                EditorUtility.DisplayDialog("错误", "没有要保存的数据！", "ok");
            }
        }
        finally {
            m_IsReady = true;
        }
    }

    private void Import()
    {
        m_IsReady = false;
        try {
            m_List.Clear();
            for (int i = 0; i < AssetProcessorDB.DBInstance.Count; ++i) {
                var info = AssetProcessorDB.DBInstance[i];
                m_List.Add(new ResourceEditUtility.BatchProcessInfo { ResPath = info.ResPath, DslPath = info.DslPath });
            }
        }
        finally {
            m_IsReady = true;
        }
    }

    private void Export()
    {
        m_IsReady = false;
        try {
            AssetProcessorDB.DBInstance.Clear();
            foreach (var info in m_List) {
                if (!string.IsNullOrEmpty(info.DslPath)) {
                    var item = new AssetProcessorInfo();
                    item.ResPath = info.ResPath;
                    item.DslPath = info.DslPath;
                    AssetProcessorDB.DBInstance.Add(item);
                }
            }
            AssetProcessorDB.DBInstance.Save();
        }
        finally {
            m_IsReady = true;
        }
    }

    private bool IsAssetPath(string path)
    {
        return ResourceEditUtility.IsAssetPath(path);
    }
    private string FilePathToRelativePath(string path)
    {
        return ResourceEditUtility.FilePathToRelativePath(path);
    }
    private string RelativePathToFilePath(string path)
    {
        return ResourceEditUtility.RelativePathToFilePath(path);
    }
    private string GetRootPath()
    {
        return ResourceEditUtility.GetRootPath();
    }

    private bool m_IsReady = false;
    private bool m_InActions = false;
    private Queue<Action<BatchResourceProcessWindow>> m_Actions = new Queue<Action<BatchResourceProcessWindow>>();

    private ResourceEditWindow m_ResourceEditWindow = null;
    private bool m_UseReimport = false;
    private List<ResourceEditUtility.BatchProcessInfo> m_List = new List<ResourceEditUtility.BatchProcessInfo>();
    private Vector2 m_Pos = Vector2.zero;
}

public class BatchLoadWindow : EditorWindow
{
    internal static void InitWindow()
    {
        BatchLoadWindow window = (BatchLoadWindow)EditorWindow.GetWindow(typeof(BatchLoadWindow));
        window.Init();
        window.Show();
    }

    private void Init()
    {
        m_IsReady = true;
    }

    private void OnGUI()
    {
        bool handle = false;
        int deleteIndex = -1;
        var rt = EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("添加", EditorStyles.toolbarButton)) {
            m_IsReady = false;
            if (m_List.Count > 0) {
                var last = m_List[m_List.Count - 1];
                m_List.Add(string.Empty);
            }
            else {
                m_List.Add(string.Empty);
            }
            m_IsReady = true;
        }
        EditorGUILayout.EndHorizontal();
        if (m_IsReady) {
            m_Pos = EditorGUILayout.BeginScrollView(m_Pos);
            for (int i = 0; i < m_List.Count; ++i) {
                EditorGUILayout.BeginHorizontal();
                var info = m_List[i];
                EditorGUILayout.LabelField("快照:", GUILayout.Width(40));
                EditorGUILayout.LabelField(info);
                if (GUILayout.Button("选择", EditorStyles.toolbarButton, GUILayout.Width(40))) {
                    m_IsReady = false;
                    string res = EditorUtility.OpenFilePanel("选择", string.Empty, "snap");
                    if (!string.IsNullOrEmpty(res)) {
                        m_List[i] = res;
                    }
                    m_IsReady = true;
                }
                if (GUILayout.Button("删除", EditorStyles.toolbarButton, GUILayout.Width(40))) {
                    deleteIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载", EditorStyles.toolbarButton)) {
            handle = true;
        }
        EditorGUILayout.EndHorizontal();
        if (deleteIndex >= 0) {
            m_List.RemoveAt(deleteIndex);
        }
        if (handle) {
            LoadAll();
        }
    }

    private void LoadAll()
    {
        m_IsReady = false;
        try {
            foreach (var filePath in m_List) {
                if (File.Exists(filePath)) {
                    try {
                        Debug.LogFormat("Loading \"{0}\"", filePath);
                        var reader = new Unity.MemoryProfilerExtension.Editor.Format.QueriedSnapshot.FileReader();
                        reader.Open(filePath);

                        var cachedSnapshot = SnapshotDataService.LoadSnapshot(reader);

                        Debug.LogFormat("Completed loading \"{0}\"", filePath);

                        Debug.LogFormat("Saving \"{0}\"", filePath);
                        var path = Path.GetDirectoryName(filePath);
                        var snapName = Path.GetFileNameWithoutExtension(filePath);

                        string exportPath1 = Path.Combine(path, snapName + "_MANAGEDHEAP_SnapshotExport_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + ".csv");
                        if (!String.IsNullOrEmpty(exportPath1)) {
                            System.IO.StreamWriter sw = new System.IO.StreamWriter(exportPath1);
                            sw.WriteLine("Managed_Objects,Size,Address");
                            long ct = cachedSnapshot.ManagedHeapSections.Count;
                            for (int i = 0; i < ct; i++) {
                                var size = cachedSnapshot.ManagedHeapSections.SectionSize[i];
                                var addr = cachedSnapshot.ManagedHeapSections.StartAddress[i];
                                sw.WriteLine("Managed," + size + "," + addr);
                                ResourceProcessor.Instance.DisplayProgressBar("write managed heap ...", i, ct);
                            }
                            sw.Flush();
                            sw.Close();
                        }

                        string exportPath2 = Path.Combine(path, snapName + "_MANAGED_SnapshotExport_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + ".csv");
                        if (!String.IsNullOrEmpty(exportPath2)) {
                            m_ManagedGroups.Clear();
                            System.IO.StreamWriter sw = new System.IO.StreamWriter(exportPath2);
                            sw.WriteLine("Index,Type,RefCount,Size,Address");

                            long ct = cachedSnapshot.SortedManagedObjects.Count;
                            for (int i = 0; i < ct; i++) {
                                var size = cachedSnapshot.SortedManagedObjects.Size(i);
                                var addr = cachedSnapshot.SortedManagedObjects.Address(i);
                                ManagedObjectInfo objInfo;
                                long index = -1;
                                int refCount = 0;
                                string typeName = string.Empty;
                                if (cachedSnapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(addr, out index)) {
                                    objInfo = cachedSnapshot.CrawledData.ManagedObjects[index];
                                    index = objInfo.ManagedObjectIndex;
                                    refCount = objInfo.RefCount;
                                    if (objInfo.ITypeDescription >= 0 && objInfo.ITypeDescription < cachedSnapshot.TypeDescriptions.Count) {
                                        typeName = cachedSnapshot.TypeDescriptions.TypeDescriptionName[objInfo.ITypeDescription];
                                    }
                                }
                                sw.WriteLine("" + index + ",\"" + typeName + "\"," + refCount + "," + size + "," + addr);

                                ManagedGroupInfo info;
                                if (m_ManagedGroups.TryGetValue(typeName, out info)) {
                                    ++info.Count;
                                    info.Size += size;
                                }
                                else {
                                    string g = string.Empty;
                                    int si = typeName.IndexOf('.');
                                    if (si > 0) {
                                        g = typeName.Substring(0, si);
                                        if (!s_ManagedGroupNames.Contains(g)) {
                                            g = string.Empty;
                                        }
                                    }
                                    info = new ManagedGroupInfo { Group = g, Type = typeName, Count = 1, Size = size };
                                    m_ManagedGroups.Add(typeName, info);
                                }
                                if (i % 1000 == 0) {
                                    ResourceProcessor.Instance.DisplayProgressBar("write managed objects ...", i, ct);
                                }
                            }
                            sw.Flush();
                            sw.Close();

                            string dir = Path.GetDirectoryName(exportPath2);
                            string fn = Path.GetFileNameWithoutExtension(exportPath2);
                            string gpath = Path.Combine(dir, fn + "_groups.csv");
                            var lastGroup = "A000000";
                            using (var outsw = new StreamWriter(gpath)) {
                                outsw.WriteLine("group,type,count,size");
                                int curCt = 0;
                                int totalCt = m_ManagedGroups.Count;
                                foreach (var pair in m_ManagedGroups) {
                                    var info = pair.Value;
                                    var g = info.Group;
                                    if (!string.IsNullOrEmpty(lastGroup) && string.IsNullOrEmpty(info.Group))
                                        g = lastGroup + "__";
                                    if (!string.IsNullOrEmpty(info.Group))
                                        lastGroup = info.Group;
                                    outsw.WriteLine("\"{0}\",\"{1}\",{2},{3}", g, info.Type, info.Count, info.Size);
                                    if (curCt % 100 == 0) {
                                        ResourceProcessor.Instance.DisplayProgressBar("write managed object group ...", curCt, totalCt);
                                    }
                                    ++curCt;
                                }
                            }
                            string gpath2 = Path.Combine(dir, fn + "_groups_forcmp.csv");
                            using (var outsw = new StreamWriter(gpath2)) {
                                outsw.WriteLine("type,count,size");
                                int curCt = 0;
                                int totalCt = m_ManagedGroups.Count;
                                foreach (var pair in m_ManagedGroups) {
                                    var info = pair.Value;
                                    outsw.WriteLine("\"{0}\",{1},{2}", info.Type, info.Count, info.Size);
                                    if (curCt % 100 == 0) {
                                        ResourceProcessor.Instance.DisplayProgressBar("write managed object group for cmp ...", curCt, totalCt);
                                    }
                                    ++curCt;
                                }
                            }
                        }

                        string exportPath = Path.Combine(path, snapName + "_NATIVE_SnapshotExport_" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_ss") + ".csv");
                        if (!String.IsNullOrEmpty(exportPath)) {
                            m_NativeGroups.Clear();
                            System.IO.StreamWriter sw = new System.IO.StreamWriter(exportPath);
                            sw.WriteLine("Index,Name,Type,RefCount,InstanceID,Size,Address");
                            long ct = cachedSnapshot.SortedNativeObjects.Count;
                            for (int i = 0; i < ct; i++) {
                                var size = cachedSnapshot.SortedNativeObjects.Size(i);
                                var addr = cachedSnapshot.SortedNativeObjects.Address(i);
                                var name = cachedSnapshot.SortedNativeObjects.Name(i);
                                var refCount = cachedSnapshot.SortedNativeObjects.Refcount(i);
                                var instanceId = cachedSnapshot.SortedNativeObjects.InstanceId(i);
                                long index;
                                if (!cachedSnapshot.NativeObjects.InstanceId2Index.TryGetValue(instanceId, out index)) {
                                    index = -1;
                                }
                                var nativeTypeIndex = cachedSnapshot.SortedNativeObjects.NativeTypeArrayIndex(i);
                                string typeName = string.Empty;
                                if (nativeTypeIndex >= 0 && nativeTypeIndex < cachedSnapshot.NativeTypes.Count) {
                                    typeName = cachedSnapshot.NativeTypes.TypeName[nativeTypeIndex];
                                }

                                sw.WriteLine("" + index + ",\"" + name + "\",\"" + typeName + "\"," + refCount + "," + instanceId + "," + size + "," + addr);

                                NativeGroupInfo info;
                                if (m_NativeGroups.TryGetValue(typeName, out info)) {
                                    ++info.Count;
                                    info.Size += size;
                                }
                                else {
                                    info = new NativeGroupInfo { Type = typeName, Count = 1, Size = size };
                                    m_NativeGroups.Add(typeName, info);
                                }
                                if (i % 100 == 0) {
                                    ResourceProcessor.Instance.DisplayProgressBar("write native objects ...", i, ct);
                                }
                            }
                            sw.Flush();
                            sw.Close();

                            string dir = Path.GetDirectoryName(exportPath);
                            string fn = Path.GetFileNameWithoutExtension(exportPath);
                            string gpath = Path.Combine(dir, fn + "_groups.csv");
                            using (var outsw = new StreamWriter(gpath)) {
                                outsw.WriteLine("type,count,size");
                                int curCt = 0;
                                int totalCt = m_NativeGroups.Count;
                                foreach (var pair in m_NativeGroups) {
                                    var info = pair.Value;
                                    outsw.WriteLine("\"{0}\",{1},{2}", info.Type, info.Count, info.Size);
                                    ResourceProcessor.Instance.DisplayProgressBar("write native object group ...", curCt, totalCt);
                                    ++curCt;
                                }
                            }
                        }

                        Debug.LogFormat("Completed saving \"{0}\"", filePath);
                    }
                    catch (Exception ex) {
                        UnityEngine.Debug.LogErrorFormat("file {0} exception {1}\n{2}", filePath, ex.Message, ex.StackTrace);
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }
        finally {
            m_IsReady = true;
        }
    }

    internal class ManagedGroupInfo
    {
        internal string Group;
        internal string Type;
        internal int Count;
        internal ulong Size;
    }
    internal class NativeGroupInfo
    {
        internal string Type;
        internal int Count;
        internal ulong Size;
    }

    private SortedDictionary<string, ManagedGroupInfo> m_ManagedGroups = new SortedDictionary<string, ManagedGroupInfo>();
    private SortedDictionary<string, NativeGroupInfo> m_NativeGroups = new SortedDictionary<string, NativeGroupInfo>();

    private bool m_IsReady = false;
    private List<string> m_List = new List<string>();
    private Vector2 m_Pos = Vector2.zero;

    private static HashSet<string> s_ManagedGroupNames = new HashSet<string> { "System" };
    private static ProfilerMarker s_CrawlManagedData = new ProfilerMarker("CrawlManagedData");

    private static string CleanStrings(string text)
    {
        return text.Replace(",", " ");
    }
}
