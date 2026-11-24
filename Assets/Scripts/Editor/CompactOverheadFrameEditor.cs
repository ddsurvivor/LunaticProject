using UnityEditor;
using UnityEngine;

/// <summary>显示紧凑外框的手调字段，避免 Image 默认 Inspector 隐藏固定斜角参数。</summary>
[CustomEditor(typeof(CompactOverheadFrame)), CanEditMultipleObjects]
public sealed class CompactOverheadFrameEditor : Editor
{
    /// <summary>直接编辑序列化外观参数，支持撤销、多选与预制体变体覆盖。</summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"), new GUIContent("背景色调与不透明度"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fillColor"), new GUIContent("背景底色"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cornerSize"), new GUIContent("斜角尺寸（参考像素）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("borderWidth"), new GUIContent("描边宽度（参考像素）"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("borderColor"), new GUIContent("描边颜色"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Maskable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_RaycastTarget"));
        serializedObject.ApplyModifiedProperties();
    }
}
