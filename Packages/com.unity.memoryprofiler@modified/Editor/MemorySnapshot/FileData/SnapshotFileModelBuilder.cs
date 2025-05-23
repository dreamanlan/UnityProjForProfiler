using System;
using System.IO;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MemoryProfilerExtension.Editor.Format;
using Unity.MemoryProfilerExtension.Editor.Format.QueriedSnapshot;

namespace Unity.MemoryProfilerExtension.Editor
{
    public class SnapshotFileModelBuilder
    {
        string m_FileName;

        public SnapshotFileModelBuilder(string fileName)
        {
            m_FileName = fileName;
        }

        public SnapshotFileModel Build()
        {
            using var reader = new FileReader();

            ReadError error = reader.Open(m_FileName);
            if (error != ReadError.Success)
                return null;

            MetaData snapshotMetadata = new MetaData(reader);

            var totalResident = 0UL;
            var totalCommitted = 0UL;
            DateTime timestamp = DateTime.Now;
            unsafe
            {
                long ticks;
                reader.ReadUnsafe(EntryType.Metadata_RecordDate, &ticks, sizeof(long), 0, 1);
                timestamp = new DateTime(ticks);

                var count = reader.GetEntryCount(EntryType.SystemMemoryRegions_Address);
                using var regionSize = reader.Read(EntryType.SystemMemoryRegions_Size, 0, count, Allocator.TempJob).Result.Reinterpret<ulong>();
                using var regionResident = reader.Read(EntryType.SystemMemoryRegions_Resident, 0, count, Allocator.TempJob).Result.Reinterpret<ulong>();
                for (int i = 0; i < count; i++)
                {
                    totalResident += regionResident[i];
                    totalCommitted += regionSize[i];
                }
            }

            var maxAvailable = 0UL;
            if (snapshotMetadata.TargetInfo.HasValue)
                maxAvailable = snapshotMetadata.TargetInfo.Value.TotalPhysicalMemory;

            bool editorPlatform = snapshotMetadata.IsEditorCapture;
            var runtimePlatform = PlatformsHelper.GetRuntimePlatform(snapshotMetadata.Platform);

            var scriptingBackendName = snapshotMetadata.TargetInfo.HasValue ? snapshotMetadata.TargetInfo.Value.ScriptingBackend.ToString() : "Unknown";

            // Fix up the name of Mono to be nicer than Mono2x
            if (snapshotMetadata.TargetInfo.HasValue && snapshotMetadata.TargetInfo.Value.ScriptingBackend == UnityEditor.ScriptingImplementation.Mono2x)
                scriptingBackendName = "Mono";

            return new SnapshotFileModel(
                Path.GetFileNameWithoutExtension(m_FileName),
                m_FileName,
                snapshotMetadata.ProductName,
                snapshotMetadata.Description,
                snapshotMetadata.SessionGUID,
                timestamp,
                runtimePlatform,
                editorPlatform,
                snapshotMetadata.UnityVersion,
                snapshotMetadata.TargetInfo.HasValue,
                totalCommitted,
                totalResident,
                maxAvailable,
                snapshotMetadata.CaptureFlags,
                scriptingBackendName);
        }
    }
}
