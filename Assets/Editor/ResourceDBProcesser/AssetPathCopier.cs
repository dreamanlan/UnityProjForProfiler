using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class AssetPathCopier
{
	[MenuItem("Assets/复制路径", false, 51)]
	static void CopyAssetPath()
	{
		var obj = Selection.activeObject;
		if (obj)
		{
			var path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
			EditorGUIUtility.systemCopyBuffer = path;
		}
	}
}
