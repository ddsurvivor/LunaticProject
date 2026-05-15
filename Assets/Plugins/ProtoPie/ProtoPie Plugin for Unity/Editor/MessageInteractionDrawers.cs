using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ProtoPie.Interaction
{
    /// <summary>
    /// Creates a property drawer for ListToPopupAttribute
    /// </summary>
    [CustomPropertyDrawer(typeof(ListToPopupAttribute))]
    public class ListToPopupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var atb = attribute as ListToPopupAttribute;
            List<string> stringList = null;
            if (atb.MyType.GetField(atb.PropertyName) != null)
            {
                stringList = atb.MyType.GetField(atb.PropertyName).GetValue(atb.MyType) as List<string>;
            }

            if (stringList != null && stringList.Count != 0)
            {
                var selectedIndex = Mathf.Max(stringList.IndexOf(property.stringValue), 0);
                selectedIndex = EditorGUI.Popup(position, property.name, selectedIndex, stringList.ToArray());
                property.stringValue = stringList[selectedIndex];
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }

    /// <summary>
    /// Determines how any property with a ReadOnly attribute is drawn in the editor
    /// </summary>
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
