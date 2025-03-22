using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.MemoryProfilerExtension.Editor.UI.PathsToRoot
{
    static class PathsToRootUtils
    {
        public const string ObjectFlagsInfoHeader = "ObjectFlags: \n";
        public const string IsDontDestroyOnLoadInfo = "IsDontDestroyOnLoad - Specifies that the object is marked as DontDestroyOnLoad.\n\n";
        public const string IsPersistentInfo = "IsPersistent - Specifies that the object is set as persistent.\nThis is e.g. the case for any object connected to a file (also referred to as Asset), and Unity's subsystems (also referred to as Managers).\n\n";
        public const string IsManagerInfo = "IsManager - Specifies that the object is marked as a Manager, i.e. an entity that manages one of Unity's engine subsystems.\n\n";
        public const string HideFlagsInfoHeader = "HideFlags: \n";
        public const string HideInHierarchyInfo = "HideInHierarchy - The object will not appear in the hiearachy.\n\n";
        public const string HideInInspectorInfo = "HideInInspector - It is not possible to view this item in the inspector.\n\n";
        public const string DontSaveInEditorInfo = "DontSaveInEditor - The object will not be saved to the scene in the editor.\n\n";
        public const string NotEditableInfo = "NotEditable - The object will not be editable in the inspector.\n\n";
        public const string DontSaveInBuildInfo = "DontSaveInBuild - The object will not be saved when building a player.\n\n";
        public const string DontUnloadUnusedAssetInfo = "DontUnloadUnusedAsset - The object will not be unloaded by 'Resources.UnloadUnusedAssets()' calls, neither by explicit calls, nor implicit ones that are triggered when a Scene is non-additively unloaded.\n\n";
        public const string DontSaveInfo = "DontSave - The object will not be saved to the Scene. It will not be destroyed when a new Scene is loaded. It is a shortcut for HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset.\n\n";
        public const string HideAndDontSaveInfo = "HideAndDontSave - The GameObject is not shown in the Hierarchy, not saved to to Scenes, and not unloaded by Resources.UnloadUnusedAssets.";

        public static readonly GUIContent FlagIcon = EditorGUIUtility.IconContent("console.infoicon");
        public static readonly GUIContent NoIconContent = new GUIContent(Icons.NoIcon, "no icon for type");
        public static readonly GUIContent CSScriptIconContent = EditorGUIUtility.IconContent("cs Script Icon", "");

        public static Dictionary<string, GUIContent> iconContent = new Dictionary<string, GUIContent>();
    }
}
