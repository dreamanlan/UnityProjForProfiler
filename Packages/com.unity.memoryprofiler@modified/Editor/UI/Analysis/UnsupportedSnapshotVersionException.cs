using System;

namespace Unity.MemoryProfilerExtension.Editor.UI
{
    public class UnsupportedSnapshotVersionException : Exception
    {
        const string k_ErrorMessage = "Memory Snapshot `{0}` is made with unsupported Unity version ({1}).";

        public UnsupportedSnapshotVersionException(CachedSnapshot snapshot)
            : base(String.Format(k_ErrorMessage, snapshot.FullPath, snapshot.MetaData.UnityVersion))
        {
        }
    }
}
