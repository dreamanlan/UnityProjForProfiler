using UnityEngine;
using System.Collections;
using UnityEditor;

public class AdbForward {
    [MenuItem("Edit/AdbForward/34999")]
    public static void DoCommand_34999()
    {
#if UNITY_EDITOR_WIN
        try {
            var p = System.Diagnostics.Process.Start("adb.exe", "forward tcp:34999 localabstract:Unity-" + Application.identifier);
            if (null == p) {
                EditorUtility.DisplayDialog("adb", "./adb forward tcp:34999 localabstract:Unity-" + Application.identifier, "ok");
            }
        }
        catch {
            EditorUtility.DisplayDialog("adb", "./adb forward tcp:34999 localabstract:Unity-" + Application.identifier, "ok");
        }
#else
        EditorUtility.DisplayDialog("adb", "./adb forward tcp:34999 localabstract:Unity-" + Application.identifier, "ok");
#endif
    }

    [MenuItem("Edit/AdbForward/12000")]
    public static void DoCommand_12000()
    {
#if UNITY_EDITOR_WIN
        try {
            var p = System.Diagnostics.Process.Start("adb.exe", "forward tcp:12000 tcp:12000");
            if (null == p) {
                EditorUtility.DisplayDialog("adb", "./adb forward tcp:12000 tcp:12000", "ok");
            }
        }
        catch {
            EditorUtility.DisplayDialog("adb", "./adb forward tcp:12000 tcp:12000", "ok");
        }
#else
        EditorUtility.DisplayDialog("adb", "./adb forward tcp:12000 tcp:12000", "ok");
#endif
    }
}
