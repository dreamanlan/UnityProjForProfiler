using UnityEngine;
namespace Unity.MemoryProfilerExtension.Editor.UI
{
    public interface IInstancIdFilter : ITableFilter<InstanceID>
    {
        public CachedSnapshot SourceSnapshot { get; }
    }
}
