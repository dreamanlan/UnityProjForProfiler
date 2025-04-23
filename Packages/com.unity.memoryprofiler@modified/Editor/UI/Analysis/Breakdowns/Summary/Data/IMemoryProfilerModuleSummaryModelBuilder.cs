namespace Unity.MemoryProfilerExtension.Editor.UI
{
    public interface IMemoryProfilerModuleSummaryModelBuilder<T> : IMemorySummaryModelBuilder<T> where T : MemorySummaryModel
    {
        long Frame { get; set; }
    }
}
