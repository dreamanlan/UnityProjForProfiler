using System;
using System.Runtime.InteropServices;
#if DEBUG_VALIDATION
using Unity.MemoryProfilerExtension.Editor.Diagnostics;
#endif
using Unity.MemoryProfilerExtension.Editor.Format;
using UnityEngine;
using static Unity.MemoryProfilerExtension.Editor.CachedSnapshot;
using Unity.MemoryProfilerExtension.Editor.Managed;
using System.Collections.Generic;
using Unity.MemoryProfilerExtension.Editor.UI.PathsToRoot;

namespace Unity.MemoryProfilerExtension.Editor
{
    public enum ObjectDataType
    {
        Unknown,
        Value,
        Object,
        Array,
        BoxedValue,
        ReferenceObject,
        ReferenceArray,
        Type,
        NativeObject,
        NativeAllocation,
    }

    public enum CodeType
    {
        Native,
        Managed,
        Unknown,
        Count,
    }

    public class ObjectDataParent
    {
        public ObjectData Obj;
        public int iField;
        public long arrayIndex;
        /// <summary>
        /// true means it should display the value/target of the field. False means it should display the owning object
        /// </summary>
        public bool expandToTarget;
        public ObjectDataParent(ObjectData obj, int iField, long arrayIndex, bool expandToTarget)
        {
            this.Obj = obj;
            this.iField = iField;
            this.arrayIndex = arrayIndex;
            this.expandToTarget = expandToTarget;
        }
    }
    public struct ObjectData
    {
        private void SetManagedType(CachedSnapshot snapshot, int iType)
        {
            m_data.managed.iType = iType;
        }

        public static InstanceID InvalidInstanceID
        {
            get
            {
                return CachedSnapshot.NativeObjectEntriesCache.InstanceIDNone;
            }
        }
        private ObjectDataType m_dataType;
        /// <summary>
        /// used for reference object/array and value to hold the owning object.
        /// </summary>
        public ObjectDataParent Parent { get; private set; }
        public ObjectData displayObject
        {
            get
            {
                if (Parent != null && !Parent.expandToTarget)
                {
                    return Parent.Obj.displayObject;
                }
                return this;
            }
        }
        [StructLayout(LayoutKind.Explicit)]
        public struct Data
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Managed
            {
                public ulong objectPtr;
                public int iType;
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct Native
            {
                public long index;
            }
            [FieldOffset(0)] public Managed managed;
            [FieldOffset(0)] public Native native;
        }
        private Data m_data;
        public int managedTypeIndex
        {
            get
            {
                switch (m_dataType)
                {
                    case ObjectDataType.Array:
                    case ObjectDataType.BoxedValue:
                    case ObjectDataType.Object:
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.Value:
                    case ObjectDataType.Type:
                        return m_data.managed.iType;
                }

                return -1;
            }
        }
        public BytesAndOffset managedObjectData;

        public ObjectDataType dataType
        {
            get
            {
                return m_dataType;
            }
        }
        public long nativeObjectIndex
        {
            get
            {
                if (m_dataType == ObjectDataType.NativeObject)
                {
                    return m_data.native.index;
                }
                return -1;
            }
        }
        public ulong hostManagedObjectPtr
        {
            get
            {
                switch (m_dataType)
                {
                    case ObjectDataType.Array:
                    case ObjectDataType.BoxedValue:
                    case ObjectDataType.Object:
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.Value:
                        return m_data.managed.objectPtr;
                }
                return 0;
            }
        }

        public int fieldIndex
        {
            get
            {
                switch (m_dataType)
                {
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.Value:
                        if (Parent != null)
                        {
                            return Parent.iField;
                        }
                        break;
                }
                return -1;
            }
        }
        public long arrayIndex
        {
            get
            {
                switch (m_dataType)
                {
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.Value:
                        if (Parent != null)
                        {
                            return Parent.arrayIndex;
                        }
                        break;
                }
                return -1;
            }
        }
        public bool dataIncludeObjectHeader
        {
            get
            {
                switch (m_dataType)
                {
                    case ObjectDataType.Unknown:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.Value:
                    case ObjectDataType.Type:
                        return false;
                    case ObjectDataType.Array:
                    case ObjectDataType.Object:
                    case ObjectDataType.BoxedValue:
                        return true;
                }
                throw new Exception("Bad datatype");
            }
        }

        public bool IsValid
        {
            get
            {
                return m_dataType != ObjectDataType.Unknown;//return data.IsValid;
            }
        }

        public bool HasValidFieldOrArrayElementData(CachedSnapshot cs)
        {
            if (IsField() || IsArrayItem())
            {
                if (Parent.Obj.dataType == ObjectDataType.ReferenceObject)
                {
                    unsafe
                    {
                        if (Parent.Obj.managedObjectData.Bytes.GetUnsafePtr() == managedObjectData.Bytes.GetUnsafePtr()
                            && managedObjectData.TryReadPointer(out var ptr) == BytesAndOffset.PtrReadError.Success)
                        {
                            cs.ManagedHeapSections.Find(ptr, cs.VirtualMachineInformation, out managedObjectData);
#if DEBUG_VALIDATION
                            if(!managedObjectData.IsValid)
                                Debug.LogError("Invalid object data");
#endif
                        }
                    }
                }
                switch (m_dataType)
                {
                    case ObjectDataType.Value:
                        return (managedObjectData.IsValid && managedObjectData.Bytes.IsCreated
                            && (managedObjectData.Bytes.Count - (long)managedObjectData.Offset) >= cs.TypeDescriptions.Size[managedTypeIndex]);
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.Object:
                    case ObjectDataType.Array:
                    case ObjectDataType.BoxedValue:
                    case ObjectDataType.Type:
                        return (!managedObjectData.IsValid || !managedObjectData.Bytes.IsCreated
                            || ((ulong)managedObjectData.Bytes.Count - managedObjectData.Offset) >= 8UL);
                    case ObjectDataType.NativeObject:
                    case ObjectDataType.NativeAllocation:
                        return true;
                    case ObjectDataType.Unknown:
                    default:
                        return false;
                }
            }
            return false;
        }


        public ObjectFlags GetFlags(CachedSnapshot cs)
        {
            switch (dataType)
            {
                case ObjectDataType.NativeObject:
                    return cs.NativeObjects.Flags[nativeObjectIndex];
                case ObjectDataType.Unknown:
                case ObjectDataType.Value:
                case ObjectDataType.Object:
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.Type:
                default:
                    return 0;
            }
        }

        public bool HasFields(CachedSnapshot cachedSnapshot)
        {
            return GetInstanceFieldCount(cachedSnapshot) > 0;
        }

        public bool TryGetObjectPointer(out ulong ptr)
        {
            switch (dataType)
            {
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.Object:
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Value:
                    ptr = hostManagedObjectPtr;
                    return true;
                default:
                    ptr = 0;
                    return false;
            }
        }

        public ulong GetObjectPointer(CachedSnapshot snapshot, bool logError = true)
        {
            switch (dataType)
            {
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.ReferenceObject:
                    return GetReferencePointer();
                case ObjectDataType.Object:
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                    return hostManagedObjectPtr;
                case ObjectDataType.Value:
                    ulong offset = 0;
                    bool isStatic = false;
                    if (IsField())
                    {
                        int fieldIdx = fieldIndex;
                        offset = (ulong)snapshot.FieldDescriptions.Offset[fieldIdx];
                        isStatic = snapshot.FieldDescriptions.IsStatic[fieldIdx] == 1;
                        if (isStatic)
                        {
                            offset = snapshot.TypeDescriptions.TypeInfoAddress[Parent.Obj.managedTypeIndex];
                            offset += (ulong)snapshot.TypeDescriptions.Size[Parent.Obj.managedTypeIndex];

                            var staticFieldIndices = snapshot.TypeDescriptions.fieldIndicesStatic[Parent.Obj.managedTypeIndex];

                            for (int i = 0; i < staticFieldIndices.Length; ++i)
                            {
                                var cFieldIdx = staticFieldIndices[i];
                                if (cFieldIdx == fieldIdx)
                                    break;
                                offset += (ulong)snapshot.FieldDescriptions.Offset[cFieldIdx];
                            }
                        }
                    }
                    else if (arrayIndex >= 0) //compute our offset within the array
                    {
                        offset += (ulong)(snapshot.VirtualMachineInformation.ArrayHeaderSize + arrayIndex * snapshot.TypeDescriptions.Size[managedTypeIndex]);
                    }

                    return isStatic ? offset : hostManagedObjectPtr + offset;
                case ObjectDataType.NativeObject:
                    return snapshot.NativeObjects.NativeObjectAddress[nativeObjectIndex];
                case ObjectDataType.NativeAllocation:
                    return snapshot.NativeAllocations.Address[m_data.native.index];
                case ObjectDataType.Type:
                    if (m_data.managed.iType >= 0)
                        return snapshot.TypeDescriptions.TypeInfoAddress[m_data.managed.iType];
                    if (logError)
                        Debug.LogError("Requesting an object pointer on an invalid data type");
                    return 0;
                default:
                    if (logError)
                        Debug.LogError("Requesting an object pointer on an invalid data type");
                    return 0;
            }
        }

        public ulong GetReferencePointer()
        {
            switch (m_dataType)
            {
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                    ulong ptr;
                    managedObjectData.TryReadPointer(out ptr);
                    return ptr;
                default:
                    UnityEngine.Debug.LogError("Requesting a reference pointer on an invalid data type");
                    return 0;
            }
        }

        public ObjectData GetBoxedValue(CachedSnapshot snapshot, bool expandToTarget)
        {
            switch (m_dataType)
            {
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                    break;
                default:
                    UnityEngine.Debug.LogError("Requesting a boxed value on an invalid data type");
                    return Invalid;
            }
            ObjectData od = this;
            od.Parent = new ObjectDataParent(this, -1, -1, expandToTarget);
            od.m_dataType = ObjectDataType.Value;
            od.managedObjectData = od.managedObjectData.Add(snapshot.VirtualMachineInformation.ObjectHeaderSize);
            return od;
        }

        public ArrayInfo GetArrayInfo(CachedSnapshot snapshot)
        {
            switch (m_dataType)
            {
                case ObjectDataType.Array:
                    return ManagedHeapArrayDataTools.GetArrayInfo(snapshot, managedObjectData, m_data.managed.iType);
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Value:
                    // unroll to actual array
                    return Parent?.Obj.GetArrayInfo(snapshot);
                case ObjectDataType.Unknown:
                case ObjectDataType.Type:
                case ObjectDataType.Object:
                case ObjectDataType.NativeObject:
                case ObjectDataType.NativeAllocation:
                default:
                    Debug.LogError("Requesting Array info on an invalid data type");
                    return null;
            }
        }

        public ObjectData GetArrayElement(CachedSnapshot snapshot, long index, bool expandToTarget)
        {
            return GetArrayElement(snapshot, GetArrayInfo(snapshot), index, expandToTarget);
        }

        public ObjectData GetArrayElement(CachedSnapshot snapshot, ArrayInfo ai, long index, bool expandToTarget)
        {
            switch (m_dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.ReferenceArray:
                    break;
                default:
                    Debug.Log("Requesting an array element on an invalid data type");
                    return Invalid;
            }
            ObjectData o = new ObjectData();
            o.Parent = new ObjectDataParent(this, -1, index, expandToTarget);
            o.SetManagedType(snapshot, ai.ElementTypeDescription);
            o.m_data.managed.objectPtr = m_data.managed.objectPtr;
            o.m_dataType = TypeToSubDataType(snapshot, ai.ElementTypeDescription);
            o.managedObjectData = ai.GetArrayElement((uint)index);
            return o;
        }

        public static ObjectDataType TypeToSubDataType(CachedSnapshot snapshot, int iType)
        {
            if (iType < 0)
                return ObjectDataType.Unknown;
            if (snapshot.TypeDescriptions.HasFlag(iType, TypeFlags.kArray))
                return ObjectDataType.ReferenceArray;
            else if (snapshot.TypeDescriptions.HasFlag(iType, TypeFlags.kValueType))
                return ObjectDataType.Value;
            else
                return ObjectDataType.ReferenceObject;
        }

        public static ObjectDataType TypeToDataType(CachedSnapshot snapshot, int iType)
        {
            if (iType < 0)
                return ObjectDataType.Unknown;
            if (snapshot.TypeDescriptions.HasFlag(iType, TypeFlags.kArray))
                return ObjectDataType.Array;
            else if (snapshot.TypeDescriptions.HasFlag(iType, TypeFlags.kValueType))
                return ObjectDataType.BoxedValue;
            else
                return ObjectDataType.Object;
        }

        // ObjectData is pointing to an object's field
        public bool IsField()
        {
            return Parent != null && Parent.iField >= 0;
        }

        // ObjectData is pointing to an item in an array
        public bool IsArrayItem()
        {
            return Parent != null && Parent.Obj.dataType == ObjectDataType.Array;
        }

        // Returns the name of the field this ObjectData is pointing at.
        // should be called only when IsField() return true
        public string GetFieldName(CachedSnapshot snapshot, bool truncateTypeName = false)
        {
            if (!IsField())
            {
                Debug.LogError("GetFieldName called on ObjectData that does not represent a field.");
                return "";
            }
            if (Parent.Obj.IsArrayItem())
            {
                return $"{Parent.Obj.GenerateArrayDescription(snapshot, truncateTypeName: truncateTypeName, includeTypeName: false)}.{snapshot.FieldDescriptions.FieldDescriptionName[Parent.iField]}";
            }
            else if (Parent.Obj.IsField())
            {
                return $"{Parent.Obj.GetFieldName(snapshot, truncateTypeName)}.{snapshot.FieldDescriptions.FieldDescriptionName[Parent.iField]}";
            }
            return snapshot.FieldDescriptions.FieldDescriptionName[Parent.iField];
        }

        // Returns the number of fields the object (that this ObjectData is currently pointing at) has
        public int GetInstanceFieldCount(CachedSnapshot snapshot)
        {
            switch (m_dataType)
            {
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.Value:
                    if (managedTypeIndex < 0 || managedTypeIndex >= snapshot.TypeDescriptions.FieldIndicesInstance.Length)
                        return 0;
                    return snapshot.TypeDescriptions.FieldIndicesInstance[managedTypeIndex].Length;
                default:
                    return 0;
            }
        }

        // Returns a new ObjectData pointing to the object's (that this ObjectData is currently pointing at) field
        // using the field index from [0, GetInstanceFieldCount()[
        public ObjectData GetInstanceFieldByIndex(CachedSnapshot snapshot, int i)
        {
            int iField = snapshot.TypeDescriptions.FieldIndicesInstance[managedTypeIndex][i];
            return GetFieldByFieldDescriptionsIndex(snapshot, iField, true);
        }

        // Returns a new ObjectData pointing to the object's (that this ObjectData is currently pointing at) field
        // using a field index from snapshot.fieldDescriptions
        public ObjectData GetFieldByFieldDescriptionsIndex(CachedSnapshot snapshot, int iField, bool expandToTarget,
            int valueTypeFieldOwningITypeDescription = -1, int valueTypeFieldIndex = -1, int offsetFromManagedObjectDataStartToValueTypesField = 0)
        {
            bool fieldResidesOnNestedValueTypeField = valueTypeFieldOwningITypeDescription >= 0;
#if DEBUG_VALIDATION
            if (!fieldResidesOnNestedValueTypeField)
                Checks.CheckEquals(offsetFromManagedObjectDataStartToValueTypesField, 0);
#endif
            ObjectData obj = this;
            ulong objectPtr = m_data.managed.objectPtr;

            switch (m_dataType)
            {
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                    objectPtr = GetReferencePointer();
                    obj = FromManagedPointer(snapshot, objectPtr, managedTypeIndex);
                    if (!obj.IsValid)
                        return Invalid;
                    break;
                case ObjectDataType.Unknown: //skip unknown/uninitialized types as some snapshots will have them
                    return Invalid;
                case ObjectDataType.BoxedValue:
                    if (fieldResidesOnNestedValueTypeField && valueTypeFieldOwningITypeDescription != m_data.managed.iType)
                    {
                        obj = GetBoxedValue(snapshot, expandToTarget);
                    }
                    break;
                default:
                    break;
            }

            var fieldOffset = snapshot.FieldDescriptions.Offset[iField];
            var fieldType = snapshot.FieldDescriptions.TypeIndex[iField];
            bool isStatic = snapshot.FieldDescriptions.IsStatic[iField] == 1;
            switch (obj.m_dataType)
            {
                case ObjectDataType.Value:
                    if (!isStatic)
                        fieldOffset -= (int)snapshot.VirtualMachineInformation.ObjectHeaderSize;
                    break;
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                    break;
                case ObjectDataType.Type:
                    if (!isStatic)
                    {
                        Debug.LogError("Requesting a non-static field on a type");
                        return Invalid;
                    }
                    break;
                default:
                    break;
            }

            ObjectData o = new ObjectData();
            o.Parent = new ObjectDataParent(obj, iField, -1, expandToTarget);
            o.SetManagedType(snapshot, fieldType);
            o.m_dataType = TypeToSubDataType(snapshot, fieldType);

            if (isStatic)
            {
                var iOwningType = obj.m_data.managed.iType;
                //the field requested might come from a base class. make sure we are using the right staticFieldBytes.
                while (iOwningType >= 0)
                {
                    var fieldIndex = Array.FindIndex(snapshot.TypeDescriptions.fieldIndicesOwnedStatic[iOwningType], x => x == iField);
                    if (fieldIndex >= 0)
                    {
                        //field iField is owned by type iOwningType
                        break;
                    }
                    iOwningType = snapshot.TypeDescriptions.BaseOrElementTypeIndex[iOwningType];
                }
                if (iOwningType < 0)
                {
                    Debug.LogError("Field requested is not owned by the type not any of its bases");
                    return Invalid;
                }

                o.m_data.managed.objectPtr = 0;
                var typeStaticData = new BytesAndOffset(snapshot.TypeDescriptions.StaticFieldBytes[iOwningType], snapshot.VirtualMachineInformation.PointerSize);
                o.managedObjectData = typeStaticData.Add((ulong)fieldOffset);
            }
            else
            {
                o.m_data.managed.objectPtr = objectPtr;
                o.managedObjectData = obj.managedObjectData.Add((ulong)fieldOffset);
            }

            if (fieldResidesOnNestedValueTypeField)
            {
                // if fieldResidesOnNestedValueTypeField is true, we know that the field is on a value type within the fieldIndicesOwnedStatic.
                // We know the value type that contains this field as well as the field index, but we don't know the entire chain of potentially nested value types.
                // That chain needs reconstructing.

                // Cyclic struct layouts are forbidden so as soon as the parent's type matches the the Value Type owning the field,
                // the full (nested) value type chain has been successfully reconsturced.
                if (obj.m_data.managed.iType != valueTypeFieldOwningITypeDescription)
                {
                    // as long as the chain has not yet been reconstructed, recurse through the value type fields until it is.
                    offsetFromManagedObjectDataStartToValueTypesField -= fieldOffset;
                    var fields = snapshot.TypeDescriptions.FieldIndicesInstance[fieldType];
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (valueTypeFieldIndex != field)
                        {
                            var offset = snapshot.FieldDescriptions.Offset[field] - (int)snapshot.VirtualMachineInformation.ObjectHeaderSize;
                            if (offsetFromManagedObjectDataStartToValueTypesField < offset)
                                continue;
                            var type = snapshot.FieldDescriptions.TypeIndex[field];

                            var size = snapshot.TypeDescriptions.HasFlag(type, TypeFlags.kValueType) ? snapshot.TypeDescriptions.Size[type] : (int)snapshot.VirtualMachineInformation.PointerSize;
                            if (offsetFromManagedObjectDataStartToValueTypesField >= offset + size)
                                continue;
                        }
                        return o.GetFieldByFieldDescriptionsIndex(snapshot, field, expandToTarget, valueTypeFieldOwningITypeDescription, valueTypeFieldIndex, offsetFromManagedObjectDataStartToValueTypesField);
                    }
                    Debug.LogError($"Nested Value Type Field was not found (addionalValueTypeFieldOffset={offsetFromManagedObjectDataStartToValueTypesField})");
                    return Invalid;
                }
            }
            return o;
        }

        /// <summary>
        /// Register a field as holding a reference to this object.
        /// This is meant to support reporting of the "References To" for this object with the field information that is holding the reference.
        /// The validity of using it in a different context is not explored.
        /// </summary>
        /// <param name="holdingField"></param>
        /// <param name="expandToTarget"></param>
        /// <returns>This object, now with a parent field.</returns>
        public ObjectData RegisterHoldingField(ObjectData holdingField, bool expandToTarget)
        {
            Debug.Assert(holdingField.IsField() || holdingField.IsArrayItem(), "Can only register holding fields for array items or fields");

            Parent = new ObjectDataParent(holdingField, holdingField.fieldIndex, holdingField.arrayIndex, expandToTarget);
            return this;
        }

        public InstanceID GetInstanceID(CachedSnapshot snapshot)
        {
            var nativeIndex = nativeObjectIndex;
            if (nativeIndex < 0)
            {
                var managedIndex = GetManagedObjectIndex(snapshot);
                if (managedIndex >= 0)
                {
                    nativeIndex = snapshot.CrawledData.ManagedObjects[managedIndex].NativeObjectIndex;
                }
            }

            if (nativeIndex >= 0)
            {
                return snapshot.NativeObjects.InstanceId[nativeIndex];
            }
            return CachedSnapshot.NativeObjectEntriesCache.InstanceIDNone;
        }

        public ObjectData GetBase(CachedSnapshot snapshot)
        {
            switch (m_dataType)
            {
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.Object:
                case ObjectDataType.Type:
                case ObjectDataType.Value:
                case ObjectDataType.BoxedValue:
                    break;
                default:
                    UnityEngine.Debug.LogError("Requesting a base on an invalid data type");
                    return Invalid;
            }

            var b = snapshot.TypeDescriptions.BaseOrElementTypeIndex[m_data.managed.iType];
            if (b == snapshot.TypeDescriptions.ITypeValueType
                || b == snapshot.TypeDescriptions.ITypeObject
                || b == snapshot.TypeDescriptions.ITypeEnum
                || b == TypeDescriptionEntriesCache.ITypeInvalid)
                return Invalid;

            ObjectData o = this;
            o.SetManagedType(snapshot, b);
            return o;
        }

        public long GetUnifiedObjectIndex(CachedSnapshot snapshot)
        {
            switch (dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                {
                    if (snapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(m_data.managed.objectPtr, out var idx))
                    {
                        return snapshot.ManagedObjectIndexToUnifiedObjectIndex(idx);
                    }
                    break;
                }
                case ObjectDataType.NativeObject:
                    return snapshot.NativeObjectIndexToUnifiedObjectIndex(m_data.native.index);
            }

            return -1;
        }

        public SourceIndex GetSourceLink(CachedSnapshot snapshot)
        {
            switch (dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                {
                    if (snapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(m_data.managed.objectPtr, out var idx))
                        return new SourceIndex(SourceIndex.SourceId.ManagedObject, idx);
                    break;
                }
                case ObjectDataType.Type:
                    return new SourceIndex(SourceIndex.SourceId.ManagedType, m_data.managed.iType);
                case ObjectDataType.NativeObject:
                    return new SourceIndex(SourceIndex.SourceId.NativeObject, m_data.native.index);
                case ObjectDataType.NativeAllocation:
                    return new SourceIndex(SourceIndex.SourceId.NativeAllocation, m_data.native.index);
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                    var targetManagedObject = GetManagedObject(snapshot);
                    if (targetManagedObject.IsValid())
                        return new SourceIndex(SourceIndex.SourceId.ManagedObject, targetManagedObject.ManagedObjectIndex);
                    else
                        return new SourceIndex();
            }

            return new SourceIndex();
        }

        public ObjectData GetReferencedObject(CachedSnapshot snapshot)
        {
            switch (dataType)
            {
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                {
                    ulong refPtr = GetReferencePointer();
                    if (refPtr == 0)
                        return ObjectData.Invalid;
                    var obj = ObjectData.FromManagedPointer(snapshot, refPtr, managedTypeIndex);
                    if (obj.IsValid)
                    {
                        obj.Parent = Parent;
                    }
                    //do not throw, if the ref pointer is not valid the object might have been null-ed
                    return obj;
                }
                default:
                    throw new Exception("GetManagedObject was called on a instance of ObjectData which does not contain an managed object.");
            }
        }

        public ManagedObjectInfo GetManagedObject(CachedSnapshot snapshot)
        {
            switch (dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                {
                    if (snapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(m_data.managed.objectPtr, out var idx))
                    {
                        return snapshot.CrawledData.ManagedObjects[idx];
                    }
                    throw new Exception("Invalid object pointer used to query object list.");
                }
                case ObjectDataType.ReferenceObject:
                case ObjectDataType.ReferenceArray:
                {
                    ulong refPtr = GetReferencePointer();
                    if (refPtr == 0)
                        return ManagedObjectInfo.Invalid;
                    if (snapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(refPtr, out var idx))
                    {
                        return snapshot.CrawledData.ManagedObjects[idx];
                    }
                    //do not throw, if the ref pointer is not valid the object might have been null-ed
                    return ManagedObjectInfo.Invalid;
                }
                case ObjectDataType.Value:
                {
                    if (Parent != null)
                    {
                        return Parent.Obj.GetManagedObject(snapshot);
                    }
                    throw new Exception("GetManagedObject was called on a instance of ObjectData which does not contain an managed object.");
                }
                case ObjectDataType.Type:
                {
                    // No Managed Object to be found behind a Type
                    return ManagedObjectInfo.Invalid;
                }
                default:
                    throw new Exception($"GetManagedObject was called on a instance of ObjectData which does not contain an managed object. ({dataType}, {hostManagedObjectPtr})");
            }
        }

        public long GetManagedObjectIndex(CachedSnapshot snapshot)
        {
            switch (dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.Object:
                case ObjectDataType.BoxedValue:
                {
                    if (snapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(m_data.managed.objectPtr, out var idx))
                    {
                        return idx;
                    }


                    break;
                }
            }

            return -1;
        }

        public long GetNativeObjectIndex(CachedSnapshot snapshot)
        {
            switch (dataType)
            {
                case ObjectDataType.NativeObject:
                    return m_data.native.index;
            }

            return -1;
        }

        public static ObjectData Invalid
        {
            get
            {
                return new ObjectData();
            }
        }

        public string GetValueAsString(CachedSnapshot cachedSnapshot)
        {
            if (isManaged)
            {
                if (!managedObjectData.Bytes.IsCreated)
                    return "null";

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeChar)
                    return managedObjectData.ReadChar().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeInt16)
                    return managedObjectData.ReadInt16().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeInt32)
                    return managedObjectData.ReadInt32().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeInt64)
                    return managedObjectData.ReadInt64().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeIntPtr)
                    return managedObjectData.ReadUInt64().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeBool)
                    return managedObjectData.ReadBoolean().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeSingle)
                    return managedObjectData.ReadSingle().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeByte)
                    return managedObjectData.ReadByte().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeDouble)
                    return managedObjectData.ReadDouble().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeUInt16)
                    return managedObjectData.ReadUInt16().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeUInt32)
                    return managedObjectData.ReadUInt32().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeUInt64)
                    return managedObjectData.ReadUInt64().ToString();

                if (managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeString)
                    return managedObjectData.ReadString(out _);
            }
            return "";
        }

        public string GetFieldDescription(CachedSnapshot cachedSnapshot, bool truncateTypeNames = false, bool addClassNameOfHoldingTypeOrObject = true, bool addFieldTypeInfo = true, bool addHoldingObjectInfo = true)
        {
            // void* or byte* on IntPtr or NativeCollections get simplified to their parent type.
            if ((managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeVoidPtr || managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeBytePtr)
                && Parent != null && (Parent.Obj.managedTypeIndex == cachedSnapshot.TypeDescriptions.ITypeIntPtr || cachedSnapshot.TypeDescriptions.IsNativeContainerType(Parent.Obj.managedTypeIndex)))
            {
                return Parent.Obj.GetFieldDescription(cachedSnapshot, truncateTypeNames, addClassNameOfHoldingTypeOrObject, addFieldTypeInfo, addHoldingObjectInfo);
            }

            string parentType = string.Empty;
            string parentTypeSeparator = string.Empty;
            if (addClassNameOfHoldingTypeOrObject)
            {
                var parentIsArray = displayObject.m_dataType == ObjectDataType.Array;
                parentType = parentIsArray ?
                    // Use the Element Type destription as the array brackets are added by GetFieldName.
                    cachedSnapshot.TypeDescriptions.TypeDescriptionName[GetArrayInfo(cachedSnapshot).ElementTypeDescription] :
                    Parent?.Obj.GenerateTypeName(cachedSnapshot, truncateTypeNames);

                if (parentType != null)
                {
                    if (parentIsArray)
                        parentType = truncateTypeNames ? PathsToRootDetailView.TruncateTypeName(parentType) : parentType;
                    else
                        // Separate parent type from field name with a dot if parent is not an array. For Arrays, GetFieldName will construct the brackets.
                        // Note: For Jagged Arrays, we have no hope of finding the parent in a non expensive way. I.e. we'd have to check references to find the parent, (or register it during crawling, which might not work if someone actively took a GCHandle on the sub array).
                        parentTypeSeparator = ".";
                }
            }
            var fieldType = cachedSnapshot.TypeDescriptions.TypeDescriptionName[managedTypeIndex];
            var staticField = cachedSnapshot.FieldDescriptions.IsStatic[fieldIndex] == 1;

            fieldType = truncateTypeNames ? PathsToRootDetailView.TruncateTypeName(fieldType) : fieldType;
            var ret = $"{parentType}{parentTypeSeparator}{GetFieldName(cachedSnapshot)}";

            if (addFieldTypeInfo)
                ret += $" ({(staticField ? "static " : "instance ")}field type: {fieldType})";

            if (!addHoldingObjectInfo)
                return ret;

            var objectName = (Parent != null && !Parent.expandToTarget) ? Parent.Obj.Parent?.Obj.GenerateObjectName(cachedSnapshot) ?? Parent.Obj.GenerateObjectName(cachedSnapshot) : GenerateObjectName(cachedSnapshot);

            return ret + $" on object {objectName}";
        }

        public string GenerateObjectName(CachedSnapshot cachedSnapshot)
        {
            if (nativeObjectIndex != -1)
                return $"{cachedSnapshot.NativeObjects.ObjectName[nativeObjectIndex]} {NativeObjectTools.ProduceNativeObjectId(nativeObjectIndex, cachedSnapshot)}";

            if (m_dataType == ObjectDataType.NativeAllocation)
                return DetailFormatter.FormatPointer(GetObjectPointer(cachedSnapshot));

            var managedObjectInfo = GetManagedObject(cachedSnapshot);
            if (managedObjectInfo.IsValid())
                return ManagedObjectTools.ProduceManagedObjectName(managedObjectInfo, cachedSnapshot, addInstanceId: true);

            return DetailFormatter.FormatPointer(GetObjectPointer(cachedSnapshot));
        }

        public string GenerateArrayDescription(CachedSnapshot cachedSnapshot, bool truncateTypeName = false, bool includeTypeName = true)
        {
            var arrayObject = this;
            var arrayIndex = this.arrayIndex;
            while (arrayObject.m_dataType != ObjectDataType.Array && arrayObject.Parent != null)
            {
                arrayIndex = arrayObject.arrayIndex;
                arrayObject = arrayObject.Parent.Obj;
            }
            var arrayInfo = arrayObject.GetArrayInfo(cachedSnapshot);
            return arrayInfo.GenerateArrayDescription(cachedSnapshot, arrayIndex, truncateTypeName, includeTypeName);
        }

        public string GenerateTypeName(CachedSnapshot cachedSnapshot, bool truncateTypeName = false)
        {
            switch (displayObject.dataType)
            {
                case ObjectDataType.Array:
                case ObjectDataType.BoxedValue:
                case ObjectDataType.Object:
                case ObjectDataType.ReferenceArray:
                case ObjectDataType.Value:
                    var typeName = displayObject.managedTypeIndex < 0 ? "<unknown type>" : cachedSnapshot.TypeDescriptions.TypeDescriptionName[displayObject.managedTypeIndex];

                    return truncateTypeName ? PathsToRootDetailView.TruncateTypeName(typeName) : typeName;

                case ObjectDataType.ReferenceObject:
                {
                    var ptr = displayObject.GetReferencePointer();
                    if (ptr != 0)
                    {
                        var obj = ObjectData.FromManagedPointer(cachedSnapshot, ptr, managedTypeIndex);
                        if (obj.IsValid && obj.managedTypeIndex != displayObject.managedTypeIndex)
                        {
                            var actualTypeName = cachedSnapshot.TypeDescriptions.TypeDescriptionName[displayObject.managedTypeIndex];
                            actualTypeName = truncateTypeName ? PathsToRootDetailView.TruncateTypeName(actualTypeName) : actualTypeName;
                            var fieldTypeName = cachedSnapshot.TypeDescriptions.TypeDescriptionName[obj.managedTypeIndex];
                            fieldTypeName = truncateTypeName ? PathsToRootDetailView.TruncateTypeName(fieldTypeName) : fieldTypeName;
                            return $"({fieldTypeName}) {actualTypeName}";
                        }
                    }

                    return cachedSnapshot.TypeDescriptions.TypeDescriptionName[displayObject.managedTypeIndex];
                }

                case ObjectDataType.Type:
                    var type = cachedSnapshot.TypeDescriptions.TypeDescriptionName[displayObject.managedTypeIndex];
                    type = truncateTypeName ? PathsToRootDetailView.TruncateTypeName(type) : type;
                    return $"Type {type}";
                case ObjectDataType.NativeObject:
                {
                    int iType = cachedSnapshot.NativeObjects.NativeTypeArrayIndex[displayObject.nativeObjectIndex];
                    return cachedSnapshot.NativeTypes.TypeName[iType];
                }
                case ObjectDataType.NativeAllocation:
                    return "Native Allocation";
                case ObjectDataType.Unknown:
                default:
                    return "<unintialized type>";
            }
        }

        public static ObjectData FromSourceLink(CachedSnapshot snapshot, SourceIndex source)
        {
            switch (source.Id)
            {
                case SourceIndex.SourceId.NativeObject:
                    return FromNativeObjectIndex(snapshot, (int)source.Index);
                case SourceIndex.SourceId.ManagedObject:
                    return FromManagedObjectIndex(snapshot, (int)source.Index);
                case SourceIndex.SourceId.ManagedType:
                    return FromManagedType(snapshot, (int)source.Index);
                case SourceIndex.SourceId.GfxResource:
                    return FromGfxResourceIndex(snapshot, (int)source.Index);
                case SourceIndex.SourceId.NativeAllocation:
                    return FromNativeAllocationIndex(snapshot, (int)source.Index);
                default:
                    return ObjectData.Invalid;
            }
        }

        public static ObjectData FromManagedType(CachedSnapshot snapshot, int iType)
        {
            ObjectData o = new ObjectData();
            o.SetManagedType(snapshot, iType);
            o.m_dataType = ObjectDataType.Type;
            o.managedObjectData = new BytesAndOffset(snapshot.TypeDescriptions.StaticFieldBytes[iType], snapshot.VirtualMachineInformation.PointerSize);
#if DEBUG_VALIDATION
            if(!o.managedObjectData.IsValid)
                Debug.LogError("Invalid object data");
#endif
            return o;
        }

        public static ObjectData FromGfxResourceIndex(CachedSnapshot snapshot, int index)
        {
            var rootReferenceId = snapshot.NativeGfxResourceReferences.RootId[index];
            if (rootReferenceId <= 0)
                return ObjectData.Invalid;

            // Lookup native object index associated with memory label root
            if (!snapshot.NativeObjects.RootReferenceIdToIndex.TryGetValue(rootReferenceId, out var nativeObjectIndex))
                return ObjectData.Invalid;

            return FromNativeObjectIndex(snapshot, nativeObjectIndex);
        }

        //index from an imaginary array composed of native objects followed by managed objects.
        public static ObjectData FromUnifiedObjectIndex(CachedSnapshot snapshot, long index)
        {
            int iNative = snapshot.UnifiedObjectIndexToNativeObjectIndex(index);
            if (iNative >= 0)
            {
                return FromNativeObjectIndex(snapshot, iNative);
            }

            int iManaged = snapshot.UnifiedObjectIndexToManagedObjectIndex(index);
            if (iManaged >= 0)
            {
                return FromManagedObjectIndex(snapshot, iManaged);
            }

            return ObjectData.Invalid;
        }

        public static ObjectData FromNativeObjectIndex(CachedSnapshot snapshot, long index)
        {
            if (index < 0 || index >= snapshot.NativeObjects.Count)
                return ObjectData.Invalid;
            ObjectData o = new ObjectData();
            o.m_dataType = ObjectDataType.NativeObject;
            o.m_data.native.index = index;
            return o;
        }

        public static ObjectData FromNativeAllocationIndex(CachedSnapshot snapshot, int index)
        {
            if (index < 0 || index >= snapshot.NativeAllocations.Count)
                return ObjectData.Invalid;
            ObjectData o = new ObjectData();
            o.m_dataType = ObjectDataType.NativeAllocation;
            o.m_data.native.index = index;
            return o;
        }

        public static ObjectData FromManagedObjectInfo(CachedSnapshot snapshot, ManagedObjectInfo moi)
        {
            if (!moi.IsKnownType)
                return ObjectData.Invalid;
            ObjectData o = new ObjectData();
            o.m_dataType = TypeToDataType(snapshot, moi.ITypeDescription);// ObjectDataType.Object;
            o.m_data.managed.objectPtr = moi.PtrObject;
            o.SetManagedType(snapshot, moi.ITypeDescription);
            o.managedObjectData = moi.data;
#if DEBUG_VALIDATION
            if(!o.managedObjectData.IsValid)
                Debug.LogError("Invalid object data");
#endif
            return o;
        }

        public static ObjectData FromManagedObjectIndex(CachedSnapshot snapshot, long index)
        {
            if (index < 0 || index >= snapshot.CrawledData.ManagedObjects.Count)
                return ObjectData.Invalid;
            ref readonly var moi = ref snapshot.CrawledData.ManagedObjects[index];

            if (index < snapshot.GcHandles.UniqueCount)
            {
                //When snapshotting we might end up getting some handle targets as they are about to be collected
                //we do restart the world temporarily this can cause us to end up with targets that are not present in the dumped heaps
                if (moi.PtrObject == 0)
                    return ObjectData.Invalid;

                if (moi.PtrObject != snapshot.GcHandles.Target[index])
                {
                    throw new Exception("bad object");
                }
            }

            return FromManagedObjectInfo(snapshot, moi);
        }

        public static ObjectData FromManagedPointer(CachedSnapshot snapshot, ulong ptr, int typeIndexOfTheFieldHoldingThePtr, int asTypeIndex = -1)
        {
            if (ptr == 0)
                return Invalid;
            if (snapshot.CrawledData.MangedObjectIndexByAddress.TryGetValue(ptr, out var idx))
            {
                return FromManagedObjectInfo(snapshot, snapshot.CrawledData.ManagedObjects[idx]);
            }
            else
            {
                var o = new ObjectData();
                o.m_data.managed.objectPtr = ptr;
                o.managedObjectData = snapshot.ManagedHeapSections.Find(ptr, snapshot.VirtualMachineInformation, out MemorySectionType memorySectionType);
                if (memorySectionType == MemorySectionType.VirtualMachine)
                {
                    // Managed Objects can (afaik) not be found in VM Memory, so ignore that here
                    return Invalid;
                }
                if (o.managedObjectData.IsValid)
                {
                    if (ManagedDataCrawler.TryParseObjectHeader(snapshot, ptr, out var info, o.managedObjectData, typeIndexOfTheFieldHoldingThePtr, heldByNativeUnityObject: false, ignoreErrorsBecauseObjectIsSpeculative: true))
                    {
                        if (asTypeIndex >= 0)
                        {
                            o.SetManagedType(snapshot, asTypeIndex);
                        }
                        else
                        {
                            o.SetManagedType(snapshot, info.ITypeDescription);
                        }

                        o.m_dataType = TypeToDataType(snapshot, info.ITypeDescription);
                        return o;
                    }
                }
#if DEBUG_VALIDATION
                else
                    Debug.LogError("Invalid object data");
#endif
            }
            return Invalid;
        }

        public bool isNativeObject
        {
            get
            {
                return dataType switch
                {
                    ObjectDataType.NativeObject => true,
                    ObjectDataType.NativeAllocation => false,
                    _ => false,
                };
            }
        }
        public bool isManaged
        {
            get
            {
                switch (dataType)
                {
                    case ObjectDataType.Value:
                    case ObjectDataType.Object:
                    case ObjectDataType.Array:
                    case ObjectDataType.BoxedValue:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.Type:
                        return true;
                }
                return false;
            }
        }
        public CodeType codeType
        {
            get
            {
                switch (dataType)
                {
                    case ObjectDataType.Value:
                    case ObjectDataType.Object:
                    case ObjectDataType.Array:
                    case ObjectDataType.BoxedValue:
                    case ObjectDataType.ReferenceObject:
                    case ObjectDataType.ReferenceArray:
                    case ObjectDataType.Type:
                        return CodeType.Managed;
                    case ObjectDataType.NativeObject:
                    case ObjectDataType.NativeAllocation:
                        return CodeType.Native;
                    default:
                        return CodeType.Unknown;
                }
            }
        }

        public bool IsGameObject(CachedSnapshot cs)
        {
            return cs.NativeObjects.NativeTypeArrayIndex[nativeObjectIndex] == cs.NativeTypes.GameObjectIdx;
        }

        public bool IsTransform(CachedSnapshot cs)
        {
            return cs.NativeTypes.IsTransformOrRectTransform(cs.NativeObjects.NativeTypeArrayIndex[nativeObjectIndex]);
        }

        public bool IsRootTransform(CachedSnapshot cs)
        {
            return cs != null && cs.HasSceneRootsAndAssetbundles && cs.SceneRoots.RootTransformInstanceIdHashSet.Contains(GetInstanceID(cs));
        }

        public bool IsRootGameObject(CachedSnapshot cs)
        {
            return cs != null && cs.HasSceneRootsAndAssetbundles && cs.SceneRoots.RootGameObjectInstanceIdHashSet.Contains(GetInstanceID(cs));
        }

        public string GetAssetPath(CachedSnapshot cs)
        {
            for (int i = 0; i < cs.SceneRoots.SceneIndexedRootTransformInstanceIds.Length; i++)
            {
                for (int ii = 0; ii < cs.SceneRoots.SceneIndexedRootTransformInstanceIds[i].Length; ii++)
                {
                    if (cs.SceneRoots.SceneIndexedRootTransformInstanceIds[i][ii].Equals(GetInstanceID(cs)))
                        return cs.SceneRoots.Path[i];
                }
            }
            return String.Empty;
        }

        public void GetAllReferencingObjects(CachedSnapshot cs, ref List<ObjectData> allReferencingObjects, HashSet<SourceIndex> foundSourceIndices = null, ObjectConnection.UnityObjectReferencesSearchMode searchMode = ObjectConnection.UnityObjectReferencesSearchMode.TreatAsOneObject)
        {
            ObjectConnection.GetAllReferencingObjects(cs, displayObject.GetSourceLink(cs), ref allReferencingObjects, foundSourceIndices, searchMode: searchMode);
        }

        public void GetAllReferencedObjects(CachedSnapshot cs, ref List<ObjectData> allReferencedObjects, bool treatUnityObjectsAsOneObject = true, bool addManagedObjectsWithFieldInfo = true, HashSet<SourceIndex> foundSourceIndices = null)
        {
            ObjectConnection.GetAllReferencedObjects(cs, displayObject.GetSourceLink(cs), ref allReferencedObjects, treatUnityObjectsAsOneObject, addManagedObjectsWithFieldInfo, foundSourceIndices);
        }

        public bool InvalidType()
        {
            return displayObject.dataType == ObjectDataType.Unknown;
        }

        public bool IsUnknownDataType()
        {
            return displayObject.dataType == ObjectDataType.Unknown;
        }
    }
}
