using UnityEngine;
using System.Collections;
using UnityEditor;

public class AdbForward {
    [MenuItem("Edit/AdbForward/34999")]
    public static void DoCommand_34999()
    {
#if UNITY_EDITOR_WIN
        System.Diagnostics.Process.Start("adb.exe", "forward tcp:34999 localabstract:Unity-" + Application.identifier);
#else
        EditorUtility.DisplayDialog("adb", "./adb forward tcp:34999 localabstract:Unity-" + Application.identifier, "ok");
#endif
    }

    [MenuItem("Edit/AdbForward/12000")]
    public static void DoCommand_12000()
    {
#if UNITY_EDITOR_WIN
        System.Diagnostics.Process.Start("adb.exe", "forward tcp:12000 tcp:12000");
#else
        EditorUtility.DisplayDialog("adb", "./adb forward tcp:12000 tcp:12000", "ok");
#endif
    }
}
