using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public abstract class DecoratorEditor : Editor
{
	protected System.Type decoratedEditorType;
	private System.Type editedObjectType;
	private Editor editorInstance;
	private static Dictionary<string, MethodInfo> decoratedMethods = new Dictionary<string, MethodInfo>();
	private static Assembly editorAssembly = Assembly.GetAssembly(typeof(Editor));
	protected bool needSceneUI = true;
	protected bool needHeaderUI = true;

	protected Editor EditorInstance
	{
		get
		{
			if (editorInstance == null && targets != null && targets.Length > 0)
			{
				editorInstance = Editor.CreateEditor(targets, decoratedEditorType);
			}

			if (editorInstance == null)
			{
				Debug.LogError("Could not create editor !");
			}

			return editorInstance;
		}
	}

	public DecoratorEditor(string editorTypeName)
	{
		this.decoratedEditorType = editorAssembly.GetTypes().Where(t => t.Name == editorTypeName).FirstOrDefault();
		Init();

		var originalEditedType = GetCustomEditorType(decoratedEditorType);

		if (originalEditedType != editedObjectType)
		{
			throw new System.ArgumentException(
				string.Format("Type {0} does not match the editor {1} type {2}",
						  editedObjectType, editorTypeName, originalEditedType));
		}
	}

	private System.Type GetCustomEditorType(System.Type type)
	{
		var flags = BindingFlags.NonPublic | BindingFlags.Instance;

		var attributes = type.GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
		var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

		return field.GetValue(attributes[0]) as System.Type;
	}

	private void Init()
	{
		var flags = BindingFlags.NonPublic | BindingFlags.Instance;

		var attributes = this.GetType().GetCustomAttributes(typeof(CustomEditor), true) as CustomEditor[];
		var field = attributes.Select(editor => editor.GetType().GetField("m_InspectedType", flags)).First();

		editedObjectType = field.GetValue(attributes[0]) as System.Type;
	}

	void OnDisable()
	{
		if (editorInstance != null)
		{
			DestroyImmediate(editorInstance);
		}
	}

	private bool assignedEditor = false;

	private void TryAssignEditor()
	{
		if (!assignedEditor)
		{
			try
			{
				CallInspectorMethod("InternalSetAssetImporterTargetEditor", EditorInstance);
			}
			catch (System.Exception e)
			{
				Debug.LogError("exeception:" + e.Message);
			}
			finally
			{
				assignedEditor = true;
			}
		}
	}

	protected void CallInspectorMethod(string methodName, params object[] args)
	{
		MethodInfo method = null;

		if (!decoratedMethods.ContainsKey(methodName))
		{
			var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

			method = decoratedEditorType.GetMethod(methodName, flags);

			if (method != null)
			{
				decoratedMethods[methodName] = method;
			}
			else
			{
				Debug.LogError(string.Format("Could not find method {0}", method));
			}
		}
		else
		{
			method = decoratedMethods[methodName];
		}

		if (method != null)
		{
			var ps = method.GetParameters();
			Debug.Log("method:" + methodName);
			foreach (var p in ps)
				Debug.Log("parameters:" + p.Name + "," + p.GetType().Name);
			method.Invoke(EditorInstance, args);
		}
	}

	public void OnSceneGUI()
	{
		if(needSceneUI)
			CallInspectorMethod("OnSceneGUI");
	}

	protected override void OnHeaderGUI()
	{
		if(needHeaderUI)
			CallInspectorMethod("OnHeaderGUI");
	}

	public override void OnInspectorGUI()
	{
		TryAssignEditor();
		EditorInstance.OnInspectorGUI();
	}

	public void ApplyRevertGUI()
	{
		CallInspectorMethod("ApplyRevertGUI");
	}

	public override void DrawPreview(Rect previewArea)
	{
		EditorInstance.DrawPreview(previewArea);
	}

	public override string GetInfoString()
	{
		return EditorInstance.GetInfoString();
	}

	public override GUIContent GetPreviewTitle()
	{
		return EditorInstance.GetPreviewTitle();
	}

	public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
	{
		EditorInstance.OnInteractivePreviewGUI(r, background);
	}

	public override void OnPreviewGUI(Rect r, GUIStyle background)
	{
		EditorInstance.OnPreviewGUI(r, background);
	}

	public override void OnPreviewSettings()
	{
		EditorInstance.OnPreviewSettings();
	}

	public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
	{
		return EditorInstance.RenderStaticPreview(assetPath, subAssets, width, height);
	}

	public override bool RequiresConstantRepaint()
	{
		return EditorInstance.RequiresConstantRepaint();
	}

	public override bool UseDefaultMargins()
	{
		return EditorInstance.UseDefaultMargins();
	}
}