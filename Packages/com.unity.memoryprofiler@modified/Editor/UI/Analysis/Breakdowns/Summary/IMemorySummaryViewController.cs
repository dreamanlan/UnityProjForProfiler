using System;

namespace Unity.MemoryProfilerExtension.Editor.UI
{
    public interface IMemorySummaryViewController : IDisposable
    {
        event Action<MemorySummaryModel, int> OnRowSelected;

        bool Normalized { get; set; }

        void ClearSelection();
        ViewController MakeSelection(int rowId);
        void Update();
    }
}
