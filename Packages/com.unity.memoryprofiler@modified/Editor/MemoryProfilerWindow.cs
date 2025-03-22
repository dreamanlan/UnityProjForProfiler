using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using Unity.MemoryProfilerExtension.Editor.UI;
using UnityEditor.Graphs;

[assembly: InternalsVisibleTo("Unity.MemoryProfilerExtension.Editor.Tests")]
namespace Unity.MemoryProfilerExtension.Editor
{
    public class MemoryProfilerWindow : EditorWindow
    {
        static MemoryProfilerWindow s_MemoryProfilerWindow = null;

        bool m_WindowInitialized = false;
        private bool m_InActions = false;
        private Queue<System.Action<MemoryProfilerWindow>> m_Actions = new Queue<System.Action<MemoryProfilerWindow>>();

        SnapshotDataService m_SnapshotDataService;
        PlayerConnectionService m_PlayerConnectionService;

        MemoryProfilerViewController m_ProfilerViewController;

        // Api exposed for testing purposes
        public PlayerConnectionService PlayerConnectionService => m_PlayerConnectionService;
        public SnapshotDataService SnapshotDataService => m_SnapshotDataService;
        public MemoryProfilerViewController ProfilerViewController => m_ProfilerViewController;

        [MenuItem("Dsl资源工具/Memory Profiler", false, 4)]
        public static void ShowWindow()
        {
            var window = GetWindow<MemoryProfilerWindow>();
            window.Show();
        }
        public static CachedSnapshot GetBaseCachedSnapshot()
        {
            if (null != s_MemoryProfilerWindow) {
                return s_MemoryProfilerWindow.GetBaseCachedSnapshotImpl();
            }
            else {
                return null;
            }
        }
        public static CachedSnapshot GetComparedCachedSnapshot()
        {
            if (null != s_MemoryProfilerWindow) {
                return s_MemoryProfilerWindow.GetComparedCachedSnapshotImpl();
            }
            else {
                return null;
            }
        }
        public static void Select(ObjectData data)
        {
            var tabCtrl = MemoryModelViewControllerBlockboard.tabBarController;
            tabCtrl.SelectedIndex = 2;

            var window = GetWindow<MemoryProfilerWindow>();
            window.Show(true);
            window.Focus();

            s_MemoryProfilerWindow.DelayExcuteUntil(w => DoSelect(data), CanSelect);
        }

        private static bool CanSelect()
        {
            var memoryTable = MemoryModelViewControllerBlockboard.memorySingleTableViewController;
            if (null == memoryTable) {
                memoryTable = MemoryModelViewControllerBlockboard.memoryComparisonBaseTableViewController;
            }

            if (null == memoryTable) {
                return false;
            }

            var tabMemory = MemoryModelViewControllerBlockboard.memorySingleViewController;
            var tabMemory2 = MemoryModelViewControllerBlockboard.memoryComparisonViewController;

            if ((null == tabMemory || null == tabMemory.SearchField) && (null == tabMemory2 || null == tabMemory2.SearchField)) {
                return false;
            }

            return true;
        }
        private static void DoSelect(ObjectData data)
        {
            var selection = MemoryModelViewControllerBlockboard.selectionDetails;

            var tabUnity = MemoryModelViewControllerBlockboard.unitySingleViewController;
            var tabMemory = MemoryModelViewControllerBlockboard.memorySingleViewController;

            var tabUnity2 = MemoryModelViewControllerBlockboard.unityComparisonViewController;
            var tabMemory2 = MemoryModelViewControllerBlockboard.memoryComparisonViewController;

            var unityTable = MemoryModelViewControllerBlockboard.unitySingleTableViewController;
            if (null == unityTable) {
                unityTable = MemoryModelViewControllerBlockboard.unityComparisonBaseTableViewController;
            }
            var unityTable2 = MemoryModelViewControllerBlockboard.unityComparisonComparedTableViewController;

            var memoryTable = MemoryModelViewControllerBlockboard.memorySingleTableViewController;
            if (null == memoryTable) {
                memoryTable = MemoryModelViewControllerBlockboard.memoryComparisonBaseTableViewController;
            }
            var memoryTable2 = MemoryModelViewControllerBlockboard.memoryComparisonComparedTableViewController;
            if (null == memoryTable) {
                return;
            }

            var snapshot = GetBaseCachedSnapshot();

            if (null != unityTable) {
                unityTable.ClearSelection();
            }
            if (null != memoryTable) {
                memoryTable.ClearSelection();
            }
            if (null != unityTable2) {
                unityTable2.ClearSelection();
            }
            if (null != memoryTable2) {
                memoryTable2.ClearSelection();
            }

            string search;
            if (data.isManaged) {
                search = data.GetObjectPointer(snapshot).ToString("x");
            }
            else if (data.nativeObjectIndex >= 0 && data.nativeObjectIndex < snapshot.NativeObjects.Count) {
                search = snapshot.NativeObjects.ObjectName[data.nativeObjectIndex];
            }
            else {
                search = data.GenerateTypeName(snapshot);
            }
            if (null != tabUnity && null != tabUnity.SearchField) {
                tabUnity.SearchField.value = search;
            }
            if (null != tabMemory && null != tabMemory.SearchField) {
                tabMemory.SearchField.value = search;
            }
            if (null != tabUnity2 && null != tabUnity2.SearchField) {
                tabUnity2.SearchField.value = search;
            }
            if (null != tabMemory2 && null != tabMemory2.SearchField) {
                tabMemory2.SearchField.value = search;
            }

            var filter = ScopedContainsTextFilter.Create(search);
            if (null != unityTable) {
                unityTable.SetFilters(filter);
            }
            if (null != memoryTable) {
                memoryTable.SetFilters(filter);
            }
            if (null != unityTable2) {
                unityTable2.SetFilters(filter);
            }
            if (null != memoryTable2) {
                memoryTable2.SetFilters(filter);
            }

            var window = GetWindow<MemoryProfilerWindow>();
            window.Show(true);
            window.Focus();

            memoryTable.TryExcuteAfterLoading(() => DoExpandAndSelect(data, snapshot, memoryTable), 3);
        }
        private static void DoExpandAndSelect(ObjectData data, CachedSnapshot snapshot, AllTrackedMemoryTableViewController table)
        {
            var m = table.Model;
            foreach (var root in m.RootNodes) {
                Queue<int> queue = new Queue<int>();
                if (TryExpandAndSelect(data, snapshot, table, queue, root))
                    break;
            }
        }
        private static bool TryExpandAndSelect(ObjectData data, CachedSnapshot snapshot, AllTrackedMemoryTableViewController table, Queue<int> queue, TreeViewItemData<AllTrackedMemoryModel.ItemData> item)
        {
            if (item.data.Source == data.GetSourceLink(snapshot)) {
                table.TryExpandAndSelect(queue, item.id, 0);
                return true;
            }
            else {
                queue.Enqueue(item.id);
                foreach (var c in item.children) {
                    if (TryExpandAndSelect(data, snapshot, table, queue, c)) {
                        return true;
                    }
                }
            }
            return false;
        }

        private CachedSnapshot GetBaseCachedSnapshotImpl()
        {
            return m_SnapshotDataService.Base;
        }
        private CachedSnapshot GetComparedCachedSnapshotImpl()
        {
            return m_SnapshotDataService.Compared;
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
        private void DeferAction(System.Action<MemoryProfilerWindow> action)
        {
            m_Actions.Enqueue(action);
        }
        private void DelayExcuteUntil(System.Action<MemoryProfilerWindow> action, System.Func<bool> condition)
        {
            DeferAction(w => DelayExcuteUntilRecursively(action, w, condition, 0));
        }
        private void DelayExcuteUntil(System.Action<MemoryProfilerWindow> action, System.Func<bool> condition, int waitFrames)
        {
            DeferAction(w => DelayExcuteUntilRecursively(action, w, condition, waitFrames));
        }
        private void DelayExcuteUntilRecursively(System.Action<MemoryProfilerWindow> action, MemoryProfilerWindow w, System.Func<bool> condition, int waitFrames)
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

        void OnEnable()
        {
            var icon = Icons.MemoryProfilerWindowTabIcon;
            titleContent = new GUIContent("Memory Profiler", icon);

            minSize = new Vector2(500, 500);

            // initialize quick search in the background so that it is ready for finding assets once a snapshot is openes
            QuickSearchUtility.InitializeQuickSearch(async: true);
        }

        void Init()
        {
            s_MemoryProfilerWindow = this;

            m_WindowInitialized = true;

            m_SnapshotDataService = new SnapshotDataService();
            m_PlayerConnectionService = new PlayerConnectionService(this, m_SnapshotDataService);

            // Analytics
            MemoryProfilerAnalytics.EnableAnalytics();

            m_ProfilerViewController = new MemoryProfilerViewController(m_PlayerConnectionService, m_SnapshotDataService);
            this.rootVisualElement.Add(m_ProfilerViewController.View);
        }

        // TODO: Move entirely away from IMGUI
#if ENABLE_CORECLR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeMigration", "UA1002")]
#endif
        void OnGUI()
        {
            ExecuteDeferredActions();
            if (m_WindowInitialized)
                return;

            Init();
        }

        void OnDisable()
        {
            m_WindowInitialized = false;

            m_ProfilerViewController?.Dispose();
            m_ProfilerViewController = null;

            m_PlayerConnectionService?.Dispose();
            m_PlayerConnectionService = null;

            m_SnapshotDataService?.Dispose();
            m_SnapshotDataService = null;

            MemoryProfilerAnalytics.DisableAnalytics();
        }
    }
}
