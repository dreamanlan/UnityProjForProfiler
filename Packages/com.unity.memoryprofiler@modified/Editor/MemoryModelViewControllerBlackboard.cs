using Unity.MemoryProfilerExtension.Editor.UI;

namespace Unity.MemoryProfilerExtension.Editor
{
    internal static class MemoryModelViewControllerBlockboard
    {
        public static ISelectionDetails selectionDetails;
        public static AnalysisTabBarController tabBarController;

        public static UnityObjectsBreakdownViewController unitySingleViewController;
        public static AllTrackedMemoryBreakdownViewController memorySingleViewController;

        public static UnityObjectsComparisonViewController unityComparisonViewController;
        public static AllTrackedMemoryComparisonViewController memoryComparisonViewController;

        public static SummaryViewController summaryViewController;
        public static ResidentMemorySummaryViewController residentSummaryViewController;
        public static GenericMemorySummaryViewController commitSummaryViewController;
        public static GenericMemorySummaryViewController managedSummaryViewController;
        public static GenericMemorySummaryViewController unitySummaryViewController;

        public static UnityObjectsTableViewController unitySingleTableViewController;
        public static AllTrackedMemoryTableViewController memorySingleTableViewController;

        public static UnityObjectsTableViewController unityComparisonBaseTableViewController;
        public static UnityObjectsTableViewController unityComparisonComparedTableViewController;

        public static AllTrackedMemoryTableViewController memoryComparisonBaseTableViewController;
        public static AllTrackedMemoryTableViewController memoryComparisonComparedTableViewController;


        public static void ClearSingle()
        {
            unitySingleViewController = null;
            memorySingleViewController = null;

            unitySingleTableViewController = null;
            memorySingleTableViewController = null;
        }
        public static void ClearComparison()
        {
            unityComparisonViewController = null;
            memoryComparisonViewController = null;

            unityComparisonBaseTableViewController = null;
            unityComparisonComparedTableViewController = null;

            memoryComparisonBaseTableViewController = null;
            memoryComparisonComparedTableViewController = null;
        }
    }
}
