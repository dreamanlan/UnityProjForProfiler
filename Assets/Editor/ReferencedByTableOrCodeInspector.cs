using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ReferencedByTableOrCode))]
public class ReferencedByTableOrCodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var info = target as ReferencedByTableOrCode;
        m_Pos = GUILayout.BeginScrollView(m_Pos);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Total Count:", GUILayout.Width(80));
        GUILayout.Label(info.Objects.Length.ToString(), GUILayout.Width(40));
        GUILayout.Label("Filter:", GUILayout.Width(40));
        string key = GUILayout.TextField(m_Key, GUILayout.Width(80));
        if (key != m_Key || string.IsNullOrEmpty(m_Key) && m_ItemList.Count <= 0) {
            m_Key = key;
            m_ItemList.Clear();
            foreach (var obj in info.Objects) {
                if (null != obj) {
                    string name = obj.name;
                    if (name.Contains(m_Key)) {
                        m_ItemList.Add(obj);
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(string.Format("Total Count ({0})", m_ItemList.Count), GUILayout.Width(120));
        GUILayout.Label(string.Format("Go To Page ({0})", m_ItemList.Count / c_ItemsPerPage + 1), GUILayout.Width(120));
        string strPage = EditorGUILayout.TextField(m_Page.ToString(), GUILayout.Width(40));
        int.TryParse(strPage, out m_Page);
        if (GUILayout.Button("Prev", GUILayout.Width(80))) {
            m_Page--;
        }
        if (GUILayout.Button("Next", GUILayout.Width(80))) {
            m_Page++;
        }
        EditorGUILayout.EndHorizontal();
        m_Page = Mathf.Max(1, Mathf.Min(m_ItemList.Count / c_ItemsPerPage + 1, m_Page));

        int index = 0;
        int totalShown = 0;
        foreach (var item in m_ItemList) {
            ++index;
            if (index <= (m_Page - 1) * c_ItemsPerPage)
                continue;
            ++totalShown;
            if (totalShown > c_ItemsPerPage)
                break;
            string name = item.name;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(name, item, item.GetType(), false);
            EditorGUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    private Vector2 m_Pos;
    private string m_Key = string.Empty;
    private List<UnityEngine.Object> m_ItemList = new List<UnityEngine.Object>();
    private int m_Page = 1;

    private const int c_ItemsPerPage = 32;
}
