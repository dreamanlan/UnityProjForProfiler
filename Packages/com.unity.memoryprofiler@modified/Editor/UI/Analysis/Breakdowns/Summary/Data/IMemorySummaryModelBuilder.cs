namespace Unity.MemoryProfilerExtension.Editor.UI
{
    public interface IMemorySummaryModelBuilder<T> where T : MemorySummaryModel
    {
        T Build();
    }
}
